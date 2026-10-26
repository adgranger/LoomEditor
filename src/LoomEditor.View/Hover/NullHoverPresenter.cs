namespace LoomEditor.View.Hover;

/// <summary>The presenter used until the control assigns a real one: never visible, ignores everything.</summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal sealed class NullHoverPresenter : IHoverPresenter
{
    /// <summary>The only instance.</summary>
    public static readonly NullHoverPresenter Instance = new();

    private NullHoverPresenter()
    {
    }

    /// <inheritdoc />
    public bool IsVisible => false;

    /// <inheritdoc />
    public bool IsMouseOver => false;

    /// <inheritdoc />
    public void Show(HoverContent content)
    {
    }

    /// <inheritdoc />
    public void Hide()
    {
    }
}
