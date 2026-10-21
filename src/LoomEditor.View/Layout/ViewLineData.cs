// Ported from microsoft/vscode: src/vs/editor/common/viewModel.ts (ViewLineData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Layout;

/// <summary>What the renderer needs to draw one view line.</summary>
/// <param name="Content">The content at this view line.</param>
/// <param name="ContinuesWithWrappedLine">Whether this line continues with a wrapped line.</param>
/// <param name="MinColumn">The minimum allowed column at this view line.</param>
/// <param name="MaxColumn">The maximum allowed column at this view line.</param>
/// <param name="StartVisibleColumn">The visible column at the start of the line (after the fauxIndent).</param>
/// <param name="Tokens">The tokens at this view line.</param>
/// <remarks>
/// Behavior difference: no <c>inlineDecorations</c> member (they come with the projection with line breaks, phase 6).
/// Threading: immutable.
/// </remarks>
internal sealed record ViewLineData(
    string Content,
    bool ContinuesWithWrappedLine,
    int MinColumn,
    int MaxColumn,
    int StartVisibleColumn,
    IViewLineTokens Tokens);
