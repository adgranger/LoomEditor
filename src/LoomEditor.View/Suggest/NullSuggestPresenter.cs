using LoomEditor.Core.Text;

namespace LoomEditor.View.Suggest;

/// <summary>The presenter used until the control assigns the real one: it never shows anything.</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal sealed class NullSuggestPresenter : ISuggestPresenter
{
    /// <summary>The shared instance.</summary>
    public static readonly NullSuggestPresenter Instance = new();

    private NullSuggestPresenter()
    {
    }

    /// <inheritdoc />
    public bool IsVisible => false;

    /// <inheritdoc />
    public void Show(SuggestListState state, Position anchor)
    {
    }

    /// <inheritdoc />
    public void Hide()
    {
    }
}
