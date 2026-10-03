using Refit;
using SoftwareWorker.BYO.Integrations.AgentMail.Model;

namespace SoftwareWorker.BYO.Integrations.AgentMail
{
    /// <summary>
    /// AgentMail API v0 - https://docs.agentmail.to/
    /// </summary>
    internal interface IAgentMailAPI
    {
        [Get("/v0/inboxes")]
        Task<AgentMailListInboxesResponse> ListInboxesAsync(
            [HeaderCollection] IDictionary<string, string> headers,
            [AliasAs("limit")] int? limit = null,
            [AliasAs("page_token")] string? pageToken = null,
            [AliasAs("ascending")] bool? ascending = null);

        [Get("/v0/inboxes/{inboxId}")]
        Task<AgentMailInbox> GetInboxAsync(
            [HeaderCollection] IDictionary<string, string> headers,
            [AliasAs("inboxId")] string inboxId);

        [Post("/v0/inboxes")]
        Task<AgentMailInbox> CreateInboxAsync(
            [HeaderCollection] IDictionary<string, string> headers,
            [Body] AgentMailCreateInboxRequest request);

        [Delete("/v0/inboxes/{inboxId}")]
        Task DeleteInboxAsync(
            [HeaderCollection] IDictionary<string, string> headers,
            [AliasAs("inboxId")] string inboxId);

        [Get("/v0/inboxes/{inboxId}/messages")]
        Task<AgentMailListMessagesResponse> ListMessagesAsync(
            [HeaderCollection] IDictionary<string, string> headers,
            [AliasAs("inboxId")] string inboxId,
            [AliasAs("limit")] int? limit = null,
            [AliasAs("page_token")] string? pageToken = null,
            [AliasAs("ascending")] bool? ascending = null);

        [Get("/v0/inboxes/{inboxId}/messages/{messageId}")]
        Task<AgentMailMessage> GetMessageAsync(
            [HeaderCollection] IDictionary<string, string> headers,
            [AliasAs("inboxId")] string inboxId,
            [AliasAs("messageId")] string messageId);
    }
}
