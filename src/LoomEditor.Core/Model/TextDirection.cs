// Ported from microsoft/vscode: src/vs/editor/common/model.ts (TextDirection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>The direction of the text of a line (Monaco: <c>TextDirection</c>).</summary>
internal enum TextDirection
{
    /// <summary>Left to right.</summary>
    LTR = 0,

    /// <summary>Right to left.</summary>
    RTL = 1,
}
