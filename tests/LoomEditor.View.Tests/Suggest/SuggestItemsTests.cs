// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/test/browser/suggest.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestItemsTests
{
    private const string Lang = "plaintext";

    private static readonly CompletionContext Invoke = new(CompletionTriggerKind.Invoke);

    private readonly LanguageFeatureRegistry<ICompletionItemProvider> _registry = new();
    private readonly TextModel _model = TestCodeEditor.CreateTextModel("FOO\nbar\nBAR\nfoo");

    private static CompletionList List(params CompletionItem[] items) => new(items);

    private static CompletionItem Item(string label, CompletionItemKind kind = CompletionItemKind.Text, string? sortText = null)
        => new(label, label, kind) { SortText = sortText };

    private static FakeCompletionProvider Provider(params CompletionItem[] items)
        => new() { OnProvide = (_, _, _) => List(items) };

    private Task<IReadOnlyList<SuggestItem>> Provide(Position? position = null, SuggestItemOptions? options = null, CompletionContext? context = null, CancellationToken? token = null)
        => SuggestItems.ProvideAsync(_registry, _model, Lang, position ?? new Position(1, 1), options ?? SuggestItemOptions.Default, context ?? Invoke, token ?? TestContext.Current.CancellationToken);

    // Monaco: 'only from'
    [Fact]
    public async Task OnlyFrom_ProviderFilter()
    {
        var foo = Provider(Item("foo"));
        var bar = Provider(Item("bar"));
        _registry.Register(Lang, foo);
        _registry.Register(Lang, bar);

        var result = await Provide(options: new SuggestItemOptions(ProviderFilter: new HashSet<ICompletionItemProvider> { foo }));

        Assert.Single(result);
        Assert.Equal("foo", result[0].TextLabel);
        Assert.Single(foo.Calls);
        Assert.Empty(bar.Calls);
    }

    // Monaco: 'Ctrl+space completions stopped working with the latest Insiders, #97650'
    [Fact]
    public async Task Issue97650_InvalidWhenInsertAndReplaceStartDiffer()
    {
        var first = new CompletionItem("one", "one") { Range = new CompletionRange(new Range(1, 1, 1, 1), new Range(1, 1, 1, 10)) };
        var second = new CompletionItem("two", "two") { Range = new CompletionRange(new Range(1, 1, 1, 1), new Range(1, 2, 1, 10)) };
        _registry.Register(Lang, Provider(first, second));

        var result = await Provide();

        Assert.False(result.Single(x => x.TextLabel == "one").IsInvalid);
        Assert.True(result.Single(x => x.TextLabel == "two").IsInvalid);
    }

    [Fact]
    public async Task ProvideAsync_DefaultRange_IsTheWordAtPosition()
    {
        _registry.Register(Lang, Provider(Item("x")));
        var result = await Provide(new Position(4, 3)); // foo|, cursor in the middle: fo|o

        var item = result.Single();
        Assert.Equal(new Position(4, 1), item.EditStart);
        Assert.Equal(new Position(4, 3), item.EditInsertEnd);
        Assert.Equal(new Position(4, 4), item.EditReplaceEnd);
    }

    [Fact]
    public async Task ProvideAsync_DefaultRange_NoWord_EmptyAtPosition()
    {
        var model = TestCodeEditor.CreateTextModel("a  b");
        _registry.Register(Lang, Provider(Item("x")));

        var result = await SuggestItems.ProvideAsync(_registry, model, Lang, new Position(1, 3), SuggestItemOptions.Default, Invoke, TestContext.Current.CancellationToken);

        var item = result.Single();
        Assert.Equal(new Position(1, 3), item.EditStart);
        Assert.Equal(new Position(1, 3), item.EditInsertEnd);
        Assert.Equal(new Position(1, 3), item.EditReplaceEnd);
    }

    [Fact]
    public async Task ProvideAsync_FillsSortText()
    {
        _registry.Register(Lang, Provider(Item("label"), Item("other", sortText: "zz")));
        var result = await Provide();

        Assert.Equal("label", result.Single(x => x.TextLabel == "label").Completion.SortText);
        Assert.Equal("zz", result.Single(x => x.TextLabel == "other").Completion.SortText);
    }

    [Fact]
    public async Task ProvideAsync_SortsBySortTextThenLabelThenKind()
    {
        _registry.Register(Lang, Provider(
            Item("b", sortText: "1"),
            Item("a", sortText: "2"),
            Item("same", CompletionItemKind.Variable, "3"),
            Item("same", CompletionItemKind.Method, "3")));

        var result = await Provide();

        Assert.Equal(["b", "a", "same", "same"], result.Select(x => x.TextLabel));
        Assert.Equal(CompletionItemKind.Method, result[2].Completion.Kind);
    }

    [Fact]
    public async Task ProvideAsync_StopsAtFirstGroupWithResults()
    {
        var specific = Provider(Item("a"));
        var star = Provider(Item("b"));
        _registry.Register("*", star);
        _registry.Register(Lang, specific);

        var result = await Provide();

        Assert.Equal(["a"], result.Select(x => x.TextLabel));
        Assert.Empty(star.Calls);
    }

    [Fact]
    public async Task ProvideAsync_EmptyGroup_GoesOnToTheNext()
    {
        var specific = Provider();
        var star = Provider(Item("b"));
        _registry.Register("*", star);
        _registry.Register(Lang, specific);

        var result = await Provide();

        Assert.Equal(["b"], result.Select(x => x.TextLabel));
        Assert.Single(specific.Calls);
    }

    [Fact]
    public async Task ProvideAsync_SameGroupProviders_BothAsked()
    {
        _registry.Register(Lang, Provider(Item("a")));
        _registry.Register(Lang, Provider(Item("b")));

        var result = await Provide();

        Assert.Equal(["a", "b"], result.Select(x => x.TextLabel).Order());
    }

    [Fact]
    public async Task ProvideAsync_FaultedProvider_OthersKept()
    {
        var faulted = FakeCompletionProvider.Pending();
        _registry.Register(Lang, faulted);
        _registry.Register(Lang, Provider(Item("ok")));
        var task = Provide();
        faulted.Fail();

        var result = await task;

        Assert.Equal(["ok"], result.Select(x => x.TextLabel));
    }

    [Fact]
    public async Task ProvideAsync_ThrowingProvider_OthersKept()
    {
        _registry.Register(Lang, new FakeCompletionProvider { OnProvide = (_, _, _) => throw new InvalidOperationException() });
        _registry.Register(Lang, Provider(Item("ok")));

        var result = await Provide();

        Assert.Equal(["ok"], result.Select(x => x.TextLabel));
    }

    [Fact]
    public async Task ProvideAsync_NullList_Ignored()
    {
        _registry.Register(Lang, new FakeCompletionProvider { OnProvide = (_, _, _) => null });
        Assert.Empty(await Provide());
    }

    [Fact]
    public async Task ProvideAsync_Deprecated_Skipped_WhenNotShown()
    {
        var deprecated = Item("old");
        deprecated.Tags = [CompletionItemTag.Deprecated];
        _registry.Register(Lang, Provider(deprecated, Item("new")));

        var hidden = await Provide(options: new SuggestItemOptions(ShowDeprecated: false));
        var shown = await Provide();

        Assert.Equal(["new"], hidden.Select(x => x.TextLabel));
        Assert.Equal(2, shown.Count);
    }

    [Fact]
    public async Task ProvideAsync_ReusedItems_NotRequested()
    {
        var provider = Provider(Item("fresh"));
        _registry.Register(Lang, provider);
        var completion = Item("old");
        completion.Range = CompletionRange.FromRange(new Range(1, 1, 1, 1));
        var reused = new SuggestItem(new Position(1, 1), completion, List(completion), provider);

        var result = await Provide(options: new SuggestItemOptions(
            ProviderItemsToReuse: new Dictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>> { [provider] = [reused] }));

        Assert.Same(reused, result.Single());
        Assert.Empty(provider.Calls);
    }

    [Fact]
    public async Task ProvideAsync_Cancelled_TaskIsCanceled()
    {
        var pending = FakeCompletionProvider.Pending();
        _registry.Register(Lang, pending);
        using var cts = new CancellationTokenSource();
        var task = Provide(token: cts.Token);

        cts.Cancel();
        pending.Complete(List(Item("a")));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(task.IsCanceled);
    }

    [Fact]
    public async Task ProvideAsync_PositionIsValidated()
    {
        var provider = Provider(Item("a"));
        _registry.Register(Lang, provider);

        await Provide(new Position(1, 99));

        Assert.Equal(new Position(1, 4), provider.Calls.Single().Position);
    }

    [Fact]
    public async Task ProvideAsync_PassesContext()
    {
        var provider = Provider(Item("a"));
        _registry.Register(Lang, provider);

        await Provide(context: new CompletionContext(CompletionTriggerKind.TriggerCharacter, "."));

        Assert.Equal(new CompletionContext(CompletionTriggerKind.TriggerCharacter, "."), provider.Calls.Single().Context);
    }

    [Fact]
    public async Task ProvideAsync_NoProviders_EmptyList() => Assert.Empty(await Provide());

    [Fact]
    public async Task ProvideAsync_StableSort()
    {
        var first = Item("same");
        var second = Item("same");
        _registry.Register(Lang, Provider(first, second));

        var result = await Provide();

        Assert.Same(first, result[0].Completion);
        Assert.Same(second, result[1].Completion);
    }
}
