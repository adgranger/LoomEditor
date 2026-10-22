// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Decorations;

/// <summary>
/// The normalized options of a decoration or tracked range (Monaco: <c>ModelDecorationOptions</c>).
/// Build one with <see cref="Register"/> (kept in a <c>static readonly</c> field) or <see cref="CreateDynamic"/>.
/// </summary>
/// <remarks>
/// The options Monaco has and this class does not: see <see cref="ModelDecorationOptionsData"/>.
/// <see cref="AffectsFont"/> is always <c>false</c> (font options are not ported). Threading: immutable; thread-safe.
/// </remarks>
internal sealed class ModelDecorationOptions
{
    private ModelDecorationOptions(ModelDecorationOptionsData data)
    {
        Description = data.Description;
        Stickiness = data.Stickiness ?? TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges;
        ZIndex = data.ZIndex ?? 0;
        ClassName = CleanOrNull(data.ClassName);
        ShouldFillLineOnLineBreak = data.ShouldFillLineOnLineBreak ?? false;
        IsWholeLine = data.IsWholeLine ?? false;
        ShowIfCollapsed = data.ShowIfCollapsed ?? false;
        CollapseOnReplaceEdit = data.CollapseOnReplaceEdit ?? false;
        GlyphMarginClassName = CleanOrNull(data.GlyphMarginClassName);
        LinesDecorationsClassName = CleanOrNull(data.LinesDecorationsClassName);
        FirstLineDecorationClassName = CleanOrNull(data.FirstLineDecorationClassName);
        MarginClassName = CleanOrNull(data.MarginClassName);
        InlineClassName = CleanOrNull(data.InlineClassName);
        InlineClassNameAffectsLetterSpacing = data.InlineClassNameAffectsLetterSpacing ?? false;
        BeforeContentClassName = CleanOrNull(data.BeforeContentClassName);
        AfterContentClassName = CleanOrNull(data.AfterContentClassName);
        HideInCommentTokens = data.HideInCommentTokens ?? false;
        HideInStringTokens = data.HideInStringTokens ?? false;
        After = data.After is null ? null : ModelDecorationInjectedTextOptions.From(data.After);
        Before = data.Before is null ? null : ModelDecorationInjectedTextOptions.From(data.Before);
    }

    /// <summary>Monaco: <c>ModelDecorationOptions.EMPTY</c>.</summary>
    public static ModelDecorationOptions Empty { get; } = Register(new ModelDecorationOptionsData { Description = "empty" });

    public string Description { get; }

    public TrackedRangeStickiness Stickiness { get; }

    public int ZIndex { get; }

    public string? ClassName { get; }

    public bool ShouldFillLineOnLineBreak { get; }

    public bool IsWholeLine { get; }

    public bool ShowIfCollapsed { get; }

    /// <summary>Whether the decoration injects text before or after its range (Monaco: <c>isOptionsInjectedText</c>).</summary>
    public bool HasInjectedText => Before is not null || After is not null;

    public bool CollapseOnReplaceEdit { get; }

    public string? GlyphMarginClassName { get; }

    public string? LinesDecorationsClassName { get; }

    public string? FirstLineDecorationClassName { get; }

    public string? MarginClassName { get; }

    public string? InlineClassName { get; }

    public bool InlineClassNameAffectsLetterSpacing { get; }

    public string? BeforeContentClassName { get; }

    public string? AfterContentClassName { get; }

    public bool HideInCommentTokens { get; }

    public bool HideInStringTokens { get; }

    public ModelDecorationInjectedTextOptions? After { get; }

    public ModelDecorationInjectedTextOptions? Before { get; }

    /// <summary>Always <c>false</c>: the font options of Monaco are not ported.</summary>
    public bool AffectsFont { get; }

    /// <summary>Creates options meant to live for the life of the program (keep them in a static field).</summary>
    public static ModelDecorationOptions Register(ModelDecorationOptionsData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new ModelDecorationOptions(data);
    }

    /// <summary>Creates options built at run time.</summary>
    public static ModelDecorationOptions CreateDynamic(ModelDecorationOptionsData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new ModelDecorationOptions(data);
    }

    /// <summary>
    /// Replaces every character that is not <c>a-z</c>, <c>A-Z</c>, <c>0-9</c>, <c>-</c> or <c>_</c> with a space
    /// (a loop, not a regex: JavaScript's class is ASCII-only).
    /// </summary>
    internal static string CleanClassName(string className)
    {
        return string.Create(className.Length, className, static (span, source) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                char c = source[i];
                bool keep = c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_';
                span[i] = keep ? c : ' ';
            }
        });
    }

    // Monaco: `options.x ? cleanClassName(options.x) : null` (an empty string gives null too).
    private static string? CleanOrNull(string? className)
    {
        return string.IsNullOrEmpty(className) ? null : CleanClassName(className);
    }
}
