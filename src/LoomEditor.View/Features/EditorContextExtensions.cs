using LoomEditor.View.Controller;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Layout;

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
}
