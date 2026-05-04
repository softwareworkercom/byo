using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Workflows
{
    [TrunkCommand("workflows", "Workflow management")]
    [BranchCommand("delete", "Delete a workflow")]
    internal class WorkflowDeleteHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var workflows = WorkflowService.GetList();

            if (workflows == null || workflows.Count == 0)
            {
                UserInterfaceService.ShowWarning("No workflows found.");
                return;
            }

            var selectedWorkflow = FolderNavigationService.NavigateAndSelect(
                workflows,
                w => w.FolderPath,
                w => w.Name,
                "workflow to delete");

            if (selectedWorkflow == null)
            {
                UserInterfaceService.ShowWarning("No workflow selected.");
                return;
            }

            // Confirm deletion
            var confirmed = UserInterfaceService.AskYesNo($"Are you sure you want to delete workflow '{selectedWorkflow.Name}'?");

            if (!confirmed)
            {
                UserInterfaceService.ShowWarning("Deletion cancelled.");
                return;
            }

            var deleted = WorkflowService.Delete(selectedWorkflow.Name);

            if (deleted)
            {
                UserInterfaceService.ShowGreen($"Workflow '{selectedWorkflow.Name}' deleted successfully.");
            }
            else
            {
                UserInterfaceService.ShowError($"Failed to delete workflow '{selectedWorkflow.Name}'.");
            }

            await Task.CompletedTask;
        }
    }
}
