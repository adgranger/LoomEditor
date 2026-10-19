// Ported from microsoft/vscode-textmate: src/grammar/grammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.TextMate.Grammar;

/// <summary>Represents a "pushed" state on the stack (as a linked list element).</summary>
/// <remarks>
/// Threading: immutable except <see cref="GetEnterPos"/> / <see cref="GetAnchorPos"/>, which <see cref="Reset"/> sets to
/// <c>-1</c> (upstream does the same); do not share a stack between threads while it is being tokenized.
/// </remarks>
internal sealed class StateStackImpl : StateStack
{
    /// <summary>Upstream's <c>StateStackImpl.NULL</c> (<c>INITIAL</c>): rule id 0, no scopes.</summary>
    public static readonly StateStackImpl Null = new(null, new RuleId(0), 0, 0, false, null, null, null);

    private int _enterPos;
    private int _anchorPos;
    private readonly RuleId _ruleId;

    /// <summary>Creates an element. Invariants are those of upstream's constructor documentation.</summary>
    /// <param name="parent">The previous state on the stack (or <see langword="null"/> for the root state).</param>
    /// <param name="ruleId">The state (rule) that this element represents.</param>
    /// <param name="enterPos">
    /// The position on the current line where this state was pushed (only relevant while tokenizing a line, to detect
    /// endless loops).
    /// </param>
    /// <param name="anchorPos">
    /// The captured anchor position when this element was pushed (only relevant while tokenizing a line).
    /// </param>
    /// <param name="beginRuleCapturedEol">
    /// The state has entered and captured \n. This means that the next line should have an anchor position of 0.
    /// </param>
    /// <param name="endRule">The "pop" (end) condition, in case it was dynamically generated through captured text.</param>
    /// <param name="nameScopesList">The scopes containing the "name" for this state.</param>
    /// <param name="contentNameScopesList">The scopes containing the "contentName" (besides "name") for this state.</param>
    public StateStackImpl(
        StateStackImpl? parent,
        RuleId ruleId,
        int enterPos,
        int anchorPos,
        bool beginRuleCapturedEol,
        string? endRule,
        AttributedScopeStack? nameScopesList,
        AttributedScopeStack? contentNameScopesList)
    {
        Parent = parent;
        _ruleId = ruleId;
        BeginRuleCapturedEol = beginRuleCapturedEol;
        EndRule = endRule;
        NameScopesList = nameScopesList;
        ContentNameScopesList = contentNameScopesList;
        Depth = parent is not null ? parent.Depth + 1 : 1;
        _enterPos = enterPos;
        _anchorPos = anchorPos;
    }

    /// <summary><c>parent</c>.</summary>
    public StateStackImpl? Parent { get; }

    /// <summary><c>ruleId</c>.</summary>
    public RuleId RuleId => _ruleId;

    /// <summary><c>beginRuleCapturedEOL</c>.</summary>
    public bool BeginRuleCapturedEol { get; }

    /// <summary><c>endRule</c>.</summary>
    public string? EndRule { get; }

    /// <summary><c>nameScopesList</c>.</summary>
    public AttributedScopeStack? NameScopesList { get; }

    /// <summary><c>contentNameScopesList</c>.</summary>
    public AttributedScopeStack? ContentNameScopesList { get; }

    /// <inheritdoc />
    public int Depth { get; }

    /// <inheritdoc />
    public bool Equals(StateStack? other) => other is StateStackImpl impl && EqualsStack(this, impl);

    /// <inheritdoc />
    StateStack StateStack.Clone() => Clone();

    /// <summary><c>clone</c>: the stack is immutable, so this is <c>this</c>.</summary>
    public StateStackImpl Clone() => this;

    /// <summary><c>reset</c>: forgets the enter and anchor positions of every element (they mean nothing across lines).</summary>
    public void Reset()
    {
        for (StateStackImpl? el = this; el is not null; el = el.Parent)
        {
            el._enterPos = -1;
            el._anchorPos = -1;
        }
    }

    /// <summary><c>pop</c>.</summary>
    public StateStackImpl? Pop() => Parent;

    /// <summary><c>safePop</c>: the parent, or this element when it is the root.</summary>
    public StateStackImpl SafePop() => Parent ?? this;

    /// <summary><c>push</c>.</summary>
    public StateStackImpl Push(
        RuleId ruleId,
        int enterPos,
        int anchorPos,
        bool beginRuleCapturedEol,
        string? endRule,
        AttributedScopeStack? nameScopesList,
        AttributedScopeStack? contentNameScopesList) =>
        new(this, ruleId, enterPos, anchorPos, beginRuleCapturedEol, endRule, nameScopesList, contentNameScopesList);

    /// <summary><c>getEnterPos</c>.</summary>
    public int GetEnterPos() => _enterPos;

    /// <summary><c>getAnchorPos</c>.</summary>
    public int GetAnchorPos() => _anchorPos;

    /// <summary><c>getRule</c>: the rule is registered by the time its state is pushed.</summary>
    public Rule GetRule(IRuleRegistry grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        return grammar.GetRule(_ruleId)!;
    }

    /// <summary><c>toString</c>: <c>[(ruleId, names, contentNames),...]</c>, outermost first.</summary>
    public override string ToString()
    {
        var r = new List<string>();
        WriteString(r);
        return "[" + string.Join(',', r) + "]";
    }

    /// <summary><c>withContentNameScopesList</c>.</summary>
    public StateStackImpl WithContentNameScopesList(AttributedScopeStack contentNameScopeStack)
    {
        if (ReferenceEquals(ContentNameScopesList, contentNameScopeStack))
        {
            return this;
        }

        return Parent!.Push(
            _ruleId,
            _enterPos,
            _anchorPos,
            BeginRuleCapturedEol,
            EndRule,
            NameScopesList,
            contentNameScopeStack);
    }

    /// <summary><c>withEndRule</c>.</summary>
    public StateStackImpl WithEndRule(string endRule)
    {
        if (EndRule == endRule)
        {
            return this;
        }

        return new StateStackImpl(
            Parent,
            _ruleId,
            _enterPos,
            _anchorPos,
            BeginRuleCapturedEol,
            endRule,
            NameScopesList,
            ContentNameScopesList);
    }

    /// <summary><c>hasSameRuleAs</c>: used to warn of endless loops.</summary>
    public bool HasSameRuleAs(StateStackImpl other)
    {
        StateStackImpl? el = this;
        while (el is not null && el._enterPos == other._enterPos)
        {
            if (el._ruleId == other._ruleId)
            {
                return true;
            }

            el = el.Parent;
        }

        return false;
    }

    /// <summary><c>toStateStackFrame</c>.</summary>
    public StateStackFrame ToStateStackFrame() => new(
        RuleId.ToNumber(_ruleId),
        null,
        null,
        BeginRuleCapturedEol,
        EndRule,
        NameScopesList?.GetExtensionIfDefined(Parent?.NameScopesList) ?? [],
        ContentNameScopesList?.GetExtensionIfDefined(NameScopesList) ?? []);

    /// <summary><c>pushFrame</c>.</summary>
    public static StateStackImpl PushFrame(StateStackImpl? self, StateStackFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        AttributedScopeStack namesScopeList = AttributedScopeStack.FromExtension(self?.NameScopesList, frame.NameScopesList)!;
        return new StateStackImpl(
            self,
            RuleId.FromNumber(frame.RuleId),
            frame.EnterPos ?? -1,
            frame.AnchorPos ?? -1,
            frame.BeginRuleCapturedEol,
            frame.EndRule,
            namesScopeList,
            AttributedScopeStack.FromExtension(namesScopeList, frame.ContentNameScopesList)!);
    }

    private static bool EqualsStack(StateStackImpl a, StateStackImpl b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (!StructuralEquals(a, b))
        {
            return false;
        }

        return AttributedScopeStack.Equals(a.ContentNameScopesList, b.ContentNameScopesList);
    }

    /// <summary>A structural equals check. Does not take into account <c>scopes</c>.</summary>
    private static bool StructuralEquals(StateStackImpl? a, StateStackImpl? b)
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

            if (a.Depth != b.Depth || a._ruleId != b._ruleId || a.EndRule != b.EndRule)
            {
                return false;
            }

            // Go to previous pair
            a = a.Parent;
            b = b.Parent;
        }
    }

    private void WriteString(List<string> res)
    {
        Parent?.WriteString(res);
        res.Add(string.Create(
            CultureInfo.InvariantCulture,
            $"({_ruleId}, {NameScopesList?.ToString()}, {ContentNameScopesList?.ToString()})"));
    }
}
