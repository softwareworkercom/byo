using Refit;
using SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph
{
    /// <summary>
    /// Microsoft Graph API v1.0
    /// Documentation: https://learn.microsoft.com/en-us/graph/api/overview?view=graph-rest-1.0
    /// </summary>
    public interface IMicrosoftGraphAPI
    {
        // Teams Operations
        [Get("/v1.0/me/joinedTeams")]
        Task<MicrosoftGraphTeamsResponse> ListJoinedTeams([HeaderCollection] IDictionary<string, string> headers);

        [Get("/v1.0/teams/{teamId}")]
        Task<MicrosoftGraphTeam> GetTeam([HeaderCollection] IDictionary<string, string> headers, string teamId);

        [Get("/v1.0/teams/{teamId}/channels")]
        Task<MicrosoftGraphChannelsResponse> ListChannels([HeaderCollection] IDictionary<string, string> headers, string teamId);

        [Get("/v1.0/teams/{teamId}/channels/{channelId}")]
        Task<MicrosoftGraphChannel> GetChannel([HeaderCollection] IDictionary<string, string> headers, string teamId, string channelId);

        [Post("/v1.0/teams/{teamId}/channels/{channelId}/messages")]
        Task<MicrosoftGraphMessage> SendChannelMessage([HeaderCollection] IDictionary<string, string> headers, string teamId, string channelId, [Body] MicrosoftGraphMessageRequest message);

        [Get("/v1.0/teams/{teamId}/channels/{channelId}/messages")]
        Task<MicrosoftGraphMessagesResponse> ListChannelMessages([HeaderCollection] IDictionary<string, string> headers, string teamId, string channelId);

        [Get("/v1.0/teams/{teamId}/channels/{channelId}/messages/{messageId}/replies")]
        Task<MicrosoftGraphMessagesResponse> ListChannelMessageReplies([HeaderCollection] IDictionary<string, string> headers, string teamId, string channelId, string messageId);

        [Get("/v1.0/chats/{chatId}/messages")]
        Task<MicrosoftGraphMessagesResponse> ListChatMessages(
            [HeaderCollection] IDictionary<string, string> headers,
            string chatId,
            [AliasAs("$top")] int? top = null,
            [AliasAs("$skiptoken")] string? skipToken = null);

        [Get("/v1.0/chats/{chatId}")]
        Task<MicrosoftGraphChat> GetChat([HeaderCollection] IDictionary<string, string> headers, string chatId);

        [Get("/v1.0/teams/{teamId}/members")]
        Task<MicrosoftGraphMembersResponse> ListTeamMembers([HeaderCollection] IDictionary<string, string> headers, string teamId);

        // Mail Operations
        [Get("/v1.0/me/messages")]
        Task<MicrosoftGraphMailMessagesResponse> ListMessages([HeaderCollection] IDictionary<string, string> headers, [AliasAs("$top")] int? top = null, [AliasAs("$skip")] int? skip = null);

        [Get("/v1.0/me/messages/{messageId}")]
        Task<MicrosoftGraphMailMessage> GetMessage([HeaderCollection] IDictionary<string, string> headers, string messageId);

        [Post("/v1.0/me/sendMail")]
        Task SendMail([HeaderCollection] IDictionary<string, string> headers, [Body] MicrosoftGraphSendMailRequest request);

        [Get("/v1.0/me/mailFolders")]
        Task<MicrosoftGraphMailFoldersResponse> ListMailFolders([HeaderCollection] IDictionary<string, string> headers);

        [Get("/v1.0/me/mailFolders/{folderId}/messages")]
        Task<MicrosoftGraphMailMessagesResponse> ListFolderMessages([HeaderCollection] IDictionary<string, string> headers, string folderId, [AliasAs("$top")] int? top = null, [AliasAs("$skip")] int? skip = null);

        // Calendar Operations
        [Get("/v1.0/me/events")]
        Task<MicrosoftGraphEventsResponse> ListEvents([HeaderCollection] IDictionary<string, string> headers, [AliasAs("$top")] int? top = null, [AliasAs("$skip")] int? skip = null);

        [Get("/v1.0/me/calendarView")]
        Task<MicrosoftGraphEventsResponse> ListCalendarView(
            [HeaderCollection] IDictionary<string, string> headers,
            [AliasAs("startDateTime")] string startDateTime,
            [AliasAs("endDateTime")] string endDateTime,
            [AliasAs("$top")] int? top = null,
            [AliasAs("$skip")] int? skip = null);

        [Get("/v1.0/me/events/{eventId}")]
        Task<MicrosoftGraphEvent> GetEvent([HeaderCollection] IDictionary<string, string> headers, string eventId);

        [Post("/v1.0/me/events")]
        Task<MicrosoftGraphEvent> CreateEvent([HeaderCollection] IDictionary<string, string> headers, [Body] MicrosoftGraphEventRequest eventRequest);

        [Patch("/v1.0/me/events/{eventId}")]
        Task<MicrosoftGraphEvent> UpdateEvent([HeaderCollection] IDictionary<string, string> headers, string eventId, [Body] MicrosoftGraphEventRequest eventRequest);

        [Delete("/v1.0/me/events/{eventId}")]
        Task DeleteEvent([HeaderCollection] IDictionary<string, string> headers, string eventId);

        [Get("/v1.0/me/calendars")]
        Task<MicrosoftGraphCalendarsResponse> ListCalendars([HeaderCollection] IDictionary<string, string> headers);

        // Contacts Operations
        [Get("/v1.0/me/contacts")]
        Task<MicrosoftGraphContactsResponse> ListContacts([HeaderCollection] IDictionary<string, string> headers, [AliasAs("$top")] int? top = null, [AliasAs("$skip")] int? skip = null);

        [Get("/v1.0/me/contacts/{contactId}")]
        Task<MicrosoftGraphContact> GetContact([HeaderCollection] IDictionary<string, string> headers, string contactId);

        [Post("/v1.0/me/contacts")]
        Task<MicrosoftGraphContact> CreateContact([HeaderCollection] IDictionary<string, string> headers, [Body] MicrosoftGraphContactRequest contactRequest);

        [Patch("/v1.0/me/contacts/{contactId}")]
        Task<MicrosoftGraphContact> UpdateContact([HeaderCollection] IDictionary<string, string> headers, string contactId, [Body] MicrosoftGraphContactRequest contactRequest);

        [Delete("/v1.0/me/contacts/{contactId}")]
        Task DeleteContact([HeaderCollection] IDictionary<string, string> headers, string contactId);

        // User Operations
        [Get("/v1.0/me")]
        Task<MicrosoftGraphUser> GetCurrentUser([HeaderCollection] IDictionary<string, string> headers);

        [Get("/v1.0/users/{userId}")]
        Task<MicrosoftGraphUser> GetUser([HeaderCollection] IDictionary<string, string> headers, string userId);

        [Get("/v1.0/users/{userId}/onlineMeetings")]
        Task<MicrosoftGraphOnlineMeetingsResponse> ListOnlineMeetings([HeaderCollection] IDictionary<string, string> headers, string userId, [AliasAs("$filter")] string? filter = null);

        [Get("/v1.0/users/{userId}/onlineMeetings/{meetingId}/transcripts")]
        Task<MicrosoftGraphMeetingTranscriptsResponse> ListOnlineMeetingTranscripts([HeaderCollection] IDictionary<string, string> headers, string userId, string meetingId);
    }
}
