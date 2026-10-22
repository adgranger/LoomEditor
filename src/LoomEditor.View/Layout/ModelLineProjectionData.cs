// Ported from microsoft/vscode: src/vs/editor/common/modelLineProjectionData.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Layout;

/// <summary>
/// How one model line is wrapped and which text is injected into it (Monaco: <c>ModelLineProjectionData</c>).
/// <code>
/// input:
/// xxxxxxxxxxxxxxxxxxxxxxxxxxx
///
/// -> Applying injections [i...i], inputWithInjections:
/// xxxxxx[iiiiiiiiii]xxxxxxxxxxxxxxxxx[ii]xxxx
///
/// -> breaking at offsets | in xxxxxx[iiiiiii|iii]xxxxxxxxxxx|xxxxxx[ii]xxxx|:
/// xxxxxx[iiiiiii
/// iii]xxxxxxxxxxx
/// xxxxxx[ii]xxxx
///
/// -> applying wrappedTextIndentLength, output:
/// xxxxxx[iiiiiii
///    iii]xxxxxxxxxxx
///    xxxxxx[ii]xxxx
/// </code>
/// </summary>
/// <remarks>Threading: the arrays are kept as given and never modified; treat the instance as immutable and thread-safe.</remarks>
internal sealed partial class ModelLineProjectionData
{
    /// <param name="injectionOffsets">The offsets of the injected texts in the input; null when nothing is injected.</param>
    /// <param name="injectionOptions">The injected texts; same length as <paramref name="injectionOffsets"/>.</param>
    /// <param name="breakOffsets">Offsets after applying injections; the last one is the length of the source after applying injections.</param>
    /// <param name="breakOffsetsVisibleColumn">The visible columns of the break offsets (fractional when full-width characters are not twice as wide).</param>
    /// <param name="wrappedTextIndentLength">The length of the indent of the continuation lines.</param>
    public ModelLineProjectionData(
        int[]? injectionOffsets,
        ModelDecorationInjectedTextOptions[]? injectionOptions,
        int[] breakOffsets,
        double[] breakOffsetsVisibleColumn,
        int wrappedTextIndentLength)
    {
        Guard.Assert((injectionOffsets is null) == (injectionOptions is null), "Both injection arrays are null or none is.");
        Guard.Assert(injectionOffsets is null || injectionOffsets.Length == injectionOptions!.Length, "The injection arrays have the same length.");
        InjectionOffsets = injectionOffsets;
        InjectionOptions = injectionOptions;
        BreakOffsets = breakOffsets;
        BreakOffsetsVisibleColumn = breakOffsetsVisibleColumn;
        WrappedTextIndentLength = wrappedTextIndentLength;
    }

    public int[]? InjectionOffsets { get; }

    public ModelDecorationInjectedTextOptions[]? InjectionOptions { get; }

    public int[] BreakOffsets { get; }

    public double[] BreakOffsetsVisibleColumn { get; }

    public int WrappedTextIndentLength { get; }

    public int GetOutputLineCount() => BreakOffsets.Length;

    public int GetMinOutputOffset(int outputLineIndex) => outputLineIndex > 0 ? WrappedTextIndentLength : 0;

    public int GetLineLength(int outputLineIndex)
    {
        // These offsets refer to model text with injected text.
        int startOffset = outputLineIndex > 0 ? BreakOffsets[outputLineIndex - 1] : 0;
        int endOffset = BreakOffsets[outputLineIndex];

        int lineLength = endOffset - startOffset;
        if (outputLineIndex > 0)
        {
            lineLength += WrappedTextIndentLength;
        }

        return lineLength;
    }

    public int GetMaxOutputOffset(int outputLineIndex) => GetLineLength(outputLineIndex);

    public int TranslateToInputOffset(int outputLineIndex, int outputOffset)
    {
        if (outputLineIndex > 0)
        {
            outputOffset = Math.Max(0, outputOffset - WrappedTextIndentLength);
        }

        int offsetInInputWithInjection = outputLineIndex == 0 ? outputOffset : BreakOffsets[outputLineIndex - 1] + outputOffset;
        int offsetInInput = offsetInInputWithInjection;

        if (InjectionOffsets is not null)
        {
            for (int i = 0; i < InjectionOffsets.Length; i++)
            {
                if (offsetInInput > InjectionOffsets[i])
                {
                    if (offsetInInput < InjectionOffsets[i] + InjectionOptions![i].Content.Length)
                    {
                        // inputOffset is within injected text
                        offsetInInput = InjectionOffsets[i];
                    }
                    else
                    {
                        offsetInInput -= InjectionOptions![i].Content.Length;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        return offsetInInput;
    }

    public OutputPosition TranslateToOutputPosition(int inputOffset, PositionAffinity affinity = PositionAffinity.None)
    {
        int inputOffsetInInputWithInjection = inputOffset;
        if (InjectionOffsets is not null)
        {
            for (int i = 0; i < InjectionOffsets.Length; i++)
            {
                if (inputOffset < InjectionOffsets[i])
                {
                    break;
                }

                if (affinity != PositionAffinity.Right && inputOffset == InjectionOffsets[i])
                {
                    break;
                }

                inputOffsetInInputWithInjection += InjectionOptions![i].Content.Length;
            }
        }

        return OffsetInInputWithInjectionsToOutputPosition(inputOffsetInInputWithInjection, affinity);
    }

    private OutputPosition OffsetInInputWithInjectionsToOutputPosition(int offsetInInputWithInjections, PositionAffinity affinity = PositionAffinity.None)
    {
        int low = 0;
        int high = BreakOffsets.Length - 1;
        int mid = 0;
        int midStart = 0;

        while (low <= high)
        {
            mid = low + ((high - low) / 2);

            int midStop = BreakOffsets[mid];
            midStart = mid > 0 ? BreakOffsets[mid - 1] : 0;

            if (affinity == PositionAffinity.Left)
            {
                if (offsetInInputWithInjections <= midStart)
                {
                    high = mid - 1;
                }
                else if (offsetInInputWithInjections > midStop)
                {
                    low = mid + 1;
                }
                else
                {
                    break;
                }
            }
            else
            {
                if (offsetInInputWithInjections < midStart)
                {
                    high = mid - 1;
                }
                else if (offsetInInputWithInjections >= midStop)
                {
                    low = mid + 1;
                }
                else
                {
                    break;
                }
            }
        }

        int outputOffset = offsetInInputWithInjections - midStart;
        if (mid > 0)
        {
            outputOffset += WrappedTextIndentLength;
        }

        return new OutputPosition(mid, outputOffset);
    }
}
