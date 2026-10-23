namespace LoomEditor.View.Hover;

/// <summary>Shows and hides the hover popup; the WinUI control provides the real one (P8-11).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IHoverPresenter
{
    /// <summary>True while the popup is shown.</summary>
    bool IsVisible { get; }

    /// <summary>True while the pointer is over the popup.</summary>
    bool IsMouseOver { get; }

    /// <summary>Shows the popup with this content, or updates it.</summary>
    void Show(HoverContent content);

    /// <summary>Hides the popup.</summary>
    void Hide();
}
