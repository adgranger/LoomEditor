# P5-60 — LinesOperations: transpose and case transforms

- **Status:** todo
- **Depends on:** P5-59
- **Size:** M

## Goal

Add `editor.action.transpose` (swap the characters around the cursor) and the seven case transforms: upper, lower,
title, snake, camel, Pascal and kebab case. They work on the selection, or on the word at the cursor when the
selection is empty. This completes `linesOperations.ts` and its test file.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/linesOperations/browser/linesOperations.ts` | 1057–1113 | `TransposeAction` |
| same | 1115–1154 | `AbstractCaseAction.run` |
| same | 1156–1184 | `UpperCaseAction`, `LowerCaseAction` |
| same | 1186–1214 | `BackwardsCompatibleRegExp` (**not** ported, see below) |
| same | 1216–1239 | `TitleCaseAction` |
| same | 1241–1268 | `SnakeCaseAction` |
| same | 1270–1296 | `CamelCaseAction` |
| same | 1298–1334 | `PascalCaseAction` |
| same | 1336–1377 | `KebabCaseAction` |
| same | 1379–1416 | the registrations (all seven are registered: every regex compiles in .NET) |
| `contrib/linesOperations/test/browser/linesOperations.test.ts` | 826–900 | test `transpose` |
| same | 902–1364 | test `toggle case` |

## Files to create or change

- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsActions.Transpose.cs` — create
- `src/LoomEditor.View/Features/LinesOperations/CaseTransforms.cs` — create: the seven `_modifyText` functions as
  `internal static string ToUpper(string text)`, …, `ToKebabCase(string text)`
- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsActions.Case.cs` — create: `AbstractCaseAction.run`
- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsFeature.cs`, `README.md` — change
- `tests/LoomEditor.View.Tests/Features/LinesOperations/LinesOperationsTests.Case.cs` — create

## Design decisions (already made — do not change)

- **All seven transforms are ported** (VS Code registers them when the browser supports the regexes; .NET supports
  every one: `\p{L}`, `\p{Lu}`, `\p{Ll}`, `\p{N}`, `\P{L}`, lookbehind `(?<=\.)`). `BackwardsCompatibleRegExp` is
  therefore not ported: the regexes are `static readonly Regex` fields with `RegexOptions.CultureInvariant`, plus
  `RegexOptions.Multiline` where the JavaScript flags have `m` (ADR 0009 decision 6). The `u` flag needs nothing.
- `String.prototype.replace(globalRegex, '$1_$2')` → `regex.Replace(text, "$1_$2")` (same substitution syntax for
  numbered groups). `replace(regex, b => b.toLocaleUpperCase())` → `regex.Replace(text, m => m.Value.ToUpperInvariant())`.
  `text.split(regex)` → `regex.Split(text)` (no capture groups in these patterns, so the results match JavaScript).
  `words.shift()?.replace(validWordStart, …)`: `split` never returns an empty array, so the first word always exists
  (`words[0]`, then the rest).
- **Casing**: `toLocaleUpperCase` / `toLocaleLowerCase` → `ToUpperInvariant` / `ToLowerInvariant`. Behavior difference
  (XML doc + `docs/PORTING.md`): the invariant culture, not the user's locale (Turkish dotted i differs).
- `editor.getConfiguredWordAtPosition(cursor)` → `WordOperations.GetWordAtPosition(viewModel.Model,
  viewModel.CursorConfig.WordSeparators, viewModel.CursorConfig.WordSegmenterLocales, cursor)` (exists; Monaco's
  `getConfiguredWordAtPosition` does exactly this).
- `TransposeAction`: `split('').reverse().join('')` reverses UTF-16 code units, like JavaScript: reverse the `char[]`
  (do not use text elements). It uses `ReplaceCommand` and `ReplaceCommandThatPreservesSelection` (exist).
- `precondition: writable` → `canExecute: () => !viewModel.CursorConfig.ReadOnly`. No keybinding (none has `kbOpts`).

## Tests

- [ ] Port `transpose`
- [ ] Port `toggle case` (one test, as upstream)
- [ ] New: `ToTitleCase_Apostrophes_FollowMonaco` (`"o'neil isn't"` → `"O'neil Isn't"`, from the `titleBoundary` regex)
- [ ] New: `CaseTransforms_AllRegexesCompile` (touches every static `Regex`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `contrib/linesOperations/browser/linesOperations.ts` (✅, P5-60); behavior difference
      "case transforms use the invariant culture"; tests table `linesOperations.test.ts` 55 / 55
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Menus and the command palette labels.

## Suggested commit message

`feat(view): add transpose and the case transforms`
