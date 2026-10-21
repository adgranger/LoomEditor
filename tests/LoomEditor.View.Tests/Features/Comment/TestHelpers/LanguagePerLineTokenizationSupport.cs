// Ported from microsoft/vscode: src/vs/editor/contrib/comment/test/browser/lineCommentCommand.test.ts (OuterMode.tokenizeEncoded)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Tests.Features.Comment.TestHelpers;

/// <summary>
/// A tokenizer that gives one token per line: of <paramref name="innerEncodedLanguageId"/> when the line starts with two spaces,
/// of the outer language otherwise (the mixed mode of the line comment tests).
/// </summary>
/// <remarks>Threading: stateless.</remarks>
internal sealed class LanguagePerLineTokenizationSupport(int outerEncodedLanguageId, int innerEncodedLanguageId) : ITokenizationSupport
{
    public IState GetInitialState() => NullState.Instance;

    public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
    {
        int encodedLanguageId = line.StartsWith("  ", StringComparison.Ordinal) ? innerEncodedLanguageId : outerEncodedLanguageId;
        uint metadata = ((uint)ColorId.DefaultForeground << MetadataConsts.ForegroundOffset)
            | ((uint)encodedLanguageId << MetadataConsts.LanguageIdOffset);
        return new EncodedTokenizationResult([0, metadata], state);
    }
}
