// Ported from microsoft/vscode: src/vs/editor/common/model/guidesTextModelPart.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Guides;

/// <summary>
/// The indent and bracket pair guides of a <see cref="TextModel"/> (Monaco: <c>GuidesTextModelPart</c>). This file has
/// <see cref="GetLinesIndentGuides"/>; the active guide and the bracket guides come in later files.
/// </summary>
/// <remarks>
/// Threading: UI thread only, like the model. Differences from Monaco: there is no dispose, so no <c>assertNotDisposed()</c>;
/// invalid line numbers throw <see cref="ArgumentOutOfRangeException"/>.
/// </remarks>
internal sealed partial class GuidesTextModelPart(TextModel textModel)
{
    private int ComputeIndentLevel(int lineIndex) =>
        TextModelUtils.ComputeIndentLevel(textModel.GetLineContent(lineIndex + 1), textModel.Options.TabSize);

    /// <summary>
    /// The number of indent guides of each line from <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/>.
    /// A blank line takes the level of the region it is in (Monaco: <c>getLinesIndentGuides</c>).
    /// </summary>
    public int[] GetLinesIndentGuides(int startLineNumber, int endLineNumber)
    {
        int lineCount = textModel.LineCount;
        if (startLineNumber < 1 || startLineNumber > lineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(startLineNumber), startLineNumber, "Illegal value for startLineNumber");
        }

        if (endLineNumber < 1 || endLineNumber > lineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(endLineNumber), endLineNumber, "Illegal value for endLineNumber");
        }

        int indentSize = textModel.Options.IndentSize;
        bool offSide = textModel.LanguageConfigurationRegistry
            .GetLanguageConfiguration(textModel.Tokenization.GetLanguageId()).FoldingRules.OffSide;

        // An empty range gives an empty array (Monaco: `new Array(n)` with a negative n would throw).
        var result = new int[Math.Max(0, endLineNumber - startLineNumber + 1)];

        int aboveContentLineIndex = -2; // -2 is a marker for not having computed it
        int aboveContentLineIndent = -1;
        int belowContentLineIndex = -2; // -2 is a marker for not having computed it
        int belowContentLineIndent = -1;

        for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            int resultIndex = lineNumber - startLineNumber;
            int currentIndent = ComputeIndentLevel(lineNumber - 1);
            if (currentIndent >= 0)
            {
                // This line has content (besides whitespace): use the line's indent
                aboveContentLineIndex = lineNumber - 1;
                aboveContentLineIndent = currentIndent;
                result[resultIndex] = CeilDiv(currentIndent, indentSize);
                continue;
            }

            if (aboveContentLineIndex == -2)
            {
                aboveContentLineIndex = -1;
                aboveContentLineIndent = -1;

                // must find previous line with content
                for (int lineIndex = lineNumber - 2; lineIndex >= 0; lineIndex--)
                {
                    int indent = ComputeIndentLevel(lineIndex);
                    if (indent >= 0)
                    {
                        aboveContentLineIndex = lineIndex;
                        aboveContentLineIndent = indent;
                        break;
                    }
                }
            }

            if (belowContentLineIndex != -1 && (belowContentLineIndex == -2 || belowContentLineIndex < lineNumber - 1))
            {
                belowContentLineIndex = -1;
                belowContentLineIndent = -1;

                // must find next line with content
                for (int lineIndex = lineNumber; lineIndex < lineCount; lineIndex++)
                {
                    int indent = ComputeIndentLevel(lineIndex);
                    if (indent >= 0)
                    {
                        belowContentLineIndex = lineIndex;
                        belowContentLineIndent = indent;
                        break;
                    }
                }
            }

            result[resultIndex] = GetIndentLevelForWhitespaceLine(offSide, aboveContentLineIndent, belowContentLineIndent);
        }

        return result;
    }

    private int GetIndentLevelForWhitespaceLine(bool offSide, int aboveContentLineIndent, int belowContentLineIndent)
    {
        int indentSize = textModel.Options.IndentSize;
        if (aboveContentLineIndent == -1 || belowContentLineIndent == -1)
        {
            // At the top or bottom of the file
            return 0;
        }

        if (aboveContentLineIndent < belowContentLineIndent)
        {
            // we are inside the region above
            return 1 + (aboveContentLineIndent / indentSize);
        }

        if (aboveContentLineIndent == belowContentLineIndent)
        {
            // we are in between two regions
            return CeilDiv(belowContentLineIndent, indentSize);
        }

        // same level as the region below with the off-side rule, otherwise inside the region that ends below
        return offSide ? CeilDiv(belowContentLineIndent, indentSize) : 1 + (belowContentLineIndent / indentSize);
    }

    // Math.ceil(a / b) for a >= 0, b > 0
    private static int CeilDiv(int a, int b) => (a + b - 1) / b;
}
