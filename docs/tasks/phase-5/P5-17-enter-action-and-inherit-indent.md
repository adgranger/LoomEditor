# P5-17 — `GetEnterAction`, `GetInheritIndentForLine`, `GetGoodIndentForLine`

- **Status:** todo
- **Depends on:** P5-16
- **Size:** M

## Goal

Core computes what Enter should do at a range (`getEnterAction`: on-enter rules + indentation) and the indentation
a line inherits from the lines above it (`getInheritIndentForLine`, `getGoodIndentForLine`). Step D uses them for
Enter and smart Tab, step E for re-indentation.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/languages/enterAction.ts` | 13–65 | `getEnterAction` |
| `common/languages/autoIndent.ts` | 27–31 | `IIndentConverter` |
| same | 40–61 | `getPrecedingValidLine` |
| same | 75–217 | `getInheritIndentForLine` |
| same | 219–302 | `getGoodIndentForLine` |

## Files to create or change

- `src/LoomEditor.Core/Languages/EnterActions.cs` — create (static class, header `enterAction.ts`)
- `src/LoomEditor.Core/Languages/IIndentConverter.cs` — create (header `autoIndent.ts`)
- `src/LoomEditor.Core/Languages/AutoIndent.cs` — create (static partial class: `GetPrecedingValidLine`, `GetInheritIndentForLine`, `GetGoodIndentForLine`)
- `tests/LoomEditor.Core.Tests/Languages/TestHelpers/TestIndentConverter.cs` — create
- `tests/LoomEditor.Core.Tests/Languages/EnterActionsTests.cs`, `AutoIndentInheritTests.cs` — create

## Design decisions (already made — do not change)

- Monaco's module functions become static classes: `EnterActions.GetEnterAction(EditorAutoIndentStrategy autoIndent, TextModel model, Range range, LanguageConfigurationRegistry registry)`
  → `CompleteEnterAction?` (the type `EnterAction` already exists, hence the plural class name) and `AutoIndent`.
- `interface IIndentConverter { string ShiftIndent(string indentation); string UnshiftIndent(string indentation); }`.
  Monaco's optional `normalizeIndentation` is not used by `autoIndent.ts`: not added (step E adds it if a command
  needs it).
- `AutoIndent.GetInheritIndentForLine(EditorAutoIndentStrategy autoIndent, IVirtualModel model, int lineNumber, bool honorIntentialIndent, LanguageConfigurationRegistry registry)`
  → `InheritIndentResult?` with `readonly record struct InheritIndentResult(string Indentation, IndentAction? Action, int? Line = null)`.
  Monaco's `honorIntentialIndent = true` default sits before a required parameter: in C# it is required; callers
  that pass `undefined` pass `true`.
- `AutoIndent.GetGoodIndentForLine(EditorAutoIndentStrategy autoIndent, IVirtualModel virtualModel, string languageId, int lineNumber, IIndentConverter indentConverter, LanguageConfigurationRegistry registry)` → `string?`.
- `getPrecedingValidLine` is private; it returns `-1`, `0` or a line number exactly like Monaco.
- `getLanguageIdAtPosition(lineNumber, 0)` → column 1 (P5-16).
- `/^\s+$/.test(text) || text === ''` and `/^\s*$/.test(…)` → `string.IsNullOrWhiteSpace` (see P5-11).
- Line 189 of `autoIndent.ts` tests `shouldIndentNextLine(i)` inside the `j` loop: keep it (`// Monaco uses i here`).
- `getEnterAction`: `if (!richEditSupport)` cannot happen (the registry never returns null): drop the check.
  `removeText || 0` → `RemoveText`; `if (!appendText)` → `string.IsNullOrEmpty`. The indentation comes from
  `LanguageConfigurationRegistry.GetIndentationAtPosition` (P5-14). `model.getLanguageIdAtPosition` →
  `model.Tokenization.GetLanguageIdAtPosition`.
- `TestIndentConverter(int indentSize = 4)`: `ShiftIndent` appends `indentSize` spaces; `UnshiftIndent` removes the
  last `indentSize` spaces (or the last tab) when present. Only for the tests of steps B, C and E that do not have a
  cursor configuration.

## Tests

No upstream unit tests: `indentation.test.ts` (step E) and `cursor.test.ts` (step D) exercise these functions
through the editor. New, with `TestLanguages.TypeScript` (P5-16) and fake tokens:

- [ ] `GetEnterAction_BetweenBraces_IsIndentOutdentWithTab`
- [ ] `GetEnterAction_AfterDocCommentStart_AppendsStar`
- [ ] `GetEnterAction_RemoveText_ShortensTheIndentation`
- [ ] `GetEnterAction_NoRuleMatches_ReturnsNull`
- [ ] `GetInheritIndentForLine_BelowFull_ReturnsNull`
- [ ] `GetInheritIndentForLine_AfterOpenBrace_IsIndent`
- [ ] `GetInheritIndentForLine_FirstNonBlankLine_HasNoIndentation`
- [ ] `GetInheritIndentForLine_AfterIndentNextLine_UsesTheLineBefore`
- [ ] `GetGoodIndentForLine_ClosingBrace_IsUnshifted`
- [ ] `GetGoodIndentForLine_AfterOnEnterRule_AppliesTheEnterAction`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (`enterAction.ts`, `autoIndent.ts` partly)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`getIndentForEnter`, `getIndentActionForType`, `getIndentMetadata` (P5-18); calling them from the cursor (step D).

## Suggested commit message

`feat(core): port the enter action and the inherited indentation`
