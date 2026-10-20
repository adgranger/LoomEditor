// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelDecoration.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Layout;

/// <summary>A model decoration in view coordinates (Monaco: <c>ViewModelDecoration</c>).</summary>
/// <remarks>A class, not a record: the decoration cache hands out the same instance for the same decoration id. Threading: immutable.</remarks>
internal sealed class ViewModelDecoration(Range range, ModelDecorationOptions options)
{
    public Range Range { get; } = range;

    public ModelDecorationOptions Options { get; } = options;
}

/// <summary>Whether a decoration hides in comment or string tokens (Monaco: <c>isModelDecorationVisible</c> and its helpers).</summary>
/// <remarks>Threading: UI thread only (reads the tokens of the model).</remarks>
internal static class ModelDecorationVisibility
{
    public static bool IsVisible(TextModel model, ModelDecoration decoration)
    {
        if (decoration.Options.HideInCommentTokens && IsInComment(model, decoration))
        {
            return false;
        }

        if (decoration.Options.HideInStringTokens && IsInString(model, decoration))
        {
            return false;
        }

        return true;
    }

    public static bool IsInComment(TextModel model, ModelDecoration decoration) =>
        TestTokensInRange(model, decoration.Range, tokenType => tokenType == StandardTokenType.Comment);

    public static bool IsInString(TextModel model, ModelDecoration decoration) =>
        TestTokensInRange(model, decoration.Range, tokenType => tokenType == StandardTokenType.String);

    // Calls the callback for every token that intersects the range. If it returns false, iteration stops and false is
    // returned. Otherwise, true is returned.
    private static bool TestTokensInRange(TextModel model, Range range, Func<StandardTokenType, bool> callback)
    {
        for (int lineNumber = range.StartLineNumber; lineNumber <= range.EndLineNumber; lineNumber++)
        {
            LineTokens lineTokens = model.Tokenization.GetLineTokens(lineNumber);
            bool isFirstLine = lineNumber == range.StartLineNumber;
            bool isEndLine = lineNumber == range.EndLineNumber;

            int tokenIdx = isFirstLine ? lineTokens.FindTokenIndexAtOffset(range.StartColumn - 1) : 0;
            while (tokenIdx < lineTokens.GetCount())
            {
                if (isEndLine)
                {
                    int startOffset = lineTokens.GetStartOffset(tokenIdx);
                    if (startOffset > range.EndColumn - 1)
                    {
                        break;
                    }
                }

                if (!callback(lineTokens.GetStandardTokenType(tokenIdx)))
                {
                    return false;
                }

                tokenIdx++;
            }
        }

        return true;
    }
}
