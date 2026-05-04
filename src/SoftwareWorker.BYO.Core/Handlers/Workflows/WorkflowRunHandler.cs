using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Workflows
{
    [TrunkCommand("workflows", "Workflow management")]
    [BranchCommand("run", "Run a workflow (execute interactive steps)")]
    [Parameter("name", "Workflow name", true, null)]
    internal class WorkflowRunHandler : BaseCommandHandler
    {
        public string? Name { get; set; }

        public override async Task ExecuteAsync()
        {
            var selectedWorkflow = WorkflowService.GetByName(Name!);

            if (selectedWorkflow == null)
            {
                UserInterfaceService.ShowWarning($"Workflow '{Name}' not found. Use 'byo workflows read' to list available workflows.");
                return;
            }

            await WorkflowExecutionService.ExecuteWorkflowAsync(selectedWorkflow);
        }
    }
}
