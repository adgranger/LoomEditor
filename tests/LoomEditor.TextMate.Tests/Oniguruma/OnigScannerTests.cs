// Ported from microsoft/vscode-oniguruma: src/test/index.test.ts
// Upstream commit: 716aeaa229e4ae2e3b0057377b55743e9a3e995b  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;
using LoomEditor.TextMate.Tests.TestHelpers;

namespace LoomEditor.TextMate.Tests.Oniguruma;

public class OnigScannerTests
{
    private static readonly IOnigLib Lib = OnigTestLib.GetOniguruma();

    // vscode-oniguruma: 'simple1'
    [Fact]
    public void Simple1()
    {
        using IOnigScanner scanner = Lib.CreateOnigScanner(["ell", "wo"]);
        using IOnigString s = Lib.CreateOnigString("Hello world!");
        AssertMatch(scanner.FindNextMatchSync(s, 0), 0, (1, 4));
        AssertMatch(scanner.FindNextMatchSync(s, 2), 1, (6, 8));
    }

    // vscode-oniguruma: 'simple2'
    [Fact]
    public void Simple2()
    {
        using IOnigScanner scanner = Lib.CreateOnigScanner(["a", "b", "c"]);
        Assert.Null(scanner.FindNextMatchSync("x", 0));
        AssertMatch(scanner.FindNextMatchSync("xxaxxbxxc", 0), 0, (2, 3));
        AssertMatch(scanner.FindNextMatchSync("xxaxxbxxc", 4), 1, (5, 6));
        AssertMatch(scanner.FindNextMatchSync("xxaxxbxxc", 7), 2, (8, 9));
        Assert.Null(scanner.FindNextMatchSync("xxaxxbxxc", 9));
    }

    // vscode-oniguruma: 'unicode1'
    [Fact]
    public void Unicode1()
    {
        using (IOnigScanner scanner1 = Lib.CreateOnigScanner(["1", "2"]))
        {
            AssertMatch(scanner1.FindNextMatchSync("ab…cde21", 5), 1, (6, 7));
        }

        using IOnigScanner scanner2 = Lib.CreateOnigScanner(["\""]);
        AssertMatch(scanner2.FindNextMatchSync("{\"…\": 1}", 1), 0, (1, 2));
    }

    // vscode-oniguruma: 'unicode2'
    [Fact]
    public void Unicode2()
    {
        using IOnigScanner scanner = Lib.CreateOnigScanner(["Y", "X"]);
        AssertMatch(scanner.FindNextMatchSync("a💻bYX", 0), 0, (4, 5));
        AssertMatch(scanner.FindNextMatchSync("a💻bYX", 1), 0, (4, 5));
        AssertMatch(scanner.FindNextMatchSync("a💻bYX", 3), 0, (4, 5));
        AssertMatch(scanner.FindNextMatchSync("a💻bYX", 4), 0, (4, 5));
        AssertMatch(scanner.FindNextMatchSync("a💻bYX", 5), 1, (5, 6));
    }

    // vscode-oniguruma: 'unicode3'
    [Fact]
    public void Unicode3()
    {
        using IOnigScanner scanner = Lib.CreateOnigScanner(["Возврат"]);
        AssertMatch(scanner.FindNextMatchSync("Возврат long_var_name;", 0), 0, (0, 7));
    }

    // vscode-oniguruma: 'unicode4'
    [Fact]
    public void Unicode4()
    {
        using IOnigScanner scanner = Lib.CreateOnigScanner(["X"]);
        AssertMatch(scanner.FindNextMatchSync("X\uD83CX", 0), 0, (0, 1));
        AssertMatch(scanner.FindNextMatchSync("X\uD83CX", 1), 0, (2, 3));
        AssertMatch(scanner.FindNextMatchSync("X\uD83CX", 2), 0, (2, 3));
        AssertMatch(scanner.FindNextMatchSync("X\uDFFFX", 0), 0, (0, 1));
        AssertMatch(scanner.FindNextMatchSync("X\uDFFFX", 1), 0, (2, 3));
        AssertMatch(scanner.FindNextMatchSync("X\uDFFFX", 2), 0, (2, 3));
        // These are actually valid, just testing the min & max
        AssertMatch(scanner.FindNextMatchSync("X" + (char)0xD800 + (char)0xDC00 + "X", 2), 0, (3, 4));
        AssertMatch(scanner.FindNextMatchSync("X" + (char)0xDBFF + (char)0xDFFF + "X", 2), 0, (3, 4));
    }

    // vscode-oniguruma: 'out of bounds'
    [Fact]
    public void OutOfBounds()
    {
        using IOnigScanner scanner = Lib.CreateOnigScanner(["X"]);
        AssertMatch(scanner.FindNextMatchSync("X💻X", -1000), 0, (0, 1));
        Assert.Null(scanner.FindNextMatchSync("X💻X", 1000));
    }

    // vscode-oniguruma: 'regex with \G'
    [Fact]
    public void RegexWithG()
    {
        using IOnigString str = Lib.CreateOnigString("first-and-second");
        using IOnigScanner scanner = Lib.CreateOnigScanner(["\\G-and"]);
        Assert.Null(scanner.FindNextMatchSync(str, 0));
        AssertMatch(scanner.FindNextMatchSync(str, 5), 0, (5, 9));
    }

    // vscode-oniguruma: 'kkos/oniguruma#192'
    [Fact]
    public void KkosOniguruma192()
    {
        using IOnigString str = Lib.CreateOnigString("    while (i < len && f(array[i]))");
        using IOnigScanner scanner = Lib.CreateOnigScanner(["(?x)\n  (?<!\\+\\+|--)(?<=[({\\[,?=>:*]|&&|\\|\\||\\?|\\*\\/|^await|[^\\._$[:alnum:]]await|^return|[^\\._$[:alnum:]]return|^default|[^\\._$[:alnum:]]default|^yield|[^\\._$[:alnum:]]yield|^)\\s*\n  (?!<\\s*[_$[:alpha:]][_$[:alnum:]]*((\\s+extends\\s+[^=>])|,)) # look ahead is not type parameter of arrow\n  (?=(<)\\s*(?:([_$[:alpha:]][-_$[:alnum:].]*)(?<!\\.|-)(:))?((?:[a-z][a-z0-9]*|([_$[:alpha:]][-_$[:alnum:].]*))(?<!\\.|-))(?=((<\\s*)|(\\s+))(?!\\?)|\\/?>))"]);
        Assert.Null(scanner.FindNextMatchSync(str, 0));
    }

    // vscode-oniguruma: 'FindOption.NotBeginPosition'
    [Fact]
    public void FindOptionNotBeginPosition()
    {
        using IOnigString str = Lib.CreateOnigString("first-and-second");
        using IOnigScanner scanner = Lib.CreateOnigScanner(["\\G-and"]);
        AssertMatch(scanner.FindNextMatchSync(str, 5), 0, (5, 9));
        Assert.Null(scanner.FindNextMatchSync(str, 5, FindOption.NotBeginPosition));
    }

    // vscode-oniguruma: 'FindOption.NotBeginString'
    [Fact]
    public void FindOptionNotBeginString()
    {
        using IOnigString str = Lib.CreateOnigString("first-and-first");
        using IOnigScanner scanner = Lib.CreateOnigScanner(["\\Afirst"]);
        Assert.Null(scanner.FindNextMatchSync(str, 10));
        AssertMatch(scanner.FindNextMatchSync(str, 0), 0, (0, 5));
        Assert.Null(scanner.FindNextMatchSync(str, 0, FindOption.NotBeginString));
    }

    // vscode-oniguruma: 'FindOption.NotEndString'
    [Fact]
    public void FindOptionNotEndString()
    {
        using IOnigString str = Lib.CreateOnigString("first-and-first");
        using IOnigScanner scanner = Lib.CreateOnigScanner(["first\\z"]);
        AssertMatch(scanner.FindNextMatchSync(str, 10), 0, (10, 15));
        Assert.Null(scanner.FindNextMatchSync(str, 10, FindOption.NotEndString));
    }

    [Fact]
    public void FindNextMatch_TwoPatternsSameStart_LowestIndexWins()
    {
        using IOnigScanner scanner = Lib.CreateOnigScanner(["b+", "x", "bb"]);
        AssertMatch(scanner.FindNextMatchSync("xa bb", 1), 0, (3, 5));
        AssertMatch(scanner.FindNextMatchSync("xa bb", 0), 1, (0, 1));
    }

    [Fact]
    public void FindNextMatch_CachedResult_IsReusedForALaterPosition()
    {
        using IOnigString str = Lib.CreateOnigString("aa b aa c");
        using IOnigScanner scanner = Lib.CreateOnigScanner(["c", "b", "z"]);
        AssertMatch(scanner.FindNextMatchSync(str, 0), 1, (3, 4));
        AssertMatch(scanner.FindNextMatchSync(str, 2), 1, (3, 4));
        AssertMatch(scanner.FindNextMatchSync(str, 4), 0, (8, 9));
        Assert.Null(scanner.FindNextMatchSync(str, 9));
        // A smaller position on the same string searches again.
        AssertMatch(scanner.FindNextMatchSync(str, 0), 1, (3, 4));
    }

    [Fact]
    public void CreateOnigScanner_InvalidPattern_Throws()
    {
        Assert.Throws<OnigException>(() => Lib.CreateOnigScanner(["a", "(b"]));
    }

    private static void AssertMatch(IOnigMatch? match, int index, params (int Start, int End)[] captures)
    {
        Assert.NotNull(match);
        Assert.Equal(index, match.Index);
        Assert.Equal(captures.Select(c => new OnigCaptureIndex(c.Start, c.End)), match.CaptureIndices);
    }
}
