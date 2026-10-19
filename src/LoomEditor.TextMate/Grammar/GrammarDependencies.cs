// Ported from microsoft/vscode-textmate: src/grammar/grammarDependencies.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Parsing;

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>AbsoluteRuleReference</c>: <see cref="TopLevelRuleReference"/> or <see cref="TopLevelRepositoryRuleReference"/>.</summary>
/// <param name="scopeName"><c>scopeName</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal abstract class AbsoluteRuleReference(string scopeName)
{
    /// <summary><c>scopeName</c>.</summary>
    public string ScopeName { get; } = scopeName;

    /// <summary><c>toKey</c>.</summary>
    public abstract string ToKey();
}

/// <summary><c>TopLevelRuleReference</c>: references the top level rule of a grammar with the given scope name.</summary>
/// <param name="scopeName"><c>scopeName</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed class TopLevelRuleReference(string scopeName) : AbsoluteRuleReference(scopeName)
{
    /// <inheritdoc />
    public override string ToKey() => ScopeName;
}

/// <summary><c>TopLevelRepositoryRuleReference</c>: references a rule in the top level repository section of a grammar.</summary>
/// <param name="scopeName"><c>scopeName</c>.</param>
/// <param name="ruleName"><c>ruleName</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed class TopLevelRepositoryRuleReference(string scopeName, string ruleName) : AbsoluteRuleReference(scopeName)
{
    /// <summary><c>ruleName</c>.</summary>
    public string RuleName { get; } = ruleName;

    /// <inheritdoc />
    public override string ToKey() => $"{ScopeName}#{RuleName}";
}

/// <summary><c>ExternalReferenceCollector</c>: the external references found so far, without duplicates.</summary>
/// <remarks>Threading: single-threaded.</remarks>
internal sealed class ExternalReferenceCollector
{
    private readonly List<AbsoluteRuleReference> _references = [];
    private readonly HashSet<string> _seenReferenceKeys = new(StringComparer.Ordinal);

    /// <summary><c>references</c>, in the order they were found.</summary>
    public IReadOnlyList<AbsoluteRuleReference> References => _references;

    /// <summary><c>visitedRule</c>: by reference, like a JavaScript <c>Set</c> of objects.</summary>
    public HashSet<RawRule> VisitedRule { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary><c>add</c>.</summary>
    public void Add(AbsoluteRuleReference reference)
    {
        if (_seenReferenceKeys.Add(reference.ToKey()))
        {
            _references.Add(reference);
        }
    }
}

/// <summary><c>ScopeDependencyProcessor</c>: finds the grammars (or parts of them) that a grammar needs.</summary>
/// <remarks>
/// <para>
/// Deliberate difference: upstream takes an <c>IGrammarRepository</c> (<c>lookup</c>, <c>injections</c>); here the two
/// functions are passed directly (ADR 0008, decision 3: the registry is synchronous).
/// </para>
/// <para>Threading: single-threaded.</para>
/// </remarks>
internal sealed partial class ScopeDependencyProcessor
{
    private readonly Func<string, RawGrammar?> _lookup;
    private readonly Func<string, IReadOnlyList<string>?> _injections;

    /// <summary>Starts at the top level rule of <paramref name="initialScopeName"/>.</summary>
    /// <param name="lookup"><c>repo.lookup</c>: the raw grammar of a scope name, if loaded.</param>
    /// <param name="initialScopeName"><c>initialScopeName</c>.</param>
    /// <param name="injections"><c>repo.injections</c>: the scope names of the grammars injected into a scope name.</param>
    public ScopeDependencyProcessor(
        Func<string, RawGrammar?> lookup,
        string initialScopeName,
        Func<string, IReadOnlyList<string>?>? injections = null)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(initialScopeName);
        _lookup = lookup;
        _injections = injections ?? (_ => null);
        InitialScopeName = initialScopeName;
        SeenFullScopeRequests.Add(initialScopeName);
        Q = [new TopLevelRuleReference(initialScopeName)];
    }

    /// <summary><c>initialScopeName</c>.</summary>
    public string InitialScopeName { get; }

    /// <summary><c>seenFullScopeRequests</c>.</summary>
    public HashSet<string> SeenFullScopeRequests { get; } = new(StringComparer.Ordinal);

    /// <summary><c>seenPartialScopeRequests</c>.</summary>
    public HashSet<string> SeenPartialScopeRequests { get; } = new(StringComparer.Ordinal);

    /// <summary><c>Q</c>: the references to process next, in order.</summary>
    public List<AbsoluteRuleReference> Q { get; private set; }

    /// <summary><c>processQueue</c>.</summary>
    public void ProcessQueue()
    {
        List<AbsoluteRuleReference> q = Q;
        Q = [];

        var deps = new ExternalReferenceCollector();
        foreach (AbsoluteRuleReference dep in q)
        {
            CollectReferencesOfReference(dep, deps);
        }

        foreach (AbsoluteRuleReference dep in deps.References)
        {
            if (dep is TopLevelRuleReference)
            {
                if (!SeenFullScopeRequests.Add(dep.ScopeName))
                {
                    // already processed
                    continue;
                }

                Q.Add(dep);
            }
            else
            {
                if (SeenFullScopeRequests.Contains(dep.ScopeName))
                {
                    // already processed in full
                    continue;
                }

                if (!SeenPartialScopeRequests.Add(dep.ToKey()))
                {
                    // already processed
                    continue;
                }

                Q.Add(dep);
            }
        }
    }
}
