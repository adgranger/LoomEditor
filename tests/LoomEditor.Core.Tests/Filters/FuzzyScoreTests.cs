// Ported from microsoft/vscode: src/vs/base/test/common/filters.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Filters;
using LoomEditor.Core.Tests.Filters.TestHelpers;

namespace LoomEditor.Core.Tests.Filters;

[Collection("Fuzzy")] // Fuzzy uses shared static tables: not thread-safe
public class FuzzyScoreTests
{
    private static readonly FuzzyScorer FuzzyScorer = Fuzzy.Score;

    private static readonly FuzzyScorer AnyScorer = (p, lp, pp, w, lw, wp, _) => Fuzzy.AnyScore(p, lp, pp, w, lw, wp);

    private static void AssertTopScore(string pattern, int expected, params string[] words)
    {
        var topScore = -(100 * 10);
        var topIdx = 0;
        for (var i = 0; i < words.Length; i++)
        {
            var m = Fuzzy.Score(pattern, pattern.ToLowerInvariant(), 0, words[i], words[i].ToLowerInvariant(), 0);
            if (m is not null && m[0] > topScore)
            {
                topScore = m[0];
                topIdx = i;
            }
        }

        Assert.True(topIdx == expected, $"{pattern} -> actual={words[topIdx]} <> expected={words[expected]}");
    }

    private static void M(string pattern, string word, string? decorated, int patternPos = 0, int wordPos = 0, bool weak = false)
        => FuzzyAssert.Matches(pattern, word, decorated, FuzzyScorer, patternPos, wordPos, weak);

    // Monaco: 'fuzzyScore, #23215'
    [Fact]
    public void FuzzyScore_Issue23215()
    {
        M("tit", "win.tit", "win.^t^i^t");
        M("title", "win.title", "win.^t^i^t^l^e");
        M("WordCla", "WordCharacterClassifier", "^W^o^r^dCharacter^C^l^assifier");
        M("WordCCla", "WordCharacterClassifier", "^W^o^r^d^Character^C^l^assifier");
    }

    // Monaco: 'fuzzyScore, #23332'
    [Fact]
    public void FuzzyScore_Issue23332() => M("dete", "\"editor.quickSuggestionsDelay\"", null);

    // Monaco: 'fuzzyScore, #23190'
    [Fact]
    public void FuzzyScore_Issue23190()
    {
        M("c:\\do", "& 'C:\\Documents and Settings'", "& '^C^:^\\^D^ocuments and Settings'");
        M("c:\\do", "& 'c:\\Documents and Settings'", "& '^c^:^\\^D^ocuments and Settings'");
    }

    // Monaco: 'fuzzyScore, #23581'
    [Fact]
    public void FuzzyScore_Issue23581()
    {
        M("close", "css.lint.importStatement", "^css.^lint.imp^ort^Stat^ement");
        M("close", "css.colorDecorators.enable", "^css.co^l^orDecorator^s.^enable");
        M("close", "workbench.quickOpen.closeOnFocusOut", "workbench.quickOpen.^c^l^o^s^eOnFocusOut");
        AssertTopScore("close", 2, "css.lint.importStatement", "css.colorDecorators.enable", "workbench.quickOpen.closeOnFocusOut");
    }

    // Monaco: 'fuzzyScore, #23458'
    [Fact]
    public void FuzzyScore_Issue23458()
    {
        M("highlight", "editorHoverHighlight", "editorHover^H^i^g^h^l^i^g^h^t");
        M("hhighlight", "editorHoverHighlight", "editor^Hover^H^i^g^h^l^i^g^h^t");
        M("dhhighlight", "editorHoverHighlight", null);
    }

    // Monaco: 'fuzzyScore, #23746'
    [Fact]
    public void FuzzyScore_Issue23746()
    {
        M("-moz", "-moz-foo", "^-^m^o^z-foo");
        M("moz", "-moz-foo", "-^m^o^z-foo");
        M("moz", "-moz-animation", "-^m^o^z-animation");
        M("moza", "-moz-animation", "-^m^o^z-^animation");
    }

    // Monaco: 'fuzzyScore'
    [Fact]
    public void FuzzyScore_Monaco()
    {
        M("ab", "abA", "^a^bA");
        M("ccm", "cacmelCase", "^ca^c^melCase");
        M("bti", "the_black_knight", null);
        M("ccm", "camelCase", null);
        M("cmcm", "camelCase", null);
        M("BK", "the_black_knight", "the_^black_^knight");
        M("KeyboardLayout=", "KeyboardLayout", null);
        M("LLL", "SVisualLoggerLogsList", "SVisual^Logger^Logs^List");
        M("LLLL", "SVilLoLosLi", null);
        M("LLLL", "SVisualLoggerLogsList", null);
        M("TEdit", "TextEdit", "^Text^E^d^i^t");
        M("TEdit", "TextEditor", "^Text^E^d^i^tor");
        M("TEdit", "Textedit", "^Text^e^d^i^t");
        M("TEdit", "text_edit", "^text_^e^d^i^t");
        M("TEditDit", "TextEditorDecorationType", "^Text^E^d^i^tor^Decorat^ion^Type");
        M("TEdit", "TextEditorDecorationType", "^Text^E^d^i^torDecorationType");
        M("Tedit", "TextEdit", "^Text^E^d^i^t");
        M("ba", "?AB?", null);
        M("bkn", "the_black_knight", "the_^black_^k^night");
        M("bt", "the_black_knight", "the_^black_knigh^t");
        M("ccm", "camelCasecm", "^camel^Casec^m");
        M("fdm", "findModel", "^fin^d^Model");
        M("fob", "foobar", "^f^oo^bar");
        M("fobz", "foobar", null);
        M("foobar", "foobar", "^f^o^o^b^a^r");
        M("form", "editor.formatOnSave", "editor.^f^o^r^matOnSave");
        M("g p", "Git: Pull", "^Git:^ ^Pull");
        M("g p", "Git: Pull", "^Git:^ ^Pull");
        M("gip", "Git: Pull", "^G^it: ^Pull");
        M("gip", "Git: Pull", "^G^it: ^Pull");
        M("gp", "Git: Pull", "^Git: ^Pull");
        M("gp", "Git_Git_Pull", "^Git_Git_^Pull");
        M("is", "ImportStatement", "^Import^Statement");
        M("is", "isValid", "^i^sValid");
        M("lowrd", "lowWord", "^l^o^wWo^r^d");
        M("myvable", "myvariable", "^m^y^v^aria^b^l^e");
        M("no", "", null);
        M("no", "match", null);
        M("ob", "foobar", null);
        M("sl", "SVisualLoggerLogsList", "^SVisual^LoggerLogsList");
        M("sllll", "SVisualLoggerLogsList", "^SVisua^l^Logger^Logs^List");
        M("Three", "HTMLHRElement", null);
        M("Three", "Three", "^T^h^r^e^e");
        M("fo", "barfoo", null);
        M("fo", "bar_foo", "bar_^f^oo");
        M("fo", "bar_Foo", "bar_^F^oo");
        M("fo", "bar foo", "bar ^f^oo");
        M("fo", "bar.foo", "bar.^f^oo");
        M("fo", "bar/foo", "bar/^f^oo");
        M("fo", "bar\\foo", "bar\\^f^oo");
    }

    // Monaco: 'fuzzyScore (first match can be weak)'
    [Fact]
    public void FuzzyScore_FirstMatchCanBeWeak()
    {
        M("Three", "HTMLHRElement", "H^TML^H^R^El^ement", weak: true);
        M("tor", "constructor", "construc^t^o^r", weak: true);
        M("ur", "constructor", "constr^ucto^r", weak: true);
        AssertTopScore("tor", 2, "constructor", "Thor", "cTor");
    }

    // Monaco: 'fuzzyScore, many matches'
    [Fact]
    public void FuzzyScore_ManyMatches()
    {
        var word = new string('a', 300);
        M("aaaaaa", word, "^a^a^a^a^a^a" + new string('a', 294));
    }

    // Monaco: 'Freeze when fjfj -> jfjf, https://github.com/microsoft/vscode/issues/91807'
    [Fact]
    public void FuzzyScore_Issue91807()
    {
        M("jfjfj", "fjfjfjfjfjfjfjfjfjfjfj", null);
        M("jfjfjfjfjfjfjfjfjfj", "fjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfj", null);
        M(
            "jfjfjfjfjfjfjfjfjfjjfjfjfjfjfjfjfjfjfjjfjfjfjfjfjfjfjfjfjjfjfjfjfjfjfjfjfjfjjfjfjfjfjfjfjfjfjfjjfjfjfjfjfjfjfjfjfj",
            "fjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfj",
            null);
        M(
            "jfjfjfjfjfjfjfjfjfj",
            "fJfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfj",
            "f^J^f^j^f^j^f^j^f^j^f^j^f^j^f^j^f^j^f^jfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfj");
        M(
            "jfjfjfjfjfjfjfjfjfj",
            "fjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfj",
            "f^j^f^j^f^j^f^j^f^j^f^j^f^j^f^j^f^j^f^jfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfjfj",
            weak: true);
    }

    // Monaco: 'fuzzyScore, issue #26423'
    [Fact]
    public void FuzzyScore_Issue26423()
    {
        M("baba", "abababab", null);
        M("fsfsfs", "dsafdsafdsafdsafdsafdsafdsafasdfdsa", null);
        M("fsfsfsfsfsfsfsf", "dsafdsafdsafdsafdsafdsafdsafasdfdsafdsafdsafdsafdsfdsafdsfdfdfasdnfdsajfndsjnafjndsajlknfdsa", null);
    }

    // Monaco: 'Fuzzy IntelliSense matching vs Haxe metadata completion, #26995'
    [Fact]
    public void FuzzyScore_Issue26995()
    {
        M("f", ":Foo", ":^Foo");
        M("f", ":foo", ":^foo");
    }

    // Monaco: 'Separator only match should not be weak #79558'
    [Fact]
    public void FuzzyScore_SeparatorOnlyMatchIsNotWeak() => M(".", "foo.bar", "foo^.bar");

    // Monaco: 'Cannot set property '1' of undefined, #26511'
    [Fact]
    public void FuzzyScore_Issue26511_DoesNotExplode()
    {
        var word = new string('a', 122);
        var pattern = new string('a', 119);
        _ = Fuzzy.Score(pattern, pattern.ToLowerInvariant(), 0, word, word.ToLowerInvariant(), 0); // must not explode
    }

    // Monaco: 'Vscode 1.12 no longer obeys 'sortText' in completion items (from language server), #26096'
    [Fact]
    public void FuzzyScore_Issue26096()
    {
        M("  ", "  group", null, patternPos: 2);
        M("  g", "  group", "  ^group", patternPos: 2);
        M("g", "  group", "  ^group");
        M("g g", "  groupGroup", null);
        M("g g", "  group Group", "  ^group^ ^Group");
        M(" g g", "  group Group", "  ^group^ ^Group", patternPos: 1);
        M("zz", "zzGroup", "^z^zGroup");
        M("zzg", "zzGroup", "^z^z^Group");
        M("g", "zzGroup", "zz^Group");
    }

    // Monaco: 'patternPos isn't working correctly #79815'
    [Fact]
    public void FuzzyScore_Issue79815()
    {
        M(":p"[1..], "prop", "^prop", patternPos: 0);
        M(":p", "prop", "^prop", patternPos: 1);
        M(":p", "prop", null, patternPos: 2);
        M(":p", "proP", "pro^P", patternPos: 1, wordPos: 1);
        M(":p", "aprop", "a^prop", patternPos: 1, weak: true);
        M(":p", "aprop", null, patternPos: 1, weak: false);
    }

    // Monaco: 'topScore - fuzzyScore'
    [Fact]
    public void TopScore_FuzzyScore()
    {
        AssertTopScore("cons", 2, "ArrayBufferConstructor", "Console", "console");
        AssertTopScore("Foo", 1, "foo", "Foo", "foo");
        AssertTopScore("onMess", 1, "onmessage", "onMessage", "onThisMegaEscape");
        AssertTopScore("CC", 1, "camelCase", "CamelCase");
        AssertTopScore("cC", 0, "camelCase", "CamelCase");
        AssertTopScore("p", 4, "parse", "posix", "pafdsa", "path", "p");
        AssertTopScore("pa", 0, "parse", "pafdsa", "path");
        AssertTopScore("log", 3, "HTMLOptGroupElement", "ScrollLogicalPosition", "SVGFEMorphologyElement", "log", "logger");
        AssertTopScore("e", 2, "AbstractWorker", "ActiveXObject", "else");
        AssertTopScore("workbench.sideb", 1, "workbench.editor.defaultSideBySideLayout", "workbench.sideBar.location");
        AssertTopScore("editor.r", 2, "diffEditor.renderSideBySide", "editor.overviewRulerlanes", "editor.renderControlCharacter", "editor.renderWhitespace");
        AssertTopScore("-mo", 1, "-ms-ime-mode", "-moz-columns");
        AssertTopScore("convertModelPosition", 0, "convertModelPositionToViewPosition", "convertViewToModelPosition");
        AssertTopScore("is", 0, "isValidViewletId", "import statement");
        AssertTopScore("title", 1, "files.trimTrailingWhitespace", "window.title");
        AssertTopScore("const", 1, "constructor", "const", "cuOnstrul");
    }

    // Monaco: 'Unexpected suggestion scoring, #28791'
    [Fact]
    public void FuzzyScore_Issue28791()
    {
        AssertTopScore("_lines", 1, "_lineStarts", "_lines");
        AssertTopScore("_lines", 1, "_lineS", "_lines");
        AssertTopScore("_lineS", 0, "_lineS", "_lines");
    }

    // Monaco: 'HTML closing tag proposal filtered out #38880'
    [Fact]
    public void FuzzyScore_Issue38880()
    {
        M("\t\t<", "\t\t</body>", "^\t^\t^</body>", patternPos: 0);
        M("\t\t<", "\t\t</body>", "\t\t^</body>", patternPos: 2);
        M("\t<", "\t</body>", "\t^</body>", patternPos: 1);
    }

    // Monaco: 'List highlight filter: Not all characters from match are highlighterd #66923'
    [Fact]
    public void FuzzyScore_Issue66923()
        => M("foo", "barbarbarbarbarbarbarbarbarbarbarbarbarbarbarbar_foo", "barbarbarbarbarbarbarbarbarbarbarbarbarbarbarbar_^f^o^o");

    // Monaco: 'Autocompletion is matched against truncated filterText to 54 characters #74133'
    [Fact]
    public void FuzzyScore_Issue74133()
    {
        M(
            "foo",
            "ffffffffffffffffffffffffffffbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbar_foo",
            "ffffffffffffffffffffffffffffbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbar_^f^o^o");
        M(
            "Aoo",
            "Affffffffffffffffffffffffffffbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbar_foo",
            "^Affffffffffffffffffffffffffffbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbar_f^o^o");
        M(
            "foo",
            "Gffffffffffffffffffffffffffffbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbarbar_foo",
            null);
    }

    // Monaco: '"Go to Symbol" with the exact method name doesn't work as expected #84787'
    [Fact]
    public void FuzzyScore_Issue84787()
        => Assert.NotNull(Fuzzy.Score(":get", ":get", 1, "get", "get", 0, new FuzzyScoreOptions(FirstMatchCanBeWeak: true, BoostFullMatch: true)));

    // Monaco: 'Wrong highlight after emoji #113404'
    [Fact]
    public void FuzzyScore_Issue113404()
    {
        M("di", "✨div classname=\"\"></div>", "✨^d^iv classname=\"\"></div>");
        M("di", "adiv classname=\"\"></div>", "adiv classname=\"\"></^d^iv>");
    }

    // Monaco: 'IntelliSense completion not correctly highlighting text in front of cursor #115250'
    [Fact]
    public void FuzzyScore_Issue115250()
    {
        M("lo", "log", "^l^og");
        FuzzyAssert.Matches(".lo", "log", "^l^og", AnyScorer);
        FuzzyAssert.Matches(".", "log", "log", AnyScorer);
    }

    // Monaco: 'anyScore should not require a strong first match'
    [Fact]
    public void AnyScore_DoesNotRequireStrongFirstMatch()
    {
        FuzzyAssert.Matches("bar", "foobAr", "foo^b^A^r", AnyScorer);
        FuzzyAssert.Matches("bar", "foobar", "foo^b^a^r", AnyScorer);
    }

    // Monaco: 'configurable full match boost'
    [Fact]
    public void FuzzyScore_ConfigurableFullMatchBoost()
    {
        var prefix = "create";
        var a = "createModelServices";
        var b = "create";
        var boost = new FuzzyScoreOptions(FirstMatchCanBeWeak: true, BoostFullMatch: true);
        var noBoost = new FuzzyScoreOptions(FirstMatchCanBeWeak: true, BoostFullMatch: false);

        var aBoost = Fuzzy.Score(prefix, prefix, 0, a, a.ToLowerInvariant(), 0, boost);
        var bBoost = Fuzzy.Score(prefix, prefix, 0, b, b.ToLowerInvariant(), 0, boost);
        Assert.NotNull(aBoost);
        Assert.NotNull(bBoost);
        Assert.True(aBoost[0] < bBoost[0]);

        // also works with wordStart > 0 (https://github.com/microsoft/vscode/issues/187921)
        var wordPrefix = "$(symbol-function) ";
        aBoost = Fuzzy.Score(prefix, prefix, 0, wordPrefix + a, (wordPrefix + a).ToLowerInvariant(), wordPrefix.Length, boost);
        bBoost = Fuzzy.Score(prefix, prefix, 0, wordPrefix + b, (wordPrefix + b).ToLowerInvariant(), wordPrefix.Length, boost);
        Assert.NotNull(aBoost);
        Assert.NotNull(bBoost);
        Assert.True(aBoost[0] < bBoost[0]);

        var aScore = Fuzzy.Score(prefix, prefix, 0, a, a.ToLowerInvariant(), 0, noBoost);
        var bScore = Fuzzy.Score(prefix, prefix, 0, b, b.ToLowerInvariant(), 0, noBoost);
        Assert.NotNull(aScore);
        Assert.NotNull(bScore);
        Assert.Equal(aScore[0], bScore[0]);
    }

    // Monaco: 'Unexpected suggest highlighting ignores whole word match in favor of matching first letter#147423'
    [Fact]
    public void FuzzyScore_Issue147423()
    {
        M("i", "machine/{id}", "machine/{^id}");
        M("ok", "obobobf{ok}/user", "^obobobf{o^k}/user");
    }

    [Fact]
    public void Default_IsMinus100()
    {
        Assert.Equal([-100, 0], FuzzyScore.Default);
    }

    [Fact]
    public void IsDefault_NullAndDefault()
    {
        Assert.True(FuzzyScore.IsDefault(null));
        Assert.True(FuzzyScore.IsDefault(FuzzyScore.Default));
        Assert.True(FuzzyScore.IsDefault([-100, 0]));
        Assert.False(FuzzyScore.IsDefault([-100, 1]));
        Assert.False(FuzzyScore.IsDefault([-100, 0, 3]));
    }

    [Fact]
    public void AnyScore_NoMatch_ReturnsZeroAndWordPos()
        => Assert.Equal([0, 2], Fuzzy.AnyScore("xyz", "xyz", 0, "abc", "abc", 2));

    [Fact]
    public void CreateMatches_Null_Empty() => Assert.Empty(Fuzzy.CreateMatches(null));

    [Fact]
    public void CreateMatches_MergesAdjacentPositions()
    {
        // Positions are stored last match first: 2, 1, 0 -> [0, 3).
        var matches = Fuzzy.CreateMatches([5, 0, 2, 1, 0]);
        Assert.Equal([new FilterMatch(0, 3)], matches);
    }

    [Fact]
    public void Score_PatternLongerThanWord_Null()
        => Assert.Null(Fuzzy.Score("abcd", "abcd", 0, "abc", "abc", 0));

    [Fact]
    public void Score_EmptyPattern_Null()
        => Assert.Null(Fuzzy.Score("", "", 0, "abc", "abc", 0));

    [Fact]
    public void Score_LongWord_UsesFirst128Characters()
    {
        var word = new string('a', 128) + "b";
        Assert.Null(Fuzzy.Score("b", "b", 0, word, word, 0));
    }
}
