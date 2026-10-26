using LoomEditor.Core.Languages.Features;
using LoomEditor.View.Features.GotoDefinition;

namespace LoomEditor.Gallery.Lsp;

/// <summary>Shows where a definition is; the Gallery has one document, so it never navigates to another one.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class GalleryLocationOpener(Action<string> setStatus) : ILocationOpener
{
    public bool Open(LocationLink target)
    {
        setStatus($"Definition in {target.Uri} (the Gallery does not open other documents)");
        return true;
    }
}
