// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/registrations.contribution.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.View.DiffEditor;

/// <summary>
/// The decoration options of the diff editor, named like Monaco's exports. What each class name draws is in
/// <c>DecorationStyles</c>.
/// </summary>
/// <remarks>
/// Behavior differences: the codicon part of <c>linesDecorationsClassName</c> is dropped (<c>insert-sign</c> /
/// <c>delete-sign</c> draw a plus / minus themselves), and <c>shouldFillLineOnLineBreak</c> is set but not honored by the
/// renderer: the area after the line break is not filled.
/// Threading: immutable statics.
/// </remarks>
internal static class DiffDecorationOptions
{
    /// <summary>A whole inserted line, with the plus sign in the margin.</summary>
    public static readonly ModelDecorationOptions LineAddBackgroundWithIndicator = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "line-insert",
        ClassName = "line-insert",
        IsWholeLine = true,
        LinesDecorationsClassName = "insert-sign",
        MarginClassName = "gutter-insert",
    });

    /// <summary>A whole deleted line, with the minus sign in the margin.</summary>
    public static readonly ModelDecorationOptions LineDeleteBackgroundWithIndicator = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "line-delete",
        ClassName = "line-delete",
        IsWholeLine = true,
        LinesDecorationsClassName = "delete-sign",
        MarginClassName = "gutter-delete",
    });

    /// <summary>A whole inserted line.</summary>
    public static readonly ModelDecorationOptions LineAddBackground = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "line-insert",
        ClassName = "line-insert",
        IsWholeLine = true,
        MarginClassName = "gutter-insert",
    });

    /// <summary>A whole deleted line.</summary>
    public static readonly ModelDecorationOptions LineDeleteBackground = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "line-delete",
        ClassName = "line-delete",
        IsWholeLine = true,
        MarginClassName = "gutter-delete",
    });

    /// <summary>Inserted characters.</summary>
    public static readonly ModelDecorationOptions AddDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "char-insert",
        ClassName = "char-insert",
        ShouldFillLineOnLineBreak = true,
    });

    /// <summary>Inserted characters covering whole lines.</summary>
    public static readonly ModelDecorationOptions WholeLineAddDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "char-insert",
        ClassName = "char-insert",
        IsWholeLine = true,
    });

    /// <summary>An empty inserted range: a thin bar.</summary>
    public static readonly ModelDecorationOptions AddDecorationEmpty = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "char-insert diff-range-empty",
        ClassName = "char-insert diff-range-empty",
    });

    /// <summary>Deleted characters.</summary>
    public static readonly ModelDecorationOptions DeleteDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "char-delete",
        ClassName = "char-delete",
        ShouldFillLineOnLineBreak = true,
    });

    /// <summary>Deleted characters covering whole lines.</summary>
    public static readonly ModelDecorationOptions WholeLineDeleteDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "char-delete",
        ClassName = "char-delete",
        IsWholeLine = true,
    });

    /// <summary>An empty deleted range: a thin bar.</summary>
    public static readonly ModelDecorationOptions DeleteDecorationEmpty = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "char-delete diff-range-empty",
        ClassName = "char-delete diff-range-empty",
    });
}
