// Ported from microsoft/vscode: src/vs/editor/common/model/guidesTextModelPart.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Guides;

internal sealed partial class GuidesTextModelPart
{
    /// <summary>
    /// The indent guide to highlight for <paramref name="lineNumber"/>, searched between <paramref name="minLineNumber"/> and
    /// <paramref name="maxLineNumber"/> (Monaco: <c>getActiveIndentGuide</c>).
    /// </summary>
    public ActiveIndentGuideInfo GetActiveIndentGuide(int lineNumber, int minLineNumber, int maxLineNumber)
    {
        int lineCount = textModel.LineCount;
        if (lineNumber < 1 || lineNumber > lineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, "Illegal value for lineNumber");
        }

        int indentSize = textModel.Options.IndentSize;
        bool offSide = textModel.LanguageConfigurationRegistry
            .GetLanguageConfiguration(textModel.Tokenization.GetLanguageId()).FoldingRules.OffSide;

        int upAboveContentLineIndex = -2; // -2 is a marker for not having computed it
        int upAboveContentLineIndent = -1;
        int upBelowContentLineIndex = -2; // -2 is a marker for not having computed it
        int upBelowContentLineIndent = -1;

        void UpResolveIndents(int line)
        {
            if (upAboveContentLineIndex != -1 && (upAboveContentLineIndex == -2 || upAboveContentLineIndex > line - 1))
            {
                upAboveContentLineIndex = -1;
                upAboveContentLineIndent = -1;

                // must find previous line with content
                for (int lineIndex = line - 2; lineIndex >= 0; lineIndex--)
                {
                    int indent = ComputeIndentLevel(lineIndex);
                    if (indent >= 0)
                    {
                        upAboveContentLineIndex = lineIndex;
                        upAboveContentLineIndent = indent;
                        break;
                    }
                }
            }

            if (upBelowContentLineIndex == -2)
            {
                upBelowContentLineIndex = -1;
                upBelowContentLineIndent = -1;

                // must find next line with content
                for (int lineIndex = line; lineIndex < lineCount; lineIndex++)
                {
                    int indent = ComputeIndentLevel(lineIndex);
                    if (indent >= 0)
                    {
                        upBelowContentLineIndex = lineIndex;
                        upBelowContentLineIndent = indent;
                        break;
                    }
                }
            }
        }

        int downAboveContentLineIndex = -2; // -2 is a marker for not having computed it
        int downAboveContentLineIndent = -1;
        int downBelowContentLineIndex = -2; // -2 is a marker for not having computed it
        int downBelowContentLineIndent = -1;

        void DownResolveIndents(int line)
        {
            if (downAboveContentLineIndex == -2)
            {
                downAboveContentLineIndex = -1;
                downAboveContentLineIndent = -1;

                // must find previous line with content
                for (int lineIndex = line - 2; lineIndex >= 0; lineIndex--)
                {
                    int indent = ComputeIndentLevel(lineIndex);
                    if (indent >= 0)
                    {
                        downAboveContentLineIndex = lineIndex;
                        downAboveContentLineIndent = indent;
                        break;
                    }
                }
            }

            if (downBelowContentLineIndex != -1 && (downBelowContentLineIndex == -2 || downBelowContentLineIndex < line - 1))
            {
                downBelowContentLineIndex = -1;
                downBelowContentLineIndent = -1;

                // must find next line with content
                for (int lineIndex = line; lineIndex < lineCount; lineIndex++)
                {
                    int indent = ComputeIndentLevel(lineIndex);
                    if (indent >= 0)
                    {
                        downBelowContentLineIndex = lineIndex;
                        downBelowContentLineIndent = indent;
                        break;
                    }
                }
            }
        }

        int startLineNumber = 0;
        bool goUp = true;
        int endLineNumber = 0;
        bool goDown = true;
        int guideIndent = 0;
        int initialIndent = 0;

        for (int distance = 0; goUp || goDown; distance++)
        {
            int upLineNumber = lineNumber - distance;
            int downLineNumber = lineNumber + distance;

            if (distance > 1 && (upLineNumber < 1 || upLineNumber < minLineNumber))
            {
                goUp = false;
            }

            if (distance > 1 && (downLineNumber > lineCount || downLineNumber > maxLineNumber))
            {
                goDown = false;
            }

            if (distance > 50000)
            {
                // stop processing
                goUp = false;
                goDown = false;
            }

            int upLineIndentLevel = -1;
            if (goUp && upLineNumber >= 1)
            {
                // compute indent level going up
                int currentIndent = ComputeIndentLevel(upLineNumber - 1);
                if (currentIndent >= 0)
                {
                    // This line has content (besides whitespace): use the line's indent
                    upBelowContentLineIndex = upLineNumber - 1;
                    upBelowContentLineIndent = currentIndent;
                    upLineIndentLevel = CeilDiv(currentIndent, indentSize);
                }
                else
                {
                    UpResolveIndents(upLineNumber);
                    upLineIndentLevel = GetIndentLevelForWhitespaceLine(offSide, upAboveContentLineIndent, upBelowContentLineIndent);
                }
            }

            int downLineIndentLevel = -1;
            if (goDown && downLineNumber <= lineCount)
            {
                // compute indent level going down
                int currentIndent = ComputeIndentLevel(downLineNumber - 1);
                if (currentIndent >= 0)
                {
                    // This line has content (besides whitespace): use the line's indent
                    downAboveContentLineIndex = downLineNumber - 1;
                    downAboveContentLineIndent = currentIndent;
                    downLineIndentLevel = CeilDiv(currentIndent, indentSize);
                }
                else
                {
                    DownResolveIndents(downLineNumber);
                    downLineIndentLevel = GetIndentLevelForWhitespaceLine(offSide, downAboveContentLineIndent, downBelowContentLineIndent);
                }
            }

            if (distance == 0)
            {
                initialIndent = upLineIndentLevel;
                continue;
            }

            if (distance == 1)
            {
                if (downLineNumber <= lineCount && downLineIndentLevel >= 0 && initialIndent + 1 == downLineIndentLevel)
                {
                    // This is the beginning of a scope, we have special handling here, since we want the
                    // child scope indent to be active, not the parent scope
                    goUp = false;
                    startLineNumber = downLineNumber;
                    endLineNumber = downLineNumber;
                    guideIndent = downLineIndentLevel;
                    continue;
                }

                if (upLineNumber >= 1 && upLineIndentLevel >= 0 && upLineIndentLevel - 1 == initialIndent)
                {
                    // This is the end of a scope, just like above
                    goDown = false;
                    startLineNumber = upLineNumber;
                    endLineNumber = upLineNumber;
                    guideIndent = upLineIndentLevel;
                    continue;
                }

                startLineNumber = lineNumber;
                endLineNumber = lineNumber;
                guideIndent = initialIndent;
                if (guideIndent == 0)
                {
                    // No need to continue
                    return new ActiveIndentGuideInfo(startLineNumber, endLineNumber, guideIndent);
                }
            }

            if (goUp)
            {
                if (upLineIndentLevel >= guideIndent)
                {
                    startLineNumber = upLineNumber;
                }
                else
                {
                    goUp = false;
                }
            }

            if (goDown)
            {
                if (downLineIndentLevel >= guideIndent)
                {
                    endLineNumber = downLineNumber;
                }
                else
                {
                    goDown = false;
                }
            }
        }

        return new ActiveIndentGuideInfo(startLineNumber, endLineNumber, guideIndent);
    }
}
