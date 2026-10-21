using System.Text.RegularExpressions;

namespace LoomEditor.View.Tests.Features.Find.TestHelpers;

internal static class RegexMatches
{
    /// <summary>The JavaScript <c>regex.exec(target)</c>: the groups of the first match (<c>null</c> for a group that did not match), or <c>null</c>.</summary>
    public static string?[]? Exec(string target, string pattern, RegexOptions options = RegexOptions.None)
    {
        Match m = new Regex(pattern, options | RegexOptions.CultureInvariant).Match(target);
        if (!m.Success)
        {
            return null;
        }

        return [.. m.Groups.Cast<Group>().Select(g => g.Success ? g.Value : null)];
    }
}
