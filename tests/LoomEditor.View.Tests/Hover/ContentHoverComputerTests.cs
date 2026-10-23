using LoomEditor.Core.Decorations;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Hover;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Hover;

using Hover = LoomEditor.Core.Languages.Features.Hover;

public sealed class ContentHoverComputerTests : IDisposable
{
    private const int OwnerId = 7;

    private readonly TextModel _model = new("let value  = 1;\nsecond line\nthird line");
    private readonly MarkerService _service = new();
    private readonly LanguageFeatureRegistry<IHoverProvider> _providers = new();
    private readonly MarkerDecorations _markers;
    private readonly ContentHoverComputer _computer;

    public ContentHoverComputerTests()
    {
        _markers = new MarkerDecorations(_model, _service, OwnerId);
        _computer = new ContentHoverComputer(_model, _markers, _providers, () => "csharp");
    }

    public void Dispose() => _markers.Dispose();

    private static HoverComputerOptions At(int line, int startColumn, int endColumn) =>
        new(new HoverAnchor(new Range(line, startColumn, line, endColumn)), HoverStartSource.Mouse);

    private static MarkerData Error(int sl, int sc, int el, int ec, string message = "message") =>
        new(MarkerSeverity.Error, message, sl, sc, el, ec);

    private static Hover Md(string text, Range? range = null) => new([new MarkdownString(text)], range);

    private sealed class FuncHoverProvider(Func<Task<Hover?>> provide) : IHoverProvider
    {
        public Task<Hover?> ProvideHoverAsync(TextModel model, Position position, CancellationToken token) => provide();
    }

    // Monaco: ContentHoverComputer.computeSync
    [Fact]
    public void ComputeSync_MarkerOnAnchor_ReturnsMarkerPart()
    {
        _service.Change("o", [Error(1, 2, 1, 5)]);

        var part = Assert.IsType<MarkerHoverPart>(Assert.Single(_computer.ComputeSync(At(1, 3, 3))));

        Assert.Equal(new Range(1, 2, 1, 5), part.Range);
        Assert.Equal("message", part.Marker.Message);
        Assert.Equal(1, part.Order);
    }

    // Monaco: ContentHoverComputer.computeSync
    [Fact]
    public void ComputeSync_AnchorOutsideTheMarker_Empty()
    {
        _service.Change("o", [Error(1, 2, 1, 5)]);

        Assert.Empty(_computer.ComputeSync(At(1, 7, 7)));
    }

    // Monaco: ContentHoverComputer.computeSync
    [Fact]
    public void ComputeSync_TwoMarkers_BothInDecorationOrder()
    {
        _service.Change("o", [Error(1, 2, 1, 6, "first"), Error(1, 3, 1, 8, "second")]);

        var parts = _computer.ComputeSync(At(1, 4, 4)).Cast<MarkerHoverPart>().ToList();

        Assert.Equal(["first", "second"], parts.Select(p => p.Marker.Message));
    }

    // Monaco: ContentHoverComputer._getLineDecorations (showIfCollapsed)
    [Fact]
    public void ComputeSync_ShowIfCollapsed_RelaxedByOneColumn()
    {
        // column 11 sits between two spaces: the empty range is not widened to a word
        _service.Change("o", [Error(1, 11, 1, 11)]);

        Assert.Single(_computer.ComputeSync(At(1, 12, 12)));
        Assert.Empty(_computer.ComputeSync(At(1, 13, 13)));
    }

    // Monaco: ContentHoverComputer._getLineDecorations
    [Fact]
    public void ComputeSync_MultiLineMarker_ClampedToTheAnchorLine()
    {
        _service.Change("o", [Error(1, 5, 2, 4)]);

        var first = Assert.IsType<MarkerHoverPart>(Assert.Single(_computer.ComputeSync(At(1, 6, 6))));
        var second = Assert.IsType<MarkerHoverPart>(Assert.Single(_computer.ComputeSync(At(2, 2, 2))));

        Assert.Equal(new Range(1, 5, 1, _model.GetLineMaxColumn(1)), first.Range);
        Assert.Equal(new Range(2, 1, 2, 4), second.Range);
    }

    // Monaco: markerHoverParticipant.computeSync (isValidRange)
    [Fact]
    public void ComputeSync_InvalidAnchorRange_Empty()
    {
        _service.Change("o", [Error(1, 2, 1, 5)]);

        Assert.Empty(_computer.ComputeSync(At(1, 3, 99)));
    }

    // Monaco: markerHoverParticipant.computeSync
    [Fact]
    public void ComputeSync_NonMarkerDecoration_Ignored()
    {
        var options = ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "other" });
        _model.DeltaDecorations(OwnerId + 1, [], [new ModelDeltaDecoration(new Range(1, 2, 1, 5), options)]);

        Assert.Empty(_computer.ComputeSync(At(1, 3, 3)));
    }

    // Monaco: ContentHoverComputer.computeAsync
    [Fact]
    public void ComputeAsync_NoProviders_Null() => Assert.Null(_computer.ComputeAsync(At(1, 3, 3), CancellationToken.None));

    // Monaco: MarkdownHoverParticipant.computeAsync
    [Fact]
    public async Task ComputeAsync_Provider_ReturnsMarkdownPart()
    {
        _providers.Register("csharp", new FakeHoverProvider((_, _) => Md("doc")));

        var parts = await _computer.ComputeAsync(At(1, 3, 5), CancellationToken.None)!;

        var part = Assert.IsType<MarkdownHoverPart>(Assert.Single(parts));
        Assert.Equal(new Range(1, 3, 1, 5), part.Range);
        Assert.Equal("doc", Assert.Single(part.Contents).Value);
        Assert.Equal(0, part.Ordinal);
        Assert.Equal(3, part.Order);
    }

    // Monaco: MarkdownHoverParticipant.computeAsync
    [Fact]
    public async Task ComputeAsync_ProviderRange_IsUsed()
    {
        _providers.Register("csharp", new FakeHoverProvider((_, _) => Md("doc", new Range(1, 1, 1, 4))));

        var parts = await _computer.ComputeAsync(At(1, 3, 3), CancellationToken.None)!;

        Assert.Equal(new Range(1, 1, 1, 4), Assert.Single(parts).Range);
    }

    // Monaco: MarkdownHoverParticipant.computeAsync
    [Fact]
    public async Task ComputeAsync_ProviderRangeSpanningLines_ReplacedByAnchorRange()
    {
        _providers.Register("csharp", new FakeHoverProvider((_, _) => Md("doc", new Range(1, 1, 2, 4))));

        var parts = await _computer.ComputeAsync(At(1, 3, 5), CancellationToken.None)!;

        Assert.Equal(new Range(1, 3, 1, 5), Assert.Single(parts).Range);
    }

    // Monaco: getHover.ts isValid
    [Fact]
    public async Task ComputeAsync_EmptyContents_Dropped()
    {
        _providers.Register("csharp", new FakeHoverProvider((_, _) => Md("")));

        Assert.Empty(await _computer.ComputeAsync(At(1, 3, 3), CancellationToken.None)!);
    }

    // Monaco: getHover.ts isValid
    [Fact]
    public async Task ComputeAsync_NullHover_Dropped()
    {
        _providers.Register("csharp", new FakeHoverProvider((_, _) => null));

        Assert.Empty(await _computer.ComputeAsync(At(1, 3, 3), CancellationToken.None)!);
    }

    // Monaco: getHover.ts executeProvider
    [Fact]
    public async Task ComputeAsync_FaultedProvider_IgnoredOthersKept()
    {
        _providers.Register("csharp", new FakeHoverProvider((_, _) => Md("ok")));
        _providers.Register("csharp", new FuncHoverProvider(() => Task.FromException<Hover?>(new InvalidOperationException())));

        var parts = await _computer.ComputeAsync(At(1, 3, 3), CancellationToken.None)!;

        var part = Assert.IsType<MarkdownHoverPart>(Assert.Single(parts));
        Assert.Equal(1, part.Ordinal);
    }

    // Monaco: getHover.ts executeProvider
    [Fact]
    public async Task ComputeAsync_ThrowingProvider_IgnoredOthersKept()
    {
        _providers.Register("csharp", new FakeHoverProvider((_, _) => Md("ok")));
        _providers.Register("csharp", new FakeHoverProvider((_, _) => throw new InvalidOperationException()));

        var parts = await _computer.ComputeAsync(At(1, 3, 3), CancellationToken.None)!;

        Assert.Equal("ok", Assert.Single(Assert.IsType<MarkdownHoverPart>(Assert.Single(parts)).Contents).Value);
    }

    // Monaco: getHover.ts (ordering by provider index)
    [Fact]
    public async Task ComputeAsync_TwoProviders_OrderedByProviderIndex()
    {
        var older = FakeHoverProvider.Pending();
        var newer = FakeHoverProvider.Pending();
        _providers.Register("csharp", older);
        _providers.Register("csharp", newer);

        Task<IReadOnlyList<IHoverPart>> task = _computer.ComputeAsync(At(1, 3, 3), CancellationToken.None)!;
        older.Complete(Md("older"));
        newer.Complete(Md("newer"));
        var parts = (await task).Cast<MarkdownHoverPart>().ToList();

        // registry order: newest first
        Assert.Equal(["newer", "older"], parts.Select(p => p.Contents[0].Value));
        Assert.Equal([0, 1], parts.Select(p => p.Ordinal));

        // and the same when the tasks complete in reverse
        var a = FakeHoverProvider.Pending();
        var b = FakeHoverProvider.Pending();
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        registry.Register("csharp", a);
        registry.Register("csharp", b);
        var computer = new ContentHoverComputer(_model, _markers, registry, () => "csharp");
        Task<IReadOnlyList<IHoverPart>> reversed = computer.ComputeAsync(At(1, 3, 3), CancellationToken.None)!;
        a.Complete(Md("a"));
        b.Complete(Md("b"));
        Assert.Equal(["b", "a"], (await reversed).Cast<MarkdownHoverPart>().Select(p => p.Contents[0].Value));
    }

    // Monaco: getHover.ts (language selection)
    [Fact]
    public void ComputeAsync_OtherLanguageProvider_NotCalled()
    {
        var provider = new FakeHoverProvider((_, _) => Md("x"));
        _providers.Register("python", provider);

        Assert.Null(_computer.ComputeAsync(At(1, 3, 3), CancellationToken.None));
        Assert.Empty(provider.Calls);
    }

    // Monaco: getHover.ts (language selection)
    [Fact]
    public async Task ComputeAsync_StarProvider_Called()
    {
        var provider = new FakeHoverProvider((_, _) => Md("x"));
        _providers.Register("*", provider);

        Assert.Single(await _computer.ComputeAsync(At(1, 3, 3), CancellationToken.None)!);
        Assert.Single(provider.Calls);
    }

    // Monaco: MarkdownHoverParticipant._getMarkdownHovers
    [Fact]
    public async Task ComputeAsync_PassesTheAnchorStartPosition()
    {
        var provider = new FakeHoverProvider((_, _) => null);
        _providers.Register("csharp", provider);

        await _computer.ComputeAsync(At(2, 4, 8), CancellationToken.None)!;

        Assert.Equal(new Position(2, 4), Assert.Single(provider.Calls));
    }
}
