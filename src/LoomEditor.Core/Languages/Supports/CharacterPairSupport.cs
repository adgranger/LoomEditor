// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/characterPair.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Supports;

/// <summary>The auto closing pairs, the surrounding pairs and the auto close before sets of a language (Monaco: <c>CharacterPairSupport</c>).</summary>
/// <remarks>Threading: immutable after construction (the pairs cache their neutral character, see <see cref="StandardAutoClosingPairConditional"/>).</remarks>
internal sealed class CharacterPairSupport
{
    public const string DefaultAutoCloseBeforeLanguageDefinedQuotes = ";:.,=}])> \n\t";
    public const string DefaultAutoCloseBeforeLanguageDefinedBrackets = "'\"`;:.,=}])> \n\t";
    public const string DefaultAutoCloseBeforeWhitespace = " \n\t";

    private readonly List<StandardAutoClosingPairConditional> _autoClosingPairs;
    private readonly IReadOnlyList<IAutoClosingPair> _surroundingPairs;
    private readonly string _autoCloseBeforeForQuotes;
    private readonly string _autoCloseBeforeForBrackets;

    public CharacterPairSupport(LanguageConfiguration config)
    {
        if (config.AutoClosingPairs is not null)
        {
            _autoClosingPairs = [.. config.AutoClosingPairs.Select(el => new StandardAutoClosingPairConditional(el))];
        }
        else if (config.Brackets is not null)
        {
            _autoClosingPairs = [.. config.Brackets.Select(b => new StandardAutoClosingPairConditional(new AutoClosingPairConditional(b.Open, b.Close)))];
        }
        else
        {
            _autoClosingPairs = [];
        }

        if (config.DocComment is { } docComment)
        {
            // IDocComment is legacy, only partially supported
            _autoClosingPairs.Add(new StandardAutoClosingPairConditional(new AutoClosingPairConditional(docComment.Open, docComment.Close ?? "")));
        }

        _autoCloseBeforeForQuotes = config.AutoCloseBefore ?? DefaultAutoCloseBeforeLanguageDefinedQuotes;
        _autoCloseBeforeForBrackets = config.AutoCloseBefore ?? DefaultAutoCloseBeforeLanguageDefinedBrackets;

        _surroundingPairs = config.SurroundingPairs ?? (IReadOnlyList<IAutoClosingPair>)_autoClosingPairs;
    }

    public IReadOnlyList<StandardAutoClosingPairConditional> GetAutoClosingPairs() => _autoClosingPairs;

    public string GetAutoCloseBeforeSet(bool forQuotes) => forQuotes ? _autoCloseBeforeForQuotes : _autoCloseBeforeForBrackets;

    public IReadOnlyList<IAutoClosingPair> GetSurroundingPairs() => _surroundingPairs;
}
