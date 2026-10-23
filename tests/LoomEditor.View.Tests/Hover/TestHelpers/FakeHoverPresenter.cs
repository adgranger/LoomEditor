using LoomEditor.View.Hover;

namespace LoomEditor.View.Tests.Hover.TestHelpers;

/// <summary>An <see cref="IHoverPresenter"/> that records what the controller asks.</summary>
internal sealed class FakeHoverPresenter : IHoverPresenter
{
    public bool IsVisible { get; private set; }

    public bool IsMouseOver { get; set; }

    public int ShowCount { get; private set; }

    public int HideCount { get; private set; }

    public HoverContent? Last { get; private set; }

    public void Show(HoverContent content)
    {
        IsVisible = true;
        ShowCount++;
        Last = content;
    }

    public void Hide()
    {
        IsVisible = false;
        HideCount++;
    }
}
