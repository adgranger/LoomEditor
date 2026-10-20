// Ported from microsoft/vscode: src/vs/editor/common/languages/languageConfigurationRegistry.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Languages;

/// <summary>Interface used to support insertion of mode specific comments (Monaco: <c>ICommentsConfiguration</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record CommentsConfiguration(
    string? LineCommentToken = null,
    bool? LineCommentNoIndent = null,
    string? BlockCommentStartToken = null,
    string? BlockCommentEndToken = null);

/// <summary>The supports built from the configuration of a language (Monaco: <c>ResolvedLanguageConfiguration</c>).</summary>
/// <remarks>Threading: UI thread; immutable except the lazy <see cref="Brackets"/> and <see cref="ElectricCharacter"/>.</remarks>
internal sealed class ResolvedLanguageConfiguration
{
    private readonly OnEnterSupport? _onEnterSupport;
    private RichEditBrackets? _brackets;
    private BracketElectricCharacterSupport? _electricCharacter;

    public ResolvedLanguageConfiguration(string languageId, LanguageConfiguration underlyingConfig)
    {
        ArgumentNullException.ThrowIfNull(languageId);
        ArgumentNullException.ThrowIfNull(underlyingConfig);
        LanguageId = languageId;
        UnderlyingConfig = underlyingConfig;

        if (UnderlyingConfig.Brackets is not null || UnderlyingConfig.IndentationRules is not null || UnderlyingConfig.OnEnterRules is not null)
        {
            // Monaco's `new OnEnterSupport(config)` runs `opts.brackets = opts.brackets || [...]` on the configuration
            // itself, so everything below (auto-closing pairs, brackets, electric characters) sees the default brackets.
            if (UnderlyingConfig.Brackets is null)
            {
                UnderlyingConfig = UnderlyingConfig with { Brackets = OnEnterSupport.DefaultBrackets };
            }

            _onEnterSupport = new OnEnterSupport(UnderlyingConfig.Brackets, UnderlyingConfig.OnEnterRules);
        }

        Comments = HandleComments(UnderlyingConfig);
        CharacterPair = new CharacterPairSupport(UnderlyingConfig);

        WordDefinition = UnderlyingConfig.WordPattern ?? WordHelper.DefaultWordRegExp;
        IndentationRules = UnderlyingConfig.IndentationRules;
        IndentRulesSupport = UnderlyingConfig.IndentationRules is { } indentationRules ? new IndentRulesSupport(indentationRules) : null;
        FoldingRules = UnderlyingConfig.Folding ?? new FoldingRules();

        BracketsNew = new LanguageBracketsConfiguration(languageId, UnderlyingConfig);
    }

    /// <summary><c>languageId</c>.</summary>
    public string LanguageId { get; }

    /// <summary><c>underlyingConfig</c> (with the default brackets when the on-enter support added them).</summary>
    public LanguageConfiguration UnderlyingConfig { get; }

    /// <summary><c>comments</c>.</summary>
    public CommentsConfiguration? Comments { get; }

    /// <summary><c>characterPair</c>.</summary>
    public CharacterPairSupport CharacterPair { get; }

    /// <summary><c>wordDefinition</c>.</summary>
    public Regex WordDefinition { get; }

    /// <summary><c>indentRulesSupport</c>.</summary>
    public IndentRulesSupport? IndentRulesSupport { get; }

    /// <summary><c>indentationRules</c>.</summary>
    public IndentationRule? IndentationRules { get; }

    /// <summary><c>foldingRules</c>.</summary>
    public FoldingRules FoldingRules { get; }

    /// <summary><c>bracketsNew</c>.</summary>
    public LanguageBracketsConfiguration BracketsNew { get; }

    /// <summary><c>brackets</c>: created on first use, <see langword="null"/> without brackets.</summary>
    public RichEditBrackets? Brackets
    {
        get
        {
            if (_brackets is null && UnderlyingConfig.Brackets is not null)
            {
                _brackets = new RichEditBrackets(LanguageId, UnderlyingConfig.Brackets);
            }

            return _brackets;
        }
    }

    /// <summary><c>electricCharacter</c>: created on first use.</summary>
    public BracketElectricCharacterSupport ElectricCharacter => _electricCharacter ??= new BracketElectricCharacterSupport(Brackets);

    /// <summary><c>getWordDefinition</c>.</summary>
    public Regex GetWordDefinition() => WordHelper.EnsureValidWordDefinition(WordDefinition);

    /// <summary><c>onEnter</c>: <see langword="null"/> without brackets, indentation rules and on-enter rules.</summary>
    public EnterAction? OnEnter(EditorAutoIndentStrategy autoIndent, string previousLineText, string beforeEnterText, string afterEnterText) =>
        _onEnterSupport?.OnEnter(autoIndent, previousLineText, beforeEnterText, afterEnterText);

    /// <summary><c>getAutoClosingPairs</c>.</summary>
    public AutoClosingPairs GetAutoClosingPairs() => new(CharacterPair.GetAutoClosingPairs());

    /// <summary><c>getAutoCloseBeforeSet</c>.</summary>
    public string GetAutoCloseBeforeSet(bool forQuotes) => CharacterPair.GetAutoCloseBeforeSet(forQuotes);

    /// <summary><c>getSurroundingPairs</c>.</summary>
    public IReadOnlyList<IAutoClosingPair> GetSurroundingPairs() => CharacterPair.GetSurroundingPairs();

    private static CommentsConfiguration? HandleComments(LanguageConfiguration conf)
    {
        CommentRule? commentRule = conf.Comments;
        if (commentRule is null)
        {
            return null;
        }

        // comment configuration
        var comments = new CommentsConfiguration();

        // JavaScript's `if (commentRule.lineComment)`: the string form '' is falsy.
        if (commentRule.LineComment is { } lineComment && !(lineComment.Comment.Length == 0 && lineComment.NoIndent is null))
        {
            comments = comments with { LineCommentToken = lineComment.Comment, LineCommentNoIndent = lineComment.NoIndent };
        }

        if (commentRule.BlockComment is { } blockComment)
        {
            comments = comments with { BlockCommentStartToken = blockComment.Open, BlockCommentEndToken = blockComment.Close };
        }

        return comments;
    }
}
