// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (IGuidesOptions, GuideOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>Monaco's <c>true | false | 'active'</c>.</summary>
internal enum GuideMode
{
    /// <summary>Monaco: <c>false</c>.</summary>
    Off = 0,

    /// <summary>Monaco: <c>true</c>.</summary>
    On = 1,

    /// <summary>Monaco: <c>'active'</c>.</summary>
    Active = 2,
}

/// <summary>Monaco's <c>false | true | 'always'</c> of <c>highlightActiveIndentation</c>.</summary>
internal enum ActiveIndentationHighlight
{
    /// <summary>Monaco: <c>false</c>.</summary>
    Off = 0,

    /// <summary>Monaco: <c>true</c>.</summary>
    On = 1,

    /// <summary>Monaco: <c>'always'</c>: highlighted even if bracket guides are highlighted.</summary>
    Always = 2,
}

/// <summary>The indent and bracket pair guide options (Monaco: <c>IGuidesOptions</c>, defaults of <c>GuideOptions</c>).</summary>
/// <remarks>Threading: immutable, safe from any thread. Monaco's <c>validate</c> is not needed: the enums cannot hold other values.</remarks>
internal sealed record EditorGuidesOptions
{
    /// <summary>Bracket pair guides. Monaco default: <c>false</c>.</summary>
    public GuideMode BracketPairs { get; init; } = GuideMode.Off;

    /// <summary>Horizontal bracket pair guides. Monaco default: <c>'active'</c>.</summary>
    public GuideMode BracketPairsHorizontal { get; init; } = GuideMode.Active;

    /// <summary>Highlight the active bracket pair. Monaco default: <c>true</c>.</summary>
    public bool HighlightActiveBracketPair { get; init; } = true;

    /// <summary>Indent guides. Monaco default: <c>true</c>.</summary>
    public bool Indentation { get; init; } = true;

    /// <summary>Highlight the active indent guide. Monaco default: <c>true</c>.</summary>
    public ActiveIndentationHighlight HighlightActiveIndentation { get; init; } = ActiveIndentationHighlight.On;
}
