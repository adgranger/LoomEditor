using Avalonia;
using LoomEditor.Avalonia.Controls.Comments;
using LoomEditor.Avalonia.Controls.Overlay;
using LoomEditor.Core.Comments;

namespace LoomEditor.Avalonia.Controls;

/// <remarks>Threading: UI thread only (not checked).</remarks>
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="CommentGutterButtonEnabled"/> styled property.</summary>
    public static readonly StyledProperty<bool> CommentGutterButtonEnabledProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(CommentGutterButtonEnabled), false);

    // The box of a thread over its zone, for the tests.
    internal CommentThreadWidget? CommentWidgetOf(CommentThread thread) => _commentZoneHost?.WidgetOf(thread);

    // The gutter "+", for the tests.
    internal CommentGutterController? CommentGutter => _commentGutter;

    /// <summary>
    /// Whether hovering the line numbers shows a "+" button that raises <see cref="AddCommentRequested"/> for that line. Defaults to
    /// <see langword="false"/>. Needs the template part <c>PART_ZoneWidgetCanvas</c>.
    /// </summary>
    public bool CommentGutterButtonEnabled
    {
        get => GetValue(CommentGutterButtonEnabledProperty);
        set => SetValue(CommentGutterButtonEnabledProperty, value);
    }

    // The optional PART_ZoneWidgetCanvas also hosts the gutter "+" and a box over each comment thread's zone; without the part there is no gutter button.
    private void InitializeCommentGutter(OverlayHost? layer)
    {
        _commentGutter?.Dispose();
        _commentGutter = null;
        _commentZoneHost?.Dispose();
        _commentZoneHost = null;
        if (layer is null)
        {
            return;
        }

        _commentGutter = new CommentGutterController(this, layer, _commentThreads, CreateMouseTarget) { Enabled = CommentGutterButtonEnabled };
        _commentGutter.ApplyTheme(EffectiveTheme);
        _commentZoneHost = new CommentZoneHost(this, layer, _commentThreads);
        _commentZoneHost.ApplyTheme(EffectiveTheme, _systemHighContrastTheme is not null);
    }

    private static void InitializeCommentThreadsProperties() =>
        CommentGutterButtonEnabledProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) =>
        {
            if (editor._commentGutter is { } gutter)
            {
                gutter.Enabled = (bool)e.NewValue!;
            }
        });

    internal void InvalidateComments() => Invalidate();
}
