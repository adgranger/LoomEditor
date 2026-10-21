// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findModel.ts (FIND_IDS)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>Ids of the commands exposed by <see cref="FindFeature"/>. Same ids as in Monaco.</summary>
/// <remarks>
/// Not registered in phase 5: <c>editor.actions.findWithArgs</c> (commands have no arguments) and
/// <c>editor.action.goToMatchFindAction</c> (it needs a quick input UI).
/// </remarks>
public static class FindCommands
{
    /// <summary>Opens the find widget.</summary>
    public const string StartFindAction = "actions.find";

    /// <summary>Opens the find widget with the selection as the search string.</summary>
    public const string StartFindWithSelection = "actions.findWithSelection";

    /// <summary>Goes to the next match.</summary>
    public const string NextMatchFindAction = "editor.action.nextMatchFindAction";

    /// <summary>Goes to the previous match.</summary>
    public const string PreviousMatchFindAction = "editor.action.previousMatchFindAction";

    /// <summary>Goes to the next match of the selection.</summary>
    public const string NextSelectionMatchFindAction = "editor.action.nextSelectionMatchFindAction";

    /// <summary>Goes to the previous match of the selection.</summary>
    public const string PreviousSelectionMatchFindAction = "editor.action.previousSelectionMatchFindAction";

    /// <summary>Opens the find widget with the replace part.</summary>
    public const string StartFindReplaceAction = "editor.action.startFindReplaceAction";

    /// <summary>Closes the find widget.</summary>
    public const string CloseFindWidgetCommand = "closeFindWidget";

    /// <summary>Toggles the case sensitive option.</summary>
    public const string ToggleCaseSensitiveCommand = "toggleFindCaseSensitive";

    /// <summary>Toggles the whole word option.</summary>
    public const string ToggleWholeWordCommand = "toggleFindWholeWord";

    /// <summary>Toggles the regular expression option.</summary>
    public const string ToggleRegexCommand = "toggleFindRegex";

    /// <summary>Toggles "find in selection".</summary>
    public const string ToggleSearchScopeCommand = "toggleFindInSelection";

    /// <summary>Toggles the preserve case option of replace.</summary>
    public const string TogglePreserveCaseCommand = "togglePreserveCase";

    /// <summary>Replaces the current match.</summary>
    public const string ReplaceOneAction = "editor.action.replaceOne";

    /// <summary>Replaces all the matches.</summary>
    public const string ReplaceAllAction = "editor.action.replaceAll";

    /// <summary>Selects all the matches.</summary>
    public const string SelectAllMatchesAction = "editor.action.selectAllMatches";
}
