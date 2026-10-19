# P4-15 — Grammar (2/6): `StateStackImpl` (the rule stack, the state of a line)

- **Status:** todo
- **Depends on:** P4-14
- **Size:** M

## Goal

The state that vscode-textmate keeps between lines exists: an immutable stack of rules with their end patterns, scope
stacks and anchor positions, with structural equality and the frame (de)serialization used by `diffStateStacks`.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/grammar/grammar.ts` | 605–904 | `StateStackImpl` (constructor, `equals`, `_equals`, `_structuralEquals`, `clone`, `_reset`, `reset`, `pop`, `safePop`, `push`, `getEnterPos`, `getAnchorPos`, `getRule`, `toString`, `_writeString`, `withContentNameScopesList`, `withEndRule`, `hasSameRuleAs`, `toStateStackFrame`, `pushFrame`), `StateStackFrame` |
| textmate: `src/main.ts` | 293–301 | `StateStack` (public interface), `INITIAL` |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/StateStack.cs` (the interface + `StateStack.Initial`), `StateStackImpl.cs`, `StateStackFrame.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/StateStackTests.cs` — create

## Design decisions (already made — do not change)

- Immutable, as upstream: `push`, `pop`, `withEndRule`… return new instances. `_enterPos` / `_anchorPos` are reset by
  `reset()` exactly like upstream (they are the only mutable fields; `clone` returns `this`, as upstream).
- `StateStack` (the interface) has `Depth`, `Clone()`, `Equals(StateStack?)`. P4-33 makes it also implement Core's
  `IState` (VS Code passes a `StateStack` as Monaco's `IState`); not in this card (Core's `IState` comes in P4-23).
- `INITIAL` is upstream's `StateStackImpl.NULL`, a real object (check its constructor arguments in the file); port it
  as `StateStackImpl.Null` and expose it as `StateStack.Initial`.
- `GetHashCode` is not overridden; equality is the `Equals(StateStack?)` method.
- `toString` / `_writeString` are kept (they help debugging and tests).

## Tests

No upstream unit tests. New:

- [ ] `Push_ThenPop_ReturnsEqualStack`
- [ ] `Equals_StructurallyEqualStacks_AreEqual`, `…_DifferentEndRule_AreNotEqual`
- [ ] `SafePop_OnRoot_ReturnsRoot`
- [ ] `ToStateStackFrame_PushFrame_RoundTrips`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`diffStateStacks` (P4-20).

## Suggested commit message

`feat(textmate): port the state stack`
