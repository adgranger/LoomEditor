using Avalonia;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// Word wrap options. Monaco: the `wordWrap`, `wordWrapColumn` and `wrappingIndent` editor options.
public sealed partial class LoomEditor
{
    private const int DefaultWordWrapColumn = 80;

    /// <summary>Identifies the <see cref="WordWrap"/> styled property.</summary>
    public static readonly StyledProperty<EditorWordWrap> WordWrapProperty =
        AvaloniaProperty.Register<LoomEditor, EditorWordWrap>(nameof(WordWrap), EditorWordWrap.Off);

    /// <summary>Identifies the <see cref="WordWrapColumn"/> styled property.</summary>
    public static readonly StyledProperty<int> WordWrapColumnProperty =
        AvaloniaProperty.Register<LoomEditor, int>(nameof(WordWrapColumn), DefaultWordWrapColumn);

    /// <summary>Identifies the <see cref="WrappingIndent"/> styled property.</summary>
    public static readonly StyledProperty<EditorWrappingIndent> WrappingIndentProperty =
        AvaloniaProperty.Register<LoomEditor, EditorWrappingIndent>(nameof(WrappingIndent), EditorWrappingIndent.Same);

    /// <summary>
    /// When lines wrap: <see cref="EditorWordWrap.Off"/> never, <see cref="EditorWordWrap.On"/> at the viewport width,
    /// <see cref="EditorWordWrap.WordWrapColumn"/> at <see cref="WordWrapColumn"/>, <see cref="EditorWordWrap.Bounded"/> at the
    /// minimum of both. Defaults to <see cref="EditorWordWrap.Off"/>.
    /// </summary>
    public EditorWordWrap WordWrap
    {
        get => GetValue(WordWrapProperty);
        set => SetValue(WordWrapProperty, value);
    }

    /// <summary>The column lines wrap at with <see cref="EditorWordWrap.WordWrapColumn"/> and <see cref="EditorWordWrap.Bounded"/>. Defaults to 80.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1; the previous value is kept.</exception>
    public int WordWrapColumn
    {
        get => GetValue(WordWrapColumnProperty);
        set => SetValue(WordWrapColumnProperty, value);
    }

    /// <summary>How wrapped continuation lines are indented. Defaults to <see cref="EditorWrappingIndent.Same"/>.</summary>
    public EditorWrappingIndent WrappingIndent
    {
        get => GetValue(WrappingIndentProperty);
        set => SetValue(WrappingIndentProperty, value);
    }

    private static void InitializeWordWrapProperties()
    {
        WordWrapProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnWrappingChanged(e));
        WordWrapColumnProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnWrappingChanged(e));
        WrappingIndentProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnWrappingChanged(e));
    }

    private void OnWrappingChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WordWrapColumnProperty && (int)e.NewValue! < 1)
        {
            SetCurrentValue(WordWrapColumnProperty, (int)e.OldValue!);
            throw new ArgumentOutOfRangeException(nameof(e), e.NewValue, "The word wrap column must be at least 1.");
        }

        ViewModel viewModel = _viewModel;
        EditorWrappingOptions wrapping = viewModel.LayoutOptions.Wrapping with
        {
            WordWrap = WordWrap switch
            {
                EditorWordWrap.On => View.Layout.WordWrap.On,
                EditorWordWrap.WordWrapColumn => View.Layout.WordWrap.WordWrapColumn,
                EditorWordWrap.Bounded => View.Layout.WordWrap.Bounded,
                _ => View.Layout.WordWrap.Off,
            },
            WordWrapColumn = WordWrapColumn,
            WrappingIndent = WrappingIndent switch
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
