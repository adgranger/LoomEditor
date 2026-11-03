using Avalonia;
using LoomEditor.View.Features.StickyScroll;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// Sticky scroll. Monaco: the `stickyScroll.enabled` and `stickyScroll.maxLineCount` editor options.
public sealed partial class LoomEditor
{
    private const int DefaultStickyScrollMaxLineCount = 5;

    /// <summary>Identifies the <see cref="StickyScrollEnabled"/> styled property.</summary>
    public static readonly StyledProperty<bool> StickyScrollEnabledProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(StickyScrollEnabled), true);

    /// <summary>Identifies the <see cref="StickyScrollMaxLineCount"/> styled property.</summary>
    public static readonly StyledProperty<int> StickyScrollMaxLineCountProperty =
        AvaloniaProperty.Register<LoomEditor, int>(nameof(StickyScrollMaxLineCount), DefaultStickyScrollMaxLineCount);

    /// <summary>Whether the nested current scopes stick at the top of the editor. Defaults to <see langword="true"/>.</summary>
    public bool StickyScrollEnabled
    {
        get => GetValue(StickyScrollEnabledProperty);
        set => SetValue(StickyScrollEnabledProperty, value);
    }

    /// <summary>The maximum number of sticky lines, from 1 to 20. Defaults to 5.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside 1–20.</exception>
    public int StickyScrollMaxLineCount
    {
        get => GetValue(StickyScrollMaxLineCountProperty);
        set => SetValue(StickyScrollMaxLineCountProperty, value);
    }

    // The toggle command changes the option in the view model: the properties follow it (no change, no re-entry).
    private void InitializeStickyScroll() => _viewModel.OptionsChanged += (_, _) =>
    {
        EditorStickyScrollOptions options = _viewModel.LayoutOptions.StickyScroll;
        StickyScrollEnabled = options.Enabled;
        StickyScrollMaxLineCount = options.MaxLineCount;
    };

    private static void InitializeStickyScrollProperties()
    {
        StickyScrollEnabledProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnStickyScrollChanged(e));
        StickyScrollMaxLineCountProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnStickyScrollChanged(e));
    }

    private void OnStickyScrollChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StickyScrollMaxLineCountProperty && (int)e.NewValue! is < 1 or > 20)
        {
            SetCurrentValue(StickyScrollMaxLineCountProperty, (int)e.OldValue!);
            throw new ArgumentOutOfRangeException(nameof(e), e.NewValue, "The sticky scroll maximum line count must be between 1 and 20.");
        }

        ViewModel viewModel = _viewModel;
        EditorStickyScrollOptions current = viewModel.LayoutOptions.StickyScroll;
        EditorStickyScrollOptions options = current with
        {
            Enabled = StickyScrollEnabled,
            MaxLineCount = StickyScrollMaxLineCount,
        };
        if (options != current)
        {
            viewModel.SetOptions(viewModel.LayoutOptions with { StickyScroll = options }, viewModel.ViewLayoutOptions);
        }
    }
}
