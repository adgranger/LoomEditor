namespace LoomEditor.Lsp.FakeServer;

/// <summary>How the fake language server behaves; see README.md for the command line form of each option.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
public sealed record FakeServerOptions
{
    /// <summary>The text document sync kind announced to the client: <c>none</c>, <c>full</c> or <c>incremental</c>.</summary>
    public string Sync { get; init; } = "incremental";

    /// <summary>Milliseconds to wait before answering completion, resolve, hover and definition requests.</summary>
    public int DelayMs { get; init; }

    /// <summary>The method that makes the server exit with code 3 immediately, without an answer.</summary>
    public string? ExitOn { get; init; }

    /// <summary>A line written to standard error at start.</summary>
    public string? Stderr { get; init; }

    /// <summary>Completion lists are marked <c>isIncomplete</c>.</summary>
    public bool Incomplete { get; init; }

    /// <summary>After <c>initialized</c> the server sends notifications and requests of its own.</summary>
    public bool Chatty { get; init; }

    /// <summary>Reads <c>--sync=</c>, <c>--delay=</c>, <c>--exit-on=</c>, <c>--stderr=</c>, <c>--incomplete</c> and <c>--chatty</c>; ignores anything else.</summary>
    public static FakeServerOptions Parse(string[] args)
    {
        FakeServerOptions options = new();
        foreach (string arg in args)
        {
            if (arg.StartsWith("--sync=", StringComparison.Ordinal))
            {
                options = options with { Sync = arg["--sync=".Length..] };
            }
            else if (arg.StartsWith("--delay=", StringComparison.Ordinal) && int.TryParse(arg["--delay=".Length..], out int delay))
            {
                options = options with { DelayMs = delay };
            }
            else if (arg.StartsWith("--exit-on=", StringComparison.Ordinal))
            {
                options = options with { ExitOn = arg["--exit-on=".Length..] };
            }
            else if (arg.StartsWith("--stderr=", StringComparison.Ordinal))
            {
                options = options with { Stderr = arg["--stderr=".Length..] };
            }
            else if (arg == "--incomplete")
            {
                options = options with { Incomplete = true };
            }
            else if (arg == "--chatty")
            {
                options = options with { Chatty = true };
            }
        }

        return options;
    }
}
