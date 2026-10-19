// Ported from microsoft/vscode: src/vs/editor/test/common/model/model.line.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tests.Model.Tokens.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class ModelLinesTokensTests
{
    private readonly record struct TestToken(int StartOffset, int Color);

    private readonly record struct BufferLineState(string Text, TestToken[] Tokens);

    private static uint[] ToTokens(TestToken[] tokens)
    {
        var result = new uint[tokens.Length << 1];
        for (int i = 0; i < tokens.Length; i++)
        {
            result[i << 1] = (uint)tokens[i].StartOffset;
            result[(i << 1) + 1] = (uint)tokens[i].Color << MetadataConsts.ForegroundOffset;
        }

        return result;
    }

    // Monaco compares the class name of each token; it encodes the foreground (no CSS here), so the foreground is compared.
    private static void AssertLineTokens(LineTokens actual, TestToken[] expected)
    {
        uint[] tmp = ToTokens(expected);
        LineTokens.ConvertToEndOffset(tmp, actual.GetLineContent().Length);
        var expectedTokens = new (int EndIndex, int Foreground)[tmp.Length >>> 1];
        for (int i = 0; i < expectedTokens.Length; i++)
        {
            expectedTokens[i] = ((int)tmp[i << 1], TokenMetadata.GetForeground(tmp[(i << 1) + 1]));
        }

        IViewLineTokens inflated = actual.Inflate();
        var actualTokens = new (int EndIndex, int Foreground)[inflated.GetCount()];
        for (int i = 0; i < actualTokens.Length; i++)
        {
            actualTokens[i] = (inflated.GetEndOffset(i), inflated.GetForeground(i));
        }

        Assert.Equal(expectedTokens, actualTokens);
    }

    private static (TextModel Model, ManualTokenizationSupport Support) CreateModel(string text)
    {
        var support = new ManualTokenizationSupport();
        var registry = new TokenizationRegistry();
        registry.Register("test", support);
        var model = new TextModel(text, DefaultEndOfLine.LF, null, registry, new LanguageIdCodec(), null);
        model.Tokenization.SetLanguageId("test");
        return (model, support);
    }

    private static void TestApplyEdits(BufferLineState[] initial, (Range Range, string Text)[] edits, BufferLineState[] expected)
    {
        string initialText = string.Join('\n', initial.Select(el => el.Text));

        (TextModel model, ManualTokenizationSupport support) = CreateModel(initialText);
        using IAttachedView view = model.AttachView();
        for (int lineIndex = 0; lineIndex < initial.Length; lineIndex++)
        {
            int lineTextLength = model.GetLineMaxColumn(lineIndex + 1) - 1;
            uint[] tokens = ToTokens(initial[lineIndex].Tokens);
            LineTokens.ConvertToEndOffset(tokens, lineTextLength);
            support.SetLineTokens(lineIndex + 1, tokens);
        }

        model.ApplyEdits([.. edits.Select(ed => new TextEdit(ed.Range, ed.Text))]);

        for (int lineIndex = 0; lineIndex < expected.Length; lineIndex++)
        {
            Assert.Equal(expected[lineIndex].Text, model.GetLineContent(lineIndex + 1));
            AssertLineTokens(model.Tokenization.GetLineTokens(lineIndex + 1), expected[lineIndex].Tokens);
        }
    }

    private static void TestLineEditTokens(
        string initialText,
        TestToken[] initialTokens,
        (int StartColumn, int EndColumn, string Text)[] edits,
        string expectedText,
        TestToken[] expectedTokens) =>
        TestApplyEdits(
            [new BufferLineState(initialText, initialTokens)],
            [.. edits.Select(ed => (new Range(1, ed.StartColumn, 1, ed.EndColumn), ed.Text))],
            [new BufferLineState(expectedText, expectedTokens)]);

    private static readonly TestToken[] HelloWorld123 = [new(0, 1), new(5, 2), new(6, 3)];

    private static BufferLineState HelloWorld(params TestToken[] tokens) => new("hello world", tokens);

    // Monaco: 'ModelLinesTokens' > 'single delete 1'
    [Fact]
    public void SingleDelete_1() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 1, 1, 2), string.Empty)],
            [new BufferLineState("ello world", [new(0, 1), new(4, 2), new(5, 3)])]);

    // Monaco: 'ModelLinesTokens' > 'single delete 2'
    [Fact]
    public void SingleDelete_2() =>
        TestApplyEdits(
            [new BufferLineState("helloworld", [new(0, 1), new(5, 2)])],
            [(new Range(1, 3, 1, 8), string.Empty)],
            [new BufferLineState("herld", [new(0, 1), new(2, 2)])]);

    // Monaco: 'ModelLinesTokens' > 'single delete 3'
    [Fact]
    public void SingleDelete_3() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 1, 1, 6), string.Empty)],
            [new BufferLineState(" world", [new(0, 2), new(1, 3)])]);

    // Monaco: 'ModelLinesTokens' > 'single delete 4'
    [Fact]
    public void SingleDelete_4() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 2, 1, 7), string.Empty)],
            [new BufferLineState("hworld", [new(0, 1), new(1, 3)])]);

    // Monaco: 'ModelLinesTokens' > 'single delete 5'
    [Fact]
    public void SingleDelete_5() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 1, 1, 12), string.Empty)],
            [new BufferLineState(string.Empty, [new(0, 1)])]);

    private static BufferLineState[] ThreeLines() =>
    [
        HelloWorld(new TestToken(0, 1), new TestToken(5, 2), new TestToken(6, 3)),
        HelloWorld(new TestToken(0, 4), new TestToken(5, 5), new TestToken(6, 6)),
        HelloWorld(new TestToken(0, 7), new TestToken(5, 8), new TestToken(6, 9)),
    ];

    // Monaco: 'ModelLinesTokens' > 'multi delete 6'
    [Fact]
    public void MultiDelete_6() =>
        TestApplyEdits(
            ThreeLines(),
            [(new Range(1, 6, 3, 6), string.Empty)],
            [HelloWorld(new TestToken(0, 1), new TestToken(5, 8), new TestToken(6, 9))]);

    // Monaco: 'ModelLinesTokens' > 'multi delete 7'
    [Fact]
    public void MultiDelete_7() =>
        TestApplyEdits(
            ThreeLines(),
            [(new Range(1, 12, 3, 12), string.Empty)],
            [HelloWorld(new TestToken(0, 1), new TestToken(5, 2), new TestToken(6, 3))]);

    // Monaco: 'ModelLinesTokens' > 'multi delete 8'
    [Fact]
    public void MultiDelete_8() =>
        TestApplyEdits(
            ThreeLines(),
            [(new Range(1, 1, 3, 1), string.Empty)],
            [HelloWorld(new TestToken(0, 7), new TestToken(5, 8), new TestToken(6, 9))]);

    // Monaco: 'ModelLinesTokens' > 'multi delete 9'
    [Fact]
    public void MultiDelete_9() =>
        TestApplyEdits(
            ThreeLines(),
            [(new Range(1, 12, 3, 1), string.Empty)],
            [new BufferLineState("hello worldhello world", [new(0, 1), new(5, 2), new(6, 3), new(11, 7), new(16, 8), new(17, 9)])]);

    // Monaco: 'ModelLinesTokens' > 'single insert 1'
    [Fact]
    public void SingleInsert_1() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 1, 1, 1), "xx")],
            [new BufferLineState("xxhello world", [new(0, 1), new(7, 2), new(8, 3)])]);

    // Monaco: 'ModelLinesTokens' > 'single insert 2'
    [Fact]
    public void SingleInsert_2() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 2, 1, 2), "xx")],
            [new BufferLineState("hxxello world", [new(0, 1), new(7, 2), new(8, 3)])]);

    // Monaco: 'ModelLinesTokens' > 'single insert 3'
    [Fact]
    public void SingleInsert_3() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 6, 1, 6), "xx")],
            [new BufferLineState("helloxx world", [new(0, 1), new(7, 2), new(8, 3)])]);

    // Monaco: 'ModelLinesTokens' > 'single insert 4'
    [Fact]
    public void SingleInsert_4() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 7, 1, 7), "xx")],
            [new BufferLineState("hello xxworld", [new(0, 1), new(5, 2), new(8, 3)])]);

    // Monaco: 'ModelLinesTokens' > 'single insert 5'
    [Fact]
    public void SingleInsert_5() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 12, 1, 12), "xx")],
            [new BufferLineState("hello worldxx", [new(0, 1), new(5, 2), new(6, 3)])]);

    // Monaco: 'ModelLinesTokens' > 'multi insert 6'
    [Fact]
    public void MultiInsert_6() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 1, 1, 1), "\n")],
            [new BufferLineState(string.Empty, [new(0, 1)]), new BufferLineState("hello world", [new(0, 1)])]);

    // Monaco: 'ModelLinesTokens' > 'multi insert 7'
    [Fact]
    public void MultiInsert_7() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 12, 1, 12), "\n")],
            [HelloWorld(HelloWorld123), new BufferLineState(string.Empty, [new(0, 1)])]);

    // Monaco: 'ModelLinesTokens' > 'multi insert 8'
    [Fact]
    public void MultiInsert_8() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123)],
            [(new Range(1, 7, 1, 7), "\n")],
            [new BufferLineState("hello ", [new(0, 1), new(5, 2)]), new BufferLineState("world", [new(0, 1)])]);

    // Monaco: 'ModelLinesTokens' > 'multi insert 9'
    [Fact]
    public void MultiInsert_9() =>
        TestApplyEdits(
            [HelloWorld(HelloWorld123), HelloWorld(new TestToken(0, 4), new TestToken(5, 5), new TestToken(6, 6))],
            [(new Range(1, 7, 1, 7), "xx\nyy")],
            [
                new BufferLineState("hello xx", [new(0, 1), new(5, 2)]),
                new BufferLineState("yyworld", [new(0, 1)]),
                HelloWorld(new TestToken(0, 4), new TestToken(5, 5), new TestToken(6, 6)),
            ]);

    // Monaco: 'ModelLinesTokens' > 'insertion on empty line'
    [Fact]
    public void InsertionOnEmptyLine()
    {
        (TextModel model, ManualTokenizationSupport support) = CreateModel("some text");
        uint[] tokens = ToTokens([new TestToken(0, 1)]);
        LineTokens.ConvertToEndOffset(tokens, model.GetLineMaxColumn(1) - 1);
        support.SetLineTokens(1, tokens);

        model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 10), string.Empty)]);

        support.SetLineTokens(1, []);

        model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "a")]);

        AssertLineTokens(model.Tokenization.GetLineTokens(1), [new TestToken(0, 1)]);
    }

    private static void Insertion(string initialText, TestToken[] initialTokens, int column, string text, string expectedText, TestToken[] expectedTokens) =>
        TestLineEditTokens(initialText, initialTokens, [(column, column, text)], expectedText, expectedTokens);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 1'
    [Fact]
    public void UpdatesTokensOnInsertion_1() =>
        Insertion("abcd efgh", [new(0, 1), new(4, 2), new(5, 3)], 1, "a", "aabcd efgh", [new(0, 1), new(5, 2), new(6, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 2'
    [Fact]
    public void UpdatesTokensOnInsertion_2() =>
        Insertion("aabcd efgh", [new(0, 1), new(5, 2), new(6, 3)], 2, "x", "axabcd efgh", [new(0, 1), new(6, 2), new(7, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 3'
    [Fact]
    public void UpdatesTokensOnInsertion_3() =>
        Insertion("axabcd efgh", [new(0, 1), new(6, 2), new(7, 3)], 3, "stu", "axstuabcd efgh", [new(0, 1), new(9, 2), new(10, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 4'
    [Fact]
    public void UpdatesTokensOnInsertion_4() =>
        Insertion("axstuabcd efgh", [new(0, 1), new(9, 2), new(10, 3)], 10, "\t", "axstuabcd\t efgh", [new(0, 1), new(10, 2), new(11, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 5'
    [Fact]
    public void UpdatesTokensOnInsertion_5() =>
        Insertion("axstuabcd\t efgh", [new(0, 1), new(10, 2), new(11, 3)], 12, "dd", "axstuabcd\t ddefgh", [new(0, 1), new(10, 2), new(13, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 6'
    [Fact]
    public void UpdatesTokensOnInsertion_6() =>
        Insertion("axstuabcd\t ddefgh", [new(0, 1), new(10, 2), new(13, 3)], 18, "xyz", "axstuabcd\t ddefghxyz", [new(0, 1), new(10, 2), new(13, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 7'
    [Fact]
    public void UpdatesTokensOnInsertion_7() =>
        Insertion("axstuabcd\t ddefghxyz", [new(0, 1), new(10, 2), new(13, 3)], 1, "x", "xaxstuabcd\t ddefghxyz", [new(0, 1), new(11, 2), new(14, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 8'
    [Fact]
    public void UpdatesTokensOnInsertion_8() =>
        Insertion("xaxstuabcd\t ddefghxyz", [new(0, 1), new(11, 2), new(14, 3)], 22, "x", "xaxstuabcd\t ddefghxyzx", [new(0, 1), new(11, 2), new(14, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 9'
    [Fact]
    public void UpdatesTokensOnInsertion_9() =>
        Insertion("xaxstuabcd\t ddefghxyzx", [new(0, 1), new(11, 2), new(14, 3)], 2, string.Empty, "xaxstuabcd\t ddefghxyzx", [new(0, 1), new(11, 2), new(14, 3)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on insertion 10'
    [Fact]
    public void UpdatesTokensOnInsertion_10() =>
        Insertion(string.Empty, [], 1, "a", "a", [new(0, 1)]);

    private static readonly TestToken[] Abcd123 = [new(0, 1), new(4, 2), new(5, 3)];

    private static void Edit(string text, TestToken[] tokens, int startColumn, int endColumn, string newText, string expectedText, TestToken[] expectedTokens) =>
        TestLineEditTokens(text, tokens, [(startColumn, endColumn, newText)], expectedText, expectedTokens);

    // Monaco: 'ModelLinesTokens' > 'delete second token 2'
    [Fact]
    public void DeleteSecondToken_2() =>
        Edit("abcdefghij", [new(0, 1), new(3, 2), new(6, 3)], 4, 7, string.Empty, "abcghij", [new(0, 1), new(3, 3)]);

    // Monaco: 'ModelLinesTokens' > 'insert right before second token'
    [Fact]
    public void InsertRightBeforeSecondToken() =>
        Edit("abcdefghij", [new(0, 1), new(3, 2), new(6, 3)], 4, 4, "hello", "abchellodefghij", [new(0, 1), new(8, 2), new(11, 3)]);

    // Monaco: 'ModelLinesTokens' > 'delete first char'
    [Fact]
    public void DeleteFirstChar() =>
        Edit("abcd efgh", Abcd123, 1, 2, string.Empty, "bcd efgh", [new(0, 1), new(3, 2), new(4, 3)]);

    // Monaco: 'ModelLinesTokens' > 'delete 2nd and 3rd chars'
    [Fact]
    public void Delete2ndAnd3rdChars() =>
        Edit("abcd efgh", Abcd123, 2, 4, string.Empty, "ad efgh", [new(0, 1), new(2, 2), new(3, 3)]);

    // Monaco: 'ModelLinesTokens' > 'delete first token'
    [Fact]
    public void DeleteFirstToken() =>
        Edit("abcd efgh", Abcd123, 1, 5, string.Empty, " efgh", [new(0, 2), new(1, 3)]);

    // Monaco: 'ModelLinesTokens' > 'delete second token'
    [Fact]
    public void DeleteSecondToken() =>
        Edit("abcd efgh", Abcd123, 5, 6, string.Empty, "abcdefgh", [new(0, 1), new(4, 3)]);

    // Monaco: 'ModelLinesTokens' > 'delete second token + a bit of the third one'
    [Fact]
    public void DeleteSecondTokenAndABitOfTheThirdOne() =>
        Edit("abcd efgh", Abcd123, 5, 7, string.Empty, "abcdfgh", [new(0, 1), new(4, 3)]);

    // Monaco: 'ModelLinesTokens' > 'delete second and third token'
    [Fact]
    public void DeleteSecondAndThirdToken() =>
        Edit("abcd efgh", Abcd123, 5, 10, string.Empty, "abcd", [new(0, 1)]);

    // Monaco: 'ModelLinesTokens' > 'delete everything'
    [Fact]
    public void DeleteEverything() =>
        Edit("abcd efgh", Abcd123, 1, 10, string.Empty, string.Empty, [new(0, 1)]);

    // Monaco: 'ModelLinesTokens' > 'noop'
    [Fact]
    public void Noop() =>
        Edit("abcd efgh", Abcd123, 1, 1, string.Empty, "abcd efgh", Abcd123);

    // Monaco: 'ModelLinesTokens' > 'equivalent to deleting first two chars'
    [Fact]
    public void EquivalentToDeletingFirstTwoChars() =>
        Edit("abcd efgh", Abcd123, 1, 3, string.Empty, "cd efgh", [new(0, 1), new(2, 2), new(3, 3)]);

    // Monaco: 'ModelLinesTokens' > 'equivalent to deleting from 5 to the end'
    [Fact]
    public void EquivalentToDeletingFrom5ToTheEnd() =>
        Edit("abcd efgh", Abcd123, 5, 10, string.Empty, "abcd", [new(0, 1)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on replace 1'
    [Fact]
    public void UpdatesTokensOnReplace_1() =>
        Edit(
            "Hello world, ciao",
            [new(0, 1), new(5, 0), new(6, 2), new(11, 0), new(13, 0)],
            1,
            6,
            "Hi",
            "Hi world, ciao",
            [new(0, 0), new(3, 2), new(8, 0), new(10, 0)]);

    // Monaco: 'ModelLinesTokens' > 'updates tokens on replace 2'
    [Fact]
    public void UpdatesTokensOnReplace_2() =>
        TestLineEditTokens(
            "Hello world, ciao",
            [new(0, 1), new(5, 0), new(6, 2), new(11, 0), new(13, 0)],
            [(1, 6, "Hi"), (8, 12, "my friends")],
            "Hi wmy friends, ciao",
            [new(0, 0), new(3, 2), new(14, 0), new(16, 0)]);

    private static void TestLineSplitTokens(
        string initialText,
        TestToken[] initialTokens,
        int splitColumn,
        string expectedText1,
        string expectedText2,
        TestToken[] expectedTokens) =>
        TestApplyEdits(
            [new BufferLineState(initialText, initialTokens)],
            [(new Range(1, splitColumn, 1, splitColumn), "\n")],
            [new BufferLineState(expectedText1, expectedTokens), new BufferLineState(expectedText2, [new(0, 1)])]);

    // Monaco: 'ModelLinesTokens' > 'split at the beginning'
    [Fact]
    public void SplitAtTheBeginning() =>
        TestLineSplitTokens("abcd efgh", Abcd123, 1, string.Empty, "abcd efgh", [new(0, 1)]);

    // Monaco: 'ModelLinesTokens' > 'split at the end'
    [Fact]
    public void SplitAtTheEnd() =>
        TestLineSplitTokens("abcd efgh", Abcd123, 10, "abcd efgh", string.Empty, Abcd123);

    // Monaco: 'ModelLinesTokens' > 'split inthe middle 1'
    [Fact]
    public void SplitInTheMiddle_1() =>
        TestLineSplitTokens("abcd efgh", Abcd123, 5, "abcd", " efgh", [new(0, 1)]);

    // Monaco: 'ModelLinesTokens' > 'split inthe middle 2'
    [Fact]
    public void SplitInTheMiddle_2() =>
        TestLineSplitTokens("abcd efgh", Abcd123, 6, "abcd ", "efgh", [new(0, 1), new(4, 2)]);

    private static void TestLineAppendTokens(
        string aText,
        TestToken[] aTokens,
        string bText,
        TestToken[] bTokens,
        string expectedText,
        TestToken[] expectedTokens) =>
        TestApplyEdits(
            [new BufferLineState(aText, aTokens), new BufferLineState(bText, bTokens)],
            [(new Range(1, aText.Length + 1, 2, 1), string.Empty)],
            [new BufferLineState(expectedText, expectedTokens)]);

    // Monaco: 'ModelLinesTokens' > 'append empty 1'
    [Fact]
    public void AppendEmpty_1() =>
        TestLineAppendTokens("abcd efgh", Abcd123, string.Empty, [], "abcd efgh", Abcd123);

    // Monaco: 'ModelLinesTokens' > 'append empty 2'
    [Fact]
    public void AppendEmpty_2() =>
        TestLineAppendTokens(string.Empty, [], "abcd efgh", Abcd123, "abcd efgh", Abcd123);

    // Monaco: 'ModelLinesTokens' > 'append 1'
    [Fact]
    public void Append_1() =>
        TestLineAppendTokens(
            "abcd efgh",
            Abcd123,
            "abcd efgh",
            [new(0, 4), new(4, 5), new(5, 6)],
            "abcd efghabcd efgh",
            [new(0, 1), new(4, 2), new(5, 3), new(9, 4), new(13, 5), new(14, 6)]);

    // Monaco: 'ModelLinesTokens' > 'append 2'
    [Fact]
    public void Append_2() =>
        TestLineAppendTokens("abcd ", [new(0, 1), new(4, 2)], "efgh", [new(0, 3)], "abcd efgh", Abcd123);

    // Monaco: 'ModelLinesTokens' > 'append 3'
    [Fact]
    public void Append_3() =>
        TestLineAppendTokens("abcd", [new(0, 1)], " efgh", [new(0, 2), new(1, 3)], "abcd efgh", Abcd123);
}
