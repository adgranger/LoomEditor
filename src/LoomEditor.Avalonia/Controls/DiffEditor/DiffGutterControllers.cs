using LoomEditor.Themes;
using LoomEditor.View.Controller;

namespace LoomEditor.Avalonia.Controls.DiffEditor;

/// <summary>
/// The phase 14 gutter controls of one side editor of a <see cref="LoomDiffEditor"/>: the block action buttons and the line checkboxes. The editor forwards its
/// draw, pointer and theme calls to both.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DiffGutterControllers(DiffBlockActionsController blockActions) : IDisposable
{
    public DiffBlockActionsController BlockActions { get; } = blockActions;

    // P15-30: the line checkboxes controller joins the block actions here.

    public void Arrange()
    {
        BlockActions.Arrange();
    }

    public void OnPointerMoved(MouseTarget target, double x, double y)
    {
        BlockActions.OnPointerMoved(target, x, y);
    }

    public void OnPointerLeft()
    {
        BlockActions.OnPointerLeft();
    }

    public void ApplyTheme(EditorTheme theme)
    {
        BlockActions.ApplyTheme(theme);
    }

    public void Dispose()
    {
        BlockActions.Dispose();
    }
}
