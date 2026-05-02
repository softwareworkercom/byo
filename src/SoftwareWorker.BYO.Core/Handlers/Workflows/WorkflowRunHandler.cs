using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Workflows
{
    [TrunkCommand("workflows", "Workflow Management")]
    [BranchCommand("run", "Run a workflow (execute interactive steps)")]
    internal class WorkflowRunHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var workflows = WorkflowService.GetList();

            if (workflows == null || workflows.Count == 0)
            {
                UserInterfaceService.ShowWarning("No workflows found. Use 'sw workflow create' to create a workflow.");
                return;
            }

            var selectedWorkflow = FolderNavigationService.NavigateAndSelect(
                workflows,
                w => w.FolderPath,
                w => w.Name,
                "workflow to run");

            if (selectedWorkflow == null)
            {
                UserInterfaceService.ShowWarning("No workflow selected.");
                return;
            }

            await WorkflowManagementService.ExecuteWorkflowAsync(selectedWorkflow);
        }
    }
}
