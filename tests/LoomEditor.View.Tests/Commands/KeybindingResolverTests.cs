using LoomEditor.View.Commands;

namespace LoomEditor.View.Tests.Commands;

public class KeybindingResolverTests
{
    private static readonly KeybindingContext Focused = new(TextInputFocus: true, EditorTextFocus: true, Writable: true, TabDoesNotMoveFocus: true);

    private static string? Resolve(KeyModifiers modifiers, KeyCode key, KeybindingContext context) =>
        KeybindingResolver.Default.Resolve(new Keybinding(modifiers, key), context);

    [Fact]
    public void Resolve_EscapeWithTwoCursors_RemovesSecondaryCursors()
    {
        KeybindingContext context = Focused with { HasMultipleSelections = true, HasNonEmptySelection = true };

        Assert.Equal("removeSecondaryCursors", Resolve(KeyModifiers.None, KeyCode.Escape, context));
        Assert.Equal("removeSecondaryCursors", Resolve(KeyModifiers.Shift, KeyCode.Escape, context));
    }

    [Fact]
    public void Resolve_EscapeWithOneSelection_CancelsTheSelection()
    {
        Assert.Equal("cancelSelection", Resolve(KeyModifiers.None, KeyCode.Escape, Focused with { HasNonEmptySelection = true }));
    }

    [Fact]
    public void Resolve_EscapeWithOneEmptyCursor_ReturnsNull()
    {
        Assert.Null(Resolve(KeyModifiers.None, KeyCode.Escape, Focused));
    }

    [Fact]
    public void Resolve_ShiftLeft_SelectsUnlessColumnSelectionIsOn()
    {
        Assert.Equal("cursorLeftSelect", Resolve(KeyModifiers.Shift, KeyCode.LeftArrow, Focused));
        Assert.Equal("cursorColumnSelectLeft", Resolve(KeyModifiers.Shift, KeyCode.LeftArrow, Focused with { ColumnSelection = true }));
    }

    [Fact]
    public void Resolve_NotWritable_TabAndCtrlBackspaceDoNothing()
    {
        KeybindingContext readOnly = Focused with { Writable = false };

        Assert.Null(Resolve(KeyModifiers.None, KeyCode.Tab, readOnly));
        Assert.Null(Resolve(KeyModifiers.Shift, KeyCode.Tab, readOnly));
        Assert.Null(Resolve(KeyModifiers.Ctrl, KeyCode.Backspace, readOnly));
        Assert.Equal("tab", Resolve(KeyModifiers.None, KeyCode.Tab, Focused));
        Assert.Equal("deleteWordLeft", Resolve(KeyModifiers.Ctrl, KeyCode.Backspace, Focused));
    }

    [Fact]
    public void Resolve_CtrlArrows_AreWordMoves()
    {
        Assert.Equal("cursorWordLeft", Resolve(KeyModifiers.Ctrl, KeyCode.LeftArrow, Focused));
        Assert.Equal("cursorWordEndRight", Resolve(KeyModifiers.Ctrl, KeyCode.RightArrow, Focused));
        Assert.Equal("cursorWordEndRightSelect", Resolve(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.RightArrow, Focused));
    }

    [Fact]
    public void Resolve_EqualWeights_TheLastRegisteredRuleWins()
    {
        var resolver = new KeybindingResolver(
        [
            new KeybindingRule(new Keybinding(KeyModifiers.None, KeyCode.F1), "first", null, 0),
            new KeybindingRule(new Keybinding(KeyModifiers.None, KeyCode.F1), "second", null, 0),
            new KeybindingRule(new Keybinding(KeyModifiers.None, KeyCode.F2), "light", null, 0),
            new KeybindingRule(new Keybinding(KeyModifiers.None, KeyCode.F2), "heavy", null, 1),
            new KeybindingRule(new Keybinding(KeyModifiers.None, KeyCode.F2), "later but light", null, 0),
        ]);

        Assert.Equal("second", resolver.Resolve(new Keybinding(KeyModifiers.None, KeyCode.F1), default));
        Assert.Equal("heavy", resolver.Resolve(new Keybinding(KeyModifiers.None, KeyCode.F2), default));
    }

    [Fact]
    public void Resolve_NoTextInputFocus_ReturnsNullForMovesButNotForUndo()
    {
        Assert.Null(Resolve(KeyModifiers.None, KeyCode.LeftArrow, default));
        Assert.Equal("undo", Resolve(KeyModifiers.Ctrl, KeyCode.KeyZ, default));
    }
    private static readonly Keybinding CtrlK = new(KeyModifiers.Ctrl, KeyCode.KeyK);
    private static readonly Keybinding CtrlC = new(KeyModifiers.Ctrl, KeyCode.KeyC);

    private static KeybindingResolver ChordResolver(Func<KeybindingContext, bool>? when = null) =>
        new([new KeybindingRule(CtrlK, "comment", when, 0, Chord: CtrlC)]);

    [Fact]
    public void Resolve_FirstKeyOfAChord_ReturnsMoreChordsNeeded()
    {
        KeybindingResolution resolution = ChordResolver().Resolve(Focused, null, CtrlK);

        Assert.Equal(KeybindingResolutionKind.MoreChordsNeeded, resolution.Kind);
        Assert.Null(resolution.CommandId);
    }

    [Fact]
    public void Resolve_SecondKeyOfAChord_ReturnsKbFoundWithTheCommand()
    {
        KeybindingResolution resolution = ChordResolver().Resolve(Focused, CtrlK, CtrlC);

        Assert.Equal(new KeybindingResolution(KeybindingResolutionKind.KbFound, "comment"), resolution);
    }

    [Fact]
    public void Resolve_SecondKeyWithoutRule_ReturnsNoMatchingKb()
    {
        KeybindingResolution resolution = ChordResolver().Resolve(Focused, CtrlK, new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyU));

        Assert.Equal(KeybindingResolutionKind.NoMatchingKb, resolution.Kind);
    }

    [Fact]
    public void Resolve_ChordWhoseWhenIsFalse_ReturnsNoMatchingKb()
    {
        KeybindingResolver resolver = ChordResolver(context => context.Writable);

        Assert.Equal(KeybindingResolutionKind.NoMatchingKb, resolver.Resolve(Focused with { Writable = false }, null, CtrlK).Kind);
        Assert.Equal(KeybindingResolutionKind.NoMatchingKb, resolver.Resolve(Focused with { Writable = false }, CtrlK, CtrlC).Kind);
    }

    [Fact]
    public void Resolve_SingleKeyWithAChordRule_ReturnsNullCommand()
    {
        Assert.Null(ChordResolver().Resolve(CtrlK, Focused));
    }
}
