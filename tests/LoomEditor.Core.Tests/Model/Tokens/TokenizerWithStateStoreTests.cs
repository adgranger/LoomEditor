using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class TokenizerWithStateStoreTests
{
    /// <summary>The state counts the lines tokenized so far; each token's foreground is that count (the line number).</summary>
    private sealed class CountingState(int count) : IState
    {
        public int Count { get; } = count;

        public IState Clone() => new CountingState(Count);

        public bool Equals(IState? other) => other is CountingState s && s.Count == Count;
    }

    private sealed class CountingSupport : ITokenizationSupport
    {
        public IState GetInitialState() => new CountingState(1);

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            int n = ((CountingState)state).Count;
            return new EncodedTokenizationResult([0, (uint)n << MetadataConsts.ForegroundOffset], new CountingState(n + 1));
        }
    }

    private sealed class ThrowingSupport : ITokenizationSupport
    {
        public IState GetInitialState() => new CountingState(1);

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class ModelInfo : ITokenizationModelInfo
    {
        public string LanguageId => "plaintext";

        public bool IsAttachedToEditor { get; set; } = true;
    }

    private static TokenizerWithStateStoreAndTextModel Create(TextModel model, ITokenizationSupport support) =>
        new(model.LineCount, support, model, new ModelInfo(), new LanguageIdCodec());

    private static int ForegroundOf(ContiguousMultilineTokens block, int lineNumber) =>
        TokenMetadata.GetForeground(block.GetLineTokens(lineNumber)![1]);

    [Fact]
    public void UpdateTokensUntilLine_TokenizesFromFirstInvalidLine()
    {
        var model = new TextModel("a\nb\nc\nd\ne");
        TokenizerWithStateStoreAndTextModel tokenizer = Create(model, new CountingSupport());

        var first = new ContiguousMultilineTokensBuilder();
        tokenizer.UpdateTokensUntilLine(first, 3);
        var second = new ContiguousMultilineTokensBuilder();
        tokenizer.UpdateTokensUntilLine(second, 5);

        ContiguousMultilineTokens block1 = Assert.Single(first.Finalize());
        Assert.Equal((1, 3), (block1.StartLineNumber, block1.EndLineNumber));
        Assert.Equal(2, ForegroundOf(block1, 2));
        ContiguousMultilineTokens block2 = Assert.Single(second.Finalize());
        Assert.Equal((4, 5), (block2.StartLineNumber, block2.EndLineNumber));
        Assert.Equal(4, ForegroundOf(block2, 4)); // continues from the state of line 3
        Assert.True(tokenizer.Store.AllStatesValid());
    }

    [Fact]
    public void TokenizeHeuristically_UsesGuessedStartState_WhenFarFromValidStates()
    {
        var model = new TextModel("a\n  b\n    c\n    d");
        TokenizerWithStateStoreAndTextModel tokenizer = Create(model, new CountingSupport());
        var builder = new ContiguousMultilineTokensBuilder();

        bool heuristic = tokenizer.TokenizeHeuristically(builder, 4, 4);

        Assert.True(heuristic);
        ContiguousMultilineTokens block = Assert.Single(builder.Finalize());
        // the guessed state comes from the less indented lines "a" and "  b" (2 lines tokenized: count 3)
        Assert.Equal(3, ForegroundOf(block, 4));
        Assert.Equal(1, tokenizer.Store.GetFirstInvalidEndStateLineNumber()); // nothing was cached
    }

    [Fact]
    public void SafeTokenize_SupportThrows_ReturnsNullTokens()
    {
        var codec = new LanguageIdCodec();
        var state = new CountingState(7);

        EncodedTokenizationResult r = TokenizerWithStateStoreAndTextModel.SafeTokenize(
            codec, "plaintext", new ThrowingSupport(), "hello", true, state);

        Assert.Equal(2, r.Tokens.Length);
        Assert.Equal(5u, r.Tokens[0]);
        Assert.Equal(LanguageId.PlainText, TokenMetadata.GetLanguageId(r.Tokens[1]));
        Assert.Same(state, r.EndState);
    }
}
