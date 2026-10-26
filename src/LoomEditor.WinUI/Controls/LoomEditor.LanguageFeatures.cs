using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;

namespace LoomEditor.WinUI.Controls;

public sealed partial class LoomEditor
{
    /// <summary>The providers of completion, hover and definition for this editor.</summary>
    public LanguageFeaturesService LanguageFeatures => _context.LanguageFeatures;

    /// <summary>The markers of this editor; <c>Markers.Change(owner, ...)</c> puts squiggles in the text.</summary>
    public MarkerService Markers => _context.Markers;

    /// <summary>The URI of the document, used when the editor is connected to a language server.</summary>
    /// <exception cref="ArgumentException">The value is null or empty.</exception>
    public string DocumentUri
    {
        get => _context.DocumentUri;
        set => _context.DocumentUri = value;
    }
}
