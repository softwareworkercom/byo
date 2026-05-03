using Refit;
using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.Telegram.Model;

namespace SoftwareWorker.BYO.Integrations.Telegram
{
    public class TelegramConnector
    {
        private readonly ITelegramAPI _api;

        public TelegramConnector(string botToken, bool isVerbose)
        {
            if (string.IsNullOrWhiteSpace(botToken))
            {
                throw new ArgumentException("Telegram bot token cannot be null or empty.", nameof(botToken));
            }

            var settings = RefitHelper.GetSettings(isVerbose, "Telegram");
            _api = RestService.For<ITelegramAPI>($"https://api.telegram.org/bot{botToken}", settings);
        }

        /// <summary>
        /// Gets basic information about the bot.
        /// </summary>
        public async Task<TelegramUser?> GetMeAsync()
        {
            var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetMeAsync());

            return response?.Ok == true ? response.Result : null;
        }

        /// <summary>
        /// Receives incoming updates (messages) for the bot using long polling.
        /// Cannot be used when a webhook is set.
        /// </summary>
        /// <param name="offset">Identifier of the first update to return.</param>
        /// <param name="limit">Limits the number of updates to be retrieved (1–100).</param>
        public async Task<List<TelegramUpdate>> GetUpdatesAsync(long? offset = null, int? limit = null)
        {
            var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetUpdatesAsync(offset, limit));

            return response?.Ok == true ? (response.Result ?? []) : [];
        }

        /// <summary>
        /// Sends a text message to a chat.
        /// </summary>
        /// <param name="chatId">Unique identifier for the target chat or username of the target channel.</param>
        /// <param name="text">Text of the message.</param>
        /// <param name="parseMode">Mode for parsing entities in the message text (e.g. "Markdown", "HTML").</param>
        public async Task<TelegramMessage?> SendMessageAsync(string chatId, string text, string? parseMode = null)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                throw new ArgumentException("Chat ID cannot be null or empty.", nameof(chatId));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Message text cannot be null or empty.", nameof(text));
            }

            var request = new TelegramSendMessageRequest
            {
                ChatId = chatId,
                Text = text,
                ParseMode = parseMode
            };

            var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.SendMessageAsync(request));

            return response?.Ok == true ? response.Result : null;
        }

        /// <summary>
        /// Registers a webhook URL so Telegram pushes updates to your HTTPS endpoint.
        /// Once set, getUpdates will stop returning new messages until the webhook is removed.
        /// </summary>
        /// <param name="url">HTTPS URL to receive updates.</param>
        /// <param name="maxConnections">Maximum allowed number of simultaneous HTTPS connections (1–100).</param>
        /// <param name="dropPendingUpdates">Pass true to drop all pending updates.</param>
        public async Task<bool> SetWebhookAsync(string url, int? maxConnections = null, bool? dropPendingUpdates = null)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Webhook URL cannot be null or empty.", nameof(url));
            }

            var request = new TelegramSetWebhookRequest
            {
                Url = url,
                MaxConnections = maxConnections,
                DropPendingUpdates = dropPendingUpdates
            };

            var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.SetWebhookAsync(request));

            return response?.Ok == true && response.Result;
        }

        /// <summary>
        /// Removes the webhook so the bot can use getUpdates again.
        /// </summary>
        /// <param name="dropPendingUpdates">Pass true to drop all pending updates.</param>
        public async Task<bool> DeleteWebhookAsync(bool dropPendingUpdates = false)
        {
            var body = new Dictionary<string, object>
            {
                ["drop_pending_updates"] = dropPendingUpdates
            };

            var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.DeleteWebhookAsync(body));

            return response?.Ok == true && response.Result;
        }

        /// <summary>
        /// Returns the current webhook configuration for the bot.
        /// </summary>
        public async Task<TelegramWebhookInfo?> GetWebhookInfoAsync()
        {
            var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetWebhookInfoAsync());

            return response?.Ok == true ? response.Result : null;
        }
    }
}
