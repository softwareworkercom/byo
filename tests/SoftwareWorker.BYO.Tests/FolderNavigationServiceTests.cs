using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;

namespace SoftwareWorker.BYO.Tests;

public sealed class FolderNavigationServiceTests
{
    private sealed record Item(string Name, string? Folder);

    private static readonly List<Item> Items =
    [
        new("A", null),
        new("C", "Ops"),
        new("B", "Ops/Deploy")
    ];

    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData(" /Ops/Deploy/ ", "Ops/Deploy")]
    [InlineData("Dev", "Dev")]
    public void NormalizePath_ShouldTrimWhitespaceAndSlashes(string? input, string expected)
    {
        Assert.Equal(expected, FolderNavigationService.NormalizePath(input));
    }

    [Fact]
    public void GetAllFolderPaths_ShouldIncludeAncestors_IgnoreCaseDuplicates_AndSort()
    {
        List<Item> items =
        [
            new("a", "Ops/Deploy/Prod"),
            new("b", "ops"),
            new("c", null),
            new("d", "/Dev/")
        ];

        var paths = FolderNavigationService.GetAllFolderPaths(items, item => item.Folder);

        Assert.Equal(["Dev", "Ops", "Ops/Deploy", "Ops/Deploy/Prod"], paths);
    }

    [Fact]
    public void NavigateAndSelect_ShouldReturnNull_WhenThereAreNoItems()
    {
        Assert.Null(FolderNavigationService.NavigateAndSelect(new List<Item>(), item => item.Folder, item => item.Name));
    }

    [Fact]
    public void NavigateAndSelect_ShouldDescendIntoFolders_AndReturnTheChosenItem()
    {
        using var console = new ConsoleScope(interactive: true);
        // Root: "/Ops/", "A"  ->  Ops: "Back", "/Deploy/", "C"
        console.PushKeys(ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);

        var selected = FolderNavigationService.NavigateAndSelect(Items, item => item.Folder, item => item.Name, "command");

        Assert.Equal("C", selected?.Name);
        Assert.Contains("command /Ops", console.Output);
    }

    [Fact]
    public void NavigateAndSelect_ShouldGoBackToTheParentFolder()
    {
        using var console = new ConsoleScope(interactive: true);
        // Root -> Ops -> Back -> Root: "/Ops/", "A"
        console.PushKeys(ConsoleKey.Enter, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter);

        var selected = FolderNavigationService.NavigateAndSelect(Items, item => item.Folder, item => item.Name);

        Assert.Equal("A", selected?.Name);
    }

    [Fact]
    public void NavigateAndSelectFolder_ShouldReturnNull_WhenThereAreNoFolders()
    {
        Assert.Null(FolderNavigationService.NavigateAndSelectFolder(null!));
        Assert.Null(FolderNavigationService.NavigateAndSelectFolder([]));
        Assert.Null(FolderNavigationService.NavigateAndSelectFolder([" ", null, "/"]));
    }

    [Fact]
    public void NavigateAndSelectFolder_ShouldReturnTheCurrentFolder_WhenSelectingTheBookmark()
    {
        using var console = new ConsoleScope(interactive: true);
        // Root: "/Dev/", "/Ops/"  ->  Ops: "Back", "/Deploy/", "Select this bookmark"
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);

        var selected = FolderNavigationService.NavigateAndSelectFolder(["Ops/Deploy", "ops", "Dev"]);

        Assert.Equal("Ops", selected);
    }

    [Fact]
    public void NavigateAndSelectFolder_ShouldDescendIntoNestedFolders()
    {
        using var console = new ConsoleScope(interactive: true);
        // Root -> Ops -> Deploy: "Back", "Select this bookmark"
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter);

        var selected = FolderNavigationService.NavigateAndSelectFolder(["Ops/Deploy", "Dev"], "bookmark");

        Assert.Equal("Ops/Deploy", selected);
        Assert.Contains("bookmark /Ops/Deploy", console.Output);
    }

    [Fact]
    public void NavigateAndSelectFolder_ShouldGoBackToTheParentFolder()
    {
        using var console = new ConsoleScope(interactive: true);
        // Root -> Ops -> Deploy -> Back (Ops) -> "Select this bookmark"
        console.PushKeys(
            ConsoleKey.DownArrow, ConsoleKey.Enter,
            ConsoleKey.DownArrow, ConsoleKey.Enter,
            ConsoleKey.Enter,
            ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);

        var selected = FolderNavigationService.NavigateAndSelectFolder(["Ops/Deploy", "Ops", "Dev"]);

        Assert.Equal("Ops", selected);
    }
}
