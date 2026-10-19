// Ported from microsoft/vscode-textmate: src/onigLib.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Oniguruma;

/// <summary><c>FindOption</c>: search options of <see cref="IOnigScanner.FindNextMatchSync(IOnigString, int, FindOption)"/>.</summary>
/// <remarks>Threading: a value type.</remarks>
[Flags]
internal enum FindOption
{
    /// <summary>No option.</summary>
    None = 0,

    /// <summary>Equivalent of <c>ONIG_OPTION_NOT_BEGIN_STRING</c>: (str) isn't considered as begin of string (fail <c>\A</c>).</summary>
    NotBeginString = 1,

    /// <summary>Equivalent of <c>ONIG_OPTION_NOT_END_STRING</c>: (end) isn't considered as end of string (fail <c>\z</c>, <c>\Z</c>).</summary>
    NotEndString = 2,

    /// <summary>
    /// Equivalent of <c>ONIG_OPTION_NOT_BEGIN_POSITION</c>: (start) isn't considered as start position of search
    /// (fail <c>\G</c>).
    /// </summary>
    NotBeginPosition = 4,

    /// <summary>Used for debugging purposes (accepted and ignored).</summary>
    DebugCall = 8,
}
