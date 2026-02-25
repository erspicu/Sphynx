using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Sphynx;

/// <summary>
/// Telegram Bot 遠端控制管理器。
///
/// 功能：
///   • Long Polling 接收訊息（背景執行，不阻塞 UI 執行緒）
///   • Chat ID 白名單驗證（拒絕未授權的聊天室）
///   • 收到文字訊息時觸發 <see cref="OnCommandReceived"/> 事件
///   • 提供 <see cref="SendNotificationAsync"/> 向所有白名單推播訊息
///   • 訂閱 <see cref="AiPtyManager.OnJobFinished"/>，任務完成後自動推播
/// </summary>
public sealed class TelegramBotManager : IDisposable
{
    // ────────────────────────────────────────────────────────────
    // 事件
    // ────────────────────────────────────────────────────────────

    /// <summary>收到白名單使用者傳來的文字指令時觸發。</summary>
    public event Action<string>? OnCommandReceived;

    // ────────────────────────────────────────────────────────────
    // 私有狀態
    // ────────────────────────────────────────────────────────────

    private readonly TelegramBotClient _bot;
    private readonly HashSet<long> _allowedChatIds;
    private CancellationTokenSource _cts = new();
    private bool _disposed;

    // ────────────────────────────────────────────────────────────
    // 建構子
    // ────────────────────────────────────────────────────────────

    /// <param name="token">BotFather 提供的 Bot Token。</param>
    /// <param name="allowedChatIds">白名單 Chat ID 清單（長整數）。</param>
    public TelegramBotManager(string token, IEnumerable<long> allowedChatIds)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Telegram Bot Token 不可為空。", nameof(token));

        _bot = new TelegramBotClient(token);
        _allowedChatIds = new HashSet<long>(allowedChatIds);
    }

    // ────────────────────────────────────────────────────────────
    // 公開 API
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 啟動 Long Polling（非阻塞，在 ThreadPool 背景執行）。
    /// </summary>
    public void StartReceiving()
    {
        _cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            // 只訂閱 Message 事件，減少無用流量
            AllowedUpdates = new[] { UpdateType.Message },
            // 啟動時丟棄積壓的舊訊息，避免重複執行
            DropPendingUpdates = true,
        };

        _bot.StartReceiving(
            updateHandler : HandleUpdateAsync,
            errorHandler  : HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cts.Token
        );
    }

    /// <summary>
    /// 停止 Long Polling。
    /// </summary>
    public void StopReceiving()
    {
        _cts.Cancel();
    }

    /// <summary>
    /// 向所有白名單 Chat ID 發送推播訊息（Markdown 格式）。
    /// </summary>
    public async Task SendNotificationAsync(string message,
        CancellationToken ct = default)
    {
        foreach (var chatId in _allowedChatIds)
        {
            try
            {
                await _bot.SendMessage(
                    chatId      : chatId,
                    text        : message,
                    parseMode   : ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            catch (Exception ex)
            {
                // 單一 Chat 失敗不應中斷其餘推播
                System.Diagnostics.Debug.WriteLine(
                    $"[Telegram] SendNotification 失敗 (chatId={chatId}): {ex.Message}");
            }
        }
    }

    // ────────────────────────────────────────────────────────────
    // AiPtyManager 事件橋接
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 訂閱 <see cref="AiPtyManager.OnJobFinished"/>。
    /// 任務完成後自動向所有白名單 Chat 推播完成通知。
    /// </summary>
    public void SubscribePtyManager(AiPtyManager ptyManager)
    {
        ptyManager.OnJobFinished += OnPtyJobFinished;
    }

    private async void OnPtyJobFinished()
    {
        const string msg = "✅ 任務已完成！請下達新的任務指令。";
        try
        {
            await SendNotificationAsync(msg);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Telegram] OnPtyJobFinished 推播失敗: {ex.Message}");
        }
    }

    // ────────────────────────────────────────────────────────────
    // 私有 Handler
    // ────────────────────────────────────────────────────────────

    private Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update             update,
        CancellationToken  ct)
    {
        // 只處理純文字訊息
        if (update.Message?.Text is not string text || string.IsNullOrWhiteSpace(text))
            return Task.CompletedTask;

        var chatId = update.Message.Chat.Id;

        // ── 白名單驗證 ─────────────────────────────────────────
        if (!_allowedChatIds.Contains(chatId))
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Telegram] 拒絕未授權的 Chat ID: {chatId}");
            return Task.CompletedTask;
        }

        System.Diagnostics.Debug.WriteLine(
            $"[Telegram] 收到指令 (chatId={chatId}): {text}");

        // 觸發事件，通知 MainForm 轉發給 PTY
        OnCommandReceived?.Invoke(text);

        return Task.CompletedTask;
    }

    private Task HandlePollingErrorAsync(
        ITelegramBotClient botClient,
        Exception          exception,
        CancellationToken  ct)
    {
        // ApiRequestException 包含 Telegram 回傳的錯誤碼
        var errMsg = exception is ApiRequestException apiEx
            ? $"Telegram API Error {apiEx.ErrorCode}: {apiEx.Message}"
            : exception.ToString();

        System.Diagnostics.Debug.WriteLine($"[Telegram] Polling 錯誤: {errMsg}");
        return Task.CompletedTask;
    }

    // ────────────────────────────────────────────────────────────
    // IDisposable
    // ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}
