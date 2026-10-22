// Ported from microsoft/vscode: src/vs/editor/common/textModelGuides.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Guides;

/// <summary>Which horizontal bracket pair guides are shown.</summary>
internal enum HorizontalGuidesState
{
    /// <summary>No horizontal guides.</summary>
    Disabled = 0,

    /// <summary>Horizontal guides only for the active bracket pair.</summary>
    EnabledForActive = 1,

    /// <summary>Horizontal guides for every bracket pair.</summary>
    Enabled = 2,
}

/// <summary>Options of the bracket pair guides.</summary>
/// <param name="IncludeInactive">Whether guides of inactive bracket pairs are included.</param>
/// <param name="HorizontalGuides">Which horizontal guides are shown.</param>
/// <param name="HighlightActive">Whether the active bracket pair guide is highlighted.</param>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
internal sealed record BracketGuideOptions(bool IncludeInactive, HorizontalGuidesState HorizontalGuides, bool HighlightActive);
