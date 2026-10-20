// Ported from microsoft/vscode: src/vs/editor/test/common/modes/supports/bracketRules.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;

namespace LoomEditor.Core.Tests.Languages.TestHelpers;

/// <summary>The brackets of a few languages, for tests.</summary>
/// <remarks>Threading: immutable.</remarks>
internal static class BracketRules
{
    private static readonly IReadOnlyList<CharacterPair> StandardBracketRules =
    [
        new("{", "}"),
        new("[", "]"),
        new("(", ")"),
    ];

    public static readonly IReadOnlyList<CharacterPair> RubyBracketRules = StandardBracketRules;

    public static readonly IReadOnlyList<CharacterPair> CppBracketRules = StandardBracketRules;

    public static readonly IReadOnlyList<CharacterPair> GoBracketRules = StandardBracketRules;

    public static readonly IReadOnlyList<CharacterPair> PhpBracketRules = StandardBracketRules;

    public static readonly IReadOnlyList<CharacterPair> VbBracketRules = StandardBracketRules;

    public static readonly IReadOnlyList<CharacterPair> LuaBracketRules = StandardBracketRules;

    public static readonly IReadOnlyList<CharacterPair> HtmlBracketRules =
    [
        new("<!--", "-->"),
        new("{", "}"),
        new("(", ")"),
    ];

    public static readonly IReadOnlyList<CharacterPair> TypescriptBracketRules =
    [
        new("${", "}"),
        new("{", "}"),
        new("[", "]"),
        new("(", ")"),
    ];

    public static readonly IReadOnlyList<CharacterPair> LatexBracketRules =
    [
        new("{", "}"),
        new("[", "]"),
        new("(", ")"),
        new("[", ")"),
        new("(", "]"),
        new(@"\left(", @"\right)"),
        new(@"\left(", @"\right."),
        new(@"\left.", @"\right)"),
        new(@"\left[", @"\right]"),
        new(@"\left[", @"\right."),
        new(@"\left.", @"\right]"),
        new(@"\left\{", @"\right\}"),
        new(@"\left\{", @"\right."),
        new(@"\left.", @"\right\}"),
        new(@"\left<", @"\right>"),
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
        new(@"\langle", @"\rangle"),
        new(@"\lvert", @"\rvert"),
        new(@"\lVert", @"\rVert"),
        new(@"\left|", @"\right|"),
        new(@"\left\vert", @"\right\vert"),
        new(@"\left\|", @"\right\|"),
        new(@"\left\Vert", @"\right\Vert"),
        new(@"\left\langle", @"\right\rangle"),
        new(@"\left\lvert", @"\right\rvert"),
        new(@"\left\lVert", @"\right\rVert"),
        new(@"\bigl\langle", @"\bigr\rangle"),
        new(@"\bigl|", @"\bigr|"),
        new(@"\bigl\vert", @"\bigr\vert"),
        new(@"\bigl\lvert", @"\bigr\rvert"),
        new(@"\bigl\|", @"\bigr\|"),
        new(@"\bigl\lVert", @"\bigr\rVert"),
        new(@"\bigl\Vert", @"\bigr\Vert"),
        new(@"\Bigl\langle", @"\Bigr\rangle"),
        new(@"\Bigl|", @"\Bigr|"),
        new(@"\Bigl\lvert", @"\Bigr\rvert"),
        new(@"\Bigl\vert", @"\Bigr\vert"),
        new(@"\Bigl\|", @"\Bigr\|"),
        new(@"\Bigl\lVert", @"\Bigr\rVert"),
        new(@"\Bigl\Vert", @"\Bigr\Vert"),
        new(@"\biggl\langle", @"\biggr\rangle"),
        new(@"\biggl|", @"\biggr|"),
        new(@"\biggl\lvert", @"\biggr\rvert"),
        new(@"\biggl\vert", @"\biggr\vert"),
        new(@"\biggl\|", @"\biggr\|"),
        new(@"\biggl\lVert", @"\biggr\rVert"),
        new(@"\biggl\Vert", @"\biggr\Vert"),
        new(@"\Biggl\langle", @"\Biggr\rangle"),
        new(@"\Biggl|", @"\Biggr|"),
        new(@"\Biggl\lvert", @"\Biggr\rvert"),
        new(@"\Biggl\vert", @"\Biggr\vert"),
        new(@"\Biggl\|", @"\Biggr\|"),
        new(@"\Biggl\lVert", @"\Biggr\rVert"),
        new(@"\Biggl\Vert", @"\Biggr\Vert"),
    ];
}
