using LoomEditor.View.Features.StickyScroll;
using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Sticky scroll. Monaco: the `stickyScroll.enabled` and `stickyScroll.maxLineCount` editor options.
public sealed partial class LoomEditor
{
    private const int DefaultStickyScrollMaxLineCount = 5;

    /// <summary>Identifies the <see cref="StickyScrollEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty StickyScrollEnabledProperty = DependencyProperty.Register(
        nameof(StickyScrollEnabled), typeof(bool), typeof(LoomEditor), new PropertyMetadata(true, OnStickyScrollChanged));

    /// <summary>Identifies the <see cref="StickyScrollMaxLineCount"/> dependency property.</summary>
    public static readonly DependencyProperty StickyScrollMaxLineCountProperty = DependencyProperty.Register(
        nameof(StickyScrollMaxLineCount), typeof(int), typeof(LoomEditor), new PropertyMetadata(DefaultStickyScrollMaxLineCount, OnStickyScrollChanged));

    /// <summary>Whether the nested current scopes stick at the top of the editor. Defaults to <see langword="true"/>.</summary>
    public bool StickyScrollEnabled
    {
        get => (bool)GetValue(StickyScrollEnabledProperty);
        set => SetValue(StickyScrollEnabledProperty, value);
    }

    /// <summary>The maximum number of sticky lines, from 1 to 20. Defaults to 5.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside 1–20.</exception>
    public int StickyScrollMaxLineCount
    {
        get => (int)GetValue(StickyScrollMaxLineCountProperty);
        set => SetValue(StickyScrollMaxLineCountProperty, value);
    }

    // The toggle command changes the option in the view model: the properties follow it (no change, no re-entry).
    private void InitializeStickyScroll() => _viewModel.OptionsChanged += (_, _) =>
    {
        EditorStickyScrollOptions options = _viewModel.LayoutOptions.StickyScroll;
        StickyScrollEnabled = options.Enabled;
        StickyScrollMaxLineCount = options.MaxLineCount;
    };

    private static void OnStickyScrollChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        if (e.Property == StickyScrollMaxLineCountProperty && (int)e.NewValue is < 1 or > 20)
        {
            editor.SetValue(StickyScrollMaxLineCountProperty, e.OldValue);
            throw new ArgumentOutOfRangeException(nameof(e), e.NewValue, "The sticky scroll maximum line count must be between 1 and 20.");
        }

        ViewModel viewModel = editor._viewModel;
        EditorStickyScrollOptions current = viewModel.LayoutOptions.StickyScroll;
        EditorStickyScrollOptions options = current with
        {
            Enabled = editor.StickyScrollEnabled,
            MaxLineCount = editor.StickyScrollMaxLineCount,
        };
        if (options != current)
        {
            viewModel.SetOptions(viewModel.LayoutOptions with { StickyScroll = options }, viewModel.ViewLayoutOptions);
        }
    }
}
