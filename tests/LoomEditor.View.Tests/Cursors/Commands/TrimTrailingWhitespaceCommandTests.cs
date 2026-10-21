using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Cursors.Commands;

// Monaco: suite 'Editor Commands - Trim Trailing Whitespace Command'
public class TrimTrailingWhitespaceCommandTests
{
    // Monaco: createInsertDeleteSingleEditOp and createSingleEditOp (a null text, no moved markers)
    private static (Range Range, string? Text) Op(int positionLineNumber, int positionColumn, int selectionLineNumber, int selectionColumn) =>
        (new Range(selectionLineNumber, selectionColumn, positionLineNumber, positionColumn), null);

    // Monaco: assertTrimTrailingWhitespaceCommand
    private static void AssertTrimTrailingWhitespaceCommand(string[] text, (Range Range, string? Text)[] expected)
    {
        using var services = new TestEditorServices();
        TextModel model = services.CreateTextModel(string.Join('\n', text));
        var op = new TrimTrailingWhitespaceCommand(new Selection(1, 1, 1, 1), [], true);
        var actual = CommandTester.GetEditOperation(model, op);
        Assert.Equal(expected, actual.Select(a => (a.Range, a.Text)));
        Assert.All(actual, a => Assert.False(a.ForceMoveMarkers));
    }

    // Monaco: assertTrimTrailingWhitespace
    private static void AssertTrimTrailingWhitespace(string[] text, Position[] cursors, (Range Range, string? Text)[] expected)
    {
        using var services = new TestEditorServices();
        TextModel model = services.CreateTextModel(string.Join('\n', text));
        Assert.Equal(expected, TrimTrailingWhitespaceCommand.TrimTrailingWhitespace(model, [.. cursors], true));
    }

    // Monaco: 'remove trailing whitespace'
    [Fact]
    public void RemoveTrailingWhitespace()
    {
        AssertTrimTrailingWhitespaceCommand([string.Empty], []);
        AssertTrimTrailingWhitespaceCommand(["text"], []);
        AssertTrimTrailingWhitespaceCommand(["text   "], [Op(1, 5, 1, 8)]);
        AssertTrimTrailingWhitespaceCommand(["text\t   "], [Op(1, 5, 1, 9)]);
        AssertTrimTrailingWhitespaceCommand(["\t   "], [Op(1, 1, 1, 5)]);
        AssertTrimTrailingWhitespaceCommand(["text\t"], [Op(1, 5, 1, 6)]);
        AssertTrimTrailingWhitespaceCommand(
            ["some text\t", "some more text", "\t  ", "even more text  ", "and some mixed\t   \t"],
            [Op(1, 10, 1, 11), Op(3, 1, 3, 4), Op(4, 15, 4, 17), Op(5, 15, 5, 20)]);

        AssertTrimTrailingWhitespace(["text   "], [new Position(1, 1), new Position(1, 2), new Position(1, 3)], [Op(1, 5, 1, 8)]);
        AssertTrimTrailingWhitespace(["text   "], [new Position(1, 1), new Position(1, 5)], [Op(1, 5, 1, 8)]);
        AssertTrimTrailingWhitespace(["text   "], [new Position(1, 1), new Position(1, 5), new Position(1, 6)], [Op(1, 6, 1, 8)]);
        AssertTrimTrailingWhitespace(
            ["some text\t", "some more text", "\t  ", "even more text  ", "and some mixed\t   \t"],
            [],
            [Op(1, 10, 1, 11), Op(3, 1, 3, 4), Op(4, 15, 4, 17), Op(5, 15, 5, 20)]);
        AssertTrimTrailingWhitespace(
            ["some text\t", "some more text", "\t  ", "even more text  ", "and some mixed\t   \t"],
            [new Position(1, 11), new Position(3, 2), new Position(5, 1), new Position(4, 1), new Position(5, 10)],
            [Op(3, 2, 3, 4), Op(4, 15, 4, 17), Op(5, 15, 5, 20)]);
    }

    private sealed class LineTokenizer(uint other, uint @string) : ITokenizationSupport
    {
        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) => line switch
        {
            "const a = `  " => new EncodedTokenizationResult([0, other, 10, @string], state),
            "  a string  " => new EncodedTokenizationResult([0, @string], state),
            "`;  " => new EncodedTokenizationResult([0, @string, 1, other], state),
            _ => throw new InvalidOperationException("Unexpected"),
        };
    }

    // Monaco: 'skips strings and regex if configured'
    [Fact]
    public void SkipsStringsAndRegexIfConfigured()
    {
        using var services = new TestEditorServices();
        const string languageId = "testLanguageId";
        services.RegisterLanguage(languageId);
        uint encodedLanguageId = (uint)services.LanguageIdCodec.EncodeLanguageId(languageId);

        uint otherMetadata =
            (encodedLanguageId << MetadataConsts.LanguageIdOffset)
            | ((uint)StandardTokenType.Other << MetadataConsts.TokenTypeOffset)
            | MetadataConsts.BalancedBracketsMask;
        uint stringMetadata =
            (encodedLanguageId << MetadataConsts.LanguageIdOffset)
            | ((uint)StandardTokenType.String << MetadataConsts.TokenTypeOffset)
            | MetadataConsts.BalancedBracketsMask;

        services.Add(services.TokenizationRegistry.Register(languageId, new LineTokenizer(otherMetadata, stringMetadata)));

        TextModel model = services.CreateTextModel(string.Join('\n', "const a = `  ", "  a string  ", "`;  "), languageId);

        model.Tokenization.ForceTokenization(1);
        model.Tokenization.ForceTokenization(2);
        model.Tokenization.ForceTokenization(3);

        var op = new TrimTrailingWhitespaceCommand(new Selection(1, 1, 1, 1), [], false);
        var actual = CommandTester.GetEditOperation(model, op);

        Assert.Equal([Op(3, 3, 3, 5)], actual.Select(a => (a.Range, a.Text)));
    }

    [Fact]
    public void TrimTrailingWhitespace_DoesNotSortTheCallersList()
    {
        using var services = new TestEditorServices();
        TextModel model = services.CreateTextModel("a \nb ");
        List<Position> cursors = [new Position(2, 1), new Position(2, 2), new Position(1, 1)];

        TrimTrailingWhitespaceCommand.TrimTrailingWhitespace(model, cursors, true);

        Assert.Equal([new Position(2, 1), new Position(2, 2), new Position(1, 1)], cursors);
    }
}
