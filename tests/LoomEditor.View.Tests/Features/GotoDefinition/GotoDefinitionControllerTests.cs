using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.GotoDefinition;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.GotoDefinition;

// New tests: Monaco's go to definition is tested through the whole editor, not at this level.
public sealed class GotoDefinitionControllerTests : IDisposable
{
    private const string Self = "inmemory://doc";

    private readonly SuggestTestEditor _editor = new("hello world\nsecond line");
    private readonly RecordingAnnouncer _announcer = new();
    private readonly GotoDefinitionController _controller;

    public GotoDefinitionControllerTests()
    {
        _editor.Context.DocumentUri = Self;
        _editor.Context.Announcer = _announcer;
        _controller = new GotoDefinitionController(_editor.Context);
    }

    public void Dispose()
    {
        _controller.Dispose();
        _editor.Dispose();
    }

    private static LocationLink InSelf(Range range, Range? selection = null) => new(Self, range, null, selection);

    private FakeDefinitionProvider Provide(params LocationLink[] links)
    {
        var provider = new FakeDefinitionProvider((_, _) => links);
        _editor.Context.LanguageFeatures.DefinitionProviders.Register("plaintext", provider);
        return provider;
    }

    private void Run(Position? position = null)
    {
        _controller.Run(position);
        _editor.Flush();
    }

    private sealed class Opener(bool result) : ILocationOpener
    {
        public List<LocationLink> Opened { get; } = [];

        public bool Open(LocationLink target)
        {
            Opened.Add(target);
            return result;
        }
    }

    [Fact]
    public void Run_SameDocument_MovesTheCursorToTheTargetSelectionRange()
    {
        Provide(InSelf(new Range(2, 1, 2, 7), new Range(2, 3, 2, 5)));

        Run(new Position(1, 2));

        Assert.Equal(Selection.Collapsed(new Position(2, 3)), _editor.Context.Selection);
    }

    [Fact]
    public void Run_UsesRangeWhenNoTargetSelectionRange()
    {
        Provide(InSelf(new Range(2, 4, 2, 7)));

        Run(new Position(1, 2));

        Assert.Equal(Selection.Collapsed(new Position(2, 4)), _editor.Context.Selection);
    }

    [Fact]
    public void Run_OtherDocument_CallsTheOpener()
    {
        var opener = new Opener(true);
        _editor.Context.LocationOpener = opener;
        var link = new LocationLink("file:///other", new Range(1, 1, 1, 2));
        Provide(link);

        Run(new Position(1, 2));

        Assert.Same(link, Assert.Single(opener.Opened));
    }

    [Fact]
    public void Run_OtherDocument_NoOpener_Announces()
    {
        Provide(new LocationLink("file:///other", new Range(1, 1, 1, 2)));

        Run(new Position(1, 2));

        Assert.Contains("Definition in file:///other", _announcer.Messages);
    }

    [Fact]
    public void Run_OpenerReturnsFalse_Announces()
    {
        _editor.Context.LocationOpener = new Opener(false);
        Provide(new LocationLink("file:///other", new Range(1, 1, 1, 2)));

        Run(new Position(1, 2));

        Assert.Contains("Definition in file:///other", _announcer.Messages);
    }

    [Fact]
    public void Run_NoResult_AnnouncesNoDefinitionForWord()
    {
        Provide();

        Run(new Position(1, 2));

        Assert.Contains("No definition found for 'hello'", _announcer.Messages);
    }

    [Fact]
    public void Run_NoResultNoWord_AnnouncesGeneric()
    {
        _editor.SetText("   ");
        Provide();

        Run(new Position(1, 2));

        Assert.Contains("No definition found", _announcer.Messages);
    }

    [Fact]
    public void Run_Announces_AriaMessage()
    {
        Provide(InSelf(new Range(2, 1, 2, 2)));

        Run(new Position(1, 2));

        Assert.Equal($"Found 1 symbol in {Self}", _announcer.Messages[0]);
    }

    [Fact]
    public void Run_ModelEditedWhileWaiting_Dropped()
    {
        FakeDefinitionProvider provider = Pending();
        _editor.MoveCaret(1, 2);
        _controller.Run();
        _editor.Type("x");

        provider.Complete([InSelf(new Range(2, 1, 2, 2))]);
        _editor.Flush();

        Assert.Empty(_announcer.Messages);
    }

    [Fact]
    public void Run_CursorMovedWhileWaiting_Dropped()
    {
        FakeDefinitionProvider provider = Pending();
        _editor.MoveCaret(1, 2);
        _controller.Run();
        _editor.MoveCaret(1, 5);

        provider.Complete([InSelf(new Range(2, 1, 2, 2))]);
        _editor.Flush();

        Assert.Empty(_announcer.Messages);
        Assert.Equal(Selection.Collapsed(new Position(1, 5)), _editor.Context.Selection);
    }

    [Fact]
    public void Run_FromMousePosition_NotDroppedWhenCursorMoved()
    {
        FakeDefinitionProvider provider = Pending();
        _controller.Run(new Position(1, 2));
        _editor.MoveCaret(1, 5);

        provider.Complete([InSelf(new Range(2, 3, 2, 4))]);
        _editor.Flush();

        Assert.Equal(Selection.Collapsed(new Position(2, 3)), _editor.Context.Selection);
    }

    [Fact]
    public void Run_SecondRunCancelsTheFirst()
    {
        var first = FakeDefinitionProvider.Pending();
        _editor.Context.LanguageFeatures.DefinitionProviders.Register("plaintext", first);
        _controller.Run(new Position(1, 1));
        _controller.Run(new Position(1, 2));
        Assert.True(first.Tokens[0].IsCancellationRequested);
        Assert.False(first.Tokens[1].IsCancellationRequested);

        first.Complete([InSelf(new Range(2, 2, 2, 3))]);
        _editor.Flush();

        // both calls share the provider's single task: only the second request is applied
        Assert.Equal(Selection.Collapsed(new Position(2, 2)), _editor.Context.Selection);
        Assert.Single(_announcer.Messages, m => m.StartsWith("Found", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_InvalidRange_Clamped()
    {
        Provide(InSelf(new Range(99, 99, 99, 99)));

        Run(new Position(1, 2));

        Assert.Equal(Selection.Collapsed(new Position(2, 12)), _editor.Context.Selection);
    }

    [Fact]
    public void Run_MultipleResults_GoesToFirstProviderResult()
    {
        Provide(InSelf(new Range(2, 5, 2, 6)), InSelf(new Range(1, 1, 1, 2)));

        Run(new Position(1, 2));

        Assert.Equal(Selection.Collapsed(new Position(2, 5)), _editor.Context.Selection);
    }

    [Fact]
    public void Run_NoProvider_DoesNothing()
    {
        Run(new Position(1, 2));

        Assert.Empty(_announcer.Messages);
    }

    [Fact]
    public void Dispose_CancelsPending()
    {
        FakeDefinitionProvider provider = Pending();
        _controller.Run(new Position(1, 2));

        _controller.Dispose();

        Assert.True(provider.Tokens[0].IsCancellationRequested);
    }

    private FakeDefinitionProvider Pending()
    {
        var provider = FakeDefinitionProvider.Pending();
        _editor.Context.LanguageFeatures.DefinitionProviders.Register("plaintext", provider);
        return provider;
    }
}
