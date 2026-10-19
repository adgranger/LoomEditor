// Ported from microsoft/vscode: src/vs/editor/common/encodedTokenAttributes.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>A font style. Values are 2^x such that a bit mask can be used (Monaco: <c>FontStyle</c>).</summary>
/// <remarks>Threading: a value type.</remarks>
[Flags]
internal enum FontStyle
{
    /// <summary>Not set.</summary>
    NotSet = -1,

    /// <summary>No style.</summary>
    None = 0,

    /// <summary>Italic.</summary>
    Italic = 1,

    /// <summary>Bold.</summary>
    Bold = 2,

    /// <summary>Underline.</summary>
    Underline = 4,

    /// <summary>Strikethrough.</summary>
    Strikethrough = 8,
}
