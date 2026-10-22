# P6-29 — `GuidesTextModelPart.GetActiveIndentGuide` and the indent guide tests

- **Status:** todo
- **Depends on:** P6-28
- **Size:** M

## Goal

The text model finds the indent guide to highlight for a line (start line, end line, level), searching up and down in
the bounds given, and the 18 upstream `getLineIndentGuide` tests pass.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/guidesTextModelPart.ts` | 41–272 | `getActiveIndentGuide` (with its local closures `up_resolveIndents`, `up_resolveIndents`/`down_resolveIndents`, `goUp`, `goDown`, the main loop) |
| `src/vs/editor/test/common/model/textModelWithTokens.test.ts` | 712–738 | suite `TextModel.getLineIndentGuide`, helper `assertIndentGuides` |
| same | 740–948 | the 18 tests (`getLineIndentGuide one level 2` … `tweaks - empty line`) |

## Files to create or change

- `src/LoomEditor.Core/Model/Guides/GuidesTextModelPart.Active.cs` — create (partial; same header)
- `tests/LoomEditor.Core.Tests/Model/Guides/TextModelGetLineIndentGuideTests.cs` — create

## Design decisions (already made — do not change)

- Monaco's closures capture mutable locals (`up_aboveContentLineIndex`, …): port them as C# **local functions** in the method
  (they capture the locals by reference, like JavaScript closures). Do not turn them into fields or a helper class.
- `BugIndicatingError('Illegal value for lineNumber')` → `ArgumentOutOfRangeException(nameof(lineNumber), …)`.
- Returns `ActiveIndentGuideInfo` (P6-27).
- Test helper `AssertIndentGuides((int Indent, int Start, int End, int Level, string Text)[] lines, int indentSize)`:
  creates a model with a registered plain language (Monaco registers `'testLang'` with no configuration: use the test language
  registry helper of the P5 language tests that has no folding rules), `UpdateOptions` with `IndentSize`, then compares, line by
  line, `GetLinesIndentGuides(1, lineCount)[i]` and `GetActiveIndentGuide(line, 1, lineCount)` with the tuple.
- Each test is a `[Fact]` with the Monaco tuples copied verbatim (they are `[indent, start, end, level, text]`).

## Tests

- [ ] Port: `TextModel.getLineIndentGuide` › all 18 tests: `getLineIndentGuide one level 2`, `two levels`, `three levels`,
      `decreasing indent`, `Java`, `Javadoc`, `Whitespace`, `Tabs`, `checker.ts`, `issue #8425 - Missing indentation lines for first level indentation`,
      `issue #8952 - Indentation guide lines going through text on .yml file`, `issue #11892 - Indent guides look funny`,
      `issue #12398 - Problem in indent guidelines`, `issue #49173`, `tweaks - no active`, `tweaks - inside scope`,
      `tweaks - scope start`, `tweaks - empty line`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `textModelWithTokens.test.ts` is 29 / 29 (phase 5 row and phase 6 table); `guidesTextModelPart.ts` row updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Bracket guides (P6-30), the view model (P6-31).

## Suggested commit message

`feat(core): find the active indent guide`
