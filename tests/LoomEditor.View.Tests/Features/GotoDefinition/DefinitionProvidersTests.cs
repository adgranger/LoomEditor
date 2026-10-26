using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.GotoDefinition;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.GotoDefinition;

// New tests: Monaco's getDefinitionsAtPosition has no unit test.
public sealed class DefinitionProvidersTests : IDisposable
{
    private readonly EditorContext _context = new();

    private static LocationLink Link(string uri) => new(uri, new Range(1, 1, 1, 1));

    private Task<IReadOnlyList<LocationLink>> Get(string languageId = "plaintext", Position? position = null) =>
        DefinitionProviders.GetDefinitionsAtPosition(
            _context.LanguageFeatures.DefinitionProviders, _context.Model, languageId, position ?? new Position(1, 1), CancellationToken.None);

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task NoProviders_Empty() => Assert.Empty(await Get());

    [Fact]
    public async Task TwoProviders_Concatenated_InProviderOrder()
    {
        var first = FakeDefinitionProvider.Pending();
        var second = FakeDefinitionProvider.Pending();
        _context.LanguageFeatures.DefinitionProviders.Register("plaintext", first);
        _context.LanguageFeatures.DefinitionProviders.Register("plaintext", second);
        IReadOnlyList<IDefinitionProvider> ordered = _context.LanguageFeatures.DefinitionProviders.Ordered("plaintext");
        var fakes = ordered.Cast<FakeDefinitionProvider>().ToList();

        Task<IReadOnlyList<LocationLink>> task = Get();
        fakes[1].Complete([Link("file:///second")]);
        fakes[0].Complete([Link("file:///first")]);

        Assert.Equal(["file:///first", "file:///second"], (await task).Select(l => l.Uri));
    }

    [Fact]
    public async Task FaultedProvider_Ignored()
    {
        var bad = FakeDefinitionProvider.Pending();
        _context.LanguageFeatures.DefinitionProviders.Register("plaintext", bad);
        _context.LanguageFeatures.DefinitionProviders.Register("plaintext", new FakeDefinitionProvider((_, _) => [Link("file:///ok")]));

        Task<IReadOnlyList<LocationLink>> task = Get();
        bad.Fail();

        Assert.Equal(["file:///ok"], (await task).Select(l => l.Uri));
    }

    [Fact]
    public async Task ThrowingProvider_Ignored()
    {
        _context.LanguageFeatures.DefinitionProviders.Register("plaintext", new FakeDefinitionProvider((_, _) => throw new InvalidOperationException("boom")));
        _context.LanguageFeatures.DefinitionProviders.Register("plaintext", new FakeDefinitionProvider((_, _) => [Link("file:///ok")]));

        Assert.Equal(["file:///ok"], (await Get()).Select(l => l.Uri));
    }

    [Fact]
    public async Task NullList_Ignored()
    {
        _context.LanguageFeatures.DefinitionProviders.Register("plaintext", new FakeDefinitionProvider((_, _) => null));

        Assert.Empty(await Get());
    }

    [Fact]
    public async Task OtherLanguage_NotCalled()
    {
        var provider = new FakeDefinitionProvider((_, _) => [Link("file:///x")]);
        _context.LanguageFeatures.DefinitionProviders.Register("csharp", provider);

        Assert.Empty(await Get("plaintext"));
        Assert.Empty(provider.Calls);
    }

    [Fact]
    public async Task PassesThePosition()
    {
        var provider = new FakeDefinitionProvider((_, _) => []);
        _context.LanguageFeatures.DefinitionProviders.Register("plaintext", provider);

        await Get(position: new Position(3, 7));

        Assert.Equal([new Position(3, 7)], provider.Calls);
    }
}
