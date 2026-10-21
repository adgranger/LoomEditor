// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findState.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Find;

/// <summary>Forces a find toggle on or off until the next state change (Monaco: <c>FindOptionOverride</c>).</summary>
internal enum FindOptionOverride
{
    /// <summary>The stored value is used.</summary>
    NotSet = 0,

    /// <summary>The toggle is on.</summary>
    True = 1,

    /// <summary>The toggle is off.</summary>
    False = 2,
}
