using Avalonia;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Folding;

namespace LoomEditor.Avalonia.Controls;

// The styled properties, forwarded to the DiffEditorOptions and to the two editors. Monaco: `diffEditorEditors.ts`
// `_adjustOptionsForSubEditor`, `_adjustOptionsForLeftHandSide`, `_adjustOptionsForRightHandSide`.
public sealed partial class LoomDiffEditor
{
    /// <summary>The features of the two editors: the default ones plus the diff navigation, without folding (Monaco turns folding off in a diff editor).</summary>
    private IReadOnlyList<Func<IEditorFeature>>? _subEditorFeatures;

    /// <summary>Identifies the <see cref="RenderSideBySide"/> styled property.</summary>
    public static readonly StyledProperty<bool> RenderSideBySideProperty = Register(
        nameof(RenderSideBySide), true, (editor, value) => editor._options.RenderSideBySide = value);

    /// <summary>Identifies the <see cref="IgnoreTrimWhitespace"/> styled property.</summary>
    public static readonly StyledProperty<bool> IgnoreTrimWhitespaceProperty = Register(
        nameof(IgnoreTrimWhitespace), true, (editor, value) => editor._options.IgnoreTrimWhitespace = value);

    /// <summary>Identifies the <see cref="RenderIndicators"/> styled property.</summary>
    public static readonly StyledProperty<bool> RenderIndicatorsProperty = Register(
        nameof(RenderIndicators), true, (editor, value) => editor._options.RenderIndicators = value);

    /// <summary>Identifies the <see cref="OriginalEditable"/> styled property.</summary>
    public static readonly StyledProperty<bool> OriginalEditableProperty = Register(
        nameof(OriginalEditable), false, (editor, value) =>
        {
            editor._options.OriginalEditable = value;
            editor.ConfigureEditors();
        });

    /// <summary>Identifies the <see cref="IsReadOnly"/> styled property.</summary>
    public static readonly StyledProperty<bool> IsReadOnlyProperty = Register(
        nameof(IsReadOnly), false, (editor, value) =>
        {
            editor._options.ReadOnly = value;
            editor.ConfigureEditors();
        });

    /// <summary>Identifies the <see cref="HideUnchangedRegions"/> styled property.</summary>
    public static readonly StyledProperty<bool> HideUnchangedRegionsProperty = Register(
        nameof(HideUnchangedRegions), false, (editor, value) =>
        {
            editor._options.HideUnchangedRegions = editor._options.HideUnchangedRegions with { Enabled = value };
            editor.ConfigureEditors();
        });

    /// <summary>Identifies the <see cref="HideUnchangedRegionsRevealLineCount"/> styled property.</summary>
    public static readonly StyledProperty<int> HideUnchangedRegionsRevealLineCountProperty = RegisterCount(
        nameof(HideUnchangedRegionsRevealLineCount), 20, (editor, value) => editor._options.HideUnchangedRegions = editor._options.HideUnchangedRegions with { RevealLineCount = value });

    /// <summary>Identifies the <see cref="HideUnchangedRegionsContextLineCount"/> styled property.</summary>
    public static readonly StyledProperty<int> HideUnchangedRegionsContextLineCountProperty = RegisterCount(
        nameof(HideUnchangedRegionsContextLineCount), 3, (editor, value) => editor._options.HideUnchangedRegions = editor._options.HideUnchangedRegions with { ContextLineCount = value });

    /// <summary>Identifies the <see cref="HideUnchangedRegionsMinimumLineCount"/> styled property.</summary>
    public static readonly StyledProperty<int> HideUnchangedRegionsMinimumLineCountProperty = RegisterCount(
        nameof(HideUnchangedRegionsMinimumLineCount), 3, (editor, value) => editor._options.HideUnchangedRegions = editor._options.HideUnchangedRegions with { MinimumLineCount = value });

    /// <summary>Identifies the <see cref="UseInlineViewWhenSpaceIsLimited"/> styled property.</summary>
    public static readonly StyledProperty<bool> UseInlineViewWhenSpaceIsLimitedProperty = Register(
        nameof(UseInlineViewWhenSpaceIsLimited), true, (editor, value) => editor._options.UseInlineViewWhenSpaceIsLimited = value);

    /// <summary>Identifies the <see cref="EnableSplitViewResizing"/> styled property.</summary>
    public static readonly StyledProperty<bool> EnableSplitViewResizingProperty = Register(
        nameof(EnableSplitViewResizing), true, (editor, value) => editor._options.EnableSplitViewResizing = value);

    /// <summary>Identifies the <see cref="RenderOverviewRuler"/> styled property.</summary>
    public static readonly StyledProperty<bool> RenderOverviewRulerProperty = Register(
        nameof(RenderOverviewRuler), true, (editor, value) => editor._options.RenderOverviewRuler = value);

    /// <summary>Identifies the <see cref="MaxComputationTimeMs"/> styled property.</summary>
    public static readonly StyledProperty<int> MaxComputationTimeMsProperty = RegisterCount(
        nameof(MaxComputationTimeMs), 5000, (editor, value) => editor._options.MaxComputationTimeMs = value);

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

    /// <summary>Side by side (true) or inline (false). Defaults to <c>true</c>.</summary>
    public bool RenderSideBySide
    {
        get => GetValue(RenderSideBySideProperty);
        set => SetValue(RenderSideBySideProperty, value);
    }

    /// <summary>Whether whitespace changes at the start and end of lines are ignored. Defaults to <c>true</c>.</summary>
    public bool IgnoreTrimWhitespace
    {
        get => GetValue(IgnoreTrimWhitespaceProperty);
        set => SetValue(IgnoreTrimWhitespaceProperty, value);
    }

    /// <summary>Whether the + / - indicators are drawn in the margin. Defaults to <c>true</c>.</summary>
    public bool RenderIndicators
    {
        get => GetValue(RenderIndicatorsProperty);
        set => SetValue(RenderIndicatorsProperty, value);
    }

    /// <summary>Whether the original side can be edited. Defaults to <c>false</c>.</summary>
    public bool OriginalEditable
    {
        get => GetValue(OriginalEditableProperty);
        set => SetValue(OriginalEditableProperty, value);
    }

    /// <summary>Whether the modified side is read-only. Defaults to <c>false</c>.</summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Whether long unchanged blocks are collapsed behind a bar. Defaults to <c>false</c>.</summary>
    public bool HideUnchangedRegions
    {
        get => GetValue(HideUnchangedRegionsProperty);
        set => SetValue(HideUnchangedRegionsProperty, value);
    }

    /// <summary>How many lines one click on the edge of a collapsed bar reveals. Defaults to 20.</summary>
    public int HideUnchangedRegionsRevealLineCount
    {
        get => GetValue(HideUnchangedRegionsRevealLineCountProperty);
        set => SetValue(HideUnchangedRegionsRevealLineCountProperty, value);
    }

    /// <summary>How many unchanged lines stay visible around each change. Defaults to 3.</summary>
    public int HideUnchangedRegionsContextLineCount
    {
        get => GetValue(HideUnchangedRegionsContextLineCountProperty);
        set => SetValue(HideUnchangedRegionsContextLineCountProperty, value);
    }

    /// <summary>The minimum number of lines an unchanged region needs to be collapsed. Defaults to 3.</summary>
    public int HideUnchangedRegionsMinimumLineCount
    {
        get => GetValue(HideUnchangedRegionsMinimumLineCountProperty);
        set => SetValue(HideUnchangedRegionsMinimumLineCountProperty, value);
    }

    /// <summary>Whether a narrow control switches to the inline view. Defaults to <c>true</c>.</summary>
    public bool UseInlineViewWhenSpaceIsLimited
    {
        get => GetValue(UseInlineViewWhenSpaceIsLimitedProperty);
        set => SetValue(UseInlineViewWhenSpaceIsLimitedProperty, value);
    }

    /// <summary>Whether the sash between the editors can be dragged. Defaults to <c>true</c>.</summary>
    public bool EnableSplitViewResizing
    {
        get => GetValue(EnableSplitViewResizingProperty);
        set => SetValue(EnableSplitViewResizingProperty, value);
    }

    /// <summary>Whether the overview ruler is drawn (by a later card; the room for it is reserved now). Defaults to <c>true</c>.</summary>
    public bool RenderOverviewRuler
    {
        get => GetValue(RenderOverviewRulerProperty);
        set => SetValue(RenderOverviewRulerProperty, value);
    }

    /// <summary>The time budget of a diff computation, in milliseconds. Defaults to 5000.</summary>
    public int MaxComputationTimeMs
    {
        get => GetValue(MaxComputationTimeMsProperty);
        set => SetValue(MaxComputationTimeMsProperty, value);
    }

#pragma warning disable AVP1001 // Only called from the static field initializers above.
    private static StyledProperty<int> RegisterCount(string name, int defaultValue, Action<LoomDiffEditor, int> apply)
    {
        StyledProperty<int> property = AvaloniaProperty.Register<LoomDiffEditor, int>(name, defaultValue);
        property.Changed.AddClassHandler<LoomDiffEditor>((editor, e) => apply(editor, (int)e.NewValue!));
        return property;
    }

    private static StyledProperty<bool> Register(string name, bool defaultValue, Action<LoomDiffEditor, bool> apply)
    {
        StyledProperty<bool> property = AvaloniaProperty.Register<LoomDiffEditor, bool>(name, defaultValue);
        property.Changed.AddClassHandler<LoomDiffEditor>((editor, e) => apply(editor, (bool)e.NewValue!));
        return property;
    }
#pragma warning restore AVP1001

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
            // Monaco: sticky scroll is turned off for both editors only while unchanged regions are hidden.
            editor.StickyScrollEnabled = !HideUnchangedRegions;
            editor.LineHeight = _lineHeight;
        }

        original.IsReadOnly = !OriginalEditable;
        modified.IsReadOnly = IsReadOnly;
    }
}
