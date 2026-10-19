# P4-20 — `SyncRegistry`, `Registry` (synchronous) and `diffStateStacks`

- **Status:** todo
- **Depends on:** P4-19
- **Size:** M

## Goal

vscode-textmate's public entry point exists: a `Registry` created with a grammar loader, a theme and injections,
which loads a grammar and the grammars it depends on, sets the theme, and gives the color map. State-stack diffs exist
for the tests.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/registry.ts` | 11–102 | `SyncRegistry` (`dispose`, `setTheme`, `getColorMap`, `addGrammar`, `lookup`, `injections`, `getDefaults`, `themeMatch`, `grammarForScopeName`) |
| textmate: `src/main.ts` | 22–195 | `RegistryOptions`, `Registry` (`dispose`, `setTheme`, `getColorMap`, `loadGrammarWithEmbeddedLanguages`, `loadGrammarWithConfiguration`, `loadGrammar`, `_loadGrammar` with `ScopeDependencyProcessor`, `_loadSingleGrammar`, `_doLoadSingleGrammar`, `addGrammar`, `_grammarForScopeName`) |
| textmate: `src/main.ts` | 303 | `parseRawGrammar` re-export (as a static method of `Registry` or a call to `RawGrammarReader`) |
| textmate: `src/diffStateStacks.ts` | 8–47 | `diffStateStacksRefEq`, `applyStateStackDiff`, `StackDiff` |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/SyncRegistry.cs`, `Registry.cs`, `RegistryOptions.cs`, `DiffStateStacks.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/RegistryTests.cs`, `DiffStateStacksTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008 decision 3: `RegistryOptions.LoadGrammar` is `Func<string, RawGrammar?>`; `RegistryOptions.GetInjections`
  is `Func<string, IReadOnlyList<string>?>?`; `OnigLib` is an `IOnigLib`; `Theme` is a `RawTheme?`; `ColorMap` is
  `IReadOnlyList<string>?`. `LoadGrammar…` methods return `IGrammar?` synchronously.
- `_ensureGrammarCache` / `_loadGrammar` keep upstream's order: process the dependency queue, then create the grammar.
- `Registry` is single-threaded and `IDisposable` (it disposes its grammars). One registry per thread (ADR 0008
  decision 5): written in the remarks.

## Tests

No upstream unit tests of the registry itself (P4-21 runs the suites through it). New:

- [ ] `LoadGrammar_LoadsIncludedGrammarsThroughTheLoader`
- [ ] `LoadGrammar_UnknownScope_ReturnsNull`
- [ ] `SetTheme_ChangesTheColorMapAndTheMetadata`
- [ ] `DiffStateStacks_ApplyDiff_RebuildsAnEqualStack` (states from tokenizing three lines)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated; "Behavior differences": synchronous registry
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

VS Code's grammar factory (P4-34).

## Suggested commit message

`feat(textmate): port the TextMate registry`
