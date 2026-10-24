using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestItemTests
{
    private static readonly Position Pos = new(1, 5);

    private static SuggestItem Make(CompletionItem completion, ICompletionItemProvider? provider = null)
        => new(Pos, completion, new CompletionList([completion]), provider ?? new FakeCompletionProvider());

    private static CompletionItem Item(string label = "Foo", CompletionRange? range = null)
        => new(label, label) { Range = range ?? CompletionRange.FromRange(new Range(1, 2, 1, 5)) };

    private sealed class ResolvingProvider : ICompletionItemProvider
    {
        public IReadOnlyList<string>? TriggerCharacters => null;

        public bool CanResolve => true;

        public int ResolveCalls { get; private set; }

        public TaskCompletionSource<CompletionItem?> Pending { get; set; } = new();

        public Task<CompletionList?> ProvideCompletionItemsAsync(LoomEditor.Core.Model.TextModel model, Position position, CompletionContext context, CancellationToken token)
            => Task.FromResult<CompletionList?>(null);

        public Task<CompletionItem?> ResolveCompletionItemAsync(CompletionItem item, CancellationToken token)
        {
            ResolveCalls++;
            return Pending.Task;
        }
    }

    [Fact]
    public void Constructor_Computes_Positions_AndLowerCases()
    {
        var completion = Item("FooBar", new CompletionRange(new Range(1, 2, 1, 4), new Range(1, 2, 1, 6)));
        completion.SortText = "ZZ";
        completion.FilterText = "FiLter";
        var item = Make(completion);

        Assert.Equal(new Position(1, 2), item.EditStart);
        Assert.Equal(new Position(1, 4), item.EditInsertEnd);
        Assert.Equal(new Position(1, 6), item.EditReplaceEnd);
        Assert.Equal("FooBar", item.TextLabel);
        Assert.Equal("foobar", item.LabelLow);
        Assert.Equal("zz", item.SortTextLow);
        Assert.Equal("filter", item.FilterTextLow);
    }

    [Fact]
    public void Constructor_SingleRange_InsertEqualsReplace()
    {
        var item = Make(Item());
        Assert.Equal(item.EditInsertEnd, item.EditReplaceEnd);
    }

    [Fact]
    public void IsInvalid_EmptyLabel() => Assert.True(Make(Item("")).IsInvalid);

    [Fact]
    public void IsInvalid_MultiLineRange()
        => Assert.True(Make(Item(range: CompletionRange.FromRange(new Range(1, 2, 2, 1)))).IsInvalid);

    [Fact]
    public void IsInvalid_RangeOnAnotherLine()
        => Assert.True(Make(Item(range: CompletionRange.FromRange(new Range(2, 1, 2, 3)))).IsInvalid);

    [Fact]
    public void IsInvalid_Valid_False() => Assert.False(Make(Item()).IsInvalid);

    [Fact]
    public void Constructor_NullRange_Throws()
        => Assert.Throws<ArgumentException>(() => Make(new CompletionItem("a", "a")));

    [Fact]
    public void SortTextLow_Empty_IsNull()
    {
        var completion = Item();
        completion.SortText = "";
        completion.FilterText = "";
        var item = Make(completion);
        Assert.Null(item.SortTextLow);
        Assert.Null(item.FilterTextLow);
    }

    [Fact]
    public void BeginResolve_ProviderCannotResolve_Null_AndIsResolved()
    {
        var item = Make(Item());
        Assert.True(item.IsResolved);
        Assert.Null(item.BeginResolve(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void BeginResolve_StartsOnce_ReturnsSameTask()
    {
        var provider = new ResolvingProvider();
        var item = Make(Item(), provider);
        Assert.False(item.IsResolved);

        Task<CompletionItem?>? first = item.BeginResolve(TestContext.Current.CancellationToken);
        Task<CompletionItem?>? second = item.BeginResolve(TestContext.Current.CancellationToken);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(1, provider.ResolveCalls);
    }

    [Fact]
    public void BeginResolve_Cancelled_Restartable()
    {
        var provider = new ResolvingProvider();
        var item = Make(Item(), provider);
        using var cts = new CancellationTokenSource();

        _ = item.BeginResolve(cts.Token);
        cts.Cancel();
        _ = item.BeginResolve(TestContext.Current.CancellationToken);

        Assert.Equal(2, provider.ResolveCalls);
        Assert.False(item.IsResolved);
    }

    [Fact]
    public async Task BeginResolve_Faulted_Restartable()
    {
        var provider = new ResolvingProvider();
        var item = Make(Item(), provider);

        Task<CompletionItem?> task = item.BeginResolve(TestContext.Current.CancellationToken)!;
        provider.Pending.SetException(new InvalidOperationException());
        await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        await Task.Delay(50, TestContext.Current.CancellationToken); // the cache is cleared by a continuation on the pool

        provider.Pending = new TaskCompletionSource<CompletionItem?>();
        _ = item.BeginResolve(TestContext.Current.CancellationToken);
        Assert.Equal(2, provider.ResolveCalls);
    }

    [Fact]
    public void CompleteResolve_MergesAndMarksResolved()
    {
        var item = Make(Item(), new ResolvingProvider());
        item.CompleteResolve(new CompletionItem("Foo", "Foo") { Detail = "details" });

        Assert.Equal("details", item.Completion.Detail);
        Assert.True(item.IsResolved);
        Assert.Null(item.BeginResolve(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void CompleteResolve_Null_MarksResolved()
    {
        var item = Make(Item(), new ResolvingProvider());
        item.CompleteResolve(null);
        Assert.True(item.IsResolved);
    }
}
