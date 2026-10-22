namespace LoomEditor.View.Features.Folding;

/// <summary>The ids of the folding commands (Monaco: the <c>id</c> of each <c>FoldingAction</c>).</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class FoldingCommands
{
    public const string Unfold = "editor.unfold";
    public const string UnfoldRecursively = "editor.unfoldRecursively";
    public const string Fold = "editor.fold";
    public const string ToggleFold = "editor.toggleFold";
    public const string FoldRecursively = "editor.foldRecursively";
    public const string ToggleFoldRecursively = "editor.toggleFoldRecursively";
    public const string FoldAllBlockComments = "editor.foldAllBlockComments";
    public const string FoldAllMarkerRegions = "editor.foldAllMarkerRegions";
    public const string UnfoldAllMarkerRegions = "editor.unfoldAllMarkerRegions";
    public const string FoldAllExcept = "editor.foldAllExcept";
    public const string UnfoldAllExcept = "editor.unfoldAllExcept";
    public const string FoldAll = "editor.foldAll";
    public const string UnfoldAll = "editor.unfoldAll";
    public const string GotoParentFold = "editor.gotoParentFold";
    public const string GotoPreviousFold = "editor.gotoPreviousFold";
    public const string GotoNextFold = "editor.gotoNextFold";
    public const string CreateFoldingRangeFromSelection = "editor.createFoldingRangeFromSelection";
    public const string RemoveManualFoldingRanges = "editor.removeManualFoldingRanges";

    private const string FoldLevelPrefix = "editor.foldLevel";

    /// <summary>The id of the command that folds the regions of a level, 1 to 7 (Monaco: <c>FoldLevelAction.ID</c>).</summary>
    public static string FoldLevel(int level) => FoldLevelPrefix + level;
}
