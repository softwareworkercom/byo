using System.CommandLine;
using System.CommandLine.Completions;

namespace SoftwareWorker.BYO.CLI.Core.Shell
{
    internal enum ShellCompletionKind
    {
        Command,
        Builtin,
        Option,
        Value,
        File,
        Directory
    }

    /// <param name="Label">Text shown in the completion menu.</param>
    /// <param name="InsertText">Text that replaces the word being completed.</param>
    /// <param name="Description">Documentation shown next to the item.</param>
    /// <param name="FilterText">Text matched against the word being completed.</param>
    internal sealed record ShellCompletionItem(
        string Label,
        string InsertText,
        string? Description,
        ShellCompletionKind Kind,
        string FilterText);

    /// <param name="ReplaceStart">Start of the text replaced by an accepted item.</param>
    /// <param name="ReplaceEnd">End of the text replaced by an accepted item.</param>
    /// <param name="WordToComplete">The (unquoted) text between the start of the word and the caret.</param>
    internal sealed record ShellCompletionResult(
        int ReplaceStart,
        int ReplaceEnd,
        string WordToComplete,
        IReadOnlyList<ShellCompletionItem> Items)
    {
        public static ShellCompletionResult Empty(int caret) => new(caret, caret, string.Empty, []);
    }

    /// <summary>
    /// Suggests commands, options, option values and file system paths for the word under the caret.
    /// </summary>
    internal sealed class ShellCompletionProvider
    {
        private const int MaxPathEntries = 500;
        private static readonly string[] PathOptionHints = ["file", "path", "folder", "dir"];

        private readonly RootCommand _rootCommand;
        private readonly IReadOnlyDictionary<string, string> _builtins;
        private readonly HashSet<string> _builtinNames;

        public ShellCompletionProvider(RootCommand rootCommand, IReadOnlyDictionary<string, string> builtins)
        {
            _rootCommand = rootCommand;
            _builtins = builtins;
            _builtinNames = [.. builtins.Keys];
        }

        public ShellCompletionResult GetCompletions(string text, int caret)
        {
            caret = Math.Clamp(caret, 0, text.Length);
            var tokens = ShellTokenizer.Tokenize(text);

            var currentIndex = tokens.FindIndex(t => t.Start <= caret && caret <= t.End);
            var current = currentIndex >= 0 ? tokens[currentIndex] : null;
            var precedingCount = currentIndex >= 0 ? currentIndex : tokens.Count(t => t.End < caret);

            var word = current == null
                ? string.Empty
                : ShellTokenizer.Tokenize(text[current.Start..caret]).FirstOrDefault()?.Value ?? string.Empty;
            var replaceStart = current?.Start ?? caret;
            var replaceEnd = current?.End ?? caret;

            var state = CommandLineAnalyzer.Walk(_rootCommand, _builtinNames, tokens, precedingCount);
            var candidates = GetCandidates(state, word);

            return new ShellCompletionResult(replaceStart, replaceEnd, word, Filter(candidates, word));
        }

        private IEnumerable<ShellCompletionItem> GetCandidates(CommandLineState state, string word)
        {
            if (state.PendingOption != null)
            {
                return GetValueCandidates(state.PendingOption, word);
            }

            if (state.IsDynamicValuePending && !CommandLineAnalyzer.IsOptionLike(word))
            {
                return LooksLikePath(word) ? GetPathCandidates(word) : [];
            }

            if (word.StartsWith('-'))
            {
                return GetOptionCandidates(state);
            }

            var command = state.CurrentCommand;
            var candidates = command.Subcommands
                .Where(c => !c.Hidden)
                .Select(c => new ShellCompletionItem(c.Name, c.Name, c.Description, ShellCompletionKind.Command, c.Name))
                .ToList();

            if (state.WordCount == 0)
            {
                candidates.AddRange(_builtins.Select(b =>
                    new ShellCompletionItem(b.Key, b.Key, b.Value, ShellCompletionKind.Builtin, b.Key)));
            }

            // Executable commands also offer their options once the user starts a new word.
            if (word.Length == 0 && state.WordCount > 0 && (command.Subcommands.Count == 0 || command.Action != null))
            {
                candidates.AddRange(GetOptionCandidates(state));
            }

            if (LooksLikePath(word))
            {
                candidates.AddRange(GetPathCandidates(word));
            }

            return candidates;
        }

        private static IEnumerable<ShellCompletionItem> GetOptionCandidates(CommandLineState state)
        {
            return CommandLineAnalyzer.GetOptionsInScope(state)
                .Where(o => !o.Hidden && !state.UsedOptions.Contains(o))
                .Select(o => new ShellCompletionItem(o.Name, o.Name, o.Description, ShellCompletionKind.Option, o.Name));
        }

        private static IEnumerable<ShellCompletionItem> GetValueCandidates(Option option, string word)
        {
            var candidates = new List<ShellCompletionItem>();

            try
            {
                candidates.AddRange(option.GetCompletions(CompletionContext.Empty).Select(c =>
                {
                    var value = c.InsertText ?? c.Label;
                    return new ShellCompletionItem(
                        c.Label,
                        ShellTokenizer.Quote(value),
                        c.Documentation ?? c.Detail,
                        ShellCompletionKind.Value,
                        value);
                }));
            }
            catch
            {
                // A faulty completion source must never break typing.
            }

            var isPathOption = PathOptionHints.Any(hint => option.Name.Contains(hint, StringComparison.OrdinalIgnoreCase));
            if (isPathOption || LooksLikePath(word))
            {
                candidates.AddRange(GetPathCandidates(word));
            }

            return candidates;
        }

        private static IEnumerable<ShellCompletionItem> GetPathCandidates(string word)
        {
            var separatorIndex = word.LastIndexOfAny(['/', '\\']);
            var typedDirectory = separatorIndex >= 0 ? word[..(separatorIndex + 1)] : string.Empty;
            var namePrefix = word[(separatorIndex + 1)..];
            var separator = separatorIndex >= 0 ? word[separatorIndex] : Path.DirectorySeparatorChar;

            string searchDirectory;
            try
            {
                var expandedDirectory = typedDirectory.StartsWith('~')
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + typedDirectory[1..]
                    : typedDirectory;
                searchDirectory = expandedDirectory.Length == 0
                    ? Environment.CurrentDirectory
                    : Path.GetFullPath(expandedDirectory, Environment.CurrentDirectory);
            }
            catch
            {
                return [];
            }

            if (!Directory.Exists(searchDirectory))
            {
                return [];
            }

            var includeHidden = namePrefix.StartsWith('.');
            var items = new List<ShellCompletionItem>();

            try
            {
                foreach (var entry in new DirectoryInfo(searchDirectory).EnumerateFileSystemInfos().Take(MaxPathEntries))
                {
                    if (!includeHidden && entry.Name.StartsWith('.'))
                    {
                        continue;
                    }

                    if (!entry.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var isDirectory = entry is DirectoryInfo;
                    var label = isDirectory ? entry.Name + separator : entry.Name;
                    var path = typedDirectory + label;

                    items.Add(new ShellCompletionItem(
                        label,
                        ShellTokenizer.Quote(path),
                        isDirectory ? "Directory" : "File",
                        isDirectory ? ShellCompletionKind.Directory : ShellCompletionKind.File,
                        path));
                }
            }
            catch
            {
                // Unreadable directories simply produce no suggestions.
            }

            return items
                .OrderBy(i => i.Kind == ShellCompletionKind.Directory ? 0 : 1)
                .ThenBy(i => i.Label, StringComparer.OrdinalIgnoreCase);
        }

        private static List<ShellCompletionItem> Filter(IEnumerable<ShellCompletionItem> candidates, string word)
        {
            var distinct = candidates.DistinctBy(c => (c.InsertText, c.Kind)).ToList();
            if (word.Length == 0)
            {
                return distinct;
            }

            // Prefix matches first, then matches anywhere in the text.
            var prefixMatches = distinct.Where(c => c.FilterText.StartsWith(word, StringComparison.OrdinalIgnoreCase));
            var containsMatches = distinct.Where(c =>
                !c.FilterText.StartsWith(word, StringComparison.OrdinalIgnoreCase) &&
                c.FilterText.Contains(word, StringComparison.OrdinalIgnoreCase));

            return prefixMatches.Concat(containsMatches).ToList();
        }

        private static bool LooksLikePath(string word)
        {
            return word.Contains('/') ||
                   word.Contains('\\') ||
                   word.StartsWith('.') ||
                   word.StartsWith('~') ||
                   (word.Length >= 2 && char.IsLetter(word[0]) && word[1] == ':');
        }
    }
}
