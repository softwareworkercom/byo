namespace SoftwareWorker.BYO.CLI.Core.Shell
{
    /// <summary>
    /// The console operations the line editor needs, so it can be driven by scripted keys in tests.
    /// </summary>
    internal interface IShellConsole
    {
        ConsoleKeyInfo ReadKey();

        bool KeyAvailable { get; }

        int WindowWidth { get; }

        int CursorLeft { get; }

        bool TreatControlCAsInput { get; set; }

        void Write(string text);
    }

    internal sealed class SystemShellConsole : IShellConsole
    {
        public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

        public bool KeyAvailable => Console.KeyAvailable;

        public int WindowWidth
        {
            get
            {
                try
                {
                    return Console.WindowWidth > 0 ? Console.WindowWidth : 120;
                }
                catch (IOException)
                {
                    return 120;
                }
            }
        }

        public int CursorLeft
        {
            get
            {
                try
                {
                    return Console.CursorLeft;
                }
                catch (IOException)
                {
                    return 0;
                }
            }
        }

        public bool TreatControlCAsInput
        {
            get => Console.TreatControlCAsInput;
            set => Console.TreatControlCAsInput = value;
        }

        public void Write(string text)
        {
            Console.Out.Write(text);
            Console.Out.Flush();
        }
    }

    /// <summary>
    /// ANSI styles used by the interactive shell.
    /// </summary>
    internal static class ShellTheme
    {
        public const string Reset = "\u001b[0m";
        public const string Prompt = "\u001b[92m";
        public const string PromptError = "\u001b[91m";
        public const string PromptSeparator = "\u001b[90m";
        public const string Ghost = "\u001b[90m";
        public const string Selection = "\u001b[7m";
        public const string MenuItem = "\u001b[39m";
        public const string MenuDescription = "\u001b[90m";
        public const string MenuSelected = "\u001b[30;46m";
        public const string MenuFooter = "\u001b[90m";

        public static string For(ShellTokenKind kind, bool isQuoted)
        {
            if (isQuoted && kind is ShellTokenKind.OptionValue or ShellTokenKind.Argument)
            {
                return "\u001b[32m";
            }

            return kind switch
            {
                ShellTokenKind.ToolName => "\u001b[90m",
                ShellTokenKind.Builtin => "\u001b[95m",
                ShellTokenKind.Command => "\u001b[96m",
                ShellTokenKind.Option => "\u001b[93m",
                ShellTokenKind.DynamicOption => "\u001b[33m",
                ShellTokenKind.OptionValue => "\u001b[97m",
                ShellTokenKind.Argument => "\u001b[97m",
                ShellTokenKind.Unknown => "\u001b[91m",
                _ => string.Empty
            };
        }
    }
}
