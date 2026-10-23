// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    private const int LongLineBoundary = 10000;

    /// <summary>
    /// Whether most of the characters of the model are in very long lines (Monaco: <c>isDominatedByLongLines</c>).
    /// Always <see langword="false"/> for a model too large for tokenization. Not cached.
    /// </summary>
    internal bool IsDominatedByLongLines()
    {
        if (IsTooLargeForTokenization)
        {
            return false;
        }

        long longLineCharCount = 0;
        long smallLineCharCount = 0;
        int lineCount = _buffer.GetLineCount();
        for (int line = 1; line <= lineCount; line++)
        {
            int lineLength = _buffer.GetLineLength(line);
            if (lineLength >= LongLineBoundary)
            {
                longLineCharCount += lineLength;
            }
            else
            {
                smallLineCharCount += lineLength;
            }
        }

        return longLineCharCount > smallLineCharCount;
    }
}
