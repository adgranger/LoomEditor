using LoomEditor.Themes;
using LoomEditor.View.Controller;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls.DiffEditor;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.DiffEditor;
#endif

/// <summary>
/// The phase 14 gutter controls of one side editor of a <see cref="LoomDiffEditor"/>: the block action buttons and the line checkboxes. The editor forwards its
/// draw, pointer and theme calls to both.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DiffGutterControllers(DiffBlockActionsController blockActions, DiffLineCheckBoxesController lineCheckBoxes) : IDisposable
{
    public DiffBlockActionsController BlockActions { get; } = blockActions;

    public DiffLineCheckBoxesController LineCheckBoxes { get; } = lineCheckBoxes;

    public void Arrange()
    {
        BlockActions.Arrange();
        LineCheckBoxes.Arrange();
    }

    public void OnPointerMoved(MouseTarget target, double x, double y)
    {
        BlockActions.OnPointerMoved(target, x, y);
        LineCheckBoxes.OnPointerMoved(target, x, y);
    }

    public void OnPointerLeft()
    {
        BlockActions.OnPointerLeft();
        LineCheckBoxes.OnPointerLeft();
    }

    public void ApplyTheme(EditorTheme theme)
    {
        BlockActions.ApplyTheme(theme);
        LineCheckBoxes.ApplyTheme(theme);
    }

    public void Dispose()
    {
        BlockActions.Dispose();
        LineCheckBoxes.Dispose();
    }
}
