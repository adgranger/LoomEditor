using System.Diagnostics.CodeAnalysis;
using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.WinUI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Windows.Foundation;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.WinUI.Accessibility;

/// <summary>A UI Automation text range over <see cref="AutomationTextRange"/>: type conversion only.</summary>
/// <remarks>Threading: UI thread only (UI Automation calls XAML peers there).</remarks>
internal sealed partial class LoomEditorTextRangeProvider(LoomEditorControl owner, IRawElementProviderSimple element, AutomationTextRange range) : ITextRangeProvider
{
    private readonly AutomationTextRange _range = range;

    // UIAutomationClient.h text attribute ids.
    private const int FontNameId = 40005;
    private const int FontSizeId = 40006;
    private const int FontWeightId = 40007;
    private const int IsHiddenId = 40013;
    private const int IsItalicId = 40014;
    private const int IsReadOnlyId = 40015;

    public ITextRangeProvider Clone()
    {
        AssertThread();
        return new LoomEditorTextRangeProvider(owner, element, _range.Clone());
    }

    public bool Compare(ITextRangeProvider textRangeProvider)
    {
        AssertThread();
        return textRangeProvider is LoomEditorTextRangeProvider p && _range.Compare(p._range);
    }

    public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
    {
        AssertThread();
        if (targetRange is not LoomEditorTextRangeProvider p)
        {
            throw new ArgumentException("The range was not created by this editor.", nameof(targetRange));
        }

        return _range.CompareEndpoints((AutomationTextEndpoint)(int)endpoint, p._range, (AutomationTextEndpoint)(int)targetEndpoint);
    }

    public string GetText(int maxLength)
    {
        AssertThread();
        return _range.GetText(maxLength);
    }

    public IRawElementProviderSimple GetEnclosingElement()
    {
        AssertThread();
        return element;
    }

    public IRawElementProviderSimple[] GetChildren()
    {
        AssertThread();
        return [];
    }

    public void GetBoundingRectangles(out double[] returnValue)
    {
        AssertThread();
        var result = new List<double>();
        foreach (AutomationRect r in owner.AutomationGeometry.GetBoundingRectangles(_range.Range))
        {
            if (ScreenCoordinates.ToScreenPixels(owner, new Rect(r.X, r.Y, r.Width, r.Height)) is { } screen)
            {
                result.Add(screen.X);
                result.Add(screen.Y);
                result.Add(screen.Width);
                result.Add(screen.Height);
            }
        }

        returnValue = [.. result];
    }

    public object GetAttributeValue(int attributeId)
    {
        AssertThread();
        return TryGetAttribute(attributeId, out object? value) ? value : DependencyProperty.UnsetValue;
    }

    public ITextRangeProvider? FindAttribute(int attributeId, object value, bool backward)
    {
        AssertThread();
        return TryGetAttribute(attributeId, out object? actual) && actual.Equals(value) ? Clone() : null;
    }

    public void ExpandToEnclosingUnit(TextUnit unit)
    {
        AssertThread();
        _range.ExpandToEnclosingUnit((AutomationTextUnit)(int)unit);
    }

    public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
    {
        AssertThread();
        return _range.FindText(text, backward, ignoreCase) is { } found ? new LoomEditorTextRangeProvider(owner, element, found) : null;
    }

    public int Move(TextUnit unit, int count)
    {
        AssertThread();
        return _range.Move((AutomationTextUnit)(int)unit, count);
    }

    public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
    {
        AssertThread();
        return _range.MoveEndpointByUnit((AutomationTextEndpoint)(int)endpoint, (AutomationTextUnit)(int)unit, count);
    }

    public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
    {
        AssertThread();
        if (targetRange is not LoomEditorTextRangeProvider p)
        {
            throw new ArgumentException("The range was not created by this editor.", nameof(targetRange));
        }

        _range.MoveEndpointByRange((AutomationTextEndpoint)(int)endpoint, p._range, (AutomationTextEndpoint)(int)targetEndpoint);
    }

    // Selecting never moves the keyboard focus.
    public void Select() => ApplySelection(AutomationTextSelection.Select);

    public void AddToSelection() => ApplySelection(AutomationTextSelection.Add);

    public void RemoveFromSelection() => ApplySelection(AutomationTextSelection.Remove);

    public void ScrollIntoView(bool alignToTop)
    {
        AssertThread();
        ViewModel viewModel = _range.Document.ViewModel;
        Range viewRange = viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(_range.Range);
        viewModel.RevealRange("automation", true, viewRange, alignToTop ? VerticalRevealType.Top : VerticalRevealType.Bottom, ScrollType.Immediate);
    }

    private void ApplySelection(Func<IReadOnlyList<Selection>, Range, IReadOnlyList<Selection>> change)
    {
        AssertThread();
        ViewModel viewModel = _range.Document.ViewModel;
        viewModel.SetSelections("automation", change(viewModel.GetSelections(), _range.Range));
    }

    // The attributes are uniform over the document (ADR 0010 "Not in phase 7").
    private bool TryGetAttribute(int attributeId, [NotNullWhen(true)] out object? value)
    {
        value = attributeId switch
        {
            FontNameId => owner.FontFamily.Source,
            FontSizeId => owner.FontSize * 0.75,
            FontWeightId => 400,
            IsHiddenId => false,
            IsItalicId => false,
            IsReadOnlyId => owner.IsReadOnly,
            _ => null,
        };
        return value is not null;
    }

    private void AssertThread() => Guard.Assert(owner.DispatcherQueue.HasThreadAccess);
}
