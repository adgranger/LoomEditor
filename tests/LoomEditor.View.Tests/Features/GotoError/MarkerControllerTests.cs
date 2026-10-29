using LoomEditor.Core.Markers;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.GotoError;
using LoomEditor.View.Tests.Accessibility.TestHelpers;

namespace LoomEditor.View.Tests.Features.GotoError;

// No upstream test for the controller without the widget.
public sealed class MarkerControllerTests : IDisposable
{
    private readonly EditorContext _context = new();
    private readonly GotoErrorFeature _feature = new();
    private readonly RecordingAnnouncer _announcer = new();

    public MarkerControllerTests()
    {
        _context.SetText("aaa bbb\nccc ddd\neee fff\nggg hhh\niii jjj");
        _context.Announcer = _announcer;
        _feature.Attach(_context);
        _context.SetSelections([new Selection(1, 1, 1, 1)]);
    }

    public void Dispose()
    {
        _feature.Dispose();
        _context.Dispose();
    }

    private void SetMarkers(params (int Line, MarkerSeverity Severity)[] markers) =>
        _context.Markers.Change("test", [.. markers.Select(m => new MarkerData(m.Severity, $"problem {m.Line}", m.Line, 2, m.Line, 4))]);

    private void Run(string id) => Assert.True(_context.Commands.Execute(id));

    private void ThreeErrors() => SetMarkers((2, MarkerSeverity.Error), (3, MarkerSeverity.Error), (5, MarkerSeverity.Error));

    private static string? Resolve(KeyModifiers modifiers, KeyCode key, KeybindingContext context) =>
        KeybindingResolver.Default.Resolve(new Keybinding(modifiers, key), context);

    [Fact]
    public void F8_MovesCaretToNextMarkerAndRevealsIt()
    {
        ThreeErrors();

        Run(GotoErrorCommands.NextInFiles);
        Assert.Equal(Selection.Collapsed(new Position(2, 2)), _context.Selection);

        Run(GotoErrorCommands.NextInFiles);
        Assert.Equal(Selection.Collapsed(new Position(3, 2)), _context.Selection);
    }

    [Fact]
    public void ShiftF8_GoesToPreviousWrapping()
    {
        ThreeErrors();

        Run(GotoErrorCommands.PrevInFiles);
        Assert.Equal(new Position(5, 2), _context.Selection.Position);

        Run(GotoErrorCommands.PrevInFiles);
        Assert.Equal(new Position(3, 2), _context.Selection.Position);
    }

    [Fact]
    public void F8_NoMarkers_DoesNothing()
    {
        Run(GotoErrorCommands.NextInFiles);

        Assert.Equal(Selection.Collapsed(new Position(1, 1)), _context.Selection);
        Assert.False(_context.MarkersNavigationVisible);
        Assert.Empty(_announcer.Messages);
    }

    [Fact]
    public void F8_Announces_LineContentSeverityMessagePositionAndIndex()
    {
        SetMarkers((2, MarkerSeverity.Warning), (3, MarkerSeverity.Warning));

        Run(GotoErrorCommands.NextInFiles);

        Assert.Equal("ccc ddd, Warning: problem 2 at 2:2. 1 of 2 problems", Assert.Single(_announcer.Messages));
    }

    [Fact]
    public void F8_Announces_SingularProblem()
    {
        SetMarkers((2, MarkerSeverity.Error));

        Run(GotoErrorCommands.NextInFiles);

        Assert.Equal("ccc ddd, Error: problem 2 at 2:2. 1 of 1 problem", Assert.Single(_announcer.Messages));
    }

    [Fact]
    public void F8_CaretInsideMarker_KeepsCaret()
    {
        ThreeErrors();
        _context.SetSelections([new Selection(2, 3, 2, 3)]);

        Run(GotoErrorCommands.NextInFiles);

        Assert.Equal(Selection.Collapsed(new Position(2, 3)), _context.Selection);
    }

    [Fact]
    public void F8_CaretOutside_MovesToMarkerStart_NoTextSelected()
    {
        ThreeErrors();

        Run(GotoErrorCommands.NextInFiles);

        Assert.True(_context.Selection.IsEmpty);
        Assert.Equal(new Position(2, 2), _context.Selection.Position);
    }

    [Fact]
    public void AltF8_SameAsF8()
    {
        ThreeErrors();

        Run(GotoErrorCommands.Next);
        Run(GotoErrorCommands.Next);
        Assert.Equal(new Position(3, 2), _context.Selection.Position);

        Run(GotoErrorCommands.Prev);
        Assert.Equal(new Position(2, 2), _context.Selection.Position);
    }

    [Fact]
    public void Escape_EndsSession_AndResetsContextKey()
    {
        ThreeErrors();
        Run(GotoErrorCommands.NextInFiles);
        Assert.True(_context.MarkersNavigationVisible);

        Run(GotoErrorCommands.Close);

        Assert.False(_context.MarkersNavigationVisible);
        Assert.False(_context.Commands.Execute(GotoErrorCommands.Close));
    }

    [Fact]
    public void CaretMoveOutsideMarker_ResetsIndex()
    {
        ThreeErrors();
        Run(GotoErrorCommands.NextInFiles);
        Run(GotoErrorCommands.NextInFiles);
        Assert.Equal(new Position(3, 2), _context.Selection.Position);

        // Out of the marker: the next move starts again from the caret.
        _context.SetSelections([new Selection(4, 1, 4, 1)]);
        Run(GotoErrorCommands.NextInFiles);

        Assert.Equal(new Position(5, 2), _context.Selection.Position);
    }

    [Fact]
    public void MarkersChanged_ResetsIndex()
    {
        ThreeErrors();
        Run(GotoErrorCommands.NextInFiles);
        Run(GotoErrorCommands.NextInFiles);

        SetMarkers((2, MarkerSeverity.Error), (3, MarkerSeverity.Error), (4, MarkerSeverity.Error), (5, MarkerSeverity.Error));
        _context.SetSelections([new Selection(3, 2, 3, 2)]);
        Run(GotoErrorCommands.NextInFiles);

        // Started again from the caret (3,2), inside the marker of line 3.
        Assert.Equal(new Position(3, 2), _context.Selection.Position);
    }

    [Fact]
    public void Navigation_StartsFromCaretPosition()
    {
        ThreeErrors();
        _context.SetSelections([new Selection(3, 6, 3, 6)]);

        Run(GotoErrorCommands.NextInFiles);

        Assert.Equal(new Position(5, 2), _context.Selection.Position);
    }

    [Fact]
    public void Keybindings_ResolveToTheFourActions()
    {
        var keys = new KeybindingContext(EditorFocus: true, EditorTextFocus: true, TextInputFocus: true);

        Assert.Equal(GotoErrorCommands.NextInFiles, Resolve(KeyModifiers.None, KeyCode.F8, keys));
        Assert.Equal(GotoErrorCommands.PrevInFiles, Resolve(KeyModifiers.Shift, KeyCode.F8, keys));
        Assert.Equal(GotoErrorCommands.Next, Resolve(KeyModifiers.Alt, KeyCode.F8, keys));
        Assert.Equal(GotoErrorCommands.Prev, Resolve(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.F8, keys));
        Assert.Equal(GotoErrorCommands.Close, Resolve(KeyModifiers.None, KeyCode.Escape, keys with { MarkersNavigationVisible = true }));
        Assert.Equal(GotoErrorCommands.Close, Resolve(KeyModifiers.Shift, KeyCode.Escape, keys with { MarkersNavigationVisible = true }));
    }

    [Fact]
    public void Escape_NotInSession_DoesNotResolveToClose()
    {
        var keys = new KeybindingContext(EditorFocus: true, EditorTextFocus: true, TextInputFocus: true);

        Assert.NotEqual(GotoErrorCommands.Close, Resolve(KeyModifiers.None, KeyCode.Escape, keys));
    }

    [Fact]
    public void Escape_InSession_WinsOverSnippetEscape()
    {
        var keys = new KeybindingContext(EditorFocus: true, EditorTextFocus: true, TextInputFocus: true, MarkersNavigationVisible: true, InSnippetMode: true);

        Assert.Equal(GotoErrorCommands.Close, Resolve(KeyModifiers.None, KeyCode.Escape, keys));
    }
}
