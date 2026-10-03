using Refit;
using SoftwareWorker.BYO.Integrations.GoogleCalendar.Model;
using SoftwareWorker.BYO.Integrations.Helpers;

namespace SoftwareWorker.BYO.Integrations.GoogleCalendar
{
    public class GoogleCalendarConnector
    {
        readonly string _apiKey;
        private IGoogleCalendar _api;

        public GoogleCalendarConnector(string key, bool isVerbose)
        {
            _apiKey = key;
            var settings = RefitHelper.GetSettings(isVerbose, "GoogleCalendar");
            _api = RestService.For<IGoogleCalendar>("https://www.googleapis.com", settings);
        }

        public async Task<List<GoogleCalendarEvent>> ListPublicHolidaysAsync(string calendarId, int? maxItems = null)
        {
            var result = await _api.ListPublicHolidays(calendarId, _apiKey);
            var items = result.items;
            return maxItems.HasValue ? items.Take(maxItems.Value).ToList() : items;
        }
    }
}
