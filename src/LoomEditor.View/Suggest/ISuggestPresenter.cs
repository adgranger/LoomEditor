using LoomEditor.Core.Text;

namespace LoomEditor.View.Suggest;

/// <summary>Shows and hides the completion list; the WinUI control provides the real one (P8-22). It reads the state and listens to its <c>Changed</c> itself.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface ISuggestPresenter
{
    /// <summary>True while the list is shown.</summary>
    bool IsVisible { get; }

    /// <summary>Shows the list under or over <paramref name="anchor"/> (the start of the typed word); called again for each new list while visible.</summary>
    void Show(SuggestListState state, Position anchor);

    /// <summary>Hides the list.</summary>
    void Hide();
}
