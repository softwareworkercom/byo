using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Integrations.GoogleCalendar;
using Spectre.Console;

namespace SoftwareWorker.BYO.Extensions.GoogleCalendar;

[TrunkCommand("google", "Google operations")]
[BranchCommand("calendar", "Fetch public holidays from Google Calendar")]
public class GoogleCalendarPublicHolidaysHandler : BaseCommandHandler
{
    private const string ApiKeySecretKey = "GoogleCalendar:ApiKey";
    private const string CalendarIdSettingKey = "GoogleCalendar:CalendarId";
    private const string MaxItemsSettingKey = "GoogleCalendar:MaxItems";

    public override async Task ExecuteAsync()
    {
        var resolvedApiKey = SecretsService.Get(ApiKeySecretKey)?.Trim();
        if (string.IsNullOrWhiteSpace(resolvedApiKey))
        {
            UserInterfaceService.ShowError($"Missing required secret '{ApiKeySecretKey}'.");
            return;
        }

        var resolvedCalendarId = SettingsService.Get(CalendarIdSettingKey)?.Trim();
        if (string.IsNullOrWhiteSpace(resolvedCalendarId))
        {
            UserInterfaceService.ShowError($"Missing required setting '{CalendarIdSettingKey}'.");
            return;
        }

        var maxItemsRaw = SettingsService.Get(MaxItemsSettingKey)?.Trim();
        if (string.IsNullOrWhiteSpace(maxItemsRaw))
        {
            UserInterfaceService.ShowError($"Missing required setting '{MaxItemsSettingKey}'.");
            return;
        }

        if (!int.TryParse(maxItemsRaw, out var maxItems) || maxItems <= 0)
        {
            UserInterfaceService.ShowError($"Invalid value for setting '{MaxItemsSettingKey}'. Expected a positive integer.");
            return;
        }

        try
        {
            var connector = new GoogleCalendarConnector(resolvedApiKey, isVerbose: false);
            var holidays = await connector.ListPublicHolidaysAsync(resolvedCalendarId, maxItems);

            if (holidays.Count == 0)
            {
                UserInterfaceService.ShowWarning("No public holidays found for the selected calendar.");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan)
                .AddColumn("[bold]Date[/]")
                .AddColumn("[bold]Holiday[/]")
                .AddColumn("[bold]Description[/]");

            foreach (var holiday in holidays)
            {
                var date = holiday.start?.date != default
                    ? holiday.start.date.ToString("yyyy-MM-dd")
                    : holiday.start?.dateTime.ToString("yyyy-MM-dd") ?? "-";

                table.AddRow(
                    Markup.Escape(date),
                    Markup.Escape(holiday.summary ?? "-"),
                    Markup.Escape(holiday.description ?? "-"));
            }

            UserInterfaceService.ShowTable(table);
        }
        catch (Exception ex)
        {
            UserInterfaceService.ShowError($"Failed to fetch public holidays: {ex.Message}");
        }
    }
}
