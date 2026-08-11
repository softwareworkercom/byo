
namespace SoftwareWorker.BYO.Integrations.GoogleCalendar.Model
{
    public class GoogleCalendarEvent
    {
        public string status { get; set; }
        public DateTime created { get; set; }
        public DateTime updated { get; set; }
        public string summary { get; set; }
        public string description { get; set; }
        public GoogleCalendarStartDate start { get; set; }
        public GoogleCalendarEndDate end { get; set; }
        public GoogleCalendarCreator creator { get; set; }

    }
}