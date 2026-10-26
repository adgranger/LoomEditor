using LoomEditor.Core.Languages.Features;

namespace LoomEditor.View.Features.GotoDefinition;

/// <summary>Opens a definition that is not in the document of the editor; the application provides it.</summary>
/// <remarks>Threading: called on the UI thread.</remarks>
public interface ILocationOpener
{
    /// <summary>
    /// Opens the document at <c>target.Uri</c> and selects <c>target.TargetSelectionRange ?? target.Range</c>.
    /// </summary>
    /// <param name="target">The definition to open.</param>
    /// <returns><see langword="false"/> if the document cannot be opened.</returns>
    bool Open(LocationLink target);
}
