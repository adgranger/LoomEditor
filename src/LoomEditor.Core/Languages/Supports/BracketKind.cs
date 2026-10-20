// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/languageBracketsConfiguration.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Supports;

/// <summary>An opening or a closing bracket of a language (Monaco: <c>BracketKindBase</c> and the union <c>BracketKind</c>).</summary>
/// <remarks>Threading: immutable once its <see cref="LanguageBracketsConfiguration"/> is constructed.</remarks>
internal abstract class BracketKind
{
    private protected BracketKind(LanguageBracketsConfiguration config, string bracketText)
    {
        Config = config;
        BracketText = bracketText;
    }

    internal LanguageBracketsConfiguration Config { get; }

    public string BracketText { get; }

    public string LanguageId => Config.LanguageId;

    public abstract bool IsOpeningBracket { get; }

    /// <summary>Adds <paramref name="item"/> unless already present (by reference): Monaco's <c>Set.add</c>, insertion ordered.</summary>
    private protected static void AddToSet<T>(List<T> set, T item)
        where T : class
    {
        if (!set.Contains(item))
        {
            set.Add(item);
        }
    }
}

/// <summary>An opening bracket kind (Monaco: <c>OpeningBracketKind</c>).</summary>
/// <remarks>Threading: immutable once its <see cref="LanguageBracketsConfiguration"/> is constructed.</remarks>
internal sealed class OpeningBracketKind : BracketKind
{
    private readonly List<ClosingBracketKind> _openedBrackets = [];

    internal OpeningBracketKind(LanguageBracketsConfiguration config, string bracketText)
        : base(config, bracketText)
    {
    }

    public override bool IsOpeningBracket => true;

    /// <summary>The closing brackets that close this bracket, in insertion order.</summary>
    public IReadOnlyList<ClosingBracketKind> OpenedBrackets => _openedBrackets;

    internal void AddOpenedBracket(ClosingBracketKind closing) => AddToSet(_openedBrackets, closing);
}

/// <summary>A closing bracket kind (Monaco: <c>ClosingBracketKind</c>).</summary>
/// <remarks>Threading: immutable once its <see cref="LanguageBracketsConfiguration"/> is constructed.</remarks>
internal sealed class ClosingBracketKind : BracketKind
{
    private readonly List<OpeningBracketKind> _openingBrackets = [];
    private readonly List<OpeningBracketKind> _openingColorizedBrackets = [];

    internal ClosingBracketKind(LanguageBracketsConfiguration config, string bracketText)
        : base(config, bracketText)
    {
    }

    public override bool IsOpeningBracket => false;

    /// <summary>Non empty list of all opening brackets this bracket closes.</summary>
    public IReadOnlyList<OpeningBracketKind> OpeningBrackets => _openingBrackets;

    /// <summary>
    /// Checks if this bracket closes the given other bracket.
    /// If the bracket infos come from different configurations, this method will return false.
    /// </summary>
    public bool Closes(OpeningBracketKind other)
    {
        if (!ReferenceEquals(other.Config, Config))
        {
            return false;
        }
        return _openingBrackets.Contains(other);
    }

    public bool ClosesColorized(OpeningBracketKind other)
    {
        if (!ReferenceEquals(other.Config, Config))
        {
            return false;
        }
        return _openingColorizedBrackets.Contains(other);
    }

    public IReadOnlyList<OpeningBracketKind> GetOpeningBrackets() => _openingBrackets;

    internal void AddOpeningBracket(OpeningBracketKind opening) => AddToSet(_openingBrackets, opening);

    internal void AddOpeningColorizedBracket(OpeningBracketKind opening) => AddToSet(_openingColorizedBrackets, opening);
}
