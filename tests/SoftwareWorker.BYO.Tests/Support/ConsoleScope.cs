using Spectre.Console;
using Spectre.Console.Testing;

namespace SoftwareWorker.BYO.Tests.Support;

/// <summary>
/// Routes <see cref="AnsiConsole"/> and <see cref="System.Console"/> output to in-memory buffers for the lifetime
/// of a test, and lets the test push keys and text into the prompts the code under test shows.
/// </summary>
internal sealed class ConsoleScope : IDisposable
{
    private readonly IAnsiConsole _originalConsole;
    private readonly TextWriter _originalOut;
    private readonly TextReader _originalIn;
    private readonly StringWriter _systemOut = new();

    public ConsoleScope(bool interactive = false, int width = 120, string? standardInput = null)
    {
        _originalConsole = AnsiConsole.Console;
        _originalOut = System.Console.Out;
        _originalIn = System.Console.In;

        Terminal = new TestConsole().Width(width);
        if (interactive)
        {
            Terminal.Interactive();
        }

        AnsiConsole.Console = Terminal;
        System.Console.SetOut(_systemOut);
        if (standardInput != null)
        {
            System.Console.SetIn(new StringReader(standardInput));
        }
    }

    public TestConsole Terminal { get; }

    public TestConsoleInput Input => Terminal.Input;

    /// <summary>Everything written through Spectre.Console.</summary>
    public string Output => Terminal.Output;

    /// <summary>Everything written through System.Console.</summary>
    public string SystemOutput => _systemOut.ToString();

    public void PushKeys(params ConsoleKey[] keys)
    {
        foreach (var key in keys)
        {
            Input.PushKey(key);
        }
    }

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        System.Console.SetOut(_originalOut);
        System.Console.SetIn(_originalIn);
        _systemOut.Dispose();
    }
}
