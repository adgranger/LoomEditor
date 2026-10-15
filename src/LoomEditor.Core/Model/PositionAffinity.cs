// Ported from microsoft/vscode: src/vs/editor/common/model.ts (PositionAffinity)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>Tells on which side to resolve a position that is on injected text.</summary>
internal enum PositionAffinity
{
    /// <summary>Prefers the left most position.</summary>
    Left = 0,

    /// <summary>Prefers the right most position.</summary>
    Right = 1,

    /// <summary>No preference.</summary>
    None = 2,

    /// <summary>If the given position is on injected text, prefers the position left of it.</summary>
    LeftOfInjectedText = 3,

    /// <summary>If the given position is on injected text, prefers the position right of it.</summary>
    RightOfInjectedText = 4,
}
