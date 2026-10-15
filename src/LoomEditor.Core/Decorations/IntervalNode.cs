// Ported from microsoft/vscode: src/vs/editor/common/model/intervalTree.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Decorations;

/// <summary>
/// A node of the <see cref="IntervalTree"/>: an interval plus its red-black links and its packed metadata.
/// Positions are relative to the node's ancestors (see <see cref="Delta"/>).
/// </summary>
/// <remarks>Threading: mutable, not thread-safe; owned by one interval tree.</remarks>
internal sealed class IntervalNode
{
    private const int ColorMask = 0b00000001;
    private const int ColorMaskInverse = 0b11111110;
    private const int ColorOffset = 0;

    private const int IsVisitedMask = 0b00000010;
    private const int IsVisitedMaskInverse = 0b11111101;
    private const int IsVisitedOffset = 1;

    private const int IsForValidationMask = 0b00000100;
    private const int IsForValidationMaskInverse = 0b11111011;
    private const int IsForValidationOffset = 2;

    private const int StickinessMask = 0b00011000;
    private const int StickinessMaskInverse = 0b11100111;
    private const int StickinessOffset = 3;

    private const int CollapseOnReplaceEditMask = 0b00100000;
    private const int CollapseOnReplaceEditMaskInverse = 0b11011111;
    private const int CollapseOnReplaceEditOffset = 5;

    private const int IsMarginMask = 0b01000000;
    private const int IsMarginMaskInverse = 0b10111111;
    private const int IsMarginOffset = 6;

    private const int AffectsFontMask = 0b10000000;
    private const int AffectsFontMaskInverse = 0b01111111;
    private const int AffectsFontOffset = 7;

    private const string ErrorClassName = "squiggly-error";
    private const string WarningClassName = "squiggly-warning";
    private const string InfoClassName = "squiggly-info";

    /// <summary>Binary encoded information for color, visited, isForValidation, stickiness, and so on.</summary>
    public int Metadata;

    public IntervalNode Parent;
    public IntervalNode Left;
    public IntervalNode Right;

    public long Start;
    public long End;
    public long Delta;
    public long MaxEnd;

    public string Id;
    public int OwnerId;
    public ModelDecorationOptions Options;

    public int CachedVersionId;
    public long CachedAbsoluteStart;
    public long CachedAbsoluteEnd;
    public Range? Range;

    public IntervalNode(string id, long start, long end)
    {
        Metadata = 0;

        Parent = this;
        Left = this;
        Right = this;
        Color = NodeColor.Red;

        Start = start;
        End = end;
        // FORCE_OVERFLOWING_TEST: Delta = start;
        Delta = 0;
        MaxEnd = end;

        Id = id;
        OwnerId = 0;
        Options = null!;
        IsForValidation = false;
        IsInGlyphMargin = false;
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges;
        CollapseOnReplaceEdit = false;
        AffectsFont = false;

        CachedVersionId = 0;
        CachedAbsoluteStart = start;
        CachedAbsoluteEnd = end;
        Range = null;

        IsVisited = false;
    }

    public NodeColor Color
    {
        get => (NodeColor)((Metadata & ColorMask) >>> ColorOffset);
        set => Metadata = (Metadata & ColorMaskInverse) | ((int)value << ColorOffset);
    }

    public bool IsVisited
    {
        get => ((Metadata & IsVisitedMask) >>> IsVisitedOffset) == 1;
        set => Metadata = (Metadata & IsVisitedMaskInverse) | ((value ? 1 : 0) << IsVisitedOffset);
    }

    public bool IsForValidation
    {
        get => ((Metadata & IsForValidationMask) >>> IsForValidationOffset) == 1;
        set => Metadata = (Metadata & IsForValidationMaskInverse) | ((value ? 1 : 0) << IsForValidationOffset);
    }

    public bool IsInGlyphMargin
    {
        get => ((Metadata & IsMarginMask) >>> IsMarginOffset) == 1;
        set => Metadata = (Metadata & IsMarginMaskInverse) | ((value ? 1 : 0) << IsMarginOffset);
    }

    public bool AffectsFont
    {
        get => ((Metadata & AffectsFontMask) >>> AffectsFontOffset) == 1;
        set => Metadata = (Metadata & AffectsFontMaskInverse) | ((value ? 1 : 0) << AffectsFontOffset);
    }

    public TrackedRangeStickiness Stickiness
    {
        get => (TrackedRangeStickiness)((Metadata & StickinessMask) >>> StickinessOffset);
        set => Metadata = (Metadata & StickinessMaskInverse) | ((int)value << StickinessOffset);
    }

    public bool CollapseOnReplaceEdit
    {
        get => ((Metadata & CollapseOnReplaceEditMask) >>> CollapseOnReplaceEditOffset) == 1;
        set => Metadata = (Metadata & CollapseOnReplaceEditMaskInverse) | ((value ? 1 : 0) << CollapseOnReplaceEditOffset);
    }

    public void Reset(int versionId, long start, long end, Range? range)
    {
        Start = start;
        End = end;
        MaxEnd = end;
        CachedVersionId = versionId;
        CachedAbsoluteStart = start;
        CachedAbsoluteEnd = end;
        Range = range;
    }

    public void SetOptions(ModelDecorationOptions options)
    {
        Options = options;
        var className = Options.ClassName;
        IsForValidation = className == ErrorClassName || className == WarningClassName || className == InfoClassName;
        IsInGlyphMargin = Options.GlyphMarginClassName is not null;
        Stickiness = Options.Stickiness;
        CollapseOnReplaceEdit = Options.CollapseOnReplaceEdit;
        AffectsFont = Options.AffectsFont;
    }

    public void SetCachedOffsets(long absoluteStart, long absoluteEnd, int cachedVersionId)
    {
        if (CachedVersionId != cachedVersionId)
        {
            Range = null;
        }

        CachedVersionId = cachedVersionId;
        CachedAbsoluteStart = absoluteStart;
        CachedAbsoluteEnd = absoluteEnd;
    }

    public void Detach()
    {
        Parent = null!;
        Left = null!;
        Right = null!;
    }
}
