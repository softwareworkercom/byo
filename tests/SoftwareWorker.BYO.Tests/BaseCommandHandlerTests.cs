using SoftwareWorker.BYO.SDK;
using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK.Model.Enums;
using SoftwareWorker.BYO.Tests.Support;

namespace SoftwareWorker.BYO.Tests;

public sealed class BaseCommandHandlerTests
{
    [Fact]
    public void BindParameters_ShouldConvertStringValuesToPropertyTypes_CaseInsensitively()
    {
        var handler = new SampleHandler();

        handler.BindParameters(new Dictionary<string, object>
        {
            ["NAME"] = "deploy",
            ["count"] = "12",
            ["enabled"] = "true",
            ["ratio"] = "0.5",
            ["amount"] = "19.99",
            ["big"] = "9000000000",
            ["shell"] = "cmd",
            ["optionalCount"] = "7"
        });

        Assert.Equal("deploy", handler.Name);
        Assert.Equal(12, handler.Count);
        Assert.True(handler.Enabled);
        Assert.Equal(0.5, handler.Ratio);
        Assert.Equal(19.99m, handler.Amount);
        Assert.Equal(9_000_000_000L, handler.Big);
        Assert.Equal(ShellTypeEnum.Cmd, handler.Shell);
        Assert.Equal(7, handler.OptionalCount);
    }

    [Fact]
    public void BindParameters_ShouldFallBackToDefaults_WhenConversionFails()
    {
        var handler = new SampleHandler
        {
            Count = 5,
            Enabled = true,
            Ratio = 1.5,
            Amount = 2m,
            Big = 3,
            Shell = ShellTypeEnum.Wsl
        };

        handler.BindParameters(new Dictionary<string, object>
        {
            ["count"] = "twelve",
            ["enabled"] = "maybe",
            ["ratio"] = "abc",
            ["amount"] = "abc",
            ["big"] = "abc",
            ["shell"] = "nope"
        });

        Assert.Equal(0, handler.Count);
        Assert.False(handler.Enabled);
        Assert.Equal(0.0, handler.Ratio);
        Assert.Equal(0m, handler.Amount);
        Assert.Equal(0L, handler.Big);
        Assert.Equal(default, handler.Shell);
    }

    [Fact]
    public void BindParameters_ShouldKeepValuesOfTheTargetType_AndConvertOtherTypes()
    {
        var handler = new SampleHandler();

        handler.BindParameters(new Dictionary<string, object>
        {
            ["count"] = 42,
            ["big"] = 7,
            ["ratio"] = 2,
            ["name"] = 123
        });

        Assert.Equal(42, handler.Count);
        Assert.Equal(7L, handler.Big);
        Assert.Equal(2.0, handler.Ratio);
        Assert.Equal("123", handler.Name);
    }

    [Fact]
    public void BindParameters_ShouldSkipReadOnlyAndUnknownProperties_AndUnconvertibleValues()
    {
        var handler = new SampleHandler();

        handler.BindParameters(new Dictionary<string, object>
        {
            ["readonly"] = "changed",
            ["missingProperty"] = "ignored",
            ["count"] = new object()
        });

        Assert.Equal("fixed", handler.ReadOnly);
        Assert.Equal(0, handler.Count);
    }

    [Fact]
    public void SetDynamicParameters_ShouldStoreValues_AndFallBackToAnEmptySet()
    {
        var handler = new SampleHandler();

        handler.SetDynamicParameters(new Dictionary<string, string> { ["extra"] = "1" });
        Assert.Equal("1", handler.DynamicParameters["extra"]);

        handler.SetDynamicParameters(null!);
        Assert.Empty(handler.DynamicParameters);
    }

    [Fact]
    public void EnsureParameters_ShouldReportMissingRequiredParameters_WhenNotInteractive()
    {
        using var console = new ConsoleScope();
        var options = new Dictionary<string, object> { ["count"] = "1" };

        var (returnedOptions, error) = BaseCommandHandler.EnsureParameters(typeof(SampleHandler), options);

        Assert.Equal("Missing required parameter(s): --name.", error);
        Assert.Same(options, returnedOptions);
    }

    [Fact]
    public void EnsureParameters_ShouldTreatEmptyValuesAsMissing_AndAcceptProvidedOnes()
    {
        using var console = new ConsoleScope();

        var (_, missing) = BaseCommandHandler.EnsureParameters(typeof(SampleHandler), new Dictionary<string, object> { ["Name"] = "" });
        var (_, provided) = BaseCommandHandler.EnsureParameters(typeof(SampleHandler), new Dictionary<string, object> { ["Name"] = "value" });

        Assert.NotNull(missing);
        Assert.Null(provided);
    }

    [Parameter("name", "Name", true, null)]
    [Parameter("count", "Count", false, null)]
    [Parameter("enabled", "Enabled", false, null)]
    [Parameter("ratio", "Ratio", false, null)]
    [Parameter("amount", "Amount", false, null)]
    [Parameter("big", "Big", false, null)]
    [Parameter("shell", "Shell", false, null)]
    [Parameter("optionalCount", "Optional count", false, null)]
    [Parameter("readonly", "Read only", false, null)]
    [Parameter("missingProperty", "Has no property", false, null)]
    private sealed class SampleHandler : BaseCommandHandler
    {
        public string? Name { get; set; }

        public int Count { get; set; }

        public bool Enabled { get; set; }

        public double Ratio { get; set; }

        public decimal Amount { get; set; }

        public long Big { get; set; }

        public ShellTypeEnum Shell { get; set; }

        public int? OptionalCount { get; set; }

        public string ReadOnly { get; } = "fixed";

        public override Task ExecuteAsync() => Task.CompletedTask;
    }
}
