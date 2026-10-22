using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Word wrap options. Monaco: the `wordWrap`, `wordWrapColumn` and `wrappingIndent` editor options.
public sealed partial class LoomEditor
{
    private const int DefaultWordWrapColumn = 80;

    /// <summary>Identifies the <see cref="WordWrap"/> dependency property.</summary>
    public static readonly DependencyProperty WordWrapProperty = DependencyProperty.Register(
        nameof(WordWrap), typeof(EditorWordWrap), typeof(LoomEditor), new PropertyMetadata(EditorWordWrap.Off, OnWrappingChanged));

    /// <summary>Identifies the <see cref="WordWrapColumn"/> dependency property.</summary>
    public static readonly DependencyProperty WordWrapColumnProperty = DependencyProperty.Register(
        nameof(WordWrapColumn), typeof(int), typeof(LoomEditor), new PropertyMetadata(DefaultWordWrapColumn, OnWrappingChanged));

    /// <summary>Identifies the <see cref="WrappingIndent"/> dependency property.</summary>
    public static readonly DependencyProperty WrappingIndentProperty = DependencyProperty.Register(
        nameof(WrappingIndent), typeof(EditorWrappingIndent), typeof(LoomEditor), new PropertyMetadata(EditorWrappingIndent.Same, OnWrappingChanged));

    /// <summary>
    /// When lines wrap: <see cref="EditorWordWrap.Off"/> never, <see cref="EditorWordWrap.On"/> at the viewport width,
    /// <see cref="EditorWordWrap.WordWrapColumn"/> at <see cref="WordWrapColumn"/>, <see cref="EditorWordWrap.Bounded"/> at the
    /// minimum of both. Defaults to <see cref="EditorWordWrap.Off"/>.
    /// </summary>
    public EditorWordWrap WordWrap
    {
        get => (EditorWordWrap)GetValue(WordWrapProperty);
        set => SetValue(WordWrapProperty, value);
    }

    /// <summary>The column lines wrap at with <see cref="EditorWordWrap.WordWrapColumn"/> and <see cref="EditorWordWrap.Bounded"/>. Defaults to 80.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1; the previous value is kept.</exception>
    public int WordWrapColumn
    {
        get => (int)GetValue(WordWrapColumnProperty);
        set => SetValue(WordWrapColumnProperty, value);
    }

    /// <summary>How wrapped continuation lines are indented. Defaults to <see cref="EditorWrappingIndent.Same"/>.</summary>
    public EditorWrappingIndent WrappingIndent
    {
        get => (EditorWrappingIndent)GetValue(WrappingIndentProperty);
        set => SetValue(WrappingIndentProperty, value);
    }

    private static void OnWrappingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        if (ReferenceEquals(e.Property, WordWrapColumnProperty) && (int)e.NewValue < 1)
        {
            editor.SetValue(WordWrapColumnProperty, e.OldValue);
            throw new ArgumentOutOfRangeException(nameof(e), e.NewValue, "The word wrap column must be at least 1.");
        }

        ViewModel viewModel = editor._viewModel;
        EditorWrappingOptions wrapping = viewModel.LayoutOptions.Wrapping with
        {
            WordWrap = editor.WordWrap switch
            {
                EditorWordWrap.On => View.Layout.WordWrap.On,
                EditorWordWrap.WordWrapColumn => View.Layout.WordWrap.WordWrapColumn,
                EditorWordWrap.Bounded => View.Layout.WordWrap.Bounded,
                _ => View.Layout.WordWrap.Off,
            },
            WordWrapColumn = editor.WordWrapColumn,
            WrappingIndent = editor.WrappingIndent switch
            {
                EditorWrappingIndent.None => View.Layout.WrappingIndent.None,
                EditorWrappingIndent.Indent => View.Layout.WrappingIndent.Indent,
                EditorWrappingIndent.DeepIndent => View.Layout.WrappingIndent.DeepIndent,
                _ => View.Layout.WrappingIndent.Same,
            },
        };
        viewModel.SetOptions(viewModel.LayoutOptions with { Wrapping = wrapping }, viewModel.ViewLayoutOptions);
    }
}
