using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Gallery.Lsp;

namespace LoomEditor.Lsp.Tests.Gallery;

public sealed class DemoProvidersTests
{
    private const string Uri = "file:///doc.cs";

    private static TextModel Model(string text) => new(text);

    [Fact]
    public async Task Completion_WordsAndKeywords_Sorted()
    {
        CompletionList? list = await new DemoCompletionProvider().ProvideCompletionItemsAsync(
            Model("zeta a alpha zeta"), new Position(1, 1), new CompletionContext(CompletionTriggerKind.Invoke), CancellationToken.None);

        Assert.NotNull(list);
        string[] words = [.. list.Suggestions.Where(i => i.Kind == CompletionItemKind.Text).Select(i => i.Label)];
        Assert.Equal(["alpha", "zeta"], words);
        Assert.Equal(DemoProviders.Keywords, list.Suggestions.Where(i => i.Kind == CompletionItemKind.Keyword).Select(i => i.Label));
    }

    [Fact]
    public async Task Completion_DotTrigger_Items()
    {
        var provider = new DemoCompletionProvider();
        CompletionList? list = await provider.ProvideCompletionItemsAsync(
            Model("Console."), new Position(1, 9), new CompletionContext(CompletionTriggerKind.TriggerCharacter, "."), CancellationToken.None);

        Assert.Equal(["."], provider.TriggerCharacters);
        Assert.Contains(list!.Suggestions, i => i.Label == "Console");
    }

    [Fact]
    public async Task Hover_OnWord()
    {
        Hover? hover = await new DemoHoverProvider().ProvideHoverAsync(Model("  hello world"), new Position(1, 5), CancellationToken.None);

        Assert.NotNull(hover);
        Assert.Equal("**hello**\n\nDemo hover from an in-process provider.", hover.Contents[0].Value);
        Assert.Equal(new Range(1, 3, 1, 8), hover.Range);
    }

    [Fact]
    public async Task Hover_NoWord_Null() =>
        Assert.Null(await new DemoHoverProvider().ProvideHoverAsync(Model("a  +  b"), new Position(1, 4), CancellationToken.None));

    [Fact]
    public async Task Definition_Class_Found()
    {
        IReadOnlyList<LocationLink>? links = await new DemoDefinitionProvider(Uri).ProvideDefinitionAsync(
            Model("class Program\n{\n    Program.Run();\n}"), new Position(3, 6), CancellationToken.None);

        LocationLink link = Assert.Single(links!);
        Assert.Equal(Uri, link.Uri);
        Assert.Equal(new Range(1, 7, 1, 14), link.Range);
    }

    [Fact]
    public async Task Definition_NotFound_Empty()
    {
        IReadOnlyList<LocationLink>? links = await new DemoDefinitionProvider(Uri).ProvideDefinitionAsync(
            Model("class A { }\nB.Run();"), new Position(2, 1), CancellationToken.None);

        Assert.Empty(links!);
    }

    [Fact]
    public void Register_AndDispose_UpdatesTheRegistries()
    {
        var features = new LanguageFeaturesService();

        IDisposable registration = DemoProviders.Register(features, "csharp", Uri);

        Assert.True(features.CompletionProviders.Has("csharp"));
        Assert.True(features.HoverProviders.Has("csharp"));
        Assert.True(features.DefinitionProviders.Has("csharp"));
        registration.Dispose();
        Assert.False(features.CompletionProviders.Has("csharp"));
        Assert.False(features.HoverProviders.Has("csharp"));
        Assert.False(features.DefinitionProviders.Has("csharp"));
    }

    [Fact]
    public void DemoMarkers_Set_FourSeverities()
    {
        var markers = new MarkerService();

        DemoMarkers.Set(markers, Model("one\ntwo\nthree\nfour\nfive"));

        IReadOnlyList<Marker> read = markers.Read();
        Assert.Equal(4, read.Count);
        Assert.Equal(
            [MarkerSeverity.Error, MarkerSeverity.Warning, MarkerSeverity.Info, MarkerSeverity.Hint],
            read.OrderBy(m => m.StartLineNumber).Select(m => m.Severity));
    }

    [Fact]
    public void DemoMarkers_Clear()
    {
        var markers = new MarkerService();
        DemoMarkers.Set(markers, Model("one\ntwo"));

        DemoMarkers.Clear(markers);

        Assert.Empty(markers.Read());
    }

    [Fact]
    public void DemoMarkers_SkipsEmptyLines()
    {
        var markers = new MarkerService();

        DemoMarkers.Set(markers, Model("\n  one two\n\n   \nthree"));

        Marker[] read = [.. markers.Read().OrderBy(m => m.StartLineNumber)];
        Assert.Equal([2, 5], read.Select(m => m.StartLineNumber));
        Assert.Equal((3, 6), (read[0].StartColumn, read[0].EndColumn));
    }
}
