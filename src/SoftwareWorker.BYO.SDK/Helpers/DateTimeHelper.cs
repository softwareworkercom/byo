namespace SoftwareWorker.BYO.SDK.Helpers
{
    public static class DateTimeHelper
    {
        public static bool TryResolveRangeStart(string dateTimeWindow, out string startDate)
        {
            startDate = string.Empty;

            if (ResolveRangeDateTime(dateTimeWindow) is not DateTime start)
            {
                return false;
            }

            startDate = $"{start:yyyy-MM-dd HH:mm}";
            return true;
        }

        private static DateTime? ResolveRangeDateTime(string dateTimeWindow)
        {
            dateTimeWindow = NormalizeDateTimeWindow(dateTimeWindow);
            if (string.IsNullOrWhiteSpace(dateTimeWindow))
            {
                return null;
            }

            var unitLength = dateTimeWindow.EndsWith("mo") ? 2 : 1;
            if (dateTimeWindow.Length <= unitLength)
            {
                return null;
            }

            var unit = dateTimeWindow[^unitLength..];
            if (!int.TryParse(dateTimeWindow[..^unitLength], out var value) || value <= 0)
            {
                return null;
            }

            var now = DateTime.Now;
            return unit switch
            {
                "m" => now.AddMinutes(-value),
                "h" => now.AddHours(-value),
                "d" => now.AddDays(-value),
                "w" => now.AddDays(-(value * 7)),
                "mo" => now.AddMonths(-value),
                _ => (DateTime?)null
            };
        }

        private static string NormalizeDateTimeWindow(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            return string.Join("", input
                .Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
