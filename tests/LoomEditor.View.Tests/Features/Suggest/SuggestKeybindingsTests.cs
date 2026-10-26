using LoomEditor.View.Commands;
using LoomEditor.View.Features.Suggest;

namespace LoomEditor.View.Tests.Features.Suggest;

// New tests: Monaco has no test of the default keybindings.
public class SuggestKeybindingsTests
{
    private static readonly KeybindingContext Focused = new(
        TextInputFocus: true, EditorTextFocus: true, Writable: true, TabDoesNotMoveFocus: true, EditorFocus: true, HasCompletionItemProvider: true);

    private static readonly KeybindingContext Visible = Focused with { SuggestWidgetVisible = true, SuggestWidgetMultipleSuggestions = true };

    private static string? Resolve(KeyModifiers modifiers, KeyCode key, KeybindingContext context) =>
        KeybindingResolver.Default.Resolve(new Keybinding(modifiers, key), context);

    [Fact]
    public void Tab_WithSuggestVisible_ResolvesToAccept()
    {
        Assert.Equal(SuggestCommands.AcceptSelectedSuggestion, Resolve(KeyModifiers.None, KeyCode.Tab, Visible));
        Assert.Equal("tab", Resolve(KeyModifiers.None, KeyCode.Tab, Focused));
    }

    [Fact]
    public void Enter_WithSuggestVisible_Accepts_WithoutIt_NewLine()
    {
        Assert.Equal(SuggestCommands.AcceptSelectedSuggestion, Resolve(KeyModifiers.None, KeyCode.Enter, Visible));
        // no rule: the control types the line break itself
        Assert.Null(Resolve(KeyModifiers.None, KeyCode.Enter, Focused));
    }

    [Fact]
    public void Escape_WithSuggestVisible_ResolvesToHide_BeforeCancelSelection()
    {
        KeybindingContext selected = Visible with { HasNonEmptySelection = true, HasMultipleSelections = true };

        Assert.Equal(SuggestCommands.HideSuggestWidget, Resolve(KeyModifiers.None, KeyCode.Escape, Visible));
        Assert.Equal(SuggestCommands.HideSuggestWidget, Resolve(KeyModifiers.None, KeyCode.Escape, selected));
        Assert.Equal(SuggestCommands.HideSuggestWidget, Resolve(KeyModifiers.Shift, KeyCode.Escape, selected));
        Assert.Equal("cancelSelection", Resolve(KeyModifiers.None, KeyCode.Escape, Focused with { HasNonEmptySelection = true }));
    }

    [Fact]
    public void Down_WithSuggestVisibleAndMultiple_ResolvesToNext_WithSingleItem_CursorDown()
    {
        Assert.Equal(SuggestCommands.SelectNextSuggestion, Resolve(KeyModifiers.None, KeyCode.DownArrow, Visible));
        Assert.Equal("cursorDown", Resolve(KeyModifiers.None, KeyCode.DownArrow, Visible with { SuggestWidgetMultipleSuggestions = false }));
        Assert.Equal("cursorDown", Resolve(KeyModifiers.None, KeyCode.DownArrow, Focused));
    }

    [Fact]
    public void Up_PageUp_PageDown_Mapping()
    {
        Assert.Equal(SuggestCommands.SelectPrevSuggestion, Resolve(KeyModifiers.None, KeyCode.UpArrow, Visible));
        Assert.Equal(SuggestCommands.SelectNextPageSuggestion, Resolve(KeyModifiers.None, KeyCode.PageDown, Visible));
        Assert.Equal(SuggestCommands.SelectPrevPageSuggestion, Resolve(KeyModifiers.None, KeyCode.PageUp, Visible));
        Assert.Equal(SuggestCommands.SelectNextPageSuggestion, Resolve(KeyModifiers.Ctrl, KeyCode.PageDown, Visible));
        Assert.Equal(SuggestCommands.SelectPrevPageSuggestion, Resolve(KeyModifiers.Ctrl, KeyCode.PageUp, Visible));
        Assert.Equal(SuggestCommands.SelectPrevSuggestion, Resolve(KeyModifiers.Ctrl, KeyCode.UpArrow, Visible));
    }

    [Fact]
    public void CtrlSpace_ResolvesToTrigger_OnlyWhenWritableAndProvider_AndNotVisible()
    {
        Assert.Equal(SuggestCommands.TriggerSuggest, Resolve(KeyModifiers.Ctrl, KeyCode.Space, Focused));
        Assert.Null(Resolve(KeyModifiers.Ctrl, KeyCode.Space, Focused with { Writable = false }));
        Assert.Null(Resolve(KeyModifiers.Ctrl, KeyCode.Space, Focused with { HasCompletionItemProvider = false }));
        Assert.Null(Resolve(KeyModifiers.Ctrl, KeyCode.Space, Visible));
    }

    [Fact]
    public void CtrlI_ResolvesToTrigger()
    {
        Assert.Equal(SuggestCommands.TriggerSuggest, Resolve(KeyModifiers.Ctrl, KeyCode.KeyI, Focused));
    }

    [Fact]
    public void CtrlDown_WithSuggestVisible_ResolvesToNext()
    {
        Assert.Equal(SuggestCommands.SelectNextSuggestion, Resolve(KeyModifiers.Ctrl, KeyCode.DownArrow, Visible));
        Assert.Equal("scrollLineDown", Resolve(KeyModifiers.Ctrl, KeyCode.DownArrow, Focused));
    }

    [Fact]
    public void NoKeyBound_ForFirstLast()
    {
        Assert.DoesNotContain(
            DefaultKeybindings.Windows,
            rule => rule.CommandId is SuggestCommands.SelectFirstSuggestion or SuggestCommands.SelectLastSuggestion);
    }
}
