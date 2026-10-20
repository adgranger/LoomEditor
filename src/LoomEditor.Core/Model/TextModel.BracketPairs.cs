// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel : ITokenizerSource
{
    string ITokenizerSource.GetValue() => GetValue();

    int ITokenizerSource.GetLineCount() => LineCount;

    int ITokenizerSource.GetLineLength(int lineNumber) => GetLineLength(lineNumber);

    IViewLineTokens ITokenizerSource.GetLineTokens(int lineNumber) => _tokenization.GetLineTokens(lineNumber);
}
