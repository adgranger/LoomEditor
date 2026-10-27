// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.DiffEditor;

/// <summary>From which side of an unchanged region a line is revealed (Monaco: <c>RevealPreference</c>).</summary>
internal enum RevealPreference
{
    /// <summary>Reveal from the side the line is closest to.</summary>
    FromCloserSide,

    /// <summary>Reveal from the top of the region.</summary>
    FromTop,

    /// <summary>Reveal from the bottom of the region.</summary>
    FromBottom,
}

/// <summary>The edge of an unchanged region that is being dragged.</summary>
internal enum UnchangedRegionDragSide
{
    /// <summary>The top edge.</summary>
    Top,

    /// <summary>The bottom edge.</summary>
    Bottom,
}
