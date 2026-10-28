using LoomEditor.View.Commands;
using LoomEditor.View.Features;

namespace LoomEditor.View.Tests.Commands;

public class DefaultKeybindingsTests
{
    // The diff navigation commands only exist in the sub editors of a diff editor.
    private sealed class NoDiffTarget : LoomEditor.View.Features.DiffNavigation.IDiffNavigationTarget
    {
        public void GoToChange(LoomEditor.View.DiffEditor.NavigationDirection direction)
        {
        }
    }

    [Fact]
    public void Windows_EveryCommandExceptTheClipboardOnesIsRegisteredInANewEditor()
    {
        var context = new EditorContext();
        using var host = new EditorFeatureHost(context, EditorFeatures.ForDiffEditor(new NoDiffTarget()));
        string[] clipboard = [DefaultKeybindings.ClipboardCut, DefaultKeybindings.ClipboardCopy, DefaultKeybindings.ClipboardPaste];

        IEnumerable<string> ids = DefaultKeybindings.Windows.Select(rule => rule.CommandId).Distinct().Except(clipboard);

        Assert.All(ids, id => Assert.True(context.Commands.Contains(id), id));
    }

    [Fact]
    public void Windows_TheOnlySameKeySameWeightRulesWithOverlappingConditionsAreTheExpectedOnes()
    {
        // Contexts to try: every combination of the eight context keys.
        var contexts = new List<KeybindingContext>();
        for (int bits = 0; bits < 256; bits++)
        {
            contexts.Add(new KeybindingContext(
                (bits & 1) != 0, (bits & 2) != 0, (bits & 4) != 0, (bits & 8) != 0,
                (bits & 16) != 0, (bits & 32) != 0, (bits & 64) != 0, (bits & 128) != 0));
        }

        var conflicts = new List<string>();
        IReadOnlyList<KeybindingRule> rules = DefaultKeybindings.Windows;
        for (int i = 0; i < rules.Count; i++)
        {
            for (int j = i + 1; j < rules.Count; j++)
            {
                KeybindingRule a = rules[i];
                KeybindingRule b = rules[j];
                if (a.Keybinding == b.Keybinding
                    && a.Chord == b.Chord
                    && a.Weight == b.Weight
                    && a.CommandId != b.CommandId
                    && contexts.Exists(c => (a.When?.Invoke(c) ?? true) && (b.When?.Invoke(c) ?? true)))
                {
                    conflicts.Add($"{a.Keybinding}: {a.CommandId} / {b.CommandId}");
                }
            }
        }

        // Monaco has none: cursorUpSelect's Ctrl+Shift+Up secondary is not shared with insertCursorAbove on Windows,
        // and the column-select Shift+arrows rule is one weight heavier than the plain select rule.
        Assert.Empty(conflicts);
    }

    [Fact]
    public void Windows_ShiftArrowsWithColumnSelectionAreHeavierThanThePlainSelect()
    {
        KeybindingRule select = DefaultKeybindings.Windows.Single(r => r.CommandId == "cursorLeftSelect");
        KeybindingRule column = DefaultKeybindings.Windows.Single(r => r.CommandId == "cursorColumnSelectLeft" && r.Keybinding.Modifiers == KeyModifiers.Shift);

        Assert.Equal(select.Keybinding, column.Keybinding);
        Assert.Equal(select.Weight + 1, column.Weight);
    }

    [Fact]
    public void Windows_FoldingRules()
    {
        KeybindingRule fold = DefaultKeybindings.Windows.Single(r => r.CommandId == "editor.fold");
        Assert.Equal(new Keybinding(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.BracketLeft), fold.Keybinding);
        Assert.Null(fold.Chord);

        KeybindingRule foldAll = DefaultKeybindings.Windows.Single(r => r.CommandId == "editor.foldAll");
        Assert.Equal(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), foldAll.Keybinding);
        Assert.Equal(new Keybinding(KeyModifiers.Ctrl, KeyCode.Digit0), foldAll.Chord);

        KeybindingRule foldLevel7 = DefaultKeybindings.Windows.Single(r => r.CommandId == "editor.foldLevel7");
        Assert.Equal(new Keybinding(KeyModifiers.Ctrl, KeyCode.Digit7), foldLevel7.Chord);
    }
}
