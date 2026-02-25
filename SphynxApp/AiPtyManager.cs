using System.Text.RegularExpressions;
using Pty.Net;

namespace Sphynx;

/// <summary>
/// 核心 PTY 管理器。
///
/// 使用 Pty.Net (ConPTY) 啟動 Claude Code，完美欺騙 CLI 工具以為連接著
/// 真實 TTY，從而保留：
///   • ANSI 256色 / TrueColor
///   • Loading 動畫（spinner）
///   • 無卡死的即時串流輸出
///
/// ⚠️  不要改用 System.Diagnostics.Process + RedirectStandardOutput，
///     那樣會導致 isTTY = false 及嚴重的 stdout 緩衝問題。
///
/// Pty.Net 0.1.x API 說明：
///   • PtyProvider.Spawn(command, cols, rows, workingDir) 同步建立 PTY
///   • IPtyConnection.PtyData   事件：每當有輸出時觸發，參數為 string（已解碼）
///   • IPtyConnection.PtyDisconnected 事件：PTY 斷開時觸發
///   • IPtyConnection.WriteAsync(string) 寫入輸入
///   • IPtyConnection.Resize(cols, rows) 調整尺寸
/// </summary>
public sealed class AiPtyManager : IDisposable
{
    // ────────────────────────────────────────────────────────────
    // 事件
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// PTY 有新的原始 ANSI 輸出時觸發。
    /// 訂閱者直接把 <c>string</c> 丟給 xterm.js 的 <c>term.write()</c>。
    /// </summary>
    public event Action<string>? OnOutput;

    /// <summary>
    /// 當去除 ANSI 碼後的純文字包含 <c>[job finished]</c> 時觸發。
    /// TelegramBotManager 訂閱此事件以發送完成推播。
    /// </summary>
    public event Action? OnJobFinished;

    /// <summary>PTY 連線中斷（子程序結束）時觸發。</summary>
    public event Action? OnProcessExited;    // ────────────────────────────────────────────────────────────
    // 內部狀態
    // ────────────────────────────────────────────────────────────

    private IPtyConnection? _pty;
    private readonly AppConfig _cfg;
    private bool _disposed;

    // ── ANSI 去除 Regex（Compiled 以提升效能） ──────────────────
    // 涵蓋：
    //   ESC [ ... m/A/B/C/…   → CSI sequences（色彩、游標移動）
    //   ESC ] … BEL / ESC \   → OSC sequences（視窗標題等）
    //   ESC [@-Z\-_]          → Fe 2-char sequences
    private static readonly Regex s_ansiRegex = new(
        @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~]|\][^\a\x1B]*(?:\a|\x1B\\))",
        RegexOptions.Compiled);

    // ── 完成標記 ────────────────────────────────────────────────
    private const string JobFinishedMarker = "[job finished]";

    // ── 系統隱藏提示（附加在每次使用者輸入的末尾） ──────────────
    // ⚠️ 不使用前置 \n\n：在 PTY raw mode 下，LF(\n) 可能被 TUI 當作
    //    Enter 鍵提前送出，導致系統 Prompt 被切斷成獨立訊息。
    //    改為空格分隔，確保與使用者文字視為同一行輸入。
    private const string SystemSuffix =
        " (System: 請在完全處理完此任務後，於輸出的最後一行單獨回傳 [job finished] 這幾個字)";

    // ────────────────────────────────────────────────────────────
    // 建構子
    // ────────────────────────────────────────────────────────────

    public AiPtyManager(AppConfig cfg)
    {
        _cfg = cfg;
    }

    // ────────────────────────────────────────────────────────────
    // 公開 API
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 啟動 PTY 並訂閱輸出事件。
    /// </summary>
    /// <param name="cols">終端機寬度（字元數），0 = 使用 appsettings 預設值。</param>
    /// <param name="rows">終端機高度（行數），0 = 使用 appsettings 預設值。</param>
    public void Start(int cols = 0, int rows = 0)
    {
        if (_pty != null)
            throw new InvalidOperationException("PTY 已在運行中，請先 Stop()。");

        // 傳入 0 時回退到 appsettings 預設
        int actualCols = cols > 0 ? cols : _cfg.TerminalCols;
        int actualRows = rows > 0 ? rows : _cfg.TerminalRows;

        // ── 設定終端機環境變數（讓 claude 以為在真實 TTY 下） ──
        Environment.SetEnvironmentVariable("TERM",        "xterm-256color");
        Environment.SetEnvironmentVariable("COLORTERM",   "truecolor");
        Environment.SetEnvironmentVariable("FORCE_COLOR", "3");

        var commandLine = BuildCommandLine();
        var workingDir  = string.IsNullOrWhiteSpace(_cfg.WorkingDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : _cfg.WorkingDirectory;

        // ── 建立 PTY，使用 xterm.js FitAddon 計算出的正確尺寸 ──
        _pty = PtyProvider.Spawn(commandLine, actualCols, actualRows, workingDir);

        // ── 訂閱輸出事件 ────────────────────────────────────────
        // PtyDataEventArgs 是 delegate: void(object sender, string data)
        // data 已是解碼後的 UTF-8 字串（含 ANSI 碼）
        _pty.PtyData        += Pty_OnData;
        _pty.PtyDisconnected += Pty_OnDisconnected;
    }

    /// <summary>
    /// 向 PTY 送出使用者訊息。
    /// 分兩步：先送文字，再單獨送 \r（Enter）。
    /// 若合併成一次 WriteAsync，readline raw mode 會把 \r 當成文字吞掉。
    /// </summary>
    public async Task SendMessageAsync(string userText)
    {
        if (_pty == null)
            throw new InvalidOperationException("PTY 尚未啟動，請先呼叫 Start()。");

        // Step 1：送出文字內容（含隱藏系統提示）
        var fullText = userText + SystemSuffix;
        await _pty.WriteAsync(fullText);

        // Step 2：單獨送出 \r（Enter），模擬真實按下 Enter 鍵
        // 必須分開呼叫，否則 readline raw mode 會把 \r 當作輸入文字的一部分
        await _pty.WriteAsync("\r");
    }

    /// <summary>
    /// 直接向 PTY 送出原始按鍵字串（例如 Ctrl+C = "\x03"）。
    /// </summary>
    public async Task SendRawAsync(string raw)
    {
        if (_pty == null) return;
        await _pty.WriteAsync(raw);
    }

    /// <summary>
    /// 調整 PTY 終端機尺寸（同步通知子程序 SIGWINCH）。
    /// </summary>
    public void Resize(int cols, int rows)
    {
        _pty?.Resize(cols, rows);
    }

    /// <summary>停止 PTY 並釋放所有資源。</summary>
    public void Stop()
    {
        if (_pty == null) return;

        _pty.PtyData        -= Pty_OnData;
        _pty.PtyDisconnected -= Pty_OnDisconnected;

        // ConPtyConnection 實作 IDisposable，透過介面呼叫
        if (_pty is IDisposable d)
            d.Dispose();

        _pty = null;
    }

    // ────────────────────────────────────────────────────────────
    // PTY 事件 Handler
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// PTY 輸出資料：攔截 [job finished]，其餘原封不動傳給 UI。
    /// </summary>
    private void Pty_OnData(object sender, string data)
    {
        // ── 攔截 [job finished] ────────────────────────────────
        var plainText = StripAnsiCodes(data);
        bool jobDone  = plainText.Contains(JobFinishedMarker, StringComparison.Ordinal);

        if (jobDone)
        {
            // 過濾掉含標記的行，避免在終端機顯示多餘資訊
            var cleaned = RemoveJobFinishedLine(data);
            if (!string.IsNullOrEmpty(cleaned))
                OnOutput?.Invoke(cleaned);

            OnJobFinished?.Invoke();
        }
        else
        {
            OnOutput?.Invoke(data);
        }
    }

    private void Pty_OnDisconnected(object sender)
    {
        OnProcessExited?.Invoke();
    }

    // ────────────────────────────────────────────────────────────
    // 私有工具方法
    // ────────────────────────────────────────────────────────────

    /// <summary>去除所有 ANSI/VT 控制碼，回傳純文字。</summary>
    private static string StripAnsiCodes(string text)
        => s_ansiRegex.Replace(text, "");

    /// <summary>
    /// 在輸出字串中，移除含有 [job finished] 的那一行。
    /// </summary>
    private static string RemoveJobFinishedLine(string rawText)
    {
        var lines = rawText.Split('\n');
        var filtered = lines.Where(line =>
            !StripAnsiCodes(line).Contains(JobFinishedMarker, StringComparison.Ordinal));
        return string.Join('\n', filtered);
    }

    /// <summary>
    /// 組合傳給 PtyProvider.Spawn 的完整命令列字串。
    /// </summary>
    private string BuildCommandLine()
    {
        // 例如: "pwsh.exe -NoLogo -NoExit -Command claude"
        var parts = new List<string> { _cfg.ShellExecutable };
        parts.AddRange(_cfg.ShellArguments);
        return string.Join(" ", parts);
    }

    // ────────────────────────────────────────────────────────────
    // IDisposable
    // ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}