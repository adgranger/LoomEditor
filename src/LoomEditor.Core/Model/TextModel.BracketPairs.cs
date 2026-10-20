// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.BracketPairs;
using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel : ITokenizerSource
{
    /// <summary>The brackets of the text: pairs, nesting levels and matching (Monaco: <c>bracketPairs</c>).</summary>
    internal BracketPairsTextModelPart BracketPairs { get; }

    private readonly ColorizedBracketPairsDecorationProvider _decorationProvider;
    private BracketPairColorizationOptions _bracketPairColorizationOptions = new();

    /// <summary>
    /// How brackets are colored (Monaco: <c>TextModelResolvedOptions.bracketPairColorizationOptions</c>). It is not in the public
    /// <c>TextModelOptions</c> (ADR 0009, decision 12); VS Code's default is enabled.
    /// </summary>
    internal BracketPairColorizationOptions BracketPairColorizationOptions => _bracketPairColorizationOptions;

    /// <summary>Changes how brackets are colored; the decorations change, so <c>DecorationsChanged</c> is raised.</summary>
    internal void UpdateBracketPairColorizationOptions(BracketPairColorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (_bracketPairColorizationOptions == options)
        {
            return;
        }

        _bracketPairColorizationOptions = options;
        var e = new ModelOptionsChangedEventArgs(tabSize: false, indentSize: false, insertSpaces: false, trimAutoWhitespace: false);
        BracketPairs.HandleDidChangeOptions(e);
        _decorationProvider.HandleDidChangeOptions();
        OptionsChanged?.Invoke(this, e);
        FireDecorationsChangedDeferred();
    }

    // Monaco: `_decorationProvider.onDidChange` → beginDeferredEmit, fire, endDeferredEmit.
    private void FireDecorationsChangedDeferred()
    {
        BeginDecorationsDeferredEmit();
        FireDecorationsChanged();
        EndDecorationsDeferredEmit();
    }

    string ITokenizerSource.GetValue() => GetValue();

    int ITokenizerSource.GetLineCount() => LineCount;

    int ITokenizerSource.GetLineLength(int lineNumber) => GetLineLength(lineNumber);

    IViewLineTokens ITokenizerSource.GetLineTokens(int lineNumber) => _tokenization.GetLineTokens(lineNumber);
}
