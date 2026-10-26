using System.Text;

namespace LoomEditor.Gallery.Lsp;

/// <summary>A language server the Gallery can start.</summary>
/// <param name="FileName">The executable.</param>
/// <param name="Arguments">Its arguments.</param>
/// <param name="LanguageId">The language id of the demo document.</param>
/// <param name="Description">Shown in the status line.</param>
internal sealed record LspServerChoice(string FileName, IReadOnlyList<string> Arguments, string LanguageId, string Description);

/// <summary>Finds the language server to start: the <c>LOOMEDITOR_LSP_COMMAND</c> variable, then <c>csharp-ls</c> (ADR 0011 decision 9).</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class LspServerLocator
{
    private const string CommandVariable = "LOOMEDITOR_LSP_COMMAND";
    private const string LanguageVariable = "LOOMEDITOR_LSP_LANGUAGE";
    private const string CsharpLsDescription = "csharp-ls (dotnet tool)";

    /// <summary>The command that installs the default server.</summary>
    internal static string InstallHint => "dotnet tool install --global csharp-ls";

    private static string CsharpLsFile => OperatingSystem.IsWindows() ? "csharp-ls.exe" : "csharp-ls";

    /// <summary>Finds a server with the real environment, file system and <c>PATH</c>.</summary>
    internal static LspServerChoice? Find() => Find(
        Environment.GetEnvironmentVariable,
        File.Exists,
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Finds a server; never throws.</summary>
    internal static LspServerChoice? Find(
        Func<string, string?> getEnvironmentVariable,
        Func<string, bool> fileExists,
        string userProfileFolder,
        IEnumerable<string> pathFolders)
    {
        string? command = getEnvironmentVariable(CommandVariable);
        if (!string.IsNullOrWhiteSpace(command))
        {
            List<string> tokens = SplitCommandLine(command);
            if (tokens.Count > 0)
            {
                string language = getEnvironmentVariable(LanguageVariable) is { Length: > 0 } value ? value : "csharp";
                return new LspServerChoice(tokens[0], tokens.GetRange(1, tokens.Count - 1), language, CommandVariable);
            }
        }

        string? tool = TryCombine(fileExists, userProfileFolder, ".dotnet", "tools", CsharpLsFile);
        if (tool is not null)
        {
            return new LspServerChoice(tool, [], "csharp", CsharpLsDescription);
        }

        foreach (string folder in pathFolders)
        {
            string? found = TryCombine(fileExists, folder, CsharpLsFile);
            if (found is not null)
            {
                return new LspServerChoice(found, [], "csharp", CsharpLsDescription);
            }
        }

        return null;
    }

    /// <summary>Splits on spaces; double quotes group a token with spaces and are removed; no escapes.</summary>
    internal static List<string> SplitCommandLine(string commandLine)
    {
        List<string> tokens = [];
        StringBuilder current = new();
        bool inQuotes = false;
        bool hasToken = false;
        foreach (char c in commandLine)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                hasToken = true;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (hasToken)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
            }
            else
            {
                current.Append(c);
                hasToken = true;
            }
        }

        if (hasToken)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    // An invalid path (an unusable PATH entry) must not break the search.
    private static string? TryCombine(Func<string, bool> fileExists, params string[] parts)
    {
        try
        {
            string path = Path.Combine(parts);
            return fileExists(path) ? path : null;
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }
}
