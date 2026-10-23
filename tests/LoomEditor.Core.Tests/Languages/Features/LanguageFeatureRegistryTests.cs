using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Tests.Languages.Features.TestHelpers;

namespace LoomEditor.Core.Tests.Languages.Features;

public class LanguageFeatureRegistryTests
{
    // Monaco: LanguageFeatureRegistry.register / all
    [Fact]
    public void Register_ThenAll_ReturnsProvider()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var provider = new FakeHoverProvider();
        registry.Register("csharp", provider);
        Assert.Same(provider, Assert.Single(registry.All("csharp")));
        Assert.True(registry.Has("csharp"));
    }

    [Fact]
    public void Register_Dispose_RemovesProvider()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var registration = registry.Register("csharp", new FakeHoverProvider());
        registration.Dispose();
        Assert.Empty(registry.All("csharp"));
        Assert.Empty(registry.AllNoModel());
    }

    [Fact]
    public void Register_DisposeTwice_RaisesChangedOnce()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var registration = registry.Register("csharp", new FakeHoverProvider());
        var raised = 0;
        registry.Changed += (_, _) => raised++;
        registration.Dispose();
        registration.Dispose();
        Assert.Equal(1, raised);
    }

    // Monaco: LanguageFeatureRegistry.onDidChange
    [Fact]
    public void Changed_ReportsEntryCount()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var counts = new List<int>();
        registry.Changed += (_, count) => counts.Add(count);
        registry.Register("a", new FakeHoverProvider());
        var second = registry.Register("b", new FakeHoverProvider());
        second.Dispose();
        Assert.Equal([1, 2, 1], counts);
    }

    // Monaco: LanguageFeatureRegistry.ordered
    [Fact]
    public void Ordered_ExactLanguageBeforeStar()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var exact = new FakeHoverProvider();
        var star = new FakeHoverProvider();
        registry.Register("*", star);
        registry.Register("csharp", exact);
        Assert.Equal<IHoverProvider>([exact, star], registry.Ordered("csharp"));
    }

    // Monaco: LanguageFeatureRegistry._compareByScoreAndTime
    [Fact]
    public void Ordered_SameScore_NewestFirst()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var older = new FakeHoverProvider();
        var newer = new FakeHoverProvider();
        registry.Register("csharp", older);
        registry.Register("csharp", newer);
        Assert.Equal<IHoverProvider>([newer, older], registry.Ordered("csharp"));
    }

    [Fact]
    public void Ordered_OtherLanguage_NotReturned()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        registry.Register("python", new FakeHoverProvider());
        Assert.Empty(registry.Ordered("csharp"));
    }

    [Fact]
    public void Ordered_StarOnly_MatchesAnyLanguage()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var star = new FakeHoverProvider();
        registry.Register("*", star);
        Assert.Same(star, Assert.Single(registry.Ordered("anything")));
    }

    [Fact]
    public void Has_NoMatch_False()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        registry.Register("python", new FakeHoverProvider());
        Assert.False(registry.Has("csharp"));
    }

    // Monaco: LanguageFeatureRegistry.orderedGroups
    [Fact]
    public void OrderedGroups_GroupsEqualScores()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var older = new FakeHoverProvider();
        var star = new FakeHoverProvider();
        var newest = new FakeHoverProvider();
        registry.Register("csharp", older);
        registry.Register("*", star);
        registry.Register("csharp", newest);
        var groups = registry.OrderedGroups("csharp");
        Assert.Equal(2, groups.Count);
        Assert.Equal<IHoverProvider>([newest, older], groups[0]);
        Assert.Equal<IHoverProvider>([star], groups[1]);
    }

    // Monaco: LanguageFeatureRegistry.allNoModel
    [Fact]
    public void AllNoModel_RegistrationOrder()
    {
        var registry = new LanguageFeatureRegistry<IHoverProvider>();
        var a = new FakeHoverProvider();
        var b = new FakeHoverProvider();
        registry.Register("x", a);
        registry.Register("*", b);
        Assert.Equal<IHoverProvider>([a, b], registry.AllNoModel());
    }

    [Fact]
    public void Register_NullProvider_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new LanguageFeatureRegistry<IHoverProvider>().Register("*", null!));

    [Fact]
    public void Register_EmptySelector_Throws() =>
        Assert.Throws<ArgumentException>(() => new LanguageFeatureRegistry<IHoverProvider>().Register("", new FakeHoverProvider()));
}
