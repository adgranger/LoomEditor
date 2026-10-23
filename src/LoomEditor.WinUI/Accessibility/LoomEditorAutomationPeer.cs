using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.WinUI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Windows.Foundation;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.WinUI.Accessibility;

/// <summary>
/// Describes the editor to UI Automation (screen readers, accessibility tools, UI tests).
/// </summary>
/// <remarks>
/// Exposes the control as an <c>Edit</c> control with the Text and Value patterns, so that screen readers can read the text.
/// Threading: UI thread only.
/// </remarks>
internal sealed partial class LoomEditorAutomationPeer(LoomEditorControl owner) : FrameworkElementAutomationPeer(owner), ITextProvider, ITextProvider2, IValueProvider
{
    protected override string GetClassNameCore() => nameof(LoomEditorControl);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? "Code editor" : name;
    }

    protected override object? GetPatternCore(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.Text or PatternInterface.Text2 or PatternInterface.Value => this,
        _ => base.GetPatternCore(patternInterface),
    };

    public ITextRangeProvider DocumentRange => Wrap(AutomationTextRange.ForDocument(owner.AutomationDocument));

    public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.Multiple;

    public ITextRangeProvider[] GetSelection()
    {
        var selections = owner.AutomationDocument.ViewModel.GetSelections();
        var result = new ITextRangeProvider[selections.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Wrap(new AutomationTextRange(owner.AutomationDocument, selections[i].Range));
        }

        return result;
    }

    public ITextRangeProvider[] GetVisibleRanges() =>
        [Wrap(new AutomationTextRange(owner.AutomationDocument, owner.AutomationGeometry.GetVisibleRange()))];

    public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement) => DocumentRange;

    public ITextRangeProvider RangeFromPoint(Point screenLocation)
    {
        Position position = ScreenCoordinates.ToElement(owner, screenLocation) is { } p
            ? owner.AutomationGeometry.PositionFromPoint(p.X, p.Y)
            : owner.AutomationDocument.ViewModel.GetSelections()[0].Position;
        return Wrap(new AutomationTextRange(owner.AutomationDocument, Core.Text.Range.Collapsed(position)));
    }

    public ITextRangeProvider GetCaretRange(out bool isActive)
    {
        isActive = owner.AutomationHasFocus;
        return Wrap(new AutomationTextRange(owner.AutomationDocument, Core.Text.Range.Collapsed(owner.AutomationDocument.ViewModel.GetSelection().Position)));
    }

    public ITextRangeProvider? RangeFromAnnotation(IRawElementProviderSimple annotationElement) => null;

    public bool IsReadOnly => owner.IsReadOnly;

    public string Value => owner.Model.GetValue(EndOfLinePreference.LF);

    public void SetValue(string value)
    {
        if (owner.IsReadOnly)
        {
            throw new ElementNotEnabledException();
        }

        owner.Text = value ?? string.Empty;
    }

    private LoomEditorTextRangeProvider Wrap(AutomationTextRange range) => new(owner, ProviderFromPeer(this), range);
}
