using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;
using Range = LoomEditor.Core.Text.Range;
using LoomEditor.TextMate.Tests.Tokenization.Worker.TestHelpers;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.TextMate.Tokenization.Worker;

namespace LoomEditor.TextMate.Tests.Tokenization.Worker;

// No upstream unit tests: textMateWorkerTokenizer.ts is only covered by integration tests in VS Code.
public class TextMateWorkerTokenizerTests
{
    private const int Controller = 7;

    private static (TextMateWorkerTokenizer Tokenizer, RecordingHost Host, TMGrammarFactory Factory) Create(int lineCount, int maxLineLength = 20_000)
    {
        LanguageRegistry languages = WorkerTestSupport.CreateLanguages();
        TMGrammarFactory factory = WorkerTestSupport.CreateFactory(languages);
        var host = new RecordingHost();
        int encoded = languages.Codec.EncodeLanguageId(WorkerTestSupport.LanguageId);
        var tokenizer = new TextMateWorkerTokenizer(
            WorkerTestSupport.Lines(lineCount),
            "\n",
            1,
            host,
            Controller,
            (id, enc) => factory.CreateGrammar(id, enc),
            WorkerTestSupport.LanguageId,
            encoded,
            maxLineLength);
        return (tokenizer, host, factory);
    }

    private static ModelContentChange Insert(int line, int column, string text) =>
        new(new Range(line, column, line, column), 0, 0, text);

    [Fact]
    public void TokenizeSlice_StopsAfter200Lines_AndSendsOneBatch()
    {
        (TextMateWorkerTokenizer tokenizer, RecordingHost host, TMGrammarFactory factory) = Create(250);
        using TMGrammarFactory _ = factory;

        bool more = tokenizer.TokenizeSlice(200, 60_000);

        Assert.True(more);
        Batch batch = Assert.Single(host.Batches);
        Assert.Equal(Controller, batch.ControllerId);
        Assert.Equal(1, batch.VersionId);
        ContiguousMultilineTokens tokens = Assert.Single(batch.Tokens);
        Assert.Equal(1, tokens.StartLineNumber);
        Assert.Equal(200, tokens.EndLineNumber);
        StateDeltas deltas = Assert.Single(batch.StateDeltas);
        Assert.Equal(1, deltas.StartLineNumber);
        Assert.Equal(200, deltas.States.Count);

        Assert.False(tokenizer.TokenizeSlice(200, 60_000));
        Assert.Equal(2, host.Batches.Count);
        Assert.Equal(201, host.Batches[1].Tokens[0].StartLineNumber);
        Assert.Equal(250, host.Batches[1].Tokens[0].EndLineNumber);
        Assert.False(tokenizer.HasInvalidLines);
    }

    [Fact]
    public void TokenizeSlice_StateUnchanged_SendsNullDelta()
    {
        (TextMateWorkerTokenizer tokenizer, RecordingHost host, TMGrammarFactory factory) = Create(5);
        using TMGrammarFactory _ = factory;
        tokenizer.TokenizeSlice(200, 60_000);
        Assert.NotNull(host.Batches[0].StateDeltas[0].States[0]); // the first end state is a change

        tokenizer.Retokenize(3, 4);
        tokenizer.TokenizeSlice(200, 60_000);

        StateDeltas deltas = Assert.Single(host.Batches[1].StateDeltas);
        Assert.Equal(3, deltas.StartLineNumber);
        Assert.Equal([null], deltas.States);
    }

    [Fact]
    public void AcceptEvents_EditInvalidatesFromEditedLine()
    {
        (TextMateWorkerTokenizer tokenizer, RecordingHost host, TMGrammarFactory factory) = Create(6);
        using TMGrammarFactory _ = factory;
        tokenizer.TokenizeSlice(200, 60_000);
        Assert.False(tokenizer.HasInvalidLines);

        tokenizer.AcceptEvents(new MirrorModelChangedEvent([Insert(4, 1, "foo ")], string.Empty, 2));

        Assert.True(tokenizer.HasInvalidLines);
        Assert.False(tokenizer.TokenizeSlice(200, 60_000));
        Batch batch = host.Batches[1];
        Assert.Equal(2, batch.VersionId);
        Assert.Equal(4, batch.Tokens[0].StartLineNumber);
        Assert.Equal(4, batch.Tokens[0].EndLineNumber);
        Assert.Equal("foo foo bar", tokenizer.GetText().Split('\n')[3]);
    }

    [Fact]
    public void Retokenize_InvalidatesTheRange()
    {
        (TextMateWorkerTokenizer tokenizer, RecordingHost host, TMGrammarFactory factory) = Create(8);
        using TMGrammarFactory _ = factory;
        tokenizer.TokenizeSlice(200, 60_000);

        tokenizer.Retokenize(2, 5);

        Assert.True(tokenizer.HasInvalidLines);
        tokenizer.TokenizeSlice(200, 60_000);
        ContiguousMultilineTokens tokens = Assert.Single(host.Batches[1].Tokens);
        Assert.Equal(2, tokens.StartLineNumber);
        Assert.Equal(4, tokens.EndLineNumber);
        Assert.False(tokenizer.HasInvalidLines);
    }

    [Fact]
    public void TokenizeSlice_LanguageWithoutGrammar_DoesNothing()
    {
        var host = new RecordingHost();
        var tokenizer = new TextMateWorkerTokenizer(WorkerTestSupport.Lines(3), "\n", 1, host, Controller, (_, _) => null, "plaintext", 1, 20_000);

        Assert.False(tokenizer.HasInvalidLines);
        Assert.False(tokenizer.TokenizeSlice(200, 60_000));
        Assert.Empty(host.Batches);
    }
}
