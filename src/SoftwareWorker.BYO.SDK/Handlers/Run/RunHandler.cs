using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Interactive
{
    [TrunkCommand("run", "Interactive execution")]
    [Parameter("target", "What to run (command or workflow)", true, "command|workflow")]
    [Parameter("name", "Name of the command/workflow to run", false, null, false)]
    [Parameter("bookmark", "Bookmark hierarchy path to locate the command/workflow", false, null, false)]
    internal class RunHandler : BaseCommandHandler
    {
        public RunTargetEnum? Target { get; set; }
        public string? Name { get; set; }
        public string? Bookmark { get; set; }

        public override async Task ExecuteAsync()
        {
            switch (ResolveTarget(Target))
            {
                case RunTargetEnum.Command:
                    var commands = CommandService.GetList().ToList();
                    if (commands.Count == 0)
                    {
                        UserInterfaceService.ShowWarning("No commands found. Use 'byo commands create' first.");
                        return;
                    }
                    await RunCommandAsync(commands, Name, Bookmark);
                    break;
                case RunTargetEnum.Workflow:
                    var workflows = WorkflowService.GetList().ToList();
                    if (workflows.Count == 0)
                    {
                        UserInterfaceService.ShowWarning("No workflows found. Use 'byo workflows create' first.");
                        return;
                    }
                    await RunWorkflowAsync(workflows, Name, Bookmark);
                    break;
            }
        }

        private static RunTargetEnum ResolveTarget(RunTargetEnum? target)
        {
            if (target.HasValue)
            {
                return target.Value;
            }

            return UserInterfaceService.Prompt(
                new SelectionPrompt<RunTargetEnum>()
                    .Title("[cyan]Select what to run:[/]")
                    .PageSize(10)
                    .UseConverter(value => value.ToString().ToLowerInvariant())
                    .AddChoices(Enum.GetValues<RunTargetEnum>()));
        }

        private static Task RunCommandAsync(List<ShellCommand> commands, string? name, string? bookmark)
        {
            if (commands.Count == 0)
            {
                UserInterfaceService.ShowWarning("No saved commands found. Use 'byo commands create' to add a command.");
                return Task.CompletedTask;
            }

            ShellCommand? selectedCommand;

            if (!string.IsNullOrWhiteSpace(name))
            {
                var normalizedBookmark = FolderNavigationService.NormalizePath(bookmark);
                selectedCommand = commands.FirstOrDefault(c =>
                    string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        FolderNavigationService.NormalizePath(c.Bookmark),
                        normalizedBookmark,
                        StringComparison.OrdinalIgnoreCase));

                if (selectedCommand == null)
                {
                    var bookmarkLabel = string.IsNullOrWhiteSpace(bookmark) ? "/" : bookmark;
                    UserInterfaceService.ShowWarning($"Command '{name}' was not found in bookmark '{bookmarkLabel}'.");
                    return Task.CompletedTask;
                }
            }
            else
            {
                selectedCommand = FolderNavigationService.NavigateAndSelect(
                    commands,
                    c => c.Bookmark,
                    c => string.IsNullOrWhiteSpace(c.Name)
                        ? c.Executable
                        : $"{c.Name} ({c.Executable})",
                    "command to run");
            }

            if (selectedCommand == null)
            {
                UserInterfaceService.ShowWarning("No command selected.");
                return Task.CompletedTask;
            }

            var resolvedExecutable = TokenService.ResolveTokens(selectedCommand.Executable);
            var resolvedDirectory = string.IsNullOrWhiteSpace(selectedCommand.Directory)
                ? null
                : TokenService.ResolveTokens(selectedCommand.Directory);

            TerminalService.Run(
                resolvedExecutable,
                resolvedDirectory,
                shell: selectedCommand.Shell);

            return Task.CompletedTask;
        }

        private static async Task RunWorkflowAsync(List<Workflow> workflows, string? name, string? bookmark)
        {
            if (workflows.Count == 0)
            {
                UserInterfaceService.ShowWarning("No workflows found. Use 'byo workflows create' to create a workflow.");
                return;
            }

            Workflow? selectedWorkflow;

            if (!string.IsNullOrWhiteSpace(name))
            {
                var normalizedBookmark = FolderNavigationService.NormalizePath(bookmark);
                selectedWorkflow = workflows.FirstOrDefault(w =>
                    string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        FolderNavigationService.NormalizePath(w.Bookmark),
                        normalizedBookmark,
                        StringComparison.OrdinalIgnoreCase));

                if (selectedWorkflow == null)
                {
                    var bookmarkLabel = string.IsNullOrWhiteSpace(bookmark) ? "/" : bookmark;
                    UserInterfaceService.ShowWarning($"Workflow '{name}' was not found in bookmark '{bookmarkLabel}'.");
                    return;
                }
            }
            else
            {
                selectedWorkflow = FolderNavigationService.NavigateAndSelect(
                    workflows,
                    w => w.Bookmark,
                    w => w.Name,
                    "workflow to run");
            }

            if (selectedWorkflow == null)
            {
                UserInterfaceService.ShowWarning("No workflow selected.");
                return;
            }

            await WorkflowExecutionService.ExecuteWorkflowAsync(selectedWorkflow);
        }
    }
}
