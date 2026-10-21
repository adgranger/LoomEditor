// Inspired by microsoft/vscode: src/vs/editor/contrib/find/browser/findController.ts (FindController and its FindWidget)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>What the find controller needs from the find widget. Tests fake it.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IFindWidget
{
    /// <summary>Whether the find input has the focus (Monaco: <c>CONTEXT_FIND_INPUT_FOCUSED</c>).</summary>
    bool IsFindInputFocused { get; }

    /// <summary>Whether the replace input was the last focused input of the widget.</summary>
    bool WasReplaceInputLastFocused { get; }

    /// <summary>Called at the end of every find start, with the focus the start asks for (also <see cref="FindStartFocusAction.NoFocusChange"/>).</summary>
    void ApplyStartFocus(FindStartFocusAction focus);

    /// <summary>Focuses the last focused element of the widget.</summary>
    void FocusLastElement();

    /// <summary>Gives the focus back to the editor.</summary>
    void FocusEditor();

    /// <summary>Flashes the toggles; <paramref name="findWidgetRevealed"/> tells whether the widget itself is visible.</summary>
    void HighlightFindOptions(bool findWidgetRevealed);
}
