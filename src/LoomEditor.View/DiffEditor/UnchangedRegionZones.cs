// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/features/hideUnchangedRegionsFeature.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.DiffEditor;

/// <summary>The data of a <see cref="ViewZoneKind.Collapsed"/> zone: the bar of one hidden unchanged region.</summary>
/// <param name="Region">The region the bar stands for (its visible counts change while the bar lives).</param>
/// <param name="IsOriginalSide">Whether the bar is in the original editor.</param>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed record CollapsedZoneData(UnchangedRegion Region, bool IsOriginalSide);

/// <summary>What the "hide unchanged regions" mode puts into the two editors.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record UnchangedRegionLayout(
    IReadOnlyList<ZoneSpec> OriginalZones,
    IReadOnlyList<ZoneSpec> ModifiedZones,
    IReadOnlyList<Range> OriginalHiddenAreas,
    IReadOnlyList<Range> ModifiedHiddenAreas,
    IReadOnlyList<ModelDeltaDecoration> OriginalDecorations,
    IReadOnlyList<ModelDeltaDecoration> ModifiedDecorations)
{
    /// <summary>Nothing hidden, no bar.</summary>
    public static UnchangedRegionLayout Empty { get; } = new([], [], [], [], [], []);
}

/// <summary>
/// Turns the unchanged regions into bars, hidden areas and decorations (Monaco: the autoruns of
/// <c>HideUnchangedRegionsFeature</c>). Not ported: compact mode, the glyph margin fold icon.
/// </summary>
/// <remarks>Threading: pure; stateless.</remarks>
internal static class UnchangedRegionZones
{
    /// <summary>The height of a bar (Monaco: <c>PlaceholderViewZone(d, 24)</c>).</summary>
    public const int BarHeight = 24;

    private static readonly ModelDecorationOptions UnchangedLines = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "diff-unchanged-lines",
        ClassName = "diff-unchanged-lines",
        IsWholeLine = true,
    });

    /// <summary>Computes the layout of <paramref name="regions"/>.</summary>
    public static UnchangedRegionLayout Compute(IReadOnlyList<UnchangedRegion> regions, int modifiedLineCount)
    {
        // A single region over the whole file means the two texts are equal: nothing to collapse.
        if (regions.Count == 1 && regions[0].ModifiedLineNumber == 1 && regions[0].LineCount == modifiedLineCount)
        {
            return UnchangedRegionLayout.Empty;
        }

        List<ZoneSpec> originalZones = [];
        List<ZoneSpec> modifiedZones = [];
        List<Range> originalHidden = [];
        List<Range> modifiedHidden = [];
        List<ModelDeltaDecoration> originalDecorations = [];
        List<ModelDeltaDecoration> modifiedDecorations = [];

        foreach (UnchangedRegion region in regions)
        {
            LineRange hiddenOriginal = region.GetHiddenOriginalRange();
            LineRange hiddenModified = region.GetHiddenModifiedRange();
            if (!region.ShouldHideControls)
            {
                originalZones.Add(Zone(hiddenOriginal, new CollapsedZoneData(region, true)));
                modifiedZones.Add(Zone(hiddenModified, new CollapsedZoneData(region, false)));
            }

            if (hiddenOriginal.ToInclusiveRange() is { } o)
            {
                originalHidden.Add(o);
            }

            if (hiddenModified.ToInclusiveRange() is { } m)
            {
                modifiedHidden.Add(m);
            }

            if (region.OriginalUnchangedRange.ToInclusiveRange() is { } ou)
            {
                originalDecorations.Add(new ModelDeltaDecoration(ou, UnchangedLines));
            }

            if (region.ModifiedUnchangedRange.ToInclusiveRange() is { } mu)
            {
                modifiedDecorations.Add(new ModelDeltaDecoration(mu, UnchangedLines));
            }
        }

        return new UnchangedRegionLayout(originalZones, modifiedZones, originalHidden, modifiedHidden, originalDecorations, modifiedDecorations);
    }

    private static ZoneSpec Zone(LineRange hidden, CollapsedZoneData data) =>
        new(ViewZoneKind.Collapsed, hidden.StartLineNumber - 1, BarHeight, data, true);
}
