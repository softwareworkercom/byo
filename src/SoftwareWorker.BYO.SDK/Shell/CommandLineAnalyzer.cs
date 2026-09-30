using System.CommandLine;

namespace SoftwareWorker.BYO.CLI.Core.Shell
{
    internal enum ShellTokenKind
    {
        ToolName,
        Builtin,
        Command,
        Option,
        DynamicOption,
        OptionValue,
        Argument,
        Unknown
    }

    internal sealed record ClassifiedToken(ShellToken Token, ShellTokenKind Kind);

    /// <summary>
    /// Where the analyzer ended up after walking a sequence of tokens.
    /// </summary>
    internal sealed class CommandLineState
    {
        public CommandLineState(RootCommand rootCommand)
        {
            CommandPath = [rootCommand];
        }

        /// <summary>
        /// The matched commands, starting with the root command.
        /// </summary>
        public List<Command> CommandPath { get; }

        public Command CurrentCommand => CommandPath[^1];

        /// <summary>
        /// A declared option that is still waiting for its value.
        /// </summary>
        public Option? PendingOption { get; set; }

        /// <summary>
        /// An undeclared (dynamic) option that is still waiting for its value.
        /// </summary>
        public bool IsDynamicValuePending { get; set; }

        public HashSet<Option> UsedOptions { get; } = [];

        /// <summary>
        /// Number of words consumed, not counting a leading tool name.
        /// </summary>
        public int WordCount { get; set; }
    }

    /// <summary>
    /// Walks shell tokens through the System.CommandLine command tree to find out what each token is.
    /// </summary>
    internal static class CommandLineAnalyzer
    {
        public const string ToolName = "byo";

        public static IReadOnlyList<ClassifiedToken> Classify(
            RootCommand rootCommand,
            IReadOnlyCollection<string> builtins,
            IReadOnlyList<ShellToken> tokens)
        {
            var state = new CommandLineState(rootCommand);
            var classified = new List<ClassifiedToken>(tokens.Count);

            for (var index = 0; index < tokens.Count; index++)
            {
                classified.Add(new ClassifiedToken(tokens[index], Advance(state, builtins, tokens[index].Value, index)));
            }

            return classified;
        }

        /// <summary>
        /// Walks the first <paramref name="count"/> tokens and returns the resulting state.
        /// </summary>
        public static CommandLineState Walk(
            RootCommand rootCommand,
            IReadOnlyCollection<string> builtins,
            IReadOnlyList<ShellToken> tokens,
            int count)
        {
            var state = new CommandLineState(rootCommand);
            for (var index = 0; index < count; index++)
            {
                Advance(state, builtins, tokens[index].Value, index);
            }

            return state;
        }

        public static IEnumerable<Option> GetOptionsInScope(CommandLineState state)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var option in state.CurrentCommand.Options)
            {
                if (seen.Add(option.Name))
                {
                    yield return option;
                }
            }

            // Recursive options (like --help) declared on parent commands also apply to their subcommands.
            foreach (var command in state.CommandPath.Take(state.CommandPath.Count - 1).Reverse())
            {
                foreach (var option in command.Options.Where(o => o.Recursive))
                {
                    if (seen.Add(option.Name))
                    {
                        yield return option;
                    }
                }
            }
        }

        public static bool TakesValue(Option option)
        {
            return option.Arity.MinimumNumberOfValues > 0;
        }

        public static bool IsOptionLike(string value)
        {
            return value.Length > 1 && value[0] == '-';
        }

        private static ShellTokenKind Advance(CommandLineState state, IReadOnlyCollection<string> builtins, string value, int index)
        {
            if (index == 0 && string.Equals(value, ToolName, StringComparison.OrdinalIgnoreCase))
            {
                return ShellTokenKind.ToolName;
            }

            var isFirstWord = state.WordCount == 0;
            state.WordCount++;

            if (state.PendingOption != null)
            {
                state.PendingOption = null;
                return ShellTokenKind.OptionValue;
            }

            if (state.IsDynamicValuePending)
            {
                state.IsDynamicValuePending = false;
                if (!IsOptionLike(value))
                {
                    return ShellTokenKind.OptionValue;
                }
            }

            if (IsOptionLike(value))
            {
                var hasInlineValue = TrySplitInlineValue(value, out var optionName);
                var option = FindOption(state, optionName);

                if (option != null)
                {
                    state.UsedOptions.Add(option);
                    state.PendingOption = !hasInlineValue && TakesValue(option) ? option : null;
                    return ShellTokenKind.Option;
                }

                // Undeclared options are forwarded as dynamic parameters and consume the next token as their value.
                state.IsDynamicValuePending = !hasInlineValue;
                return state.CurrentCommand.TreatUnmatchedTokensAsErrors
                    ? ShellTokenKind.Unknown
                    : ShellTokenKind.DynamicOption;
            }

            var subcommand = state.CurrentCommand.Subcommands.FirstOrDefault(c =>
                !c.Hidden && (c.Name == value || c.Aliases.Contains(value)));
            if (subcommand != null)
            {
                state.CommandPath.Add(subcommand);
                return ShellTokenKind.Command;
            }

            if (isFirstWord && builtins.Contains(value))
            {
                return ShellTokenKind.Builtin;
            }

            return state.CurrentCommand.Arguments.Count > 0 ? ShellTokenKind.Argument : ShellTokenKind.Unknown;
        }

        private static bool TrySplitInlineValue(string value, out string optionName)
        {
            var separatorIndex = value.IndexOfAny([':', '=']);
            optionName = separatorIndex > 0 ? value[..separatorIndex] : value;
            return separatorIndex > 0;
        }

        private static Option? FindOption(CommandLineState state, string name)
        {
            return GetOptionsInScope(state).FirstOrDefault(o => o.Name == name || o.Aliases.Contains(name));
        }
    }
}
