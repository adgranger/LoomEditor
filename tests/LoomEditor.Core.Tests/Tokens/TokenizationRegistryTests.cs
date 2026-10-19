using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Tokens;

public class TokenizationRegistryTests
{
    private sealed class FakeSupport : ITokenizationSupport
    {
        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
            NullTokenize.NullTokenizeEncoded(1, state);
    }

    [Fact]
    public void TokenizationRegistry_RegisterAndSetColorMap_RaiseChanged()
    {
        var registry = new TokenizationRegistry();
        var events = new List<TokenizationSupportChangedEventArgs>();
        registry.Changed += (_, e) => events.Add(e);
        var support = new FakeSupport();

        IDisposable registration = registry.Register("csharp", support);
        registry.SetColorMap([0u, 0xFF112233u, 0xFF445566u]);
        registration.Dispose();

        Assert.Equal(3, events.Count);
        Assert.Equal(["csharp"], events[0].ChangedLanguages);
        Assert.False(events[0].ChangedColorMap);
        Assert.Equal(["csharp"], events[1].ChangedLanguages);
        Assert.True(events[1].ChangedColorMap);
        Assert.Equal(["csharp"], events[2].ChangedLanguages);
        Assert.False(events[2].ChangedColorMap);
    }

    [Fact]
    public void Get_ReturnsTheSupport_UntilItsRegistrationIsDisposed()
    {
        var registry = new TokenizationRegistry();
        var support = new FakeSupport();

        IDisposable registration = registry.Register("json", support);
        Assert.Same(support, registry.Get("json"));
        Assert.Null(registry.Get("other"));

        registration.Dispose();
        Assert.Null(registry.Get("json"));
    }

    [Fact]
    public void Dispose_OfAReplacedRegistration_DoesNotRemoveTheNewSupport()
    {
        var registry = new TokenizationRegistry();
        var first = new FakeSupport();
        var second = new FakeSupport();
        IDisposable firstRegistration = registry.Register("json", first);
        registry.Register("json", second);
        int raised = 0;
        registry.Changed += (_, _) => raised++;

        firstRegistration.Dispose();

        Assert.Same(second, registry.Get("json"));
        Assert.Equal(0, raised);
    }

    [Fact]
    public void GetDefaultBackground_NeedsAColorMapWithTheBackgroundEntry()
    {
        var registry = new TokenizationRegistry();

        Assert.Null(registry.GetColorMap());
        Assert.Null(registry.GetDefaultBackground());

        registry.SetColorMap([0u, 0xFFAAAAAAu]);
        Assert.Null(registry.GetDefaultBackground());

        registry.SetColorMap([0u, 0xFFAAAAAAu, 0xFF010203u]);
        Assert.Equal(0xFF010203u, registry.GetDefaultBackground());
        Assert.Equal(3, registry.GetColorMap()!.Count);
    }

    [Fact]
    public void HandleChange_RaisesForTheGivenLanguages()
    {
        var registry = new TokenizationRegistry();
        TokenizationSupportChangedEventArgs? received = null;
        registry.Changed += (_, e) => received = e;

        registry.HandleChange(["a", "b"]);

        Assert.Equal(["a", "b"], received!.ChangedLanguages);
        Assert.False(received.ChangedColorMap);
    }
}
