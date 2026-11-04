#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

/// <summary>Which of the two block action buttons of a <see cref="LoomDiffEditor"/> was clicked.</summary>
public enum DiffBlockAction
{
    /// <summary>The button of <see cref="LoomDiffEditor.PrimaryBlockActionGlyph"/>.</summary>
    Primary,

    /// <summary>The button of <see cref="LoomDiffEditor.SecondaryBlockActionGlyph"/>.</summary>
    Secondary,
}
