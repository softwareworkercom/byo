using Refit;
using SoftwareWorker.BYO.Integrations.Telegram.Model;

namespace SoftwareWorker.BYO.Integrations.Telegram
{
    /// <summary>
    /// Telegram Bot API - https://core.telegram.org/bots/api
    /// </summary>
    internal interface ITelegramAPI
    {
        [Get("/getMe")]
        Task<TelegramResponse<TelegramUser>> GetMeAsync();

        [Get("/getUpdates")]
        Task<TelegramResponse<List<TelegramUpdate>>> GetUpdatesAsync(
            [AliasAs("offset")] long? offset = null,
            [AliasAs("limit")] int? limit = null,
            [AliasAs("timeout")] int? timeout = null);

        [Post("/sendMessage")]
        Task<TelegramResponse<TelegramMessage>> SendMessageAsync(
            [Body] TelegramSendMessageRequest request);

        [Post("/setWebhook")]
        Task<TelegramResponse<bool>> SetWebhookAsync(
            [Body] TelegramSetWebhookRequest request);

        [Post("/deleteWebhook")]
        Task<TelegramResponse<bool>> DeleteWebhookAsync(
            [Body(BodySerializationMethod.UrlEncoded)] IDictionary<string, object> body);

        [Get("/getWebhookInfo")]
        Task<TelegramResponse<TelegramWebhookInfo>> GetWebhookInfoAsync();
    }
}
