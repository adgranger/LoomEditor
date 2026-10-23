using LoomEditor.Core.Threading;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Tests.Highlighting.TestHelpers;

namespace LoomEditor.View.Tests.Features.Scheduling;

// The continuation is specified by the card: all tests are new.
public class UiContinuationTests
{
    private sealed class RefusingDispatcher : IUiDispatcher
    {
        public bool Post(Action action) => false;
    }

    [Fact]
    public void Then_Success_PostsTheResult()
    {
        var dispatcher = new ManualUiDispatcher();
        var results = new List<int>();

        UiContinuation.Then(Task.FromResult(42), dispatcher, results.Add, token: CancellationToken.None);

        Assert.Empty(results);
        dispatcher.RunAll();
        Assert.Equal([42], results);
    }

    [Fact]
    public void Then_Faulted_CallsOnError()
    {
        var dispatcher = new ManualUiDispatcher();
        Exception? error = null;
        var boom = new InvalidOperationException("boom");

        UiContinuation.Then(Task.FromException<int>(boom), dispatcher, _ => Assert.Fail("no result"), e => error = e, token: CancellationToken.None);
        dispatcher.RunAll();

        Assert.Same(boom, Assert.IsType<AggregateException>(error).InnerException);
    }

    [Fact]
    public void Then_Faulted_NoOnError_DoesNothing()
    {
        var dispatcher = new ManualUiDispatcher();

        UiContinuation.Then(Task.FromException<int>(new InvalidOperationException()), dispatcher, _ => Assert.Fail("no result"), token: CancellationToken.None);
        dispatcher.RunAll();

        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public void Then_CancelledTask_CallsNothing()
    {
        var dispatcher = new ManualUiDispatcher();

        UiContinuation.Then(Task.FromCanceled<int>(new CancellationToken(true)), dispatcher, _ => Assert.Fail("no result"), _ => Assert.Fail("no error"), token: CancellationToken.None);
        dispatcher.RunAll();

        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public void Then_OperationCanceledFault_CallsNothing()
    {
        var dispatcher = new ManualUiDispatcher();

        UiContinuation.Then(Task.FromException<int>(new OperationCanceledException()), dispatcher, _ => Assert.Fail("no result"), _ => Assert.Fail("no error"), token: CancellationToken.None);
        dispatcher.RunAll();

        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public void Then_TokenCancelledBeforeCompletion_CallsNothing()
    {
        var dispatcher = new ManualUiDispatcher();
        using var cts = new CancellationTokenSource();
        var source = new TaskCompletionSource<int>();
        UiContinuation.Then(source.Task, dispatcher, _ => Assert.Fail("no result"), _ => Assert.Fail("no error"), token: cts.Token);

        cts.Cancel();
        source.SetResult(1);
        dispatcher.RunAll();
    }

    [Fact]
    public void Then_TokenCancelledAfterCompletionBeforeRunAll_CallsNothing()
    {
        var dispatcher = new ManualUiDispatcher();
        using var cts = new CancellationTokenSource();
        UiContinuation.Then(Task.FromResult(1), dispatcher, _ => Assert.Fail("no result"), token: cts.Token);

        cts.Cancel();
        dispatcher.RunAll();
    }

    [Fact]
    public void Then_PostRefused_DropsTheResult()
    {
        UiContinuation.Then(Task.FromResult(1), new RefusingDispatcher(), _ => Assert.Fail("no result"), token: CancellationToken.None);
    }

    [Fact]
    public void Then_CompletedFromAnotherThread_CallbackRunsOnRunAllThread()
    {
        var dispatcher = new ManualUiDispatcher();
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        int callbackThread = -1;
        UiContinuation.Then(source.Task, dispatcher, _ => callbackThread = Environment.CurrentManagedThreadId, token: CancellationToken.None);

        Task.Run(() => source.SetResult(1), TestContext.Current.CancellationToken);
        SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, TimeSpan.FromSeconds(5));
        Assert.Equal(1, dispatcher.PendingCount);
        dispatcher.RunAll();

        Assert.Equal(Environment.CurrentManagedThreadId, callbackThread);
    }

    [Fact]
    public void Then_InlineDispatcher_RunsSynchronouslyForCompletedTask()
    {
        int result = 0;

        UiContinuation.Then(Task.FromResult(7), InlineUiDispatcher.Instance, r => result = r, token: CancellationToken.None);

        Assert.Equal(7, result);
    }

    [Fact]
    public void Then_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => UiContinuation.Then<int>(null!, InlineUiDispatcher.Instance, _ => { }, token: TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() => UiContinuation.Then(Task.FromResult(1), null!, _ => { }, token: TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() => UiContinuation.Then(Task.FromResult(1), InlineUiDispatcher.Instance, null!, token: TestContext.Current.CancellationToken));
    }
}
