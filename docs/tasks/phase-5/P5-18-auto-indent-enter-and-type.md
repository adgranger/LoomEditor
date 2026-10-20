# P5-18 — `GetIndentForEnter`, `GetIndentActionForType`, `GetIndentMetadata`

- **Status:** done
- **Depends on:** P5-17
- **Size:** S

## Goal

Core has the rest of Monaco's `autoIndent.ts`: the indentation before and after Enter, the re-indentation when a
typed character makes the line match `decreaseIndentPattern`, and the indent metadata of a line.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/languages/autoIndent.ts` | 304–354 | `getIndentForEnter` |
| same | 360–433 | `getIndentActionForType` |
| same | 435–448 | `getIndentMetadata` |
| same | 450–476 | `createVirtualModelWithModifiedTokensAtLine` |

## Files to create or change

- `src/LoomEditor.Core/Languages/AutoIndent.Enter.cs` — create (the other part of `static partial class AutoIndent`)
- `tests/LoomEditor.Core.Tests/Languages/AutoIndentEnterAndTypeTests.cs` — create

## Design decisions (already made — do not change)

- `AutoIndent.GetIndentForEnter(EditorAutoIndentStrategy autoIndent, TextModel model, Range range, IIndentConverter indentConverter, LanguageConfigurationRegistry registry)`
  → `(string BeforeEnter, string AfterEnter)?`.
- `AutoIndent.GetIndentActionForType(EditorAutoIndentStrategy autoIndent, AutoClosingPairs autoClosingPairs, TextModel model, Range range, string ch, IIndentConverter indentConverter, LanguageConfigurationRegistry registry)`
  → `string?`. Monaco passes the whole `CursorConfiguration` (a View type): Core takes the only two values it reads,
  `cursorConfig.autoIndent` and `cursorConfig.autoClosingPairs`. `autoClosingPairsOpenByEnd.get(ch)` →
  `TryGetValue(ch, out var pairs) && pairs.Count > 0`.
- `AutoIndent.GetIndentMetadata(TextModel model, int lineNumber, LanguageConfigurationRegistry registry)` → `int?`.
- `createVirtualModelWithModifiedTokensAtLine` → a private nested `sealed class` implementing `IVirtualModel`; its
  `ForceTokenization` does nothing (Monaco's object has no `forceTokenization`).
- `range.getStartPosition()` → `range.Start`; `range.isEmpty()` → `range.IsEmpty`; `/^\s*$/` → `string.IsNullOrWhiteSpace`.
- `IndentationContextProcessor.IsLanguageDifferentFromLineStart` (P5-16).

## Tests

No upstream unit tests (`indentation.test.ts` `Auto Indent On Type - …` suites, step E, and `cursor.test.ts`, step
C, exercise them through typing). New, with `TestLanguages.TypeScript` and fake tokens:

- [x] `GetIndentForEnter_BelowFull_ReturnsNull`
- [x] `GetIndentForEnter_AfterOpenBrace_ShiftsTheNewLine`
- [x] `GetIndentForEnter_BeforeClosingBrace_UnshiftsTheNewLine`
- [x] `GetIndentActionForType_ClosingBrace_Outdents`
- [x] `GetIndentActionForType_AlreadyDecreased_ReturnsNull` (intentional indentation is kept)
- [x] `GetIndentActionForType_OpenBraceAfterIndentNextLine_UsesTheInheritedIndentation`
- [x] `GetIndentMetadata_OutOfRangeLine_ReturnsNull`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`autoIndent.ts` complete)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The cursor cut points that call these functions (step D).

## Suggested commit message

`feat(core): port the auto-indentation for enter and typing`
