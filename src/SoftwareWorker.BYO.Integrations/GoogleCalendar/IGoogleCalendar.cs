using Refit;
using SoftwareWorker.BYO.Integrations.GoogleCalendar.Model;

namespace SoftwareWorker.BYO.Integrations.GoogleCalendar
{
    /// <summary>
    /// https://developers.google.com/calendar/api/v3/reference
    /// </summary>
    internal interface IGoogleCalendar
    {
        [Get("/calendar/v3/calendars/{calendarId}.official%23holiday%40group.v.calendar.google.com/events?key={apiKey}")]
        Task<GoogleCalendarResponse> ListPublicHolidays([AliasAs("calendarId")] string calendarId, string apiKey);
    }
}
