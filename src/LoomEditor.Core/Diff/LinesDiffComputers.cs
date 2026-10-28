// Ported from microsoft/vscode: src/vs/editor/common/diff/linesDiffComputers.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Diff;

/// <summary>Factory for line diff computers (Monaco's <c>linesDiffComputers</c>; no legacy computer).</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class LinesDiffComputers
{
    /// <summary><c>getDefault</c>.</summary>
    public static ILinesDiffComputer GetDefault() => new DefaultLinesDiffComputer();
}
