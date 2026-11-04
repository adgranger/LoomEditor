namespace LoomEditor.Architecture.Tests;

/// <summary>
/// The public API of LoomEditor.Avalonia matches LoomEditor.WinUI's (phase 15, decision 4): every WinUI member has an Avalonia twin.
/// <c>AvaloniaParityExceptions.txt</c> lists the key prefixes of WinUI members that never get one.
/// </summary>
public class AvaloniaApiParityTests
{
    private static readonly string[] ApiFiles = ["PublicAPI.Shipped.txt", "PublicAPI.Unshipped.txt"];

    private static readonly string[] Modifiers = ["static ", "readonly ", "const ", "override ", "abstract ", "virtual ", "~", "*REMOVED*"];

    [Theory]
    [InlineData("LoomEditor.WinUI.Controls.LoomEditor.SetHiddenAreas(System.Collections.Generic.IReadOnlyList<LoomEditor.Core.Text.Range>! ranges) -> void",
        "LoomEditor.Avalonia.Controls.LoomEditor.SetHiddenAreas")]
    [InlineData("static readonly LoomEditor.WinUI.Controls.LoomEditor.FontSizeProperty -> Microsoft.UI.Xaml.DependencyProperty!",
        "LoomEditor.Avalonia.Controls.LoomEditor.FontSizeProperty")]
    [InlineData("LoomEditor.WinUI.Controls.LoomEditor.SelectionChanged -> System.EventHandler<LoomEditor.WinUI.Controls.X!>?",
        "LoomEditor.Avalonia.Controls.LoomEditor.SelectionChanged")]
    public void ParityKey_ReducesASignature(string line, string expected)
    {
        Assert.Equal(expected, ParityKey(line));
    }

    [Fact]
    public void MissingMembers_AreOnlyTheExceptions()
    {
        HashSet<string> winUi = ReadKeys("LoomEditor.WinUI");
        HashSet<string> avalonia = ReadKeys("LoomEditor.Avalonia");
        string[] exceptions = ReadList("AvaloniaParityExceptions.txt");

        string[] missing = winUi
            .Where(key => !avalonia.Contains(key) && !exceptions.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(missing.Length == 0,
            $"LoomEditor.Avalonia lacks these LoomEditor.WinUI public members (add them, or add a prefix to AvaloniaParityExceptions.txt with the reason):{Environment.NewLine}"
            + string.Join(Environment.NewLine, missing));
    }

    /// <summary>Reduces a PublicAPI line to a key shared by both backends, or null for a comment or an empty line.</summary>
    internal static string? ParityKey(string line)
    {
        string text = line.Trim();
        if (text.Length == 0 || text.StartsWith('#'))
        {
            return null;
        }

        bool stripped;
        do
        {
            stripped = false;
            foreach (string modifier in Modifiers)
            {
                if (text.StartsWith(modifier, StringComparison.Ordinal))
                {
                    text = text[modifier.Length..];
                    stripped = true;
                }
            }
        }
        while (stripped);

        int arrow = text.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrow >= 0)
        {
            text = text[..arrow];
        }

        int parenthesis = text.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis >= 0)
        {
            text = text[..parenthesis];
        }

        return text
            .Replace("LoomEditor.WinUI.Controls.", "LoomEditor.Avalonia.Controls.", StringComparison.Ordinal)
            .Replace("LoomEditor.WinUI.", "LoomEditor.Avalonia.", StringComparison.Ordinal);
    }

    private static HashSet<string> ReadKeys(string project)
    {
        string folder = Path.Combine(RepositoryLayout.Source.FullName, project);
        return ApiFiles
            .SelectMany(file => File.ReadAllLines(Path.Combine(folder, file)))
            .Select(ParityKey)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string[] ReadList(string file) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, file))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToArray();
}
