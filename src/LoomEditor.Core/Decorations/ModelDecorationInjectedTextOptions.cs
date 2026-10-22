// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>The normalized injected text of a decoration (Monaco: <c>ModelDecorationInjectedTextOptions</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class ModelDecorationInjectedTextOptions
{
    private ModelDecorationInjectedTextOptions(InjectedTextOptions options)
    {
        Content = options.Content ?? "";
        InlineClassName = string.IsNullOrEmpty(options.InlineClassName) ? null : options.InlineClassName;
        InlineClassNameAffectsLetterSpacing = options.InlineClassNameAffectsLetterSpacing;
        AttachedData = options.AttachedData;
        CursorStops = options.CursorStops;
        WidthInEm = options.WidthInEm is { } w && double.IsFinite(w) && w >= 0 ? w : null;
    }

    private ModelDecorationInjectedTextOptions(ModelDecorationInjectedTextOptions other, string content)
    {
        Content = content;
        InlineClassName = other.InlineClassName;
        InlineClassNameAffectsLetterSpacing = other.InlineClassNameAffectsLetterSpacing;
        AttachedData = other.AttachedData;
        CursorStops = other.CursorStops;
        WidthInEm = other.WidthInEm;
    }

    public string Content { get; }

    public string? InlineClassName { get; }

    public bool InlineClassNameAffectsLetterSpacing { get; }

    public double? WidthInEm { get; }

    public object? AttachedData { get; }

    public InjectedTextCursorStops? CursorStops { get; }

    /// <summary>A copy with another <see cref="Content"/> (Monaco: <c>{ ...options, content }</c>).</summary>
    public ModelDecorationInjectedTextOptions WithContent(string content) => new(this, content);

    public static ModelDecorationInjectedTextOptions From(InjectedTextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new ModelDecorationInjectedTextOptions(options);
    }
}
