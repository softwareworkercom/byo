using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Secrets
{
    [TrunkCommand("secrets", "Encrypted secrets operations")]
    [BranchCommand("read", "Read secrets")]
    public class SecretsReadHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var secretsTable = new Table();
            secretsTable.Title = new TableTitle("Secrets");
            secretsTable.AddColumn("Name");
            secretsTable.AddColumn("Value");

            var secrets = SecretsService.GetList() ?? new Dictionary<string, string>();

            if (secrets.Count == 0)
            {
                UserInterfaceService.ShowWarning("No secrets found. Use 'byo secrets update --key <key> --value <value>' to add one.");
                return;
            }

            foreach (var item in secrets.OrderBy(c => c.Key))
            {
                secretsTable.AddRow(
                    Markup.Escape(item.Key),
                    Markup.Escape(item.Value ?? string.Empty)
                );
            }

            UserInterfaceService.ShowTable(secretsTable);
        }
    }
}
