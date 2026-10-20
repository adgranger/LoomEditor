using LoomEditor.Core.Model;
using Range = LoomEditor.Core.Text.Range;
using LoomEditor.TextMate.Tests.Tokenization.Worker.TestHelpers;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.TextMate.Tokenization.Worker;

namespace LoomEditor.TextMate.Tests.Tokenization.Worker;

public class TokenizationWorkerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30); // generous: the worker runs at BelowNormal priority and can starve while the machine is busy

    [Fact]
    public void Worker_ProcessesMessagesInOrder_AndStopsOnDispose()
    {
        LanguageRegistry languages = WorkerTestSupport.CreateLanguages();
        int encoded = languages.Codec.EncodeLanguageId(WorkerTestSupport.LanguageId);
        var host = new RecordingHost();
        int factoryThread = -1;
        TMGrammarFactory? factory = null;
        var worker = new TokenizationWorker(
            () =>
            {
                factoryThread = Environment.CurrentManagedThreadId;
                factory = WorkerTestSupport.CreateFactory(languages);
                return factory;
            },
            host);

        try
        {
            // New model (3 lines), then an edit of line 2 that makes it 2 lines long: both are posted before any tokenization.
            worker.Post(new NewModelMessage(1, WorkerTestSupport.Lines(3), "\n", 1, WorkerTestSupport.LanguageId, encoded, 20_000));
            worker.Post(new ModelChangedMessage(1, new MirrorModelChangedEvent([new ModelContentChange(new Range(2, 1, 2, 8), 0, 0, "foo\nbar")], string.Empty, 2)));

            Assert.True(SpinWait.SpinUntil(() => host.Batches.Any(b => b.VersionId == 2 && b.Tokens.Any(t => t.EndLineNumber == 4)), Timeout));
            Batch last = host.Batches[^1];
            Assert.Equal(1, last.ControllerId);
            Assert.Equal(2, last.VersionId);
            Assert.NotEqual(Environment.CurrentManagedThreadId, factoryThread);

            // Messages for a model that was removed are ignored (and do not stop the loop).
            worker.Post(new RemovedModelMessage(1));
            worker.Post(new ModelChangedMessage(1, new MirrorModelChangedEvent([], string.Empty, 3)));
            int count = host.Batches.Count;

            worker.Post(new NewModelMessage(2, WorkerTestSupport.Lines(1), "\n", 1, WorkerTestSupport.LanguageId, encoded, 20_000));
            Assert.True(SpinWait.SpinUntil(() => host.Batches.Count > count && host.Batches[^1].ControllerId == 2, Timeout));
        }
        finally
        {
            worker.Dispose();
        }

        Assert.NotNull(factory);
        worker.Post(new NewModelMessage(3, WorkerTestSupport.Lines(1), "\n", 1, WorkerTestSupport.LanguageId, encoded, 20_000));
        Thread.Sleep(50);
        Assert.DoesNotContain(host.Batches, b => b.ControllerId == 3); // stopped: nothing is tokenized anymore
    }
}
