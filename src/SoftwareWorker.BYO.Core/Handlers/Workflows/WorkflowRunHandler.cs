using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Workflows
{
    [TrunkCommand("workflows", "Workflow management")]
    [BranchCommand("run", "Run a workflow (execute interactive steps)")]
    [Parameter("name", "Workflow name", true, null)]
    [Parameter("bookmark", "Bookmark hierarchy path (e.g. DevOps/Deploy)", true, null)]
    internal class WorkflowRunHandler : BaseCommandHandler
    {
        public string? Name { get; set; }
        public string? Bookmark { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Bookmark))
            {
                UserInterfaceService.ShowError("Bookmark is required.");
                return;
            }

            var normalizedBookmark = FolderNavigationService.NormalizePath(Bookmark);
            var selectedWorkflow = WorkflowService.GetList().FirstOrDefault(w =>
                w.Name.Equals(Name!, StringComparison.OrdinalIgnoreCase) &&
                FolderNavigationService.NormalizePath(w.Bookmark).Equals(normalizedBookmark, StringComparison.OrdinalIgnoreCase));

            if (selectedWorkflow == null)
            {
                UserInterfaceService.ShowWarning($"Workflow '{Name}' not found in bookmark '{normalizedBookmark}'. Use 'byo workflows list' to see all available workflows.");
                return;
            }

            await WorkflowExecutionService.ExecuteWorkflowAsync(selectedWorkflow);
        }
    }
}
