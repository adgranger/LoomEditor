using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Features.GotoDefinition;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.GotoDefinition;

// New tests: Monaco's Ctrl+click and F12 are tested through the whole editor.
public sealed class GotoDefinitionFeatureTests : IDisposable
{
    private const string Self = "inmemory://doc";

    private readonly SuggestTestEditor _editor = new("hello world\nsecond line");
    private readonly GotoDefinitionFeature _feature = new();

    public GotoDefinitionFeatureTests()
    {
        _editor.Context.DocumentUri = Self;
        _editor.Context.Announcer = new RecordingAnnouncer();
        _editor.Context.LanguageFeatures.DefinitionProviders.Register(
            "plaintext", new FakeDefinitionProvider((_, _) => [new LocationLink(Self, new Range(2, 3, 2, 5))]));
        _feature.Attach(_editor.Context);
    }

    public void Dispose()
    {
        _feature.Dispose();
        _editor.Dispose();
    }

    private static EditorMouseEvent Mouse(bool left = true, bool ctrl = true, bool alt = false, bool shift = false) =>
        new(5, 5, left, false, alt, ctrl, shift, false, 4, 0);

    private static MouseTarget Text(int line = 1, int column = 2) =>
        new(MouseTargetType.ContentText, new Position(line, column), column, Range.Collapsed(new Position(line, column)), false, null);

    private void Up(EditorMouseEvent mouse, MouseTarget target)
    {
        _editor.Context.UserInputEvents.EmitMouseUp(new EditorMouseEventArgs(mouse, target));
        _editor.Flush();
    }

    private bool Jumped() => _editor.Context.Selection == Selection.Collapsed(new Position(2, 3));

    [Fact]
    public void Attach_RegistersTheCommand() =>
        Assert.True(_editor.Context.Commands.Execute(GotoDefinitionCommands.RevealDefinition));

    [Fact]
    public void Command_WithoutProvider_NoOp()
    {
        using var editor = new SuggestTestEditor("a");
        using var feature = new GotoDefinitionFeature();
        feature.Attach(editor.Context);

        editor.Context.Commands.Execute(GotoDefinitionCommands.RevealDefinition);
        editor.Flush();

        Assert.Equal(Selection.Collapsed(new Position(1, 1)), editor.Context.Selection);
    }

    [Fact]
    public void Command_WithProvider_Jumps()
    {
        _editor.MoveCaret(1, 1);

        _editor.Context.Commands.Execute(GotoDefinitionCommands.RevealDefinition);
        _editor.Flush();

        Assert.True(Jumped());
    }

    [Fact]
    public void CtrlClick_OnText_RunsAtThePosition()
    {
        Up(Mouse(), Text());

        Assert.True(Jumped());
    }

    [Fact]
    public void Click_WithoutCtrl_DoesNothing()
    {
        Up(Mouse(ctrl: false), Text());

        Assert.False(Jumped());
    }

    [Fact]
    public void CtrlAltClick_DoesNothing()
    {
        Up(Mouse(alt: true), Text());

        Assert.False(Jumped());
    }

    [Fact]
    public void CtrlClick_OnGutter_DoesNothing()
    {
        Up(Mouse(), new MouseTarget(MouseTargetType.GutterLineNumbers, new Position(1, 1), 1, Range.Collapsed(new Position(1, 1)), false, null));

        Assert.False(Jumped());
    }

    [Fact]
    public void CtrlRightClick_DoesNothing()
    {
        Up(Mouse(left: false), Text());

        Assert.False(Jumped());
    }

    [Fact]
    public void F12_Resolves_WithProvider_AndNotWithout()
    {
        var focused = new KeybindingContext(EditorTextFocus: true, TextInputFocus: true);
        var f12 = new Keybinding(KeyModifiers.None, KeyCode.F12);

        Assert.Equal(GotoDefinitionCommands.RevealDefinition, KeybindingResolver.Default.Resolve(f12, focused with { HasDefinitionProvider = true }));
        Assert.Null(KeybindingResolver.Default.Resolve(f12, focused));
    }

    [Fact]
    public void Dispose_Unsubscribes()
    {
        _feature.Dispose();

        Up(Mouse(), Text());

        Assert.False(Jumped());
        Assert.False(_editor.Context.Commands.Execute(GotoDefinitionCommands.RevealDefinition));
    }
}
