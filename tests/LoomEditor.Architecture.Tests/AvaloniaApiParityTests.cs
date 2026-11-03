namespace LoomEditor.Architecture.Tests;

/// <summary>
/// The public API of LoomEditor.Avalonia must grow to match LoomEditor.WinUI's (phase 15, decision 4).
/// <c>AvaloniaParityPending.txt</c> lists the WinUI members that Avalonia does not have yet; a card that adds a member
/// removes its line. <c>AvaloniaParityExceptions.txt</c> lists key prefixes that never get an Avalonia twin.
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
    public void MissingMembers_MatchThePendingList()
    {
        HashSet<string> winUi = ReadKeys("LoomEditor.WinUI");
        HashSet<string> avalonia = ReadKeys("LoomEditor.Avalonia");
        string[] exceptions = ReadList("AvaloniaParityExceptions.txt");
        string[] pending = ReadList("AvaloniaParityPending.txt");

        string[] missing = winUi
            .Where(key => !avalonia.Contains(key) && !exceptions.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] toAdd = missing.Except(pending).ToArray();
        string[] toRemove = pending.Except(missing).ToArray();
        Assert.True(toAdd.Length == 0 && toRemove.Length == 0,
            "AvaloniaParityPending.txt is out of date."
            + (toAdd.Length > 0 ? $"{Environment.NewLine}Add:{Environment.NewLine}{string.Join(Environment.NewLine, toAdd)}" : string.Empty)
            + (toRemove.Length > 0 ? $"{Environment.NewLine}Remove:{Environment.NewLine}{string.Join(Environment.NewLine, toRemove)}" : string.Empty));
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
