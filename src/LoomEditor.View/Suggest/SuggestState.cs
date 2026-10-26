// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Suggest;

/// <summary>Whether a completion session is active and how it started (Monaco: <c>State</c>).</summary>
/// <remarks>Threading: plain enum, safe from any thread.</remarks>
internal enum SuggestState
{
    /// <summary>No session.</summary>
    Idle = 0,

    /// <summary>A session started by the user (Ctrl+Space).</summary>
    Manual = 1,

    /// <summary>A session started by typing.</summary>
    Auto = 2,
}
