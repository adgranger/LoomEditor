// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Theme;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// What <see cref="AttributedScopeStack"/> needs from the grammar that owns it (upstream passes the <c>Grammar</c>
/// itself; this interface is implemented by it, P4-17).
/// </summary>
/// <remarks>Threading: single-threaded, like the grammar.</remarks>
internal interface IScopeAttributesContext
{
    /// <summary><c>themeProvider</c>.</summary>
    IThemeProvider ThemeProvider { get; }

    /// <summary><c>getMetadataForScope</c>.</summary>
    BasicScopeAttributes GetMetadataForScope(string scopeName);
}

/// <summary><c>AttributedScopeStackFrame</c>: one pushed scope stack element, as stored by a state stack.</summary>
/// <param name="EncodedTokenAttributes"><c>encodedTokenAttributes</c>.</param>
/// <param name="ScopeNames"><c>scopeNames</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record AttributedScopeStackFrame(uint EncodedTokenAttributes, List<string> ScopeNames);

/// <summary>
/// <c>AttributedScopeStack</c>: the scope names of a token and the metadata merged from the theme and the basic scope
/// attributes. Invariant: if there is a parent, the scope path extends the parent's.
/// </summary>
/// <remarks>
/// Deliberate difference: font attributes are not ported (ADR 0008, decision 3), so the stack has no
/// <c>fontAttributes</c> and <c>createRoot</c> takes no font attribute.
/// Threading: immutable.
/// </remarks>
internal sealed class AttributedScopeStack
{
    private AttributedScopeStack(
        AttributedScopeStack? parent,
        ScopeStack scopePath,
        uint tokenAttributes,
        StyleAttributes? styleAttributes)
    {
        Parent = parent;
        ScopePath = scopePath;
        TokenAttributes = tokenAttributes;
        StyleAttributes = styleAttributes;
    }

    /// <summary><c>parent</c>.</summary>
    public AttributedScopeStack? Parent { get; }

    /// <summary><c>scopePath</c>.</summary>
    public ScopeStack ScopePath { get; }

    /// <summary><c>tokenAttributes</c>.</summary>
    public uint TokenAttributes { get; }

    /// <summary><c>styleAttributes</c>: the theme match of this scope path.</summary>
    public StyleAttributes? StyleAttributes { get; }

    /// <summary><c>scopeName</c>: the name of the innermost scope.</summary>
    public string ScopeName => ScopePath.ScopeName;

    /// <summary><c>fromExtension</c>: pushes the frames on top of <paramref name="namesScopeList"/>.</summary>
    public static AttributedScopeStack? FromExtension(
        AttributedScopeStack? namesScopeList,
        IReadOnlyList<AttributedScopeStackFrame> contentNameScopesList)
    {
        ArgumentNullException.ThrowIfNull(contentNameScopesList);
        AttributedScopeStack? current = namesScopeList;
        ScopeStack? scopeNames = namesScopeList?.ScopePath;
        foreach (AttributedScopeStackFrame frame in contentNameScopesList)
        {
            scopeNames = ScopeStack.Push(scopeNames, frame.ScopeNames);
            current = new AttributedScopeStack(current, scopeNames!, frame.EncodedTokenAttributes, null);
        }

        return current;
    }

    /// <summary><c>createRoot</c>.</summary>
    public static AttributedScopeStack CreateRoot(string scopeName, uint tokenAttributes) =>
        new(null, new ScopeStack(null, scopeName), tokenAttributes, null);

    /// <summary><c>createRootAndLookUpScopeName</c>.</summary>
    public static AttributedScopeStack CreateRootAndLookUpScopeName(
        string scopeName,
        uint tokenAttributes,
        IScopeAttributesContext grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        BasicScopeAttributes rawRootMetadata = grammar.GetMetadataForScope(scopeName);
        var scopePath = new ScopeStack(null, scopeName);
        StyleAttributes? rootStyle = grammar.ThemeProvider.ThemeMatch(scopePath);

        uint resolvedTokenAttributes = MergeAttributes(tokenAttributes, rawRootMetadata, rootStyle);
        return new AttributedScopeStack(null, scopePath, resolvedTokenAttributes, rootStyle);
    }

    /// <summary><c>toString</c>: the scope names, space separated.</summary>
    public override string ToString() => string.Join(' ', GetScopeNames());

    /// <summary>
    /// <c>equals</c> (static): structural, walking both stacks and comparing scope names and token attributes. Not an
    /// override of <see cref="object.Equals(object?)"/>, as upstream.
    /// </summary>
    public static bool Equals(AttributedScopeStack? a, AttributedScopeStack? b)
    {
        while (true)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is null || b is null)
            {
                // End of list reached only for one
                return false;
            }

            if (a.ScopeName != b.ScopeName || a.TokenAttributes != b.TokenAttributes)
            {
                return false;
            }

            // Go to previous pair
            a = a.Parent;
            b = b.Parent;
        }
    }

    /// <summary><c>equals</c> (instance).</summary>
    public bool EqualsStack(AttributedScopeStack? other) => Equals(this, other);

    /// <summary>
    /// <c>pushAttributed</c>: pushes a scope path (<paramref name="scopePath"/> may hold several space separated
    /// scope names; <see langword="null"/> pushes nothing).
    /// </summary>
    public AttributedScopeStack PushAttributed(string? scopePath, IScopeAttributesContext grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        if (scopePath is null)
        {
            return this;
        }

        if (!scopePath.Contains(' ', StringComparison.Ordinal))
        {
            // This is the common case and much faster
            return PushAttributedScope(this, scopePath, grammar);
        }

        AttributedScopeStack result = this;
        foreach (string scope in scopePath.Split(' '))
        {
            result = PushAttributedScope(result, scope, grammar);
        }

        return result;
    }

    /// <summary><c>getScopeNames</c>: outermost first.</summary>
    public List<string> GetScopeNames() => ScopePath.GetSegments();

    /// <summary>
    /// <c>getExtensionIfDefined</c>: the frames above <paramref name="base"/> (outermost first), or
    /// <see langword="null"/> when this stack does not extend it.
    /// </summary>
    public List<AttributedScopeStackFrame>? GetExtensionIfDefined(AttributedScopeStack? @base)
    {
        var result = new List<AttributedScopeStackFrame>();
        AttributedScopeStack? self = this;

        while (self is not null && !ReferenceEquals(self, @base))
        {
            result.Add(new AttributedScopeStackFrame(
                self.TokenAttributes,
                self.ScopePath.GetExtensionIfDefined(self.Parent?.ScopePath)!));
            self = self.Parent;
        }

        if (!ReferenceEquals(self, @base))
        {
            return null;
        }

        result.Reverse();
        return result;
    }

    private static uint MergeAttributes(
        uint existingTokenAttributes,
        BasicScopeAttributes basicScopeAttributes,
        StyleAttributes? styleAttributes)
    {
        FontStyle fontStyle = FontStyle.NotSet;
        int foreground = 0;
        int background = 0;

        if (styleAttributes is not null)
        {
            fontStyle = styleAttributes.FontStyle;
            foreground = styleAttributes.ForegroundId;
            background = styleAttributes.BackgroundId;
        }

        return EncodedTokenAttributes.Set(
            existingTokenAttributes,
            basicScopeAttributes.LanguageId,
            basicScopeAttributes.TokenType,
            null,
            fontStyle,
            foreground,
            background);
    }

    private static AttributedScopeStack PushAttributedScope(
        AttributedScopeStack target,
        string scopeName,
        IScopeAttributesContext grammar)
    {
        BasicScopeAttributes rawMetadata = grammar.GetMetadataForScope(scopeName);

        ScopeStack newPath = target.ScopePath.Push(scopeName);
        StyleAttributes? scopeThemeMatchResult = grammar.ThemeProvider.ThemeMatch(newPath);
        uint metadata = MergeAttributes(target.TokenAttributes, rawMetadata, scopeThemeMatchResult);
        return new AttributedScopeStack(target, newPath, metadata, scopeThemeMatchResult);
    }
}
