using Refit;
using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph
{
    public class MicrosoftGraphConnector
    {
        private readonly IMicrosoftGraphAPI _api;
        private readonly string _accessToken;
        private readonly bool _isVerbose;

        public MicrosoftGraphConnector(string accessToken, bool isVerbose = false)
        {
            _accessToken = accessToken;
            _isVerbose = isVerbose;
            _api = RestService.For<IMicrosoftGraphAPI>("https://graph.microsoft.com");
        }

        private IDictionary<string, string> GetHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_accessToken}" },
                { "Content-Type", "application/json" }
            };
        }

        // Teams Operations
        public async Task<List<MicrosoftGraphTeam>?> ListJoinedTeamsAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListJoinedTeams(GetHeaders()));
            return result?.Value;
        }

        public async Task<MicrosoftGraphTeam?> GetTeamAsync(string teamId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetTeam(GetHeaders(), teamId));
        }

        public async Task<List<MicrosoftGraphChannel>?> ListChannelsAsync(string teamId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListChannels(GetHeaders(), teamId));
            return result?.Value;
        }

        public async Task<MicrosoftGraphChannel?> GetChannelAsync(string teamId, string channelId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetChannel(GetHeaders(), teamId, channelId));
        }

        public async Task<MicrosoftGraphMessage?> SendChannelMessageAsync(string teamId, string channelId, string content, string contentType = "html")
        {
            var message = new MicrosoftGraphMessageRequest
            {
                Body = new MicrosoftGraphMessageBody
                {
                    ContentType = contentType,
                    Content = content
                }
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.SendChannelMessage(GetHeaders(), teamId, channelId, message));
        }

        public async Task<List<MicrosoftGraphMessage>?> ListChannelMessagesAsync(string teamId, string channelId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListChannelMessages(GetHeaders(), teamId, channelId));
            return result?.Value;
        }

        public async Task<List<MicrosoftGraphMessage>?> ListChannelMessageRepliesAsync(string teamId, string channelId, string messageId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListChannelMessageReplies(GetHeaders(), teamId, channelId, messageId));
            return result?.Value;
        }

        public async Task<List<MicrosoftGraphMessage>?> ListChatMessagesAsync(string chatId)
        {
            var allMessages = new List<MicrosoftGraphMessage>();
            const int maxPages = 200;
            const int top = 50;

            string? skipToken = null;

            for (var page = 0; page < maxPages; page++)
            {
                var pageResult = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListChatMessages(GetHeaders(), chatId, top, skipToken));

                if (pageResult?.Value is { Count: > 0 })
                {
                    allMessages.AddRange(pageResult.Value);
                }
                else
                {
                    break;
                }

                skipToken = ExtractSkipToken(pageResult?.ODataNextLink);
                if (string.IsNullOrWhiteSpace(skipToken))
                {
                    break;
                }
            }

            return allMessages;
        }

        private static string? ExtractSkipToken(string? oDataNextLink)
        {
            if (string.IsNullOrWhiteSpace(oDataNextLink))
            {
                return null;
            }

            const string tokenPrefix = "$skiptoken=";
            var startIndex = oDataNextLink.IndexOf(tokenPrefix, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                return null;
            }

            startIndex += tokenPrefix.Length;
            var endIndex = oDataNextLink.IndexOf('&', startIndex);
            var encodedToken = endIndex >= 0
                ? oDataNextLink[startIndex..endIndex]
                : oDataNextLink[startIndex..];

            return string.IsNullOrWhiteSpace(encodedToken)
                ? null
                : Uri.UnescapeDataString(encodedToken);
        }

        public async Task<MicrosoftGraphChat?> GetChatAsync(string chatId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetChat(GetHeaders(), chatId));
        }

        public async Task<List<MicrosoftGraphMember>?> ListTeamMembersAsync(string teamId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListTeamMembers(GetHeaders(), teamId));
            return result?.Value;
        }

        // Mail Operations with Pagination
        public async Task<List<MicrosoftGraphMailMessage>?> ListMessagesAsync(int maxResults = 1000)
        {
            var allMessages = new List<MicrosoftGraphMailMessage>();
            int skip = 0;
            int top = 100;

            while (allMessages.Count < maxResults)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListMessages(GetHeaders(), top, skip));

                if (result?.Value == null || result.Value.Count == 0)
                    break;

                allMessages.AddRange(result.Value);

                if (result.Value.Count < top || string.IsNullOrEmpty(result.ODataNextLink))
                    break;

                skip += top;
            }

            return allMessages.Take(maxResults).ToList();
        }

        public async Task<List<MicrosoftGraphMailMessage>?> ListMessagesSinceAsync(DateTime cutoffDateTimeUtc)
        {
            var allMessages = new List<MicrosoftGraphMailMessage>();
            int skip = 0;
            const int top = 100;
            const int maxPages = 500;

            for (var page = 0; page < maxPages; page++)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListMessages(GetHeaders(), top, skip));

                if (result?.Value == null || result.Value.Count == 0)
                {
                    break;
                }

                var pageMessages = result.Value;
                allMessages.AddRange(pageMessages.Where(message => message.ReceivedDateTime >= cutoffDateTimeUtc));

                var reachedCutoff = pageMessages.Any(message => message.ReceivedDateTime < cutoffDateTimeUtc);
                if (reachedCutoff || pageMessages.Count < top || string.IsNullOrEmpty(result.ODataNextLink))
                {
                    break;
                }

                skip += top;
            }

            return allMessages;
        }

        public async Task<MicrosoftGraphMailMessage?> GetMessageAsync(string messageId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetMessage(GetHeaders(), messageId));
        }

        public async Task SendMailAsync(string subject, string body, List<string> toAddresses, string contentType = "HTML")
        {
            var message = new MicrosoftGraphMailMessageRequest
            {
                Subject = subject,
                Body = new MicrosoftGraphMessageBody
                {
                    ContentType = contentType,
                    Content = body
                },
                ToRecipients = toAddresses.Select(email => new MicrosoftGraphRecipient
                {
                    EmailAddress = new MicrosoftGraphEmailAddress
                    {
                        Address = email
                    }
                }).ToList()
            };

            var request = new MicrosoftGraphSendMailRequest
            {
                Message = message,
                SaveToSentItems = true
            };

            await ResilienceHelper.ExecuteWithResilienceAsync<object?>(
                async () => { await _api.SendMail(GetHeaders(), request); return null; });
        }

        public async Task<List<MicrosoftGraphMailFolder>?> ListMailFoldersAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListMailFolders(GetHeaders()));
            return result?.Value;
        }

        public async Task<List<MicrosoftGraphMailMessage>?> ListFolderMessagesAsync(string folderId, int maxResults = 1000)
        {
            var allMessages = new List<MicrosoftGraphMailMessage>();
            int skip = 0;
            int top = 100;

            while (allMessages.Count < maxResults)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListFolderMessages(GetHeaders(), folderId, top, skip));

                if (result?.Value == null || result.Value.Count == 0)
                    break;

                allMessages.AddRange(result.Value);

                if (result.Value.Count < top || string.IsNullOrEmpty(result.ODataNextLink))
                    break;

                skip += top;
            }

            return allMessages.Take(maxResults).ToList();
        }

        // Calendar Operations with Pagination
        public async Task<List<MicrosoftGraphEvent>?> ListEventsAsync(int maxResults = 1000)
        {
            var allEvents = new List<MicrosoftGraphEvent>();
            int skip = 0;
            int top = 100;

            while (allEvents.Count < maxResults)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListEvents(GetHeaders(), top, skip));

                if (result?.Value == null || result.Value.Count == 0)
                    break;

                allEvents.AddRange(result.Value);

                if (result.Value.Count < top || string.IsNullOrEmpty(result.ODataNextLink))
                    break;

                skip += top;
            }

            return allEvents.Take(maxResults).ToList();
        }

        public async Task<List<MicrosoftGraphEvent>?> ListCalendarViewAsync(DateTime startDateTimeUtc, DateTime endDateTimeUtc, int maxResults = 1000)
        {
            var allEvents = new List<MicrosoftGraphEvent>();
            int skip = 0;
            int top = 100;

            var start = startDateTimeUtc.ToUniversalTime().ToString("o");
            var end = endDateTimeUtc.ToUniversalTime().ToString("o");

            while (allEvents.Count < maxResults)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListCalendarView(GetHeaders(), start, end, top, skip));

                if (result?.Value == null || result.Value.Count == 0)
                    break;

                allEvents.AddRange(result.Value);

                if (result.Value.Count < top || string.IsNullOrEmpty(result.ODataNextLink))
                    break;

                skip += top;
            }

            return allEvents.Take(maxResults).ToList();
        }

        public async Task<MicrosoftGraphEvent?> GetEventAsync(string eventId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetEvent(GetHeaders(), eventId));
        }

        public async Task<MicrosoftGraphEvent?> CreateEventAsync(MicrosoftGraphEventRequest eventRequest)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateEvent(GetHeaders(), eventRequest));
        }

        public async Task<MicrosoftGraphEvent?> UpdateEventAsync(string eventId, MicrosoftGraphEventRequest eventRequest)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateEvent(GetHeaders(), eventId, eventRequest));
        }

        public async Task DeleteEventAsync(string eventId)
        {
            await ResilienceHelper.ExecuteWithResilienceAsync<object?>(
                async () => { await _api.DeleteEvent(GetHeaders(), eventId); return null; });
        }

        public async Task<List<MicrosoftGraphCalendar>?> ListCalendarsAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListCalendars(GetHeaders()));
            return result?.Value;
        }

        // Contacts Operations with Pagination
        public async Task<List<MicrosoftGraphContact>?> ListContactsAsync(int maxResults = 1000)
        {
            var allContacts = new List<MicrosoftGraphContact>();
            int skip = 0;
            int top = 100;

            while (allContacts.Count < maxResults)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListContacts(GetHeaders(), top, skip));

                if (result?.Value == null || result.Value.Count == 0)
                    break;

                allContacts.AddRange(result.Value);

                if (result.Value.Count < top || string.IsNullOrEmpty(result.ODataNextLink))
                    break;

                skip += top;
            }

            return allContacts.Take(maxResults).ToList();
        }

        public async Task<MicrosoftGraphContact?> GetContactAsync(string contactId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetContact(GetHeaders(), contactId));
        }

        public async Task<MicrosoftGraphContact?> CreateContactAsync(MicrosoftGraphContactRequest contactRequest)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateContact(GetHeaders(), contactRequest));
        }

        public async Task<MicrosoftGraphContact?> UpdateContactAsync(string contactId, MicrosoftGraphContactRequest contactRequest)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateContact(GetHeaders(), contactId, contactRequest));
        }

        public async Task DeleteContactAsync(string contactId)
        {
            await ResilienceHelper.ExecuteWithResilienceAsync<object?>(
                async () => { await _api.DeleteContact(GetHeaders(), contactId); return null; });
        }

        // User Operations
        public async Task<MicrosoftGraphUser?> GetCurrentUserAsync()
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetCurrentUser(GetHeaders()));
        }

        public async Task<MicrosoftGraphUser?> GetUserAsync(string userId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetUser(GetHeaders(), userId));
        }
    }
}
