using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.Comments;

internal sealed partial class CommentThreadWidget : UserControl
{
    private readonly Border _border = new();
    private readonly StackPanel _panel = new() { Spacing = 4 };
    private readonly StackPanel _rows = new() { Spacing = 4 };
    private FontFamily _codeFont = new("Cascadia Mono, Consolas, Courier New");

    /// <summary>
    /// The height, in whole pixels, the content needs at <paramref name="width"/>. The bordered box is measured, not this control: the
    /// control has the zone's height and would answer with it.
    /// </summary>
    public int MeasureContentHeight(double width)
    {
        _border.Measure(new Windows.Foundation.Size(width, double.PositiveInfinity));
        return (int)Math.Ceiling(_border.DesiredSize.Height);
    }

    private void BuildBox()
    {
        _border.BorderThickness = new Thickness(1);
        _border.CornerRadius = new CornerRadius(6);
        _border.Padding = new Thickness(8);
        _border.Background = _background;
        _border.BorderBrush = _borderBrush;
        _border.Child = _panel;
        Content = _border;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CommentThreadAutomationPeer(this);

    /// <summary>Takes the editor's UI font size (the hover's) and its code font, and rebuilds.</summary>
    public void ApplyFont(double fontSize, FontFamily codeFont)
    {
        ArgumentNullException.ThrowIfNull(codeFont);
        _fontSize = fontSize;
        _codeFont = codeFont;
        Rebuild();
    }

    private void SetAutomationId(string id) => AutomationProperties.SetAutomationId(this, id);

    private void SetAutomationName(string name) => AutomationProperties.SetName(this, name);

    private void SetAutomationHelp(string help) => AutomationProperties.SetHelpText(this, help);
}
