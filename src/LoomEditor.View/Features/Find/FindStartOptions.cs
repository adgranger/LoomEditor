// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findController.ts (FindStartFocusAction, IFindStartOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>What the find widget focuses when the find starts (Monaco: <c>FindStartFocusAction</c>).</summary>
internal enum FindStartFocusAction
{
    /// <summary>The focus stays where it is.</summary>
    NoFocusChange,

    /// <summary>The find input is focused.</summary>
    FocusFindInput,

    /// <summary>The replace input is focused.</summary>
    FocusReplaceInput,
}

/// <summary>How to start a find (Monaco: <c>IFindStartOptions</c>).</summary>
/// <remarks>
/// Not ported: <c>seedSearchStringFromGlobalClipboard</c> (the find clipboard is macOS only, so it is kept only as a flag
/// that has no effect). Threading: immutable; thread-safe.
/// </remarks>
internal sealed record FindStartOptions(
    bool ForceRevealReplace,
    SeedSearchStringFromSelection SeedSearchStringFromSelection,
    bool SeedSearchStringFromNonEmptySelection,
    bool SeedSearchStringFromGlobalClipboard,
    FindStartFocusAction ShouldFocus,
    bool ShouldAnimate,
    bool UpdateSearchScope,
    bool Loop);
