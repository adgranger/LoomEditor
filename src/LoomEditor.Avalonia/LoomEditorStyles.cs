using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace LoomEditor.Avalonia;

/// <summary>The styles of the LoomEditor controls: add it to <c>Application.Styles</c> after your theme.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class LoomEditorStyles : Styles
{
    /// <summary>Loads the control templates (<c>Themes/Generic.axaml</c>) and the icons (<c>Themes/Icons.axaml</c>).</summary>
    public LoomEditorStyles()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
