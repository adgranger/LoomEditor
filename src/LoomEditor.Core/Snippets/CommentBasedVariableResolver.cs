// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (CommentBasedVariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Snippets;

/// <summary>Resolves <c>LINE_COMMENT</c>, <c>BLOCK_COMMENT_START</c> and <c>BLOCK_COMMENT_END</c> (Monaco: <c>CommentBasedVariableResolver</c>).</summary>
/// <remarks>The comment rules are those of the language at the position. Threading: UI thread only; resolvers are created per insertion.</remarks>
internal sealed class CommentBasedVariableResolver(TextModel model, Position position) : IVariableResolver
{
    /// <inheritdoc />
    public string? Resolve(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        string languageId = model.Tokenization.GetLanguageIdAtPosition(position.LineNumber, position.Column);
        if (model.LanguageConfigurationRegistry.GetLanguageConfiguration(languageId).Comments is not { } config)
        {
            return null;
        }

        string? value = variable.Name switch
        {
            "LINE_COMMENT" => config.LineCommentToken,
            "BLOCK_COMMENT_START" => config.BlockCommentStartToken,
            "BLOCK_COMMENT_END" => config.BlockCommentEndToken,
            _ => null,
        };
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
