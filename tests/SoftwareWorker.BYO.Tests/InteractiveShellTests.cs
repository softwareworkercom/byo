using SoftwareWorker.BYO.CLI.Engine;
using SoftwareWorker.BYO.CLI.Shell;
using System.CommandLine;
using System.CommandLine.Completions;

namespace SoftwareWorker.BYO.Tests;

public class InteractiveShellTests
{
    private static readonly HashSet<string> BuiltinNames = [.. InteractiveShell.Builtins.Keys];

    #region Tokenizer

    [Fact]
    public void Tokenize_ShouldSplitOnWhitespaceAndUnquoteValues()
    {
        var tokens = ShellTokenizer.Tokenize("sqlserver query --filename \"C:\\My Queries\\a.sql\" --name='x y'");

        Assert.Equal(["sqlserver", "query", "--filename", "C:\\My Queries\\a.sql", "--name=x y"], tokens.Select(t => t.Value));
        Assert.True(tokens[3].IsQuoted);
        Assert.Equal(27, tokens[3].Start);
        Assert.Equal(48, tokens[3].End);
    }

    [Fact]
    public void Tokenize_ShouldMarkUnterminatedQuotes()
    {
        var token = Assert.Single(ShellTokenizer.Tokenize("\"C:\\My Dir"));

        Assert.Equal("C:\\My Dir", token.Value);
        Assert.False(token.IsTerminated);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("with space", "\"with space\"")]
    [InlineData("say \"hi\"", "'say \"hi\"'")]
    public void Quote_ShouldOnlyQuoteWhenNeeded(string value, string expected)
    {
        Assert.Equal(expected, ShellTokenizer.Quote(value));
    }

    #endregion

    #region Analyzer

    [Fact]
    public void Classify_ShouldIdentifyCommandsOptionsValuesAndErrors()
    {
        var rootCommand = CreateRootCommand();
        var tokens = ShellTokenizer.Tokenize("byo sqlserver query --filename a.sql --ServiceId 5 bogus");

        var kinds = CommandLineAnalyzer.Classify(rootCommand, BuiltinNames, tokens).Select(t => t.Kind);

        Assert.Equal(
            [
                ShellTokenKind.ToolName,
                ShellTokenKind.Command,
                ShellTokenKind.Command,
                ShellTokenKind.Option,
                ShellTokenKind.OptionValue,
                ShellTokenKind.DynamicOption,
                ShellTokenKind.OptionValue,
                ShellTokenKind.Unknown
            ],
            kinds);
    }

    [Fact]
    public void Classify_ShouldRecognizeBuiltinsOnlyAsFirstWord()
    {
        var rootCommand = CreateRootCommand();

        // "help <command>" is valid, so commands after a builtin are still recognized.
        var kinds = CommandLineAnalyzer.Classify(rootCommand, BuiltinNames, ShellTokenizer.Tokenize("help settings exit"))
            .Select(t => t.Kind);

        Assert.Equal([ShellTokenKind.Builtin, ShellTokenKind.Command, ShellTokenKind.Unknown], kinds);
    }

    #endregion

    #region Completion

    [Fact]
    public void GetCompletions_OnEmptyLine_ShouldOfferCommandsAndBuiltins()
    {
        var labels = GetCompletionLabels(string.Empty);

        Assert.Contains("sqlserver", labels);
        Assert.Contains("settings", labels);
        Assert.Contains("exit", labels);
    }

    [Fact]
    public void GetCompletions_ShouldRankPrefixMatchesBeforeContainsMatches()
    {
        var labels = GetCompletionLabels("se");

        Assert.Equal(["settings", "secrets", "sqlserver"], labels);
    }

    [Fact]
    public void GetCompletions_AfterCommand_ShouldOfferSubcommands()
    {
        var result = CreateProvider().GetCompletions("sqlserver ", 10);

        Assert.Equal(["query"], result.Items.Select(i => i.Label));
        Assert.Equal(10, result.ReplaceStart);
    }

    [Fact]
    public void GetCompletions_ForOptions_ShouldSkipOptionsAlreadyUsed()
    {
        var labels = GetCompletionLabels("sqlserver query --filename a.sql --");

        Assert.Contains("--connection", labels);
        Assert.Contains("--help", labels);
        Assert.DoesNotContain("--filename", labels);
    }

    [Fact]
    public void GetCompletions_InMiddleOfLine_ShouldReplaceTheWholeWord()
    {
        var result = CreateProvider().GetCompletions("sqlserver qu --filename a.sql", 12);

        Assert.Equal("qu", result.WordToComplete);
        Assert.Equal(10, result.ReplaceStart);
        Assert.Equal(12, result.ReplaceEnd);
        Assert.Equal("query", Assert.Single(result.Items).InsertText);
    }

    [Fact]
    public void GetCompletions_ForOptionValue_ShouldUseCompletionSources()
    {
        var labels = GetCompletionLabels("sqlserver query --connection ");

        Assert.Equal(["Production", "Staging"], labels);
    }

    [Fact]
    public void GetCompletions_ForPathValue_ShouldListDirectoryEntriesAndQuoteSpaces()
    {
        var directory = Directory.CreateTempSubdirectory("byo-shell-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "Orders Search.sql"), string.Empty);
            Directory.CreateDirectory(Path.Combine(directory.FullName, "archive"));
            var line = $"sqlserver query --filename {directory.FullName}{Path.DirectorySeparatorChar}";

            var items = CreateProvider().GetCompletions(line, line.Length).Items;

            Assert.Equal(["archive" + Path.DirectorySeparatorChar, "Orders Search.sql"], items.Select(i => i.Label));
            Assert.Equal(ShellCompletionKind.Directory, items[0].Kind);
            Assert.Equal($"\"{Path.Combine(directory.FullName, "Orders Search.sql")}\"", items[1].InsertText);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetCompletions_WithBuiltInCommands_ShouldOfferPipeSeparatedParameterValues()
    {
        var provider = new ShellCompletionProvider(CommandsRouter.BuildRootCommand(), InteractiveShell.Builtins);

        var labels = provider.GetCompletions("run --target ", 13).Items.Select(i => i.Label);

        Assert.Equal(["command", "workflow"], labels);
    }

    #endregion

    #region History

    [Fact]
    public void History_ShouldMoveRepeatedEntriesToTheEndAndSuggestTheLatestMatch()
    {
        var history = new ShellHistory();
        history.Add("settings list");
        history.Add("settings set --key a");
        history.Add("settings list");

        Assert.Equal(["settings set --key a", "settings list"], history.Entries);
        Assert.Equal("settings list", history.FindSuggestion("settings"));
        Assert.Equal("settings set --key a", history.FindSuggestion("settings s"));
        Assert.Null(history.FindSuggestion("settings list"));
    }

    [Fact]
    public void History_ShouldNotPersistLinesThatLookSensitive()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"byo-history-{Guid.NewGuid():N}.txt");
        try
        {
            var history = ShellHistory.Load(filePath);
            history.Add("settings list");
            history.Add("secrets set --name ApiKey --password hunter2");

            Assert.Equal(2, history.Entries.Count);
            Assert.Equal(["settings list"], ShellHistory.Load(filePath).Entries);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Theory]
    [InlineData("help settings list", "settings list --help")]
    [InlineData("help", "--help")]
    [InlineData("byo settings list", "settings list")]
    [InlineData("byo sqlserver query --filename \"C:\\My Queries\\a.sql\"", "sqlserver query --filename \"C:\\My Queries\\a.sql\"")]
    public void History_ShouldSaveTheExecutedCommand(string input, string expected)
    {
        Assert.Equal(expected, InteractiveShell.GetHistoryEntry(input));
    }

    [Fact]
    public void History_ShouldClearPersistedEntries()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"byo-history-{Guid.NewGuid():N}.txt");
        try
        {
            var history = ShellHistory.Load(filePath);
            history.Add("settings list");
            history.Add("settings set --key a");

            history.Clear();

            Assert.Empty(history.Entries);
            Assert.Empty(ShellHistory.Load(filePath).Entries);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void History_ShouldSkipStandaloneToolName()
    {
        Assert.Null(InteractiveShell.GetHistoryEntry("byo"));
    }

    #endregion

    #region Line editor

    [Fact]
    public void Editor_TabWithSingleMatch_ShouldCompleteAndAddSpace()
    {
        var result = ReadLine(Keys.Type("sqlse"), Keys.Press(ConsoleKey.Tab), Keys.Press(ConsoleKey.Enter));

        Assert.Equal("sqlserver ", result.Text);
    }

    [Fact]
    public void Editor_TypingShouldOpenMenuAndEnterShouldAcceptTheSelection()
    {
        var result = ReadLine(
            Keys.Type("se"),
            Keys.Press(ConsoleKey.DownArrow),
            Keys.Press(ConsoleKey.Enter),
            Keys.Press(ConsoleKey.Enter));

        Assert.Equal(ShellReadStatus.Submitted, result.Status);
        Assert.Equal("secrets ", result.Text);
    }

    [Fact]
    public void Editor_EnterOnCompleteWord_ShouldSubmit()
    {
        var result = ReadLine(Keys.Type("settings"), Keys.Press(ConsoleKey.Enter));

        Assert.Equal("settings", result.Text);
    }

    [Fact]
    public void Editor_EscapeShouldCloseMenuThenClearLine()
    {
        var editor = CreateEditor(out var console);
        console.Enqueue(Keys.Type("se"), Keys.Press(ConsoleKey.Escape), Keys.Press(ConsoleKey.Enter));

        Assert.Equal("se", editor.ReadLine(Prompt).Text);

        console.Enqueue(Keys.Type("se"), Keys.Press(ConsoleKey.Escape), Keys.Press(ConsoleKey.Escape), Keys.Press(ConsoleKey.Enter));

        Assert.Equal(string.Empty, editor.ReadLine(Prompt).Text);
    }

    [Fact]
    public void Editor_UpArrow_ShouldNavigateHistoryFilteredByTypedPrefix()
    {
        var history = new ShellHistory();
        history.Add("settings list");
        history.Add("sqlserver query");
        history.Add("settings set");

        var result = ReadLine(
            history,
            Keys.Type("settings"),
            Keys.Press(ConsoleKey.UpArrow),
            Keys.Press(ConsoleKey.UpArrow),
            Keys.Press(ConsoleKey.UpArrow),
            Keys.Press(ConsoleKey.Enter));

        Assert.Equal("settings list", result.Text);
    }

    [Fact]
    public void Editor_DownArrowPastNewestEntry_ShouldRestoreDraft()
    {
        var history = new ShellHistory();
        history.Add("settings list");

        var result = ReadLine(
            history,
            Keys.Press(ConsoleKey.UpArrow),
            Keys.Press(ConsoleKey.DownArrow),
            Keys.Press(ConsoleKey.Enter));

        Assert.Equal(string.Empty, result.Text);
    }

    [Fact]
    public void Editor_RightArrowAtEnd_ShouldAcceptHistorySuggestion()
    {
        var history = new ShellHistory();
        history.Add("settings list --filter abc");

        var fullResult = ReadLine(history, Keys.Type("settings l"), Keys.Press(ConsoleKey.RightArrow), Keys.Press(ConsoleKey.Enter));
        var wordResult = ReadLine(history, Keys.Type("settings l"), Keys.Press(ConsoleKey.RightArrow, control: true), Keys.Press(ConsoleKey.Enter));

        Assert.Equal("settings list --filter abc", fullResult.Text);
        Assert.Equal("settings list", wordResult.Text);
    }

    [Fact]
    public void Editor_UndoAndRedo_ShouldRestoreWordsTypedAsOneStep()
    {
        var result = ReadLine(
            Keys.Type("abc def"),
            Keys.Press(ConsoleKey.Z, control: true),
            Keys.Press(ConsoleKey.Z, control: true),
            Keys.Press(ConsoleKey.Y, control: true),
            Keys.Press(ConsoleKey.Enter));

        Assert.Equal("abc ", result.Text);
    }

    [Fact]
    public void Editor_ControlBackspace_ShouldDeletePreviousPathSegment()
    {
        var result = ReadLine(Keys.Type("xyz missing\\folder\\queries"), Keys.Press(ConsoleKey.Backspace, control: true), Keys.Press(ConsoleKey.Enter));

        Assert.Equal("xyz missing\\folder\\", result.Text);
    }

    [Fact]
    public void Editor_TypingOverSelection_ShouldReplaceIt()
    {
        var result = ReadLine(
            Keys.Type("xyz hello"),
            Keys.Press(ConsoleKey.LeftArrow, control: true, shift: true),
            Keys.Type("world"),
            Keys.Press(ConsoleKey.Home),
            Keys.Press(ConsoleKey.Delete),
            Keys.Press(ConsoleKey.Enter));

        Assert.Equal("yz world", result.Text);
    }

    [Fact]
    public void Editor_ControlC_ShouldCancelAndControlD_ShouldEndInput()
    {
        Assert.Equal(ShellReadStatus.Cancelled, ReadLine(Keys.Type("xyz"), Keys.Press(ConsoleKey.C, control: true)).Status);
        Assert.Equal(ShellReadStatus.EndOfInput, ReadLine(Keys.Press(ConsoleKey.D, control: true)).Status);
    }

    [Fact]
    public void Editor_ShouldHighlightCommandsAndRenderMenuDescriptions()
    {
        var editor = CreateEditor(out var console);
        console.Enqueue(Keys.Type("sqlserver q"), Keys.Press(ConsoleKey.Escape), Keys.Press(ConsoleKey.Enter));

        editor.ReadLine(Prompt);

        Assert.Contains(ShellTheme.For(ShellTokenKind.Command, false) + "sqlserver", console.Output);
        Assert.Contains("Run a SQL query", console.Output);
    }

    [Fact]
    public void Editor_ShouldControlCtrlCHandlingOnlyWhileReading()
    {
        var editor = CreateEditor(out var console);
        console.Enqueue(Keys.Press(ConsoleKey.Enter));

        editor.ReadLine(Prompt);

        Assert.True(console.WasControlCTreatedAsInput);
        Assert.False(console.TreatControlCAsInput);
    }

    #endregion

    #region Helpers

    private static readonly ShellPrompt Prompt = new("byo> ", "byo> ");

    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand();

        var connection = new Option<string>("--connection") { Description = "Connection string setting name" };
        connection.CompletionSources.Add(_ => [new CompletionItem("Production"), new CompletionItem("Staging")]);

        var query = new Command("query", "Run a SQL query from a file")
        {
            new Option<string>("--filename") { Description = "Path of the .sql file" },
            connection
        };
        query.TreatUnmatchedTokensAsErrors = false;
        query.SetAction(_ => { });

        rootCommand.Add(new Command("sqlserver", "SQL Server tools") { query });
        rootCommand.Add(new Command("settings", "Settings operations") { new Command("list", "Read settings"), new Command("set", "Set a setting") });
        rootCommand.Add(new Command("secrets", "Encrypted secrets operations"));
        return rootCommand;
    }

    private static ShellCompletionProvider CreateProvider() => new(CreateRootCommand(), InteractiveShell.Builtins);

    private static List<string> GetCompletionLabels(string text) =>
        CreateProvider().GetCompletions(text, text.Length).Items.Select(i => i.Label).ToList();

    private static LineEditor CreateEditor(out FakeShellConsole console, ShellHistory? history = null)
    {
        var rootCommand = CreateRootCommand();
        var provider = new ShellCompletionProvider(rootCommand, InteractiveShell.Builtins);
        console = new FakeShellConsole();
        return new LineEditor(
            console,
            history ?? new ShellHistory(),
            provider.GetCompletions,
            text => CommandLineAnalyzer.Classify(rootCommand, BuiltinNames, ShellTokenizer.Tokenize(text)));
    }

    private static ShellReadResult ReadLine(params ConsoleKeyInfo[][] keys) => ReadLine(new ShellHistory(), keys);

    private static ShellReadResult ReadLine(ShellHistory history, params ConsoleKeyInfo[][] keys)
    {
        var editor = CreateEditor(out var console, history);
        console.Enqueue(keys);
        return editor.ReadLine(Prompt);
    }

    private static class Keys
    {
        public static ConsoleKeyInfo[] Type(string text) =>
            text.Select(c => new ConsoleKeyInfo(
                c,
                c == ' ' ? ConsoleKey.Spacebar : char.IsLetter(c) ? Enum.Parse<ConsoleKey>(char.ToUpperInvariant(c).ToString()) : ConsoleKey.Oem1,
                char.IsUpper(c),
                false,
                false)).ToArray();

        public static ConsoleKeyInfo[] Press(ConsoleKey key, bool control = false, bool shift = false)
        {
            var character = key switch
            {
                ConsoleKey.Enter => '\r',
                ConsoleKey.Tab => '\t',
                ConsoleKey.Escape => '\u001b',
                ConsoleKey.Backspace => '\b',
                _ when control && key is >= ConsoleKey.A and <= ConsoleKey.Z => (char)(key - ConsoleKey.A + 1),
                _ => '\0'
            };

            return [new ConsoleKeyInfo(character, key, shift, false, control)];
        }
    }

    private sealed class FakeShellConsole : IShellConsole
    {
        private readonly Queue<ConsoleKeyInfo> _keys = new();
        private readonly System.Text.StringBuilder _output = new();

        public string Output => _output.ToString();

        public bool WasControlCTreatedAsInput { get; private set; }

        public bool KeyAvailable => false;

        public int WindowWidth => 80;

        public int CursorLeft => 0;

        public bool TreatControlCAsInput
        {
            get => field;
            set
            {
                field = value;
                WasControlCTreatedAsInput |= value;
            }
        }

        public void Enqueue(params ConsoleKeyInfo[][] keys)
        {
            foreach (var key in keys.SelectMany(k => k))
            {
                _keys.Enqueue(key);
            }
        }

        public ConsoleKeyInfo ReadKey() =>
            _keys.TryDequeue(out var key) ? key : throw new InvalidOperationException("The test ran out of keys.");

        public void Write(string text) => _output.Append(text);
    }

    #endregion
}
