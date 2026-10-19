// Ported from microsoft/vscode-textmate: src/encodedTokenAttributes.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>StandardTokenType</c> of vscode-textmate (not Monaco's <c>LoomEditor.Core.Tokens.StandardTokenType</c>).</summary>
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

/// <summary><c>OptionalStandardTokenType</c>: must have the same values as <see cref="StandardTokenType"/>, plus <see cref="NotSet"/>.</summary>
/// <remarks>Threading: a value type.</remarks>
internal enum OptionalStandardTokenType
{
    /// <summary>Any other token.</summary>
    Other = 0,

    /// <summary>A comment.</summary>
    Comment = 1,

    /// <summary>A string.</summary>
    String = 2,

    /// <summary>A regular expression.</summary>
    RegEx = 3,

    /// <summary>Indicates that no token type is set.</summary>
    NotSet = 8,
}
