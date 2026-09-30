using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Engine;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;
using System.CommandLine;
using System.Diagnostics;

namespace SoftwareWorker.BYO.CLI.Core.Shell
{
    /// <summary>
    /// Read-eval-print loop for BYO commands with autocompletion, syntax highlighting and history.
    /// Each command runs in a child process, like a regular shell, so Ctrl+C stops the command without
    /// closing the shell and plugins installed during the session are picked up by the next command.
    /// </summary>
    internal sealed class InteractiveShell
    {
        public static readonly IReadOnlyDictionary<string, string> Builtins = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["exit"] = "Exit the interactive shell",
            ["quit"] = "Exit the interactive shell",
            ["clear"] = "Clear the screen",
            ["cls"] = "Clear the screen",
            ["history"] = "Show the command history",
            ["help"] = "Show help for BYO or for a command (help <command>)"
        };

        private static readonly HashSet<string> BuiltinNames = [.. Builtins.Keys];

        private readonly ShellHistory _history;
        private RootCommand _rootCommand;
        private ShellCompletionProvider _completionProvider;
        private int _lastExitCode;

        private InteractiveShell(RootCommand rootCommand, ShellHistory history)
        {
            _history = history;
            _rootCommand = rootCommand;
            _completionProvider = new ShellCompletionProvider(rootCommand, Builtins);
        }

        public static int Run()
        {
            var shell = new InteractiveShell(
                CommandsRouter.BuildRootCommand(),
                ShellHistory.Load(SystemConstants.STORAGE_SHELL_HISTORY_FILE));

            return shell.RunLoop();
        }

        private int RunLoop()
        {
            // Spectre.Console enables virtual terminal processing on Windows while detecting ANSI support.
            var supportsAnsi = AnsiConsole.Profile.Capabilities.Ansi;
            var editor = supportsAnsi
                ? new LineEditor(new SystemShellConsole(), _history, GetCompletions, Classify)
                : null;

            ShowBanner(supportsAnsi);

            while (true)
            {
                var result = editor != null ? editor.ReadLine(CreatePrompt()) : ReadLineWithoutEditor();

                if (result.Status == ShellReadStatus.EndOfInput)
                {
                    return 0;
                }

                if (result.Status == ShellReadStatus.Cancelled || string.IsNullOrWhiteSpace(result.Text))
                {
                    continue;
                }

                _history.Add(result.Text);

                if (!Execute(result.Text))
                {
                    return 0;
                }
            }
        }

        private ShellCompletionResult GetCompletions(string text, int caret) => _completionProvider.GetCompletions(text, caret);

        private IReadOnlyList<ClassifiedToken> Classify(string text) =>
            CommandLineAnalyzer.Classify(_rootCommand, BuiltinNames, ShellTokenizer.Tokenize(text));

        /// <summary>
        /// Runs one input line. Returns false when the shell should exit.
        /// </summary>
        private bool Execute(string line)
        {
            var args = ShellTokenizer.Tokenize(line).Select(t => t.Value).ToList();
            if (args.Count > 0 && string.Equals(args[0], CommandLineAnalyzer.ToolName, StringComparison.OrdinalIgnoreCase))
            {
                args.RemoveAt(0);
            }

            if (args.Count == 0)
            {
                return true;
            }

            switch (args[0])
            {
                case "exit":
                case "quit":
                    return false;
                case "clear":
                case "cls":
                    AnsiConsole.Clear();
                    return true;
                case "history":
                    ShowHistory();
                    return true;
                case "help":
                    _lastExitCode = RunInChildProcess([.. args.Skip(1), "--help"]);
                    return true;
            }

            _lastExitCode = RunInChildProcess(args);

            // Installing or removing plugins changes the available commands.
            if (args[0] == "plugins")
            {
                ReloadCommands();
            }

            return true;
        }

        private static int RunInChildProcess(IReadOnlyList<string> args)
        {
            // The child process receives Ctrl+C from the shared console; the shell ignores it and keeps running.
            ConsoleCancelEventHandler ignoreCancel = (_, eventArgs) => eventArgs.Cancel = true;
            Console.CancelKeyPress += ignoreCancel;

            try
            {
                using var process = Process.Start(CommandsBuilder.CreateSelfProcessStartInfo(args));
                if (process == null)
                {
                    UserInterfaceService.ShowError("Unable to start the command.");
                    return 1;
                }

                process.WaitForExit();
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                UserInterfaceService.ShowError($"Unable to start the command: {ex.Message}");
                return 1;
            }
            finally
            {
                Console.CancelKeyPress -= ignoreCancel;
            }
        }

        private void ReloadCommands()
        {
            try
            {
                _rootCommand = CommandsRouter.BuildRootCommand();
                _completionProvider = new ShellCompletionProvider(_rootCommand, Builtins);
            }
            catch (Exception ex)
            {
                UserInterfaceService.ShowWarning($"Unable to reload commands: {ex.Message}");
            }
        }

        private ShellPrompt CreatePrompt()
        {
            var separatorStyle = _lastExitCode == 0 ? ShellTheme.PromptSeparator : ShellTheme.PromptError;
            return new ShellPrompt(
                "byo> ",
                $"{ShellTheme.Prompt}byo{ShellTheme.Reset}{separatorStyle}>{ShellTheme.Reset} ");
        }

        private ShellReadResult ReadLineWithoutEditor()
        {
            Console.Write("byo> ");
            var line = Console.ReadLine();
            return line == null
                ? new ShellReadResult(ShellReadStatus.EndOfInput, string.Empty)
                : new ShellReadResult(ShellReadStatus.Submitted, line);
        }

        private static void ShowBanner(bool supportsAnsi)
        {
            UserInterfaceService.ShowCyan($"BYO interactive shell {CommandsBuilder.GetCurrentVersion()}");
            UserInterfaceService.ShowGrey(supportsAnsi
                ? "Tab or Ctrl+Space: complete | Up/Down: history | Right: accept suggestion | Ctrl+D or 'exit': quit"
                : "This terminal does not support ANSI, so autocompletion and highlighting are disabled. Type 'exit' to quit.");
            UserInterfaceService.WriteLine();
        }

        private void ShowHistory()
        {
            var entries = _history.Entries;
            var start = Math.Max(0, entries.Count - 50);
            for (var index = start; index < entries.Count; index++)
            {
                UserInterfaceService.ShowMarkup($"[grey]{index + 1,4}[/]  {Markup.Escape(entries[index])}");
            }
        }
    }
}
