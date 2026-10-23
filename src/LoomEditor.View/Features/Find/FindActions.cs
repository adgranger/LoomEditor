// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findController.ts (the find actions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;

namespace LoomEditor.View.Features.Find;

/// <summary>What the find commands do (Monaco: <c>StartFindAction</c>, <c>NextMatchFindAction</c>…), as plain methods.</summary>
/// <remarks>
/// Behavior differences: synchronous; no find clipboard.
/// Threading: UI thread only.
/// </remarks>
internal static class FindActions
{
    private static SeedSearchStringFromSelection SeedFromOption(EditorFindOptions options, bool when = true) =>
        when && options.SeedSearchStringFromSelection != SeedSearchStringMode.Never ? SeedSearchStringFromSelection.Single : SeedSearchStringFromSelection.None;

    /// <summary>Ctrl+F: opens the find widget and seeds it with the word or selection.</summary>
    public static void StartFind(FindController controller) => controller.Start(new FindStartOptions(
        ForceRevealReplace: false,
        SeedSearchStringFromSelection: SeedFromOption(controller.Options),
        SeedSearchStringFromNonEmptySelection: controller.Options.SeedSearchStringFromSelection == SeedSearchStringMode.Selection,
        SeedSearchStringFromGlobalClipboard: false,
        ShouldFocus: FindStartFocusAction.FocusFindInput,
        ShouldAnimate: true,
        UpdateSearchScope: false,
        Loop: controller.Options.Loop));

    /// <summary>Opens the find widget with the selection, even over several lines, without moving the focus.</summary>
    public static void StartFindWithSelection(FindController controller) => controller.Start(new FindStartOptions(
        ForceRevealReplace: false,
        SeedSearchStringFromSelection: SeedSearchStringFromSelection.Multiple,
        SeedSearchStringFromNonEmptySelection: false,
        SeedSearchStringFromGlobalClipboard: false,
        ShouldFocus: FindStartFocusAction.NoFocusChange,
        ShouldAnimate: true,
        UpdateSearchScope: false,
        Loop: controller.Options.Loop));

    /// <summary>F3: goes to the next match; starts a find first when the widget was never opened.</summary>
    public static void NextMatch(FindController controller) => MatchFindAction(controller, true);

    /// <summary>Shift+F3: goes to the previous match.</summary>
    public static void PreviousMatch(FindController controller) => MatchFindAction(controller, false);

    private static void MatchFindAction(FindController controller, bool next)
    {
        var shouldCloseOnResult = controller.Options.CloseOnResult;
        var wasFindWidgetVisible = controller.State.IsRevealed;

        bool RunMatch()
        {
            Selection previousSelection = controller.ViewModel.GetSelection();
            var result = next ? controller.MoveToNextMatch() : controller.MoveToPrevMatch();
            if (!result || previousSelection == controller.ViewModel.GetSelection())
            {
                return false;
            }

            controller.ViewModel.Model.PushStackElement();
            if (shouldCloseOnResult && wasFindWidgetVisible && controller.IsFindInputFocused)
            {
                controller.CloseFindWidget();
            }

            return true;
        }

        if (!RunMatch())
        {
            controller.Start(new FindStartOptions(
                ForceRevealReplace: false,
                SeedSearchStringFromSelection: SeedFromOption(controller.Options, controller.State.SearchString.Length == 0),
                SeedSearchStringFromNonEmptySelection: controller.Options.SeedSearchStringFromSelection == SeedSearchStringMode.Selection,
                SeedSearchStringFromGlobalClipboard: true,
                ShouldFocus: FindStartFocusAction.NoFocusChange,
                ShouldAnimate: true,
                UpdateSearchScope: false,
                Loop: controller.Options.Loop));
            if (!RunMatch() && wasFindWidgetVisible && controller.State.MatchesCount == 0 && controller.State.SearchString.Length > 0)
            {
                controller.Announcer.Announce(AccessibilityMessages.FindNoResultsAgain(controller.State.SearchString));
            }
        }
    }

    /// <summary>Goes to the next match of the selected text.</summary>
    public static void NextSelectionMatch(FindController controller) => SelectionMatchFindAction(controller, c => c.MoveToNextMatch());

    /// <summary>Goes to the previous match of the selected text.</summary>
    public static void PreviousSelectionMatch(FindController controller) => SelectionMatchFindAction(controller, c => c.MoveToPrevMatch());

    private static void SelectionMatchFindAction(FindController controller, Func<FindController, bool> run)
    {
        var selectionSearchString = SelectionSearchString.Get(controller.ViewModel, SeedSearchStringFromSelection.Single, false);
        if (!string.IsNullOrEmpty(selectionSearchString))
        {
            controller.SetSearchString(selectionSearchString);
        }

        if (!run(controller))
        {
            controller.Start(new FindStartOptions(
                ForceRevealReplace: false,
                SeedSearchStringFromSelection: SeedSearchStringFromSelection.None,
                SeedSearchStringFromNonEmptySelection: false,
                SeedSearchStringFromGlobalClipboard: false,
                ShouldFocus: FindStartFocusAction.NoFocusChange,
                ShouldAnimate: true,
                UpdateSearchScope: false,
                Loop: controller.Options.Loop));
            run(controller);
        }
    }

    /// <summary>Ctrl+H: opens the find widget with the replace part; does nothing in a read-only editor.</summary>
    public static void StartFindReplace(FindController controller)
    {
        if (controller.ViewModel.CursorConfig.ReadOnly)
        {
            return;
        }

        Selection currentSelection = controller.ViewModel.GetSelection();
        var findInputFocused = controller.IsFindInputFocused;
        var seedSearchStringFromSelection = !currentSelection.IsEmpty
            && currentSelection.Range.StartLineNumber == currentSelection.Range.EndLineNumber
            && controller.Options.SeedSearchStringFromSelection != SeedSearchStringMode.Never
            && !findInputFocused;

        // findInputFocused true -> no seed, FocusReplaceInput; seeded -> FocusReplaceInput; else FocusFindInput (the find input is still empty)
        var shouldFocus = findInputFocused || seedSearchStringFromSelection
            ? FindStartFocusAction.FocusReplaceInput
            : FindStartFocusAction.FocusFindInput;

        controller.Start(new FindStartOptions(
            ForceRevealReplace: true,
            SeedSearchStringFromSelection: seedSearchStringFromSelection ? SeedSearchStringFromSelection.Single : SeedSearchStringFromSelection.None,
            SeedSearchStringFromNonEmptySelection: controller.Options.SeedSearchStringFromSelection == SeedSearchStringMode.Selection,
            SeedSearchStringFromGlobalClipboard: false,
            ShouldFocus: shouldFocus,
            ShouldAnimate: true,
            UpdateSearchScope: false,
            Loop: controller.Options.Loop));
    }
}
