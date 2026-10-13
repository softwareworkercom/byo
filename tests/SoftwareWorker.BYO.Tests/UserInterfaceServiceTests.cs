using SoftwareWorker.BYO.SDK.Model.Enums;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;
using Spectre.Console;
using System.Data;

namespace SoftwareWorker.BYO.Tests;

public sealed class UserInterfaceServiceTests
{
    #region Output

    [Fact]
    public void ShowMethods_ShouldWriteTheMessageText_EscapingMarkup()
    {
        using var console = new ConsoleScope();

        UserInterfaceService.ShowRed("red [x]");
        UserInterfaceService.ShowError("error");
        UserInterfaceService.ShowYellow("yellow");
        UserInterfaceService.ShowWarning("warning");
        UserInterfaceService.ShowBlue("blue");
        UserInterfaceService.ShowInformation("info");
        UserInterfaceService.ShowComment("comment");
        UserInterfaceService.ShowGreen("green");
        UserInterfaceService.ShowSuccess("success");
        UserInterfaceService.ShowCyan("cyan");
        UserInterfaceService.ShowGrey("grey");
        UserInterfaceService.ShowColoredMessage("colored", "magenta");
        UserInterfaceService.ShowMarkup("[bold]markup[/]");
        UserInterfaceService.WriteLine();
        UserInterfaceService.WriteLine("plain");

        Assert.Equal(
            ["red [x]", "error", "yellow", "warning", "blue", "info", "comment", "green", "success", "cyan", "grey", "colored", "markup", "", "plain"],
            console.Terminal.Lines);
    }

    [Fact]
    public void ShowRule_Panel_Renderable_AndTable_ShouldRender()
    {
        using var console = new ConsoleScope();

        UserInterfaceService.ShowRule("Section");
        UserInterfaceService.ShowPanel(new Panel("inside"));
        UserInterfaceService.ShowRenderable(new Markup("renderable"));
        UserInterfaceService.ShowTable(new Table().AddColumn("Col").AddRow("cell"));

        Assert.Contains("Section", console.Output);
        Assert.Contains("inside", console.Output);
        Assert.Contains("renderable", console.Output);
        Assert.Contains("cell", console.Output);
    }

    [Fact]
    public void ShowTable_WithDataTable_ShouldDisambiguateColumnNames_AndSanitizeControlCharacters()
    {
        using var console = new ConsoleScope();
        var dataTable = new DataTable { CaseSensitive = true };
        dataTable.Columns.Add("Name");
        dataTable.Columns.Add("name");
        dataTable.Rows.Add("first\tvalue", "second");
        dataTable.Rows.Add(DBNull.Value, "[markup]");

        UserInterfaceService.ShowTable(dataTable);

        Assert.Contains("name (duplicate)", console.Output);
        Assert.Contains("first value", console.Output);
        Assert.Contains("[markup]", console.Output);
    }

    [Theory]
    [InlineData(null, 5, "")]
    [InlineData("", 5, "")]
    [InlineData("abc", 5, "abc")]
    [InlineData("abcdefgh", 5, "ab...")]
    [InlineData("abcdefgh", 3, "abc")]
    [InlineData("abcdefgh", 0, "")]
    [InlineData("a\r\nb\nc\rd", 10, "a b c d")]
    public void TruncateWithEllipsis_ShouldFlattenLineBreaksAndTruncate(string? text, int maxLength, string expected)
    {
        Assert.Equal(expected, UserInterfaceService.TruncateWithEllipsis(text, maxLength));
    }

    [Fact]
    public void SingleLineStatus_ShouldOverwriteTheLineAndCompleteOnce()
    {
        using var console = new ConsoleScope();

        UserInterfaceService.UpdateSingleLineStatus("working...");
        UserInterfaceService.UpdateSingleLineStatus("done");
        UserInterfaceService.CompleteSingleLineStatus();
        UserInterfaceService.CompleteSingleLineStatus();

        Assert.Equal("\rworking...\rdone      " + Environment.NewLine, console.SystemOutput);
    }

    [Fact]
    public void Clear_Width_AndInteractivity_ShouldReflectTheConsole()
    {
        using var console = new ConsoleScope(width: 100);

        UserInterfaceService.Clear();

        Assert.Equal(100, UserInterfaceService.GetConsoleWidth());
        Assert.False(UserInterfaceService.IsInteractive);
    }

    [Fact]
    public void WaitForEnter_ShouldShowTheMessageAndReadALine()
    {
        using var console = new ConsoleScope(standardInput: Environment.NewLine);

        UserInterfaceService.WaitForEnter("Go on");

        Assert.Contains("Go on", console.Output);
    }

    #endregion

    #region Progress and status

    [Fact]
    public void ExecuteWithProgress_ShouldRunTheAction()
    {
        using var console = new ConsoleScope();
        var completed = 0;

        UserInterfaceService.ExecuteWithProgress(ctx =>
        {
            var task = ctx.AddTask("Working", maxValue: 2);
            task.Increment(2);
            completed = 2;
        });

        Assert.Equal(2, completed);
    }

    [Fact]
    public async Task ExecuteWithProgressAsync_ShouldRunTheAction()
    {
        using var console = new ConsoleScope();
        var completed = false;

        await UserInterfaceService.ExecuteWithProgressAsync(async ctx =>
        {
            ctx.AddTask("Working").Increment(1);
            await Task.Yield();
            completed = true;
        });

        Assert.True(completed);
    }

    [Fact]
    public async Task ExecuteWithProgressAsync_OverItems_ShouldCollectResultsAndReportFailures()
    {
        using var console = new ConsoleScope();

        var results = await UserInterfaceService.ExecuteWithProgressAsync(
            [1, 2, 3],
            item => item == 2 ? throw new InvalidOperationException("boom") : Task.FromResult($"item-{item}"),
            "Processing");

        Assert.Equal("item-1", results[1]);
        Assert.Null(results[2]);
        Assert.Equal("item-3", results[3]);
        Assert.Contains("Error processing 2: boom", console.Output);
    }

    [Fact]
    public async Task ExecuteWithStatusAsync_ShouldRunTheAction()
    {
        using var console = new ConsoleScope();
        var ran = false;

        await UserInterfaceService.ExecuteWithStatusAsync("Loading", _ =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        Assert.True(ran);
    }

    #endregion

    #region Prompts

    [Fact]
    public void AskYesNo_ShouldMapTheSelectionToABoolean()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);

        Assert.True(UserInterfaceService.AskYesNo("Continue?"));
        Assert.False(UserInterfaceService.AskYesNo("Continue?"));
    }

    [Fact]
    public void AskInput_ShouldIgnoreEmptyInput_UntilTextIsEntered()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("value");

        var input = UserInterfaceService.AskInput("Name");

        Assert.Equal("value", input);
        Assert.Contains("Name:", console.Output);
    }

    [Fact]
    public void AskInput_ShouldAllowEmptyInput_WhenRequested()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.Enter);

        Assert.Equal(string.Empty, UserInterfaceService.AskInput("Name", allowEmpty: true));
    }

    [Fact]
    public void AskInput_WithDefault_ShouldReturnTheDefaultOrTheTypedValue()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("other");

        Assert.Equal("fallback", UserInterfaceService.AskInput("Name", "fallback"));
        Assert.Equal("other", UserInterfaceService.AskInput("Name", "fallback"));
    }

    [Fact]
    public void AskSecret_ShouldReturnTheInputWithoutEchoingIt()
    {
        using var console = new ConsoleScope(interactive: true);
        console.Input.PushTextWithEnter("s3cret-value");

        var secret = UserInterfaceService.AskSecret("Token");

        Assert.Equal("s3cret-value", secret);
        Assert.DoesNotContain("s3cret-value", console.Output);
    }

    [Fact]
    public void Confirm_ShouldUseTheDefault_OrTheTypedAnswer()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("n");
        console.PushKeys(ConsoleKey.Enter);

        Assert.True(UserInterfaceService.Confirm("Proceed?"));
        Assert.False(UserInterfaceService.Confirm("Proceed?"));
        Assert.False(UserInterfaceService.Confirm("Proceed?", defaultValue: false));
    }

    [Fact]
    public void Ask_ShouldConvertTheInput_AndFallBackToTheDefault()
    {
        using var console = new ConsoleScope(interactive: true);
        console.Input.PushTextWithEnter("42");
        console.PushKeys(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("typed");

        Assert.Equal(42, UserInterfaceService.Ask<int>("Number"));
        Assert.Equal(7, UserInterfaceService.Ask("Number", 7));
        Assert.Equal("typed", UserInterfaceService.Prompt(new TextPrompt<string>("Text")));
    }

    [Fact]
    public void SelectSingleItem_ShouldReturnTheHighlightedItem()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);

        Assert.Equal("b", UserInterfaceService.SelectSingleItem("letter", ["a", "b", "c"]));
        Assert.Equal(3, UserInterfaceService.SelectSingleItem("number", [1, 2, 3], number => $"n{number}"));
        Assert.Contains("Select letter:", console.Output);
    }

    [Fact]
    public void SelectMultipleItems_ShouldReturnAllItems_WhenNothingIsSelected()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.Enter);

        Assert.Equal(["a", "b", "c"], UserInterfaceService.SelectMultipleItems("letters", ["a", "b", "c"]));
    }

    [Fact]
    public void SelectMultipleItems_ShouldReturnTheToggledItems()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.Spacebar, ConsoleKey.DownArrow, ConsoleKey.Spacebar, ConsoleKey.Enter);

        Assert.Equal(["a", "b"], UserInterfaceService.SelectMultipleItems("letters", ["a", "b", "c"]));
    }

    [Fact]
    public void SelectBoolean_AndSelectEnum_ShouldReturnTheHighlightedValue()
    {
        using var console = new ConsoleScope(interactive: true);
        // Spectre highlights the prompt's default value first, and for a bool prompt that is "false".
        console.PushKeys(ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.UpArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.Enter);

        Assert.False(UserInterfaceService.SelectBoolean("Enabled?"));
        Assert.True(UserInterfaceService.SelectBoolean("Enabled?"));
        Assert.Equal(ShellTypeEnum.Cmd, UserInterfaceService.SelectEnum<ShellTypeEnum>());
        Assert.Equal(MessageColorEnum.Red, UserInterfaceService.SelectEnum<MessageColorEnum>("Pick a color"));
        Assert.Contains("Select ShellTypeEnum:", console.Output);
        Assert.Contains("Pick a color:", console.Output);
    }

    [Fact]
    public void SelectMonth_ShouldReturnTheHighlightedMonth()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        var expected = DateTime.Now.AddMonths(-1);

        var selected = UserInterfaceService.SelectMonth(3);

        Assert.Equal((expected.Year, expected.Month), (selected.Year, selected.Month));
    }

    [Fact]
    public void SelectDateTimeRangeFromNow_ShouldSubtractTheChosenRange()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
        var before = DateTime.Now;

        var start = UserInterfaceService.SelectDateTimeRangeFromNow();

        Assert.InRange(start, before.AddHours(-1).AddSeconds(-5), DateTime.Now.AddHours(-1).AddSeconds(5));
    }

    [Fact]
    public void SelectTimeframe_ShouldDefaultToThePastWeek()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.Enter);
        var before = DateTime.Now;

        var (from, to) = UserInterfaceService.SelectTimeframe();

        Assert.InRange(to, before, DateTime.Now.AddSeconds(5));
        Assert.Equal(7, Math.Round((to - from).TotalDays));
    }

    [Fact]
    public void SelectTime_ShouldValidateHourAndMinute()
    {
        using var console = new ConsoleScope(interactive: true);
        console.Input.PushTextWithEnter("25");
        console.Input.PushTextWithEnter("9");
        console.Input.PushTextWithEnter("61");
        console.Input.PushTextWithEnter("5");

        var time = UserInterfaceService.SelectTime();

        Assert.Equal("09:05", time);
        Assert.Contains("Hour must be between 0 and 23", console.Output);
        Assert.Contains("Minute must be between 0 and 59", console.Output);
    }

    [Fact]
    public void SelectPath_ShouldAcceptExistingFolders_AndRejectOthers()
    {
        using var console = new ConsoleScope(interactive: true);
        var directory = Directory.CreateTempSubdirectory("byo-ui-").FullName;
        try
        {
            console.Input.PushTextWithEnter(directory);
            console.Input.PushTextWithEnter(Path.Combine(directory, "missing"));

            Assert.Equal(directory, UserInterfaceService.SelectPath());
            Assert.Null(UserInterfaceService.SelectPath());
            Assert.Contains("Invalid folder location.", console.Output);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SelectFolder_ShouldReturnTheCurrentFolder_NavigateIntoChildren_AndCancel()
    {
        using var console = new ConsoleScope(interactive: true);
        var root = Directory.CreateTempSubdirectory("byo-ui-").FullName;
        var child = Path.Combine(root, "alpha");
        Directory.CreateDirectory(child);
        try
        {
            // Choices: "..", ". (Select Current Folder)", "alpha", "--- Enter path manually ---", "--- Cancel ---"
            console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
            Assert.Equal(root, UserInterfaceService.SelectFolder(root, "Pick a folder"));
            Assert.Contains("Pick a folder", console.Output);

            console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter);
            Assert.Equal(child, UserInterfaceService.SelectFolder(root));

            console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
            Assert.Null(UserInterfaceService.SelectFolder(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SelectFolder_ShouldAcceptAManuallyEnteredPath_AndRetryAfterAnInvalidOne()
    {
        using var console = new ConsoleScope(interactive: true, standardInput: Environment.NewLine);
        var root = Directory.CreateTempSubdirectory("byo-ui-").FullName;
        var child = Path.Combine(root, "alpha");
        Directory.CreateDirectory(child);
        try
        {
            console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
            console.Input.PushTextWithEnter(child);
            Assert.Equal(child, UserInterfaceService.SelectFolder(root));

            console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
            console.Input.PushTextWithEnter(Path.Combine(root, "missing"));
            console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
            Assert.Null(UserInterfaceService.SelectFolder(root));
            Assert.Contains("Invalid folder path.", console.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    #endregion
}
