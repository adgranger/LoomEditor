using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Tests.TestHelpers;

using Hover = LoomEditor.Core.Languages.Features.Hover;

internal sealed class FakeCompletionProvider(IReadOnlyList<string>? triggerCharacters = null) : ICompletionItemProvider
{
    private TaskCompletionSource<CompletionList?>? _pending;

    public IReadOnlyList<string>? TriggerCharacters { get; } = triggerCharacters;

    public Func<TextModel, Position, CompletionContext, CompletionList?>? OnProvide { get; set; }

    public List<(Position Position, CompletionContext Context)> Calls { get; } = [];

    public List<CancellationToken> Tokens { get; } = [];

    public bool CanResolve { get; set; }

    public Func<CompletionItem, CompletionItem?>? OnResolve { get; set; }

    public static FakeCompletionProvider Pending() => new() { _pending = new TaskCompletionSource<CompletionList?>() };

    public void Complete(CompletionList? list) => _pending!.SetResult(list);

    public void Fail() => _pending!.SetException(new InvalidOperationException("provider failed"));

    public Task<CompletionList?> ProvideCompletionItemsAsync(TextModel model, Position position, CompletionContext context, CancellationToken token)
    {
        Calls.Add((position, context));
        Tokens.Add(token);
        return _pending is not null ? _pending.Task : Task.FromResult(OnProvide?.Invoke(model, position, context));
    }

    public Task<CompletionItem?> ResolveCompletionItemAsync(CompletionItem item, CancellationToken token) =>
        Task.FromResult(OnResolve?.Invoke(item));
}

internal sealed class FakeHoverProvider(Func<TextModel, Position, Hover?>? onProvide = null) : IHoverProvider
{
    private TaskCompletionSource<Hover?>? _pending;

    public List<Position> Calls { get; } = [];

    public static FakeHoverProvider Pending() => new() { _pending = new TaskCompletionSource<Hover?>() };

    public void Complete(Hover? hover) => _pending!.SetResult(hover);

    public Task<Hover?> ProvideHoverAsync(TextModel model, Position position, CancellationToken token)
    {
        Calls.Add(position);
        return _pending is not null ? _pending.Task : Task.FromResult(onProvide?.Invoke(model, position));
    }
}

internal sealed class FakeDefinitionProvider(Func<TextModel, Position, IReadOnlyList<LocationLink>?>? onProvide = null) : IDefinitionProvider
{
    private TaskCompletionSource<IReadOnlyList<LocationLink>?>? _pending;

    public List<Position> Calls { get; } = [];

    public static FakeDefinitionProvider Pending() => new() { _pending = new TaskCompletionSource<IReadOnlyList<LocationLink>?>() };

    public void Complete(IReadOnlyList<LocationLink>? links) => _pending!.SetResult(links);

    public Task<IReadOnlyList<LocationLink>?> ProvideDefinitionAsync(TextModel model, Position position, CancellationToken token)
    {
        Calls.Add(position);
        return _pending is not null ? _pending.Task : Task.FromResult(onProvide?.Invoke(model, position));
    }
}
