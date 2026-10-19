// Ported from microsoft/vscode-textmate: src/grammar/grammarDependencies.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>IncludeReferenceKind</c>.</summary>
/// <remarks>Threading: a value type.</remarks>
internal enum IncludeReferenceKind
{
    /// <summary><c>$base</c>.</summary>
    Base,

    /// <summary><c>$self</c>.</summary>
    Self,

    /// <summary><c>#name</c>: a rule of the current repository.</summary>
    RelativeReference,

    /// <summary><c>source.js</c>: another grammar.</summary>
    TopLevelReference,

    /// <summary><c>source.js#name</c>: a rule of another grammar's repository.</summary>
    TopLevelRepositoryReference,
}

/// <summary><c>IncludeReference</c>: what an <c>include</c> string refers to.</summary>
/// <remarks>Threading: immutable.</remarks>
internal abstract class IncludeReference
{
    /// <summary><c>kind</c>.</summary>
    public abstract IncludeReferenceKind Kind { get; }

    /// <summary><c>parseInclude</c>.</summary>
    public static IncludeReference Parse(string include)
    {
        ArgumentNullException.ThrowIfNull(include);
        if (include == "$base")
        {
            return new BaseReference();
        }
        else if (include == "$self")
        {
            return new SelfReference();
        }

        int indexOfSharp = include.IndexOf('#', StringComparison.Ordinal);
        if (indexOfSharp == -1)
        {
            return new TopLevelReference(include);
        }
        else if (indexOfSharp == 0)
        {
            return new RelativeReference(include[1..]);
        }
        else
        {
            string scopeName = include[..indexOfSharp];
            string ruleName = include[(indexOfSharp + 1)..];
            return new TopLevelRepositoryReference(scopeName, ruleName);
        }
    }
}

/// <summary><c>BaseReference</c>.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class BaseReference : IncludeReference
{
    /// <inheritdoc />
    public override IncludeReferenceKind Kind => IncludeReferenceKind.Base;
}

/// <summary><c>SelfReference</c>.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class SelfReference : IncludeReference
{
    /// <inheritdoc />
    public override IncludeReferenceKind Kind => IncludeReferenceKind.Self;
}

/// <summary><c>RelativeReference</c>.</summary>
/// <param name="ruleName"><c>ruleName</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed class RelativeReference(string ruleName) : IncludeReference
{
    /// <inheritdoc />
    public override IncludeReferenceKind Kind => IncludeReferenceKind.RelativeReference;

    /// <summary><c>ruleName</c>.</summary>
    public string RuleName { get; } = ruleName;
}

/// <summary><c>TopLevelReference</c>.</summary>
/// <param name="scopeName"><c>scopeName</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed class TopLevelReference(string scopeName) : IncludeReference
{
    /// <inheritdoc />
    public override IncludeReferenceKind Kind => IncludeReferenceKind.TopLevelReference;

    /// <summary><c>scopeName</c>.</summary>
    public string ScopeName { get; } = scopeName;
}

/// <summary><c>TopLevelRepositoryReference</c>.</summary>
/// <param name="scopeName"><c>scopeName</c>.</param>
/// <param name="ruleName"><c>ruleName</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed class TopLevelRepositoryReference(string scopeName, string ruleName) : IncludeReference
{
    /// <inheritdoc />
    public override IncludeReferenceKind Kind => IncludeReferenceKind.TopLevelRepositoryReference;

    /// <summary><c>scopeName</c>.</summary>
    public string ScopeName { get; } = scopeName;

    /// <summary><c>ruleName</c>.</summary>
    public string RuleName { get; } = ruleName;
}
