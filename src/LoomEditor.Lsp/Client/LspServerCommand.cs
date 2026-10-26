namespace LoomEditor.Lsp.Client;

/// <summary>How to start a language server process.</summary>
/// <param name="FileName">The executable.</param>
/// <param name="Arguments">The arguments, one list item each (never joined into a command line).</param>
/// <param name="WorkingDirectory">The working directory of the process; the current one when <c>null</c>.</param>
/// <param name="Environment">Variables added to the environment of the process.</param>
/// <remarks>Threading: immutable.</remarks>
public sealed record LspServerCommand(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null);
