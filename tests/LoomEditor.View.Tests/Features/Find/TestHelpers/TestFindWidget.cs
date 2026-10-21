// Ported from microsoft/vscode: src/vs/editor/contrib/find/test/browser/findController.test.ts (TestFindController)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Features.Find;

namespace LoomEditor.View.Tests.Features.Find.TestHelpers;

/// <summary>A find widget without UI: records what the controller asks of it, like Monaco's <c>TestFindController</c>.</summary>
internal sealed class TestFindWidget : IFindWidget
{
    public bool HasFocus { get; set; }

    public int HighlightFindOptionsCount { get; private set; }

    public int FocusEditorCount { get; private set; }

    public bool IsFindInputFocused { get; private set; }

    public bool WasReplaceInputLastFocused { get; set; }

    public void ApplyStartFocus(FindStartFocusAction focus)
    {
        if (focus != FindStartFocusAction.NoFocusChange)
        {
            HasFocus = true;
        }

        IsFindInputFocused = focus == FindStartFocusAction.FocusFindInput;
    }

    public void FocusLastElement()
    {
    }

    public void FocusEditor() => FocusEditorCount++;

    public void HighlightFindOptions(bool findWidgetRevealed) => HighlightFindOptionsCount++;
}
