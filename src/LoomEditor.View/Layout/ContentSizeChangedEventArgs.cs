// Ported from microsoft/vscode: src/vs/editor/common/viewModelEventDispatcher.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Raised when the width or the height of the scrollable content changes. Monaco: <c>ContentSizeChangedEvent</c>.</summary>
/// <remarks>Threading: immutable, safe from any thread. <c>attemptToMerge</c> (event batching) is not ported.</remarks>
internal sealed class ContentSizeChangedEventArgs : EventArgs
{
    public ContentSizeChangedEventArgs(int oldContentWidth, int oldContentHeight, int contentWidth, int contentHeight)
    {
        OldContentWidth = oldContentWidth;
        OldContentHeight = oldContentHeight;
        ContentWidth = contentWidth;
        ContentHeight = contentHeight;
    }

    public int OldContentWidth { get; }

    public int OldContentHeight { get; }

    public int ContentWidth { get; }

    public int ContentHeight { get; }

    public bool ContentWidthChanged => OldContentWidth != ContentWidth;

    public bool ContentHeightChanged => OldContentHeight != ContentHeight;

    /// <summary>Whether neither the width nor the height changed.</summary>
    public bool IsNoOp => !ContentWidthChanged && !ContentHeightChanged;
}
