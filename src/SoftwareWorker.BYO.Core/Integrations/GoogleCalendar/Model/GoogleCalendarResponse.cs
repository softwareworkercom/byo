namespace SoftwareWorker.BYO.Integrations.GoogleCalendar.Model
{
    public class GoogleCalendarResponse
    {
        public string summary { get; set; }
        public string timeZone { get; set; }
        public DateTime updated { get; set; }
        public List<GoogleCalendarEvent> items { get; set; }

    }
}
