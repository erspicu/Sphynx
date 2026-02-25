using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace Sphynx;

/// <summary>
/// 主視窗：協調 AiPtyManager、TelegramBotManager、WebView2/xterm.js。
///
/// 職責：
///   1. 初始化 WebView2，載入 terminal.html
///   2. 訂閱 AiPtyManager.OnOutput，將 ANSI 串流原封不動推給 xterm.js
///   3. 訂閱 TelegramBotManager.OnCommandReceived，轉發至 PTY
///   4. 所有 UI 更新均透過 Invoke/BeginInvoke 確保執行緒安全
/// </summary>
public partial class MainForm : Form
{
    // ────────────────────────────────────────────────────────────
    // 依賴物件
    // ────────────────────────────────────────────────────────────

    private readonly AppConfig           _cfg;
    private readonly AiPtyManager        _ptyMgr;
    private TelegramBotManager?          _telegramMgr;

    // WebView2 是否已完成導覽（NavigationCompleted 後才能 ExecuteScript）
    private bool _terminalReady = false;

    // 關閉旗標：防止 Form 已 Disposing 後，PTY/Telegram 背景事件仍觸發 BeginInvoke
    private volatile bool _isClosing = false;

    // ────────────────────────────────────────────────────────────
    // 建構子
    // ────────────────────────────────────────────────────────────

    public MainForm()
    {
        InitializeComponent();

        _cfg    = AppConfig.Load();
        _ptyMgr = new AiPtyManager(_cfg);

        // 訂閱 PTY 事件
        _ptyMgr.OnOutput        += PtyMgr_OnOutput;
        _ptyMgr.OnJobFinished   += PtyMgr_OnJobFinished;
        _ptyMgr.OnProcessExited += PtyMgr_OnProcessExited;    }

    // ────────────────────────────────────────────────────────────
    // Form 生命週期
    // ────────────────────────────────────────────────────────────

    private async void MainForm_Load(object sender, EventArgs e)
    {
        SetStatus("正在初始化 WebView2…");

        // ── 初始化 WebView2 環境 ───────────────────────────────
        // EnsureCoreWebView2Async 必須在 UI 執行緒呼叫
        try
        {
            await webViewTerminal.EnsureCoreWebView2Async(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"WebView2 初始化失敗：{ex.Message}\n\n" +
                "請確認已安裝 Microsoft Edge WebView2 Runtime。",
                "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
            return;
        }

        // 關閉 DevTools 快捷鍵（生產環境）
        webViewTerminal.CoreWebView2.Settings.AreDevToolsEnabled = true;
        webViewTerminal.CoreWebView2.Settings.IsStatusBarEnabled = false;

        // WebMessageReceived：接收 xterm.js FitAddon 通知的 resize 事件，
        // 同步更新 ConPTY 的終端機尺寸（解決 TTY 欄寬不符問題）
        webViewTerminal.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

        // NavigationCompleted 後才能 ExecuteScriptAsync
        webViewTerminal.NavigationCompleted += WebView_NavigationCompleted;

        // 載入本地 terminal.html
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "terminal.html");
        if (!File.Exists(htmlPath))
        {
            MessageBox.Show($"找不到 terminal.html：{htmlPath}",
                "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
            return;
        }

        webViewTerminal.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
    }

    private void WebView_NavigationCompleted(
        object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess || _isClosing) return;

        _terminalReady = true;
        SetStatus("終端機就緒，正在啟動 Claude PTY…");

        // ── 立即用固定尺寸啟動 PTY ─────────────────────────────
        // 不等 xterm.js FitAddon resize 事件，避免事件未觸發導致 PTY 永不啟動。
        // cols=160 是 xterm.js 在 1280px 視窗下 14px 字型的合理預設值。
        // 視窗縮放後 WebMessageReceived 會持續同步真實尺寸。
        StartPty(160, 40);

        // ── 啟動 Telegram Bot（若 Token 已設定） ──────────────
        if (!string.IsNullOrWhiteSpace(_cfg.TelegramBotToken) &&
            _cfg.AllowedChatIds.Count > 0)
        {
            try
            {
                _telegramMgr = new TelegramBotManager(
                    _cfg.TelegramBotToken,
                    _cfg.AllowedChatIds);
                _telegramMgr.SubscribePtyManager(_ptyMgr);
                _telegramMgr.OnCommandReceived += TelegramMgr_OnCommandReceived;
                _telegramMgr.StartReceiving();
            }
            catch (Exception ex)
            {
                SetStatus($"⚠️ Telegram Bot 啟動失敗: {ex.Message}");
            }
        }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _isClosing    = true;
        _terminalReady = false;

        // ① 先取消事件訂閱，避免關閉期間仍有 PTY 輸出觸發 BeginInvoke
        _ptyMgr.OnOutput        -= PtyMgr_OnOutput;
        _ptyMgr.OnJobFinished   -= PtyMgr_OnJobFinished;
        _ptyMgr.OnProcessExited -= PtyMgr_OnProcessExited;

        // ② 停止 Telegram（若有）
        try { _telegramMgr?.StopReceiving(); } catch { /* 已停止則忽略 */ }
        try { _telegramMgr?.Dispose();       } catch { }

        // ③ 停止 PTY
        try { _ptyMgr.Stop();    } catch { }
        try { _ptyMgr.Dispose(); } catch { }
    }

    // ────────────────────────────────────────────────────────────
    // UI 事件 Handler
    // ────────────────────────────────────────────────────────────

    private void btnSend_Click(object sender, EventArgs e)
        => SendInputAsync();

    private void txtInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true; // 避免 TextBox 自己換行
            SendInputAsync();
        }
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        // 送出 Ctrl+C 中斷目前 Claude 操作
        _ = _ptyMgr.SendRawAsync("\x03");
        SetStatus("已送出 Ctrl+C 中斷信號");
    }

    private void btnEnter_Click(object sender, EventArgs e)
    {
        // PTY raw mode 的 Enter = \r（CR）。
        // \r\n 中的 \n 在 raw mode 會被 TUI 當成第二個按鍵，不可用。
        _ = _ptyMgr.SendRawAsync("\r");
        SetStatus("已送出 Enter");
    }

    private void btnClear_Click(object sender, EventArgs e)
    {
        if (_terminalReady)
            _ = webViewTerminal.ExecuteScriptAsync("clearTerminal()");
    }

    // ────────────────────────────────────────────────────────────
    // 核心：送出指令給 PTY
    // ────────────────────────────────────────────────────────────

    private async void SendInputAsync()
    {
        var text = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        txtInput.Clear();
        btnSend.Enabled = false;

        try
        {
            await _ptyMgr.SendMessageAsync(text);
        }
        catch (Exception ex)
        {
            AppendTerminalError($"送出指令失敗：{ex.Message}");
        }
        finally
        {
            btnSend.Enabled = true;
            txtInput.Focus();
        }
    }

    // ────────────────────────────────────────────────────────────
    // PTY 事件 Handler（來自背景執行緒，必須 Invoke）
    // ────────────────────────────────────────────────────────────

    private void PtyMgr_OnOutput(string rawAnsiText)
    {
        if (_isClosing || IsDisposed) return;

        BeginInvoke(async () =>
        {
            if (_isClosing || !_terminalReady) return;

            // 將 raw ANSI 字串 JSON 序列化（確保正確跳脫引號、反斜線等）
            // 然後呼叫 xterm.js 的 writeToTerminal()
            var jsonStr = JsonSerializer.Serialize(rawAnsiText);
            await webViewTerminal.ExecuteScriptAsync($"writeToTerminal({jsonStr})");
        });
    }

    private void PtyMgr_OnJobFinished()
    {
        if (_isClosing || IsDisposed) return;
        BeginInvoke(() =>
        {
            SetStatus("✅ 任務完成，等待下一個指令");
        });
    }

    private void PtyMgr_OnProcessExited()
    {
        if (_isClosing || IsDisposed) return;
        BeginInvoke(() =>
        {
            SetStatus("⚠️ Claude 程序已結束（PTY 斷開）");
            AppendTerminalError("\r\n[Sphynx] Claude 程序已結束，請重新啟動程式或重連。\r\n");
        });
    }

    // ────────────────────────────────────────────────────────────
    // WebView2 WebMessage Handler — PTY Resize 橋接
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 接收 xterm.js FitAddon onResize 事件，同步更新 ConPTY 終端機尺寸。
    /// 解決 TTY 欄寬（cols）與 xterm.js 渲染寬度不一致導致的排版錯亂。
    /// </summary>
    private void WebView_WebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_isClosing) return;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson);
            var root      = doc.RootElement;
            if (root.GetProperty("type").GetString() != "resize") return;

            int cols = root.GetProperty("cols").GetInt32();
            int rows = root.GetProperty("rows").GetInt32();
            if (cols <= 0 || rows <= 0) return;

            // PTY 已用固定尺寸啟動，後續視窗縮放通知 ConPTY 同步（SIGWINCH）
            _ptyMgr.Resize(cols, rows);
            SetStatus($"✅ Claude PTY 運行中 | 終端機: {cols}×{rows}");
        }
        catch { }
    }

    /// <summary>用正確的終端機尺寸啟動 PTY，並更新狀態列。</summary>
    private void StartPty(int cols, int rows)
    {
        try
        {
            _ptyMgr.Start(cols, rows);
            var hasTelegram = _telegramMgr != null;
            SetStatus($"✅ Claude PTY 已啟動 ({cols}×{rows})" +
                      (hasTelegram ? " | Telegram Bot 已上線" : " | Telegram Bot 未設定"));
        }
        catch (Exception ex)
        {
            SetStatus($"❌ PTY 啟動失敗: {ex.Message}");
            AppendTerminalError($"PTY 啟動失敗：{ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────
    // Telegram 事件 Handler（來自 ThreadPool，必須 Invoke）
    // ────────────────────────────────────────────────────────────

    private void TelegramMgr_OnCommandReceived(string command)
    {
        if (_isClosing || IsDisposed) return;

        BeginInvoke(async () =>
        {
            if (_isClosing) return;
            SetStatus($"📩 Telegram 指令: {command[..Math.Min(command.Length, 50)]}…");

            // 在 terminal 顯示來自 Telegram 的指令提示
            if (_terminalReady)
            {
                var notice = JsonSerializer.Serialize(
                    $"\r\n\x1b[33m[Telegram 指令]\x1b[0m {command}\r\n");
                await webViewTerminal.ExecuteScriptAsync($"writeToTerminal({notice})");
            }

            try
            {
                await _ptyMgr.SendMessageAsync(command);
            }
            catch (Exception ex)
            {
                AppendTerminalError($"轉發 Telegram 指令失敗：{ex.Message}");
            }
        });
    }

    // ────────────────────────────────────────────────────────────
    // 工具方法
    // ────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        // lblStatus 在 Designer 中定義，已在 UI 執行緒呼叫時可直接設定
        if (InvokeRequired)
            BeginInvoke(() => lblStatus.Text = message);
        else
            lblStatus.Text = message;
    }

    private void AppendTerminalError(string message)
    {
        if (_isClosing || IsDisposed || !_terminalReady) return;

        BeginInvoke(async () =>
        {
            if (_isClosing || !_terminalReady) return;
            var jsonStr = JsonSerializer.Serialize(
                $"\x1b[31m{message}\x1b[0m");
            await webViewTerminal.ExecuteScriptAsync($"writeToTerminal({jsonStr})");
        });
    }
}
