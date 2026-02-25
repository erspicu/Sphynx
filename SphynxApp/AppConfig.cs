using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sphynx;

/// <summary>
/// 對應 appsettings.json 的設定模型。
/// 第一次執行若檔案不存在，會自動建立範本。
/// </summary>
public sealed class AppConfig
{
    // ── Telegram ──────────────────────────────────────────
    public string TelegramBotToken { get; set; } = "";

    /// <summary>允許發送指令的 Telegram Chat ID 白名單（長整數）。</summary>
    public List<long> AllowedChatIds { get; set; } = new();

    // ── PTY / Claude ──────────────────────────────────────
    /// <summary>工作目錄，預設為使用者家目錄。</summary>
    public string WorkingDirectory { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// 啟動 Claude Code 所使用的 Shell 可執行檔。
    /// Windows 建議: "pwsh.exe"（PowerShell 7+）
    /// </summary>
    public string ShellExecutable { get; set; } = "pwsh.exe";

    /// <summary>
    /// 傳給 Shell 的參數陣列（含啟動 claude 的命令）。
    /// </summary>
    public string[] ShellArguments { get; set; } =
        new[] { "-NoLogo", "-NoExit", "-Command", "claude" };

    // ── Terminal size ─────────────────────────────────────
    public int TerminalCols { get; set; } = 220;
    public int TerminalRows { get; set; } = 50;

    // ── Serializer options ────────────────────────────────
    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string s_configPath =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    /// <summary>從檔案載入設定；若不存在則建立預設範本。</summary>
    public static AppConfig Load()
    {
        if (!File.Exists(s_configPath))
        {
            var defaultCfg = new AppConfig();
            File.WriteAllText(s_configPath,
                JsonSerializer.Serialize(defaultCfg, s_jsonOpts));
            return defaultCfg;
        }

        var json = File.ReadAllText(s_configPath);
        return JsonSerializer.Deserialize<AppConfig>(json, s_jsonOpts) ?? new AppConfig();
    }

    /// <summary>將當前設定寫回檔案。</summary>
    public void Save()
    {
        File.WriteAllText(s_configPath,
            JsonSerializer.Serialize(this, s_jsonOpts));
    }
}
