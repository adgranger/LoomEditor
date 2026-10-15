// Ported from microsoft/vscode: src/vs/editor/common/model/intervalTree.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>Color of an interval tree node.</summary>
/// <remarks>Threading: plain enum; thread-safe.</remarks>
internal enum NodeColor
{
    Black = 0,
    Red = 1,
}
