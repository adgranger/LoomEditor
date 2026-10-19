using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tests.Model.Tokens.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class DefaultBackgroundTokenizerTests
{
    private sealed class PlainState : IState
    {
        public IState Clone() => this;

        public bool Equals(IState? other) => other is PlainState;
    }

    private sealed class PlainSupport : ITokenizationSupport
    {
        public IState GetInitialState() => new PlainState();

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
            new([0, 0], new PlainState());
    }

    private sealed class ModelInfo : ITokenizationModelInfo
    {
        public string LanguageId => "plaintext";

        public bool IsAttachedToEditor => true;
    }

    private sealed class RecordingStore : IBackgroundTokenizationStore
    {
        public List<int> TokenizedLines { get; } = [];

        public int FinishedCount { get; private set; }

        public void SetTokens(IReadOnlyList<ContiguousMultilineTokens> tokens)
        {
            foreach (ContiguousMultilineTokens block in tokens)
            {
                for (int line = block.StartLineNumber; line <= block.EndLineNumber; line++)
                {
                    TokenizedLines.Add(line);
                }
            }
        }

        public void SetEndState(int lineNumber, IState state)
        {
        }

        public void BackgroundTokenizationFinished() => FinishedCount++;
    }

    [Fact]
    public void DefaultBackgroundTokenizer_RunsInIdleSlices_UntilFinished()
    {
        var model = new TextModel(string.Join('\n', Enumerable.Range(1, 200).Select(i => $"line {i}")));
        var tokenizer = new TokenizerWithStateStoreAndTextModel(
            model.LineCount, new PlainSupport(), model, new ModelInfo(), new LanguageIdCodec());
        var store = new RecordingStore();
        var scheduler = new ManualTokenizationScheduler();
        using var background = new DefaultBackgroundTokenizer(tokenizer, store, scheduler);

        background.HandleChanges();
        background.HandleChanges(); // already scheduled: no second callback
        Assert.Equal(1, scheduler.PendingIdleCount);

        // an expired deadline: each idle slice tokenizes for ~1ms, then asks for a new idle slice
        int slices = 0;
        while (scheduler.PendingIdleCount > 0 && slices < 10_000)
        {
            scheduler.RunIdle(0);
            slices++;
        }

        Assert.True(tokenizer.Store.AllStatesValid());
        Assert.Equal(Enumerable.Range(1, 200), store.TokenizedLines.Order());
        Assert.Equal(1, store.FinishedCount);
        Assert.Equal(0, scheduler.PendingIdleCount);
    }
}
