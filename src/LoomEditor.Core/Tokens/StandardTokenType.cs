// Ported from microsoft/vscode: src/vs/editor/common/encodedTokenAttributes.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>A standard token type (Monaco: <c>StandardTokenType</c>).</summary>
/// <remarks>Threading: a value type.</remarks>
internal enum StandardTokenType
{
    /// <summary>Any other token.</summary>
    Other = 0,

    /// <summary>A comment.</summary>
    Comment = 1,

    /// <summary>A string.</summary>
    String = 2,

    /// <summary>A regular expression.</summary>
    RegEx = 3,
}
