using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Threading;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Controller;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;
using LoomEditor.View.Hover;

namespace LoomEditor.View.Features;

/// <summary>Internal helpers for features that need the view model behind an <see cref="IEditorContext"/>.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class EditorContextExtensions
{
    /// <summary>The view model of the editor, or null when the context is not an <see cref="EditorContext"/>.</summary>
    public static ViewModel? GetViewModel(this IEditorContext context) => (context as EditorContext)?.ViewModel;

    /// <summary>The mouse events of the editor, in model coordinates, or null when the context is not an <see cref="EditorContext"/>.</summary>
    public static ViewUserInputEvents? GetUserInputEvents(this IEditorContext context) => (context as EditorContext)?.UserInputEvents;

    /// <summary>The find controller of the editor, or null when the Find feature is not attached.</summary>
    public static FindController? GetFindController(this IEditorContext context) => (context as EditorContext)?.FindController;

    /// <summary>The folding feature of the editor, or null when the Folding feature is not attached.</summary>
    public static FoldingFeature? GetFoldingFeature(this IEditorContext context) => (context as EditorContext)?.FoldingFeature;

    /// <summary>The announcer of the editor, or the null announcer when the context is not an <see cref="EditorContext"/>.</summary>
    public static IEditorAnnouncer GetAnnouncer(this IEditorContext context) =>
        (context as EditorContext)?.Announcer ?? NullEditorAnnouncer.Instance;

    /// <summary>The language feature providers of the editor, or null when the context is not an <see cref="EditorContext"/>.</summary>
    public static LanguageFeaturesService? GetLanguageFeatures(this IEditorContext context) => (context as EditorContext)?.LanguageFeatures;

    /// <summary>The markers of the editor, or null when the context is not an <see cref="EditorContext"/>.</summary>
    public static MarkerService? GetMarkers(this IEditorContext context) => (context as EditorContext)?.Markers;

    /// <summary>The marker decorations of the editor, or null when the context is not an <see cref="EditorContext"/>.</summary>
    public static MarkerDecorations? GetMarkerDecorations(this IEditorContext context) => (context as EditorContext)?.MarkerDecorations;

    /// <summary>The UI dispatcher of the editor, or the inline dispatcher when the context is not an <see cref="EditorContext"/>.</summary>
    public static IUiDispatcher GetUiDispatcher(this IEditorContext context) =>
        (context as EditorContext)?.UiDispatcher ?? InlineUiDispatcher.Instance;

    /// <summary>The URI of the document, or <c>inmemory://model/0</c> when the context is not an <see cref="EditorContext"/>.</summary>
    public static string GetDocumentUri(this IEditorContext context) => (context as EditorContext)?.DocumentUri ?? "inmemory://model/0";

    /// <summary>The hover popup of the editor, or the null presenter when the context is not an <see cref="EditorContext"/> (read at use time: the control assigns it later).</summary>
    public static IHoverPresenter GetHoverPresenter(this IEditorContext context) =>
        (context as EditorContext)?.HoverPresenter ?? NullHoverPresenter.Instance;

    /// <summary>The completion list of the editor, or the null presenter when the context is not an <see cref="EditorContext"/> (read at use time: the control assigns it later).</summary>
    public static LoomEditor.View.Suggest.ISuggestPresenter GetSuggestPresenter(this IEditorContext context) =>
        (context as EditorContext)?.SuggestPresenter ?? LoomEditor.View.Suggest.NullSuggestPresenter.Instance;

    /// <summary>The language id of the model (<c>plaintext</c> by default).</summary>
    public static string GetLanguageId(this IEditorContext context) => context.Model.Tokenization.GetLanguageId();
}
