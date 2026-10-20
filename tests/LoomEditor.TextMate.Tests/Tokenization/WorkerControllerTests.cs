using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tokens;
using LoomEditor.TextMate.Tests.TestHelpers;
using LoomEditor.TextMate.Tests.Tokenization.Worker.TestHelpers;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.TextMate.Tokenization.Worker;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.TextMate.Tests.Tokenization;

// No upstream unit tests: textMateWorkerTokenizerController.ts is only covered by integration tests in VS Code.
public class WorkerControllerTests
{
    // Where the controller delivers its results.
    private sealed class RecordingStore : IBackgroundTokenizationStore
    {
        public List<ContiguousMultilineTokens> Tokens { get; } = [];

        public Dictionary<int, IState> EndStates { get; } = [];

        public int FinishedCount { get; private set; }

        public void SetTokens(IReadOnlyList<ContiguousMultilineTokens> tokens) => Tokens.AddRange(tokens);

        public void SetEndState(int lineNumber, IState state) => EndStates[lineNumber] = state;

        public void BackgroundTokenizationFinished() => FinishedCount++;
    }

    // Runs the messages of the controller synchronously, but only tokenizes when the test says so.
    private sealed class SynchronousWorker(ITextMateWorkerHost host, Func<string, int, CreateGrammarResult?> grammars) : ITokenizationWorker
    {
        private readonly Dictionary<int, TextMateWorkerTokenizer> _models = [];

        public bool IsDisposed { get; private set; }

        public void Post(WorkerMessage message)
        {
            switch (message)
            {
                case NewModelMessage m:
                    _models[m.ControllerId] = new TextMateWorkerTokenizer([.. m.Lines], m.Eol, m.VersionId, host, m.ControllerId, grammars, m.LanguageId, m.EncodedLanguageId, m.MaxTokenizationLineLength);
                    break;
                case ModelChangedMessage m:
                    _models[m.ControllerId].AcceptEvents(m.Event);
                    break;
                case RetokenizeMessage m:
                    _models[m.ControllerId].Retokenize(m.StartLineNumber, m.EndLineNumberExclusive);
                    break;
                case RemovedModelMessage m:
                    _models.Remove(m.ControllerId);
                    break;
            }
        }

        public bool RunSlice() => _models.Values.Aggregate(false, (more, m) => m.TokenizeSlice(TokenizationWorker.MaxLinesPerSlice, 60_000) | more);

        public void RunToCompletion()
        {
            while (RunSlice())
            {
            }
        }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TMGrammarFactory _grammarFactory;

        public Fixture(int lineCount, string text = "foo bar")
        {
            LanguageRegistry languages = WorkerTestSupport.CreateLanguages();
            _grammarFactory = WorkerTestSupport.CreateFactory(languages);
            Model = new TextModel(string.Join('\n', Enumerable.Repeat(text, lineCount)), DefaultEndOfLine.LF, null, new TokenizationRegistry(), languages.Codec, null);
            Model.Tokenization.SetLanguageId(WorkerTestSupport.LanguageId);
            Factory = new ThreadedBackgroundTokenizerFactory(
                Dispatcher,
                languages.Codec,
                (_, host) =>
                {
                    WorkerCreations++;
                    return Worker = new SynchronousWorker(host, (id, enc) => _grammarFactory.CreateGrammar(id, enc));
                });
        }

        public TextModel Model { get; }

        public ManualUiDispatcher Dispatcher { get; } = new();

        public RecordingStore Store { get; } = new();

        public ThreadedBackgroundTokenizerFactory Factory { get; }

        public SynchronousWorker Worker { get; private set; } = null!;

        public int WorkerCreations { get; private set; }

        // The tokenizer of an attached model (the controller only exists while the model is attached).
        public IBackgroundTokenizer CreateAttachedTokenizer()
        {
            Model.AttachView();
            return Factory.CreateBackgroundTokenizer(Model, Store)!;
        }

        public void Dispose()
        {
            Factory.Dispose();
            _grammarFactory.Dispose();
        }
    }

    private static void Insert(TextModel model, int line, int column, string text) =>
        model.ApplyEdits([new LoomEditor.Core.Text.TextEdit(new Range(line, column, line, column), text)]);

    [Fact]
    public void Results_ForCurrentVersion_AreStored()
    {
        using var f = new Fixture(5);
        f.CreateAttachedTokenizer();

        f.Worker.RunToCompletion();
        f.Dispatcher.RunAll();

        ContiguousMultilineTokens tokens = Assert.Single(f.Store.Tokens);
        Assert.Equal((1, 5), (tokens.StartLineNumber, tokens.EndLineNumber));
        Assert.Equal([1, 2, 3, 4, 5], f.Store.EndStates.Keys.Order());
    }

    [Fact]
    public void Results_AfterAnEdit_AreRebased()
    {
        using var f = new Fixture(5);
        f.CreateAttachedTokenizer();
        f.Worker.RunToCompletion();

        // A line is inserted above the batch before the batch is delivered: line 1 is touched, lines 2-5 become 3-6.
        Insert(f.Model, 1, 1, "x\n");
        f.Dispatcher.RunAll();

        ContiguousMultilineTokens tokens = Assert.Single(f.Store.Tokens);
        Assert.Equal((3, 6), (tokens.StartLineNumber, tokens.EndLineNumber));
        Assert.Equal([3, 4, 5, 6], f.Store.EndStates.Keys.Order());
    }

    [Fact]
    public void Results_ForEditedLines_AreDropped()
    {
        using var f = new Fixture(5);
        f.CreateAttachedTokenizer();
        f.Worker.RunToCompletion();

        Insert(f.Model, 3, 1, "zz"); // line 3 will be tokenized again by the worker
        f.Dispatcher.RunAll();

        Assert.Equal([(1, 2), (4, 5)], f.Store.Tokens.Select(t => (t.StartLineNumber, t.EndLineNumber)));
        Assert.Equal([1, 2, 4, 5], f.Store.EndStates.Keys.Order());
    }

    [Fact]
    public void Results_AfterTheWorkerSawTheEdit_AreStoredForTheCurrentVersion()
    {
        using var f = new Fixture(5);
        f.CreateAttachedTokenizer();
        f.Worker.RunToCompletion();
        f.Dispatcher.RunAll();
        f.Store.Tokens.Clear();

        Insert(f.Model, 2, 1, "zz");
        f.Worker.RunToCompletion(); // the worker tokenizes line 2 for version 2
        f.Dispatcher.RunAll();

        ContiguousMultilineTokens tokens = Assert.Single(f.Store.Tokens);
        Assert.Equal((2, 2), (tokens.StartLineNumber, tokens.EndLineNumber));
    }

    [Fact]
    public void Results_ForDisposedController_AreIgnored()
    {
        using var f = new Fixture(5);
        IBackgroundTokenizer tokenizer = f.CreateAttachedTokenizer();
        f.Worker.RunToCompletion();

        tokenizer.Dispose(); // the model is detached before the batch arrives
        f.Dispatcher.RunAll();

        Assert.Empty(f.Store.Tokens);
        Assert.Empty(f.Store.EndStates);
        Assert.Equal(0, f.Factory.ControllerCount);
    }

    [Fact]
    public void LastLineReceived_MarksBackgroundTokenizationFinished()
    {
        using var f = new Fixture(250);
        f.CreateAttachedTokenizer();

        f.Worker.RunSlice(); // lines 1-200
        f.Dispatcher.RunAll();
        Assert.Equal(0, f.Store.FinishedCount);

        f.Worker.RunSlice(); // lines 201-250
        f.Dispatcher.RunAll();
        Assert.True(f.Store.FinishedCount > 0);
    }

    [Fact]
    public void TooLargeForSyncing_UsesNoWorker()
    {
        int limit = TextModel.ModelSyncLimit;
        TextModel.ModelSyncLimit = 10;
        try
        {
            using var f = new Fixture(5); // 39 characters

            Assert.True(f.Model.IsTooLargeForSyncing);
            Assert.Null(f.Factory.CreateBackgroundTokenizer(f.Model, f.Store));
            Assert.Equal(0, f.WorkerCreations);
        }
        finally
        {
            TextModel.ModelSyncLimit = limit;
        }
    }

    [Fact]
    public void Controller_ExistsOnlyWhileTheModelIsAttached()
    {
        using var f = new Fixture(3);
        IBackgroundTokenizer tokenizer = Assert.IsAssignableFrom<IBackgroundTokenizer>(f.Factory.CreateBackgroundTokenizer(f.Model, f.Store));
        Assert.Equal(0, f.Factory.ControllerCount);

        IAttachedView view = f.Model.AttachView();
        Assert.Equal(1, f.Factory.ControllerCount);

        view.Dispose();
        Assert.Equal(0, f.Factory.ControllerCount);
        tokenizer.Dispose();
    }

    [Fact]
    public void SetGrammarDefinitions_DisposesTheWorker_AndTheNextModelGetsANewOne()
    {
        using var f = new Fixture(3);
        f.CreateAttachedTokenizer();
        SynchronousWorker first = f.Worker;

        f.Factory.SetGrammarDefinitions([]);

        Assert.True(first.IsDisposed);
        Assert.Equal(0, f.Factory.ControllerCount);
        f.Factory.CreateBackgroundTokenizer(f.Model, f.Store);
        Assert.Equal(2, f.WorkerCreations);
    }
}
