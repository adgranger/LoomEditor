// Ported from microsoft/vscode: src/vs/editor/common/viewModel/inlineDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.View.Layout;

/// <summary>What <see cref="InjectedTextInlineDecorationsComputer"/> needs to know about a model line (Monaco: <c>IInjectedTextInlineDecorationsComputerContext</c>).</summary>
/// <remarks>Threading: implementation defined.</remarks>
internal interface IInjectedTextInlineDecorationsComputerContext
{
    /// <summary>The injections options for a model line number.</summary>
    IReadOnlyList<ModelDecorationInjectedTextOptions>? GetInjectionOptions(int modelLineNumber);

    /// <summary>The injection offsets for a model line number.</summary>
    IReadOnlyList<int>? GetInjectionOffsets(int modelLineNumber);

    /// <summary>The break offsets for a model line number.</summary>
    IReadOnlyList<int> GetBreakOffsets(int modelLineNumber);

    /// <summary>The wrapped text indent length for a model line number.</summary>
    int GetWrappedTextIndentLength(int modelLineNumber);

    /// <summary>The view line number of the first output line of a model line.</summary>
    int GetBaseViewLineNumber(int modelLineNumber);
}

/// <summary>Computes the inline decorations that style the injected text of a model line (Monaco: <c>InjectedTextInlineDecorationsComputer</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class InjectedTextInlineDecorationsComputer(IInjectedTextInlineDecorationsComputerContext context)
{
    /// <summary>The inline decorations of a model line, one list per output (view) line.</summary>
    public IReadOnlyList<IReadOnlyList<InlineDecoration>> GetInlineDecorations(int modelLineNumber)
    {
        IReadOnlyList<int>? injectionOffsets = context.GetInjectionOffsets(modelLineNumber);
        if (injectionOffsets is null)
        {
            return [];
        }

        var lineInlineDecorations = new List<IReadOnlyList<InlineDecoration>>();
        int totalInjectedTextLengthBefore = 0;
        int currentInjectedOffset = 0;

        IReadOnlyList<ModelDecorationInjectedTextOptions> injectionOptions = context.GetInjectionOptions(modelLineNumber)!;
        IReadOnlyList<int> breakOffsets = context.GetBreakOffsets(modelLineNumber);

        for (int outputLineIndex = 0; outputLineIndex < breakOffsets.Count; outputLineIndex++)
        {
            var inlineDecorations = new List<InlineDecoration>();
            lineInlineDecorations.Add(inlineDecorations);

            int lineStartOffsetInInputWithInjections = outputLineIndex > 0 ? breakOffsets[outputLineIndex - 1] : 0;
            int lineEndOffsetInInputWithInjections = breakOffsets[outputLineIndex];

            while (currentInjectedOffset < injectionOffsets.Count)
            {
                ModelDecorationInjectedTextOptions options = injectionOptions[currentInjectedOffset];
                int length = options.Content.Length;
                int injectedTextStartOffsetInInputWithInjections = injectionOffsets[currentInjectedOffset] + totalInjectedTextLengthBefore;
                int injectedTextEndOffsetInInputWithInjections = injectedTextStartOffsetInInputWithInjections + length;
                bool isWidthOnly = length == 0 && options.WidthInEm is not null;
                bool isLastOutputLine = outputLineIndex == breakOffsets.Count - 1;
                bool isAtInternalWrapBoundary = injectedTextStartOffsetInInputWithInjections == lineEndOffsetInInputWithInjections && !isLastOutputLine;

                if (injectedTextStartOffsetInInputWithInjections > lineEndOffsetInInputWithInjections || (isWidthOnly && isAtInternalWrapBoundary))
                {
                    // Injected text only starts in later wrapped lines.
                    break;
                }

                bool isInLine = isWidthOnly
                    ? lineStartOffsetInInputWithInjections <= injectedTextStartOffsetInInputWithInjections
                        && (injectedTextStartOffsetInInputWithInjections < lineEndOffsetInInputWithInjections || isLastOutputLine)
                    : lineStartOffsetInInputWithInjections < injectedTextEndOffsetInInputWithInjections;
                if (isInLine && !string.IsNullOrEmpty(options.InlineClassName))
                {
                    // Injected text ends after or in this line (but also starts in or before this line).
                    int wrappedTextIndentLength = context.GetWrappedTextIndentLength(modelLineNumber);
                    int offset = outputLineIndex > 0 ? wrappedTextIndentLength : 0;
                    int start = offset + Math.Max(injectedTextStartOffsetInInputWithInjections - lineStartOffsetInInputWithInjections, 0);
                    int end = offset + Math.Min(
                        injectedTextEndOffsetInInputWithInjections - lineStartOffsetInInputWithInjections,
                        lineEndOffsetInInputWithInjections - lineStartOffsetInInputWithInjections);
                    if (start != end || isWidthOnly)
                    {
                        int viewLineNumber = context.GetBaseViewLineNumber(modelLineNumber) + outputLineIndex;
                        var range = new Range(viewLineNumber, start + 1, viewLineNumber, end + 1);
                        InlineDecorationType type = isWidthOnly
                            ? InlineDecorationType.WidthOnly
                            : options.InlineClassNameAffectsLetterSpacing
                                ? InlineDecorationType.RegularAffectingLetterSpacing
                                : InlineDecorationType.Regular;
                        inlineDecorations.Add(new InlineDecoration(range, options.InlineClassName, type));
                    }
                }

                if (injectedTextEndOffsetInInputWithInjections <= lineEndOffsetInInputWithInjections)
                {
                    totalInjectedTextLengthBefore += length;
                    currentInjectedOffset++;
                }
                else
                {
                    // Injected text breaks into next line, process it again.
                    break;
                }
            }
        }

        return lineInlineDecorations;
    }
}
