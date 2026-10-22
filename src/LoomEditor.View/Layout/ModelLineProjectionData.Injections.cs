// Ported from microsoft/vscode: src/vs/editor/common/modelLineProjectionData.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Layout;

internal sealed partial class ModelLineProjectionData
{
    /// <summary>Moves an output position so that it never stops inside injected text or at a wrap seam, per the affinity.</summary>
    public OutputPosition NormalizeOutputPosition(int outputLineIndex, int outputOffset, PositionAffinity affinity)
    {
        if (InjectionOffsets is not null)
        {
            int offsetInInputWithInjections = OutputPositionToOffsetInInputWithInjections(outputLineIndex, outputOffset);
            int normalizedOffsetInUnwrappedLine = NormalizeOffsetInInputWithInjectionsAroundInjections(offsetInInputWithInjections, affinity);
            if (normalizedOffsetInUnwrappedLine != offsetInInputWithInjections)
            {
                // injected text caused a change
                return OffsetInInputWithInjectionsToOutputPosition(normalizedOffsetInUnwrappedLine, affinity);
            }
        }

        if (affinity == PositionAffinity.Left)
        {
            if (outputLineIndex > 0 && outputOffset == GetMinOutputOffset(outputLineIndex))
            {
                return new OutputPosition(outputLineIndex - 1, GetMaxOutputOffset(outputLineIndex - 1));
            }
        }
        else if (affinity == PositionAffinity.Right)
        {
            int maxOutputLineIndex = GetOutputLineCount() - 1;
            if (outputLineIndex < maxOutputLineIndex && outputOffset == GetMaxOutputOffset(outputLineIndex))
            {
                return new OutputPosition(outputLineIndex + 1, GetMinOutputOffset(outputLineIndex + 1));
            }
        }

        return new OutputPosition(outputLineIndex, outputOffset);
    }

    /// <summary>The injected text at an output position, or null on model text.</summary>
    public InjectedText? GetInjectedText(int outputLineIndex, int outputOffset)
    {
        int offset = OutputPositionToOffsetInInputWithInjections(outputLineIndex, outputOffset);
        var injectedText = GetInjectedTextAtOffset(offset);
        return injectedText is null ? null : new InjectedText(InjectionOptions![injectedText.Value.InjectedTextIndex]);
    }

    private int OutputPositionToOffsetInInputWithInjections(int outputLineIndex, int outputOffset)
    {
        if (outputLineIndex > 0)
        {
            outputOffset = Math.Max(0, outputOffset - WrappedTextIndentLength);
        }

        return (outputLineIndex > 0 ? BreakOffsets[outputLineIndex - 1] : 0) + outputOffset;
    }

    private int NormalizeOffsetInInputWithInjectionsAroundInjections(int offsetInInputWithInjections, PositionAffinity affinity)
    {
        var found = GetInjectedTextAtOffset(offsetInInputWithInjections);
        if (found is null)
        {
            return offsetInInputWithInjections;
        }

        var injectedText = found.Value;
        if (affinity == PositionAffinity.None)
        {
            if (offsetInInputWithInjections == injectedText.OffsetInInputWithInjections + injectedText.Length
                && HasRightCursorStop(InjectionOptions![injectedText.InjectedTextIndex].CursorStops))
            {
                return injectedText.OffsetInInputWithInjections + injectedText.Length;
            }

            int result = injectedText.OffsetInInputWithInjections;
            if (HasLeftCursorStop(InjectionOptions![injectedText.InjectedTextIndex].CursorStops))
            {
                return result;
            }

            int index = injectedText.InjectedTextIndex - 1;
            while (index >= 0 && InjectionOffsets![index] == InjectionOffsets![injectedText.InjectedTextIndex])
            {
                if (HasRightCursorStop(InjectionOptions![index].CursorStops))
                {
                    break;
                }

                result -= InjectionOptions![index].Content.Length;
                if (HasLeftCursorStop(InjectionOptions![index].CursorStops))
                {
                    break;
                }

                index--;
            }

            return result;
        }

        if (affinity is PositionAffinity.Right or PositionAffinity.RightOfInjectedText)
        {
            int result = injectedText.OffsetInInputWithInjections + injectedText.Length;
            int index = injectedText.InjectedTextIndex;
            // traverse all injected text that touch each other
            while (index + 1 < InjectionOffsets!.Length && InjectionOffsets[index + 1] == InjectionOffsets[index])
            {
                result += InjectionOptions![index + 1].Content.Length;
                index++;
            }

            return result;
        }

        if (affinity is PositionAffinity.Left or PositionAffinity.LeftOfInjectedText)
        {
            int result = injectedText.OffsetInInputWithInjections;
            int index = injectedText.InjectedTextIndex;
            // traverse all injected text that touch each other
            while (index - 1 >= 0 && InjectionOffsets![index - 1] == InjectionOffsets[index])
            {
                result -= InjectionOptions![index - 1].Content.Length;
                index--;
            }

            return result;
        }

        throw new ArgumentOutOfRangeException(nameof(affinity));
    }

    private (int InjectedTextIndex, int OffsetInInputWithInjections, int Length)? GetInjectedTextAtOffset(int offsetInInputWithInjections)
    {
        if (InjectionOffsets is not null)
        {
            int totalInjectedTextLengthBefore = 0;
            for (int i = 0; i < InjectionOffsets.Length; i++)
            {
                int length = InjectionOptions![i].Content.Length;
                int start = InjectionOffsets[i] + totalInjectedTextLengthBefore;
                int end = start + length;

                if (start > offsetInInputWithInjections)
                {
                    // Injected text starts later; all later injected texts have an even larger offset.
                    break;
                }

                if (offsetInInputWithInjections <= end)
                {
                    // Injected text ends after or with the given position (but also starts with or before it).
                    return (i, start, length);
                }

                totalInjectedTextLengthBefore += length;
            }
        }

        return null;
    }

    private static bool HasRightCursorStop(InjectedTextCursorStops? cursorStop) =>
        cursorStop is null or InjectedTextCursorStops.Right or InjectedTextCursorStops.Both;

    private static bool HasLeftCursorStop(InjectedTextCursorStops? cursorStop) =>
        cursorStop is null or InjectedTextCursorStops.Left or InjectedTextCursorStops.Both;
}
