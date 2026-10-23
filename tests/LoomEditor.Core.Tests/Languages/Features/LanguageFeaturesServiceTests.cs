using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Tests.Languages.Features.TestHelpers;

namespace LoomEditor.Core.Tests.Languages.Features;

public class LanguageFeaturesServiceTests
{
    [Fact]
    public void Registries_AreIndependent()
    {
        var a = new LanguageFeaturesService();
        var b = new LanguageFeaturesService();
        a.HoverProviders.Register("*", new FakeHoverProvider());
        Assert.False(b.HoverProviders.Has("x"));
        Assert.NotSame(a.HoverProviders, b.HoverProviders);
    }

    [Fact]
    public void Registries_AreTypedPerFeature()
    {
        var service = new LanguageFeaturesService();
        service.HoverProviders.Register("*", new FakeHoverProvider());
        Assert.False(service.CompletionProviders.Has("x"));
        Assert.False(service.DefinitionProviders.Has("x"));
    }
}
