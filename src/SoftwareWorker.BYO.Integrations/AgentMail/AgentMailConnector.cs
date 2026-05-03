using Refit;
using SoftwareWorker.BYO.Integrations.AgentMail.Model;
using SoftwareWorker.BYO.Integrations.Helpers;

namespace SoftwareWorker.BYO.Integrations.AgentMail
{
    public class AgentMailConnector
    {
        private readonly IAgentMailAPI _api;
        private readonly Dictionary<string, string> _headers;

        public AgentMailConnector(string apiKey, bool isVerbose, string? baseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("AgentMail API key cannot be null or empty.", nameof(apiKey));
            }

            _headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {apiKey}" }
            };

            var settings = RefitHelper.GetSettings(isVerbose, "AgentMail");
            _api = RestService.For<IAgentMailAPI>(baseUrl ?? "https://api.agentmail.to", settings);
        }

        public async Task<List<AgentMailInbox>> ListInboxesAsync(int? limit = null, string? pageToken = null, bool? ascending = null)
        {
            var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListInboxesAsync(_headers, limit, pageToken, ascending));

            return response?.GetInboxes() ?? [];
        }

        public async Task<AgentMailInbox?> GetInboxAsync(string inboxId)
        {
            if (string.IsNullOrWhiteSpace(inboxId))
            {
                throw new ArgumentException("Inbox ID cannot be null or empty.", nameof(inboxId));
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetInboxAsync(_headers, inboxId));
        }

        public async Task<AgentMailInbox?> CreateInboxAsync(string? username = null, string? domain = null, string? displayName = null)
        {
            var request = new AgentMailCreateInboxRequest
            {
                Username = username,
                Domain = domain,
                DisplayName = displayName
            };

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateInboxAsync(_headers, request));
        }

        public async Task DeleteInboxAsync(string inboxId)
        {
            if (string.IsNullOrWhiteSpace(inboxId))
            {
                throw new ArgumentException("Inbox ID cannot be null or empty.", nameof(inboxId));
            }

            await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.DeleteInboxAsync(_headers, inboxId));
        }

        public async Task<List<AgentMailMessage>> ListMessagesAsync(string inboxId, int? limit = null, string? pageToken = null, bool? ascending = null)
        {
            if (string.IsNullOrWhiteSpace(inboxId))
            {
                throw new ArgumentException("Inbox ID cannot be null or empty.", nameof(inboxId));
            }

            var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListMessagesAsync(_headers, inboxId, limit, pageToken, ascending));

            return response?.GetMessages() ?? [];
        }

        public async Task<AgentMailMessage?> GetMessageAsync(string inboxId, string messageId)
        {
            if (string.IsNullOrWhiteSpace(inboxId))
            {
                throw new ArgumentException("Inbox ID cannot be null or empty.", nameof(inboxId));
            }

            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException("Message ID cannot be null or empty.", nameof(messageId));
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetMessageAsync(_headers, inboxId, messageId));
        }
    }
}
