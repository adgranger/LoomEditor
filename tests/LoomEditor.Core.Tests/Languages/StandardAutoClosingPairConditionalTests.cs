// Ported from microsoft/vscode: src/vs/editor/test/common/modes/languageConfiguration.test.ts (suite 'StandardAutoClosingPairConditional')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages;

// 'language configurations priorities' is ported with the registry (P5-14). The last tests are new.
public class StandardAutoClosingPairConditionalTests
{
    private static void AssertIsOK(StandardAutoClosingPairConditional v, bool other, bool comment, bool str, bool regex)
    {
        Assert.Equal(other, v.IsOK(StandardTokenType.Other));
        Assert.Equal(comment, v.IsOK(StandardTokenType.Comment));
        Assert.Equal(str, v.IsOK(StandardTokenType.String));
        Assert.Equal(regex, v.IsOK(StandardTokenType.RegEx));
    }

    private static StandardAutoClosingPairConditional Create(params string[]? notIn) =>
        new(new AutoClosingPairConditional("{", "}", notIn));

    // Monaco: 'Missing notIn'
    [Fact]
    public void MissingNotIn() => AssertIsOK(Create(null), true, true, true, true);

    // Monaco: 'Empty notIn'
    [Fact]
    public void EmptyNotIn() => AssertIsOK(Create(), true, true, true, true);

    // Monaco: 'Invalid notIn'
    [Fact]
    public void InvalidNotIn() => AssertIsOK(Create("bla"), true, true, true, true);

    // Monaco: 'notIn in strings'
    [Fact]
    public void NotInInStrings() => AssertIsOK(Create("string"), true, true, false, true);

    // Monaco: 'notIn in comments'
    [Fact]
    public void NotInInComments() => AssertIsOK(Create("comment"), true, false, true, true);

    // Monaco: 'notIn in regex'
    [Fact]
    public void NotInInRegex() => AssertIsOK(Create("regex"), true, true, true, false);

    // Monaco: 'notIn in strings nor comments'
    [Fact]
    public void NotInInStringsNorComments() => AssertIsOK(Create("string", "comment"), true, false, false, true);

    // Monaco: 'notIn in strings nor regex'
    [Fact]
    public void NotInInStringsNorRegex() => AssertIsOK(Create("string", "regex"), true, true, false, false);

    // Monaco: 'notIn in comments nor regex'
    [Fact]
    public void NotInInCommentsNorRegex() => AssertIsOK(Create("comment", "regex"), true, false, true, false);

    // Monaco: 'notIn in strings, comments nor regex'
    [Fact]
    public void NotInInStringsCommentsNorRegex() => AssertIsOK(Create("string", "comment", "regex"), true, false, false, false);

    [Fact]
    public void FindNeutralCharacter_SkipsCharactersOfThePair()
    {
        var digits = new StandardAutoClosingPairConditional(new AutoClosingPairConditional("0123", "456789a"));

        Assert.Equal("b", digits.FindNeutralCharacter());
        Assert.Equal("0", Create().FindNeutralCharacter());
    }

    [Fact]
    public void AutoClosingPairs_IndexesByFirstAndLastCharacters()
    {
        var brace = new StandardAutoClosingPairConditional(new AutoClosingPairConditional("{", "}"));
        var comment = new StandardAutoClosingPairConditional(new AutoClosingPairConditional("/**", " */"));
        var empty = new StandardAutoClosingPairConditional(new AutoClosingPairConditional("", ""));

        var pairs = new AutoClosingPairs([brace, comment, empty]);

        Assert.Equal([comment], pairs.AutoClosingPairsOpenByStart["/"]);
        Assert.Equal([comment], pairs.AutoClosingPairsOpenByEnd["*"]);
        Assert.Equal([comment], pairs.AutoClosingPairsCloseByStart[" "]);
        Assert.Equal([brace], pairs.AutoClosingPairsCloseByEnd["}"]);
        Assert.Equal([empty], pairs.AutoClosingPairsOpenByStart[""]);
        Assert.Equal(["}"], pairs.AutoClosingPairsCloseSingleChar.Keys);
    }
}
