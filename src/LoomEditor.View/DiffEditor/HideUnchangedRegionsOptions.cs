// Ported from microsoft/vscode: src/vs/editor/common/config/diffEditor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.DiffEditor;

/// <summary>The settings of the "hide unchanged regions" mode of the diff editor (Monaco: <c>hideUnchangedRegions</c>).</summary>
/// <param name="Enabled">Whether unchanged regions are collapsed.</param>
/// <param name="ContextLineCount">How many unchanged lines stay visible around each change.</param>
/// <param name="MinimumLineCount">The minimum number of lines an unchanged region needs to be collapsed.</param>
/// <param name="RevealLineCount">How many lines one click on a collapsed region reveals.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record HideUnchangedRegionsOptions(
    bool Enabled = false, int ContextLineCount = 3, int MinimumLineCount = 3, int RevealLineCount = 20)
{
    /// <summary>The same options with every count clamped to at least 0.</summary>
    public HideUnchangedRegionsOptions Validate() => this with
    {
        ContextLineCount = Math.Max(0, ContextLineCount),
        MinimumLineCount = Math.Max(0, MinimumLineCount),
        RevealLineCount = Math.Max(0, RevealLineCount),
    };
}
