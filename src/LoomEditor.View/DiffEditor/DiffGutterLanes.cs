namespace LoomEditor.View.DiffEditor;

/// <summary>
/// The glyph margin lanes of a side-by-side <c>LoomDiffEditor</c>, left to right: select, line action, primary block action, secondary block
/// action, each present only when used (phase 14 decision 1). A lane index is -1 when the lane is absent; lane <c>k</c> sits at
/// <c>GlyphMarginLeft + k × LineHeight</c>.
/// </summary>
/// <param name="Count">The number of lanes; 0 means no glyph margin.</param>
/// <param name="Select">The lane of the line checkboxes.</param>
/// <param name="LineAction">The lane of the per-line action button.</param>
/// <param name="PrimaryBlockAction">The lane of the primary block action buttons.</param>
/// <param name="SecondaryBlockAction">The lane of the secondary block action buttons.</param>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal readonly record struct DiffGutterLanes(int Count, int Select, int LineAction, int PrimaryBlockAction, int SecondaryBlockAction)
{
    /// <summary>The lanes in use for the options; none when not side by side.</summary>
    public static DiffGutterLanes Compute(
        bool sideBySide, bool lineSelection, bool lineAction, bool blockActions, bool primaryBlockAction, bool secondaryBlockAction)
    {
        if (!sideBySide)
        {
            return new(0, -1, -1, -1, -1);
        }

        int count = 0;
        int select = lineSelection ? count++ : -1;
        int lineActionLane = lineSelection && lineAction ? count++ : -1;
        int primary = blockActions && primaryBlockAction ? count++ : -1;
        int secondary = blockActions && secondaryBlockAction ? count++ : -1;
        return new(count, select, lineActionLane, primary, secondary);
    }
}
