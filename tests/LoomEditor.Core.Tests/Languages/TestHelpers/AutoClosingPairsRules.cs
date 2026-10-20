// Ported from microsoft/vscode: src/vs/editor/test/common/modes/supports/autoClosingPairsRules.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;

namespace LoomEditor.Core.Tests.Languages.TestHelpers;

/// <summary>The auto closing pairs of a few languages, for tests.</summary>
/// <remarks>Threading: immutable.</remarks>
internal static class AutoClosingPairsRules
{
    public static readonly IReadOnlyList<AutoClosingPairConditional> JavascriptAutoClosingPairsRules =
    [
        new("{", "}"),
        new("[", "]"),
        new("(", ")"),
        new("'", "'", ["string", "comment"]),
        new("\"", "\"", ["string"]),
        new("`", "`", ["string", "comment"]),
        new("/**", " */", ["string"]),
    ];

    public static readonly IReadOnlyList<AutoClosingPair> LatexAutoClosingPairsRules =
    [
        new(@"\left(", @"\right)"),
        new(@"\left[", @"\right]"),
        new(@"\left\{", @"\right\}"),
        new(@"\bigl(", @"\bigr)"),
        new(@"\bigl[", @"\bigr]"),
        new(@"\bigl\{", @"\bigr\}"),
        new(@"\Bigl(", @"\Bigr)"),
        new(@"\Bigl[", @"\Bigr]"),
        new(@"\Bigl\{", @"\Bigr\}"),
        new(@"\biggl(", @"\biggr)"),
        new(@"\biggl[", @"\biggr]"),
        new(@"\biggl\{", @"\biggr\}"),
        new(@"\Biggl(", @"\Biggr)"),
        new(@"\Biggl[", @"\Biggr]"),
        new(@"\Biggl\{", @"\Biggr\}"),
        new(@"\(", @"\)"),
        new(@"\[", @"\]"),
        new(@"\{", @"\}"),
        new("{", "}"),
        new("[", "]"),
        new("(", ")"),
        new("`", "'"),
    ];
}
