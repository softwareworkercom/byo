using SoftwareWorker.BYO.SDK;
using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK.Services;

namespace SoftwareWorker.BYO.Tests.Support;

// These handlers deliberately carry no [TrunkCommand] attribute, so the command scanner never
// picks them up as real CLI commands; the command builder tests wire them in by type name.

[Parameter("name", "Name", true, null)]
[Parameter("mode", "Mode", false, "fast|slow")]
public sealed class EchoHandler : BaseCommandHandler
{
    public static EchoHandler? LastInstance { get; set; }

    public string? Name { get; set; }

    public string? Mode { get; set; }

    public override Task ExecuteAsync()
    {
        LastInstance = this;
        UserInterfaceService.WriteLine($"echo {Name} {Mode}");
        return Task.CompletedTask;
    }
}

public sealed class ThrowingHandler : BaseCommandHandler
{
    public override Task ExecuteAsync() => throw new InvalidOperationException("boom");
}

[AllowDynamicParameters]
public sealed class DynamicHandler : BaseCommandHandler
{
    public static IReadOnlyDictionary<string, string>? LastDynamicParameters { get; set; }

    public override Task ExecuteAsync()
    {
        LastDynamicParameters = DynamicParameters;
        UserInterfaceService.WriteLine("dynamic handler ran");
        return Task.CompletedTask;
    }
}

public sealed class NotAHandler
{
}
