// Ported from microsoft/vscode: src/vs/editor/contrib/gotoSymbol/browser/referencesModel.ts (ReferencesModel, reduced to one flat list)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.GotoDefinition;

/// <summary>
/// The definitions found for a position: sorted by uri then range, without duplicates.
/// Not ported: <c>FileReferences</c>, previews, <c>nextOrPreviousReference</c>, <c>clone</c>, <c>dispose</c>.
/// </summary>
/// <remarks>Threading: immutable after construction; thread-safe.</remarks>
internal sealed class DefinitionResults
{
    private readonly LocationLink? _providersFirst;

    /// <summary>Sorts and de-duplicates <paramref name="links"/>.</summary>
    public DefinitionResults(IReadOnlyList<LocationLink> links)
    {
        ArgumentNullException.ThrowIfNull(links);
        _providersFirst = links.Count > 0 ? links[0] : null;

        // OrderBy is stable, like Monaco's Array.prototype.sort.
        var kept = new List<LocationLink>();
        foreach (LocationLink link in links.OrderBy(l => l, Comparer<LocationLink>.Create(CompareReferences)))
        {
            if (kept.Count == 0 || CompareReferences(link, kept[^1]) != 0)
            {
                kept.Add(link);
            }
        }

        References = kept;
    }

    /// <summary>The kept links.</summary>
    public IReadOnlyList<LocationLink> References { get; }

    /// <summary>Whether there is no link.</summary>
    public bool IsEmpty => References.Count == 0;

    /// <summary>The link the first provider gave, else the first link; null when empty (Monaco: <c>firstReference</c>).</summary>
    public LocationLink? FirstReference => References.FirstOrDefault(l => ReferenceEquals(l, _providersFirst)) ?? (References.Count > 0 ? References[0] : null);

    /// <summary>The link closest to <paramref name="position"/> in <paramref name="uri"/> (Monaco: <c>nearestReference</c>).</summary>
    public LocationLink? NearestReference(string uri, Position position) =>
        References
            .OrderByDescending(l => CommonPrefixLength(l.Uri, uri))
            .ThenBy(l => Math.Abs(l.Range.StartLineNumber - position.LineNumber) * 100 + Math.Abs(l.Range.StartColumn - position.Column))
            .FirstOrDefault();

    /// <summary>The link of <paramref name="uri"/> whose range contains <paramref name="position"/> (Monaco: <c>referenceAt</c>).</summary>
    public LocationLink? ReferenceAt(string uri, Position position) =>
        References.FirstOrDefault(l => string.Equals(l.Uri, uri, StringComparison.Ordinal) && l.Range.Contains(position));

    /// <summary>The message announced for the results (Monaco: <c>ariaMessage</c>).</summary>
    /// <param name="currentDocumentUri">Unused: Monaco only uses it to label the file of the cursor.</param>
    public string AriaMessage(string? currentDocumentUri)
    {
        if (References.Count == 0)
        {
            return "No results found";
        }

        if (References.Count == 1)
        {
            return $"Found 1 symbol in {References[0].Uri}";
        }

        int files = References.Select(l => l.Uri).Distinct(StringComparer.Ordinal).Count();
        return files == 1
            ? string.Create(CultureInfo.InvariantCulture, $"Found {References.Count} symbols in {References[0].Uri}")
            : string.Create(CultureInfo.InvariantCulture, $"Found {References.Count} symbols in {files} files");
    }

    // Monaco: _compareReferences
    private static int CompareReferences(LocationLink a, LocationLink b)
    {
        int byUri = string.CompareOrdinal(a.Uri, b.Uri);
        return byUri != 0 ? byUri : Range.CompareUsingStarts(a.Range, b.Range);
    }

    // Monaco: commonPrefixLength
    private static int CommonPrefixLength(string a, string b)
    {
        int length = Math.Min(a.Length, b.Length);
        int i = 0;
        while (i < length && a[i] == b[i])
        {
            i++;
        }

        return i;
    }
}
