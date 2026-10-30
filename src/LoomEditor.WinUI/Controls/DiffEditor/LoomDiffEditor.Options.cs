using LoomEditor.Themes;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Folding;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// The dependency properties, forwarded to the DiffEditorOptions and to the two editors. Monaco: `diffEditorEditors.ts`
// `_adjustOptionsForSubEditor`, `_adjustOptionsForLeftHandSide`, `_adjustOptionsForRightHandSide`.
public sealed partial class LoomDiffEditor
{
    /// <summary>The features of the two editors: the default ones plus the diff navigation, without folding (Monaco turns folding off in a diff editor).</summary>
    private IReadOnlyList<Func<IEditorFeature>>? _subEditorFeatures;

    /// <summary>Identifies the <see cref="OriginalText"/> dependency property.</summary>
    public static readonly DependencyProperty OriginalTextProperty = DependencyProperty.Register(
        nameof(OriginalText), typeof(string), typeof(LoomDiffEditor),
        new PropertyMetadata(string.Empty, (d, e) => ((LoomDiffEditor)d).ApplyText(((LoomDiffEditor)d)._original, (string?)e.NewValue)));

    /// <summary>Identifies the <see cref="ModifiedText"/> dependency property.</summary>
    public static readonly DependencyProperty ModifiedTextProperty = DependencyProperty.Register(
        nameof(ModifiedText), typeof(string), typeof(LoomDiffEditor),
        new PropertyMetadata(string.Empty, (d, e) => ((LoomDiffEditor)d).ApplyText(((LoomDiffEditor)d)._modified, (string?)e.NewValue)));

    /// <summary>Identifies the <see cref="LanguageId"/> dependency property.</summary>
    public static readonly DependencyProperty LanguageIdProperty = DependencyProperty.Register(
        nameof(LanguageId), typeof(string), typeof(LoomDiffEditor),
        new PropertyMetadata("plaintext", (d, _) => ((LoomDiffEditor)d).ApplyLanguageAndTheme()));

    /// <summary>Identifies the <see cref="RenderSideBySide"/> dependency property.</summary>
    public static readonly DependencyProperty RenderSideBySideProperty = Register(
        nameof(RenderSideBySide), true, (editor, value) => editor._options.RenderSideBySide = value);

    /// <summary>Identifies the <see cref="IgnoreTrimWhitespace"/> dependency property.</summary>
    public static readonly DependencyProperty IgnoreTrimWhitespaceProperty = Register(
        nameof(IgnoreTrimWhitespace), true, (editor, value) => editor._options.IgnoreTrimWhitespace = value);

    /// <summary>Identifies the <see cref="RenderIndicators"/> dependency property.</summary>
    public static readonly DependencyProperty RenderIndicatorsProperty = Register(
        nameof(RenderIndicators), true, (editor, value) => editor._options.RenderIndicators = value);

    /// <summary>Identifies the <see cref="OriginalEditable"/> dependency property.</summary>
    public static readonly DependencyProperty OriginalEditableProperty = Register(
        nameof(OriginalEditable), false, (editor, value) =>
        {
            editor._options.OriginalEditable = value;
            editor.ConfigureEditors();
        });

    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = Register(
        nameof(IsReadOnly), false, (editor, value) =>
        {
            editor._options.ReadOnly = value;
            editor.ConfigureEditors();
        });

    /// <summary>Identifies the <see cref="HideUnchangedRegions"/> dependency property.</summary>
    public static readonly DependencyProperty HideUnchangedRegionsProperty = Register(
        nameof(HideUnchangedRegions), false, (editor, value) =>
        {
            editor._options.HideUnchangedRegions = editor._options.HideUnchangedRegions with { Enabled = value };
            editor.ConfigureEditors();
        });

    /// <summary>Identifies the <see cref="HideUnchangedRegionsRevealLineCount"/> dependency property.</summary>
    public static readonly DependencyProperty HideUnchangedRegionsRevealLineCountProperty = RegisterCount(
        nameof(HideUnchangedRegionsRevealLineCount), 20, (editor, value) => editor._options.HideUnchangedRegions = editor._options.HideUnchangedRegions with { RevealLineCount = value });

    /// <summary>Identifies the <see cref="HideUnchangedRegionsContextLineCount"/> dependency property.</summary>
    public static readonly DependencyProperty HideUnchangedRegionsContextLineCountProperty = RegisterCount(
        nameof(HideUnchangedRegionsContextLineCount), 3, (editor, value) => editor._options.HideUnchangedRegions = editor._options.HideUnchangedRegions with { ContextLineCount = value });

    /// <summary>Identifies the <see cref="HideUnchangedRegionsMinimumLineCount"/> dependency property.</summary>
    public static readonly DependencyProperty HideUnchangedRegionsMinimumLineCountProperty = RegisterCount(
        nameof(HideUnchangedRegionsMinimumLineCount), 3, (editor, value) => editor._options.HideUnchangedRegions = editor._options.HideUnchangedRegions with { MinimumLineCount = value });

    /// <summary>Identifies the <see cref="UseInlineViewWhenSpaceIsLimited"/> dependency property.</summary>
    public static readonly DependencyProperty UseInlineViewWhenSpaceIsLimitedProperty = Register(
        nameof(UseInlineViewWhenSpaceIsLimited), true, (editor, value) => editor._options.UseInlineViewWhenSpaceIsLimited = value);

    /// <summary>Identifies the <see cref="EnableSplitViewResizing"/> dependency property.</summary>
    public static readonly DependencyProperty EnableSplitViewResizingProperty = Register(
        nameof(EnableSplitViewResizing), true, (editor, value) => editor._options.EnableSplitViewResizing = value);

    /// <summary>Identifies the <see cref="RenderOverviewRuler"/> dependency property.</summary>
    public static readonly DependencyProperty RenderOverviewRulerProperty = Register(
        nameof(RenderOverviewRuler), true, (editor, value) => editor._options.RenderOverviewRuler = value);

    /// <summary>Identifies the <see cref="MaxComputationTimeMs"/> dependency property.</summary>
    public static readonly DependencyProperty MaxComputationTimeMsProperty = DependencyProperty.Register(
        nameof(MaxComputationTimeMs), typeof(int), typeof(LoomDiffEditor),
        new PropertyMetadata(5000, (d, e) => ((LoomDiffEditor)d)._options.MaxComputationTimeMs = (int)e.NewValue));

    private double _lineHeight;

    /// <summary>
    /// The line height of both editors, as <see cref="LoomEditor.LineHeight"/>: 0 is automatic, below 8 a multiplier of the font size, else DIPs (Monaco:
    /// <c>lineHeight</c>). The two sides stay aligned. Reading it gives the height in use. Defaults to 0.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, <c>NaN</c> or infinite.</exception>
    public double LineHeight
    {
        get => _modified?.LineHeight ?? _lineHeight;
        set
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The line height must be a finite number, 0 or greater.");
            }

            _lineHeight = value;
            ConfigureEditors();
        }
    }

    private EditorTheme _theme = EditorTheme.Dark;

    /// <summary>The original text. Defaults to an empty string.</summary>
    public string OriginalText
    {
        get => (string)GetValue(OriginalTextProperty);
        set => SetValue(OriginalTextProperty, value);
    }

    /// <summary>The modified text; an edit of the user in <see cref="Modified"/> is reflected here. Defaults to an empty string.</summary>
    public string ModifiedText
    {
        get => (string)GetValue(ModifiedTextProperty);
        set => SetValue(ModifiedTextProperty, value);
    }

    /// <summary>The language of both texts, see <see cref="LoomEditor.LanguageId"/>. Defaults to <c>plaintext</c>.</summary>
    public string LanguageId
    {
        get => (string)GetValue(LanguageIdProperty);
        set => SetValue(LanguageIdProperty, value);
    }

    /// <summary>
    /// The theme of both editors, see <see cref="LoomEditor.Theme"/> (a plain property for the same reason). Defaults to
    /// <see cref="EditorTheme.Dark"/>.
    /// </summary>
    public EditorTheme Theme
    {
        get => _theme;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _theme = value;
            ApplyLanguageAndTheme();
        }
    }

    /// <summary>Side by side (true) or inline (false). Defaults to <c>true</c>.</summary>
    public bool RenderSideBySide
    {
        get => (bool)GetValue(RenderSideBySideProperty);
        set => SetValue(RenderSideBySideProperty, value);
    }

    /// <summary>Whether whitespace changes at the start and end of lines are ignored. Defaults to <c>true</c>.</summary>
    public bool IgnoreTrimWhitespace
    {
        get => (bool)GetValue(IgnoreTrimWhitespaceProperty);
        set => SetValue(IgnoreTrimWhitespaceProperty, value);
    }

    /// <summary>Whether the + / - indicators are drawn in the margin. Defaults to <c>true</c>.</summary>
    public bool RenderIndicators
    {
        get => (bool)GetValue(RenderIndicatorsProperty);
        set => SetValue(RenderIndicatorsProperty, value);
    }

    /// <summary>Whether the original side can be edited. Defaults to <c>false</c>.</summary>
    public bool OriginalEditable
    {
        get => (bool)GetValue(OriginalEditableProperty);
        set => SetValue(OriginalEditableProperty, value);
    }

    /// <summary>Whether the modified side is read-only. Defaults to <c>false</c>.</summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Whether long unchanged blocks are collapsed behind a bar. Defaults to <c>false</c>.</summary>
    public bool HideUnchangedRegions
    {
        get => (bool)GetValue(HideUnchangedRegionsProperty);
        set => SetValue(HideUnchangedRegionsProperty, value);
    }

    /// <summary>How many lines one click on the edge of a collapsed bar reveals. Defaults to 20.</summary>
    public int HideUnchangedRegionsRevealLineCount
    {
        get => (int)GetValue(HideUnchangedRegionsRevealLineCountProperty);
        set => SetValue(HideUnchangedRegionsRevealLineCountProperty, value);
    }

    /// <summary>How many unchanged lines stay visible around each change. Defaults to 3.</summary>
    public int HideUnchangedRegionsContextLineCount
    {
        get => (int)GetValue(HideUnchangedRegionsContextLineCountProperty);
        set => SetValue(HideUnchangedRegionsContextLineCountProperty, value);
    }

    /// <summary>The minimum number of lines an unchanged region needs to be collapsed. Defaults to 3.</summary>
    public int HideUnchangedRegionsMinimumLineCount
    {
        get => (int)GetValue(HideUnchangedRegionsMinimumLineCountProperty);
        set => SetValue(HideUnchangedRegionsMinimumLineCountProperty, value);
    }

    /// <summary>Whether a narrow control switches to the inline view. Defaults to <c>true</c>.</summary>
    public bool UseInlineViewWhenSpaceIsLimited
    {
        get => (bool)GetValue(UseInlineViewWhenSpaceIsLimitedProperty);
        set => SetValue(UseInlineViewWhenSpaceIsLimitedProperty, value);
    }

    /// <summary>Whether the sash between the editors can be dragged. Defaults to <c>true</c>.</summary>
    public bool EnableSplitViewResizing
    {
        get => (bool)GetValue(EnableSplitViewResizingProperty);
        set => SetValue(EnableSplitViewResizingProperty, value);
    }

    /// <summary>Whether the overview ruler is drawn (by a later card; the room for it is reserved now). Defaults to <c>true</c>.</summary>
    public bool RenderOverviewRuler
    {
        get => (bool)GetValue(RenderOverviewRulerProperty);
        set => SetValue(RenderOverviewRulerProperty, value);
    }

    /// <summary>The time budget of a diff computation, in milliseconds. Defaults to 5000.</summary>
    public int MaxComputationTimeMs
    {
        get => (int)GetValue(MaxComputationTimeMsProperty);
        set => SetValue(MaxComputationTimeMsProperty, value);
    }

    private static DependencyProperty RegisterCount(string name, int defaultValue, Action<LoomDiffEditor, int> apply) =>
        DependencyProperty.Register(
            name, typeof(int), typeof(LoomDiffEditor), new PropertyMetadata(defaultValue, (d, e) => apply((LoomDiffEditor)d, (int)e.NewValue)));

    private static DependencyProperty Register(string name, bool defaultValue, Action<LoomDiffEditor, bool> apply) =>
        DependencyProperty.Register(
            name, typeof(bool), typeof(LoomDiffEditor), new PropertyMetadata(defaultValue, (d, e) => apply((LoomDiffEditor)d, (bool)e.NewValue)));

    private List<Func<IEditorFeature>> CreateSubEditorFeatures()
    {
        // Created once to recognise the folding feature by type: the factories are the only list there is.
        List<Func<IEditorFeature>> features = [];
        foreach (Func<IEditorFeature> factory in EditorFeatures.ForDiffEditor(this))
        {
            if (factory() is not FoldingFeature)
            {
                features.Add(factory);
            }
        }

        return features;
    }

    private void ApplyText(LoomEditor? editor, string? text)
    {
        // Before the template is applied OnApplyTemplate sets the texts.
        if (_isSyncingText || editor is null)
        {
            return;
        }

        _isSyncingText = true;
        try
        {
            editor.ResetText(text ?? string.Empty);
        }
        finally
        {
            _isSyncingText = false;
        }
    }

    private void ApplyLanguageAndTheme()
    {
        foreach (LoomEditor? editor in new[] { _original, _modified })
        {
            if (editor is not null)
            {
                editor.LanguageId = LanguageId;
                editor.Theme = Theme;
            }
        }

        InvalidateOverview();
    }

    // Everything else the sub-editors get from this control. Safe to call again: it only assigns properties.
    private void ConfigureEditors()
    {
        if (_original is not { } original || _modified is not { } modified)
        {
            return;
        }

        foreach (LoomEditor editor in new[] { original, modified })
        {
            editor.Features = _subEditorFeatures ??= CreateSubEditorFeatures();
            editor.IsInDiffEditor = true;
            editor.MinimapEnabled = false;
            editor.OverviewRulerEnabled = false;
            // Monaco: sticky scroll is turned off for both editors only while unchanged regions are hidden.
            editor.StickyScrollEnabled = !HideUnchangedRegions;
            editor.LineHeight = _lineHeight;
        }

        original.IsReadOnly = !OriginalEditable;
        modified.IsReadOnly = IsReadOnly;
    }
}
