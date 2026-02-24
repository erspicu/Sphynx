using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SphynxApp
{
    public class TelegramBotManager
    {
        private readonly ITelegramBotClient _botClient;
        private readonly long _allowedChatId; // 白名單 ChatId
        public event EventHandler<string>? OnCommandReceived;

        public TelegramBotManager(string token, long allowedChatId)
        {
            _botClient = new TelegramBotClient(token);
            _allowedChatId = allowedChatId;
        }

        public void Start()
        {
            var receiverOptions = new ReceiverOptions 
            { 
                AllowedUpdates = Array.Empty<UpdateType>() 
            };
            
            // 使用位置參數以避免版本間的命名參數差異
            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions,
                CancellationToken.None
            );
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { Text: { } messageText } message) return;
            
            // 安全驗證：僅接受白名單 ID 的指令
            if (message.Chat.Id != _allowedChatId) return;

            OnCommandReceived?.Invoke(this, messageText);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // 實務上可記錄 Log
            return Task.CompletedTask;
        }

        public async Task SendNotificationAsync(string message)
        {
            try
            {
                await _botClient.SendMessage(_allowedChatId, message);
            }
            catch (Exception)
            {
                // 忽略發送錯誤
            }
        }
    }
}
