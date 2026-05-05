using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Interactive
{
    [TrunkCommand("run", "Interactive execution")]
    [Parameter("target", "What to run (command or workflow)", true, "command|workflow")]
    internal class RunHandler : BaseCommandHandler
    {
        public RunTargetEnum? Target { get; set; }

        public override async Task ExecuteAsync()
        {
            switch (Target)
            {
                case RunTargetEnum.Command:
                    var commands = CommandService.GetList().ToList();
                    if (commands.Count == 0)
                    {
                        UserInterfaceService.ShowWarning("No commands found. Use 'byo commands create' first.");
                        return;
                    }
                    await RunCommandAsync(commands);
                    break;
                case RunTargetEnum.Workflow:
                    var workflows = WorkflowService.GetList().ToList();
                    if (workflows.Count == 0)
                    {
                        UserInterfaceService.ShowWarning("No workflows found. Use 'byo workflows create' first.");
                        return;
                    }
                    await RunWorkflowAsync(workflows);
                    break;
            }
        }

        private static Task RunCommandAsync(List<ShellCommand> commands)
        {
            if (commands.Count == 0)
            {
                UserInterfaceService.ShowWarning("No saved commands found. Use 'byo commands create' to add a command.");
                return Task.CompletedTask;
            }

            var selectedCommand = FolderNavigationService.NavigateAndSelect(
                commands,
                c => c.Bookmark,
                c => string.IsNullOrWhiteSpace(c.Name)
                    ? c.Executable
                    : $"{c.Name} ({c.Executable})",
                "command to run");

            if (selectedCommand == null)
            {
                UserInterfaceService.ShowWarning("No command selected.");
                return Task.CompletedTask;
            }

            UserInterfaceService.ShowGreen("Executing command...");

            TerminalService.Run(
                selectedCommand.Executable,
                selectedCommand.Directory,
                shell: selectedCommand.Shell);

            return Task.CompletedTask;
        }

        private static async Task RunWorkflowAsync(List<Workflow> workflows)
        {
            if (workflows.Count == 0)
            {
                UserInterfaceService.ShowWarning("No workflows found. Use 'byo workflows create' to create a workflow.");
                return;
            }

            var selectedWorkflow = FolderNavigationService.NavigateAndSelect(
                workflows,
                w => w.Bookmark,
                w => w.Name,
                "workflow to run");

            if (selectedWorkflow == null)
            {
                UserInterfaceService.ShowWarning("No workflow selected.");
                return;
            }

            await WorkflowExecutionService.ExecuteWorkflowAsync(selectedWorkflow);
        }
    }
}
