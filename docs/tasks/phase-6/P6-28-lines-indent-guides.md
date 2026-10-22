# P6-28 — `GuidesTextModelPart`: `GetLinesIndentGuides`

- **Status:** done
- **Depends on:** P6-27
- **Size:** S

## Goal

The text model computes how many indent guides each line of a range has (`model.Guides.GetLinesIndentGuides`), including
blank lines, which take the level of the region they are in (off-side languages differ).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/guidesTextModelPart.ts`.

| Lines | What to port |
|---|---|
| 18–39 | the class, its constructor, `getLanguageConfiguration`, `_computeIndentLevel` |
| 471–560 | `getLinesIndentGuides` |
| 562–588 | `_getIndentLevelForWhitespaceLine` |

Also `src/vs/editor/common/model/textModel.ts`: search `this._guidesTextModelPart` / `get guides()` for how the part is created and exposed.

## Files to create or change

- `src/LoomEditor.Core/Model/Guides/GuidesTextModelPart.cs` — create (header `guidesTextModelPart.ts`; `partial`, this card's file)
- `src/LoomEditor.Core/Model/TextModel.Guides.cs` — create: `internal GuidesTextModelPart Guides { get; }` initialized in the
  constructor(s) of `TextModel` (look at how `BracketPairs` is created and do the same)
- `tests/LoomEditor.Core.Tests/Model/Guides/GuidesTextModelPartIndentTests.cs` — create

## Design decisions (already made — do not change)

- `GuidesTextModelPart(TextModel textModel)`; the language configuration comes from the model's own registry
  (`TextModel.LanguageConfigurationRegistry`, P5-14/P5-15), not a service. `offSide` = the folding rules' `OffSide` of the
  model's language (read it exactly like `Features/Folding/IndentRangeProvider` does; if that code reads it from a different
  place, use the same place).
- `computeIndentLevel` → `TextModelUtils.ComputeIndentLevel(line, tabSize)` (exists).
- `assertNotDisposed()`: the part has no dispose in LoomEditor: drop it (one comment).
- `throw new Error('Illegal value for startLineNumber')` → `ArgumentOutOfRangeException(nameof(startLineNumber), …)` (same for end).
- `Math.ceil(a / b)` / `Math.floor(a / b)` on integers: use integer arithmetic that gives the same results for non-negative
  values (`(a + b - 1) / b`, `a / b`), or `(int)Math.Ceiling((double)a / b)`; add a test with a value that is not a multiple.
- `indentSize` is `TextModel.Options.IndentSize` (already resolved from `tabSize` when `indentSize: 'tabSize'`).

## Tests

The upstream tests (`TextModel.getLineIndentGuide`, 18) need `getActiveIndentGuide` too: they are ported in P6-29. New:

- [x] `GetLinesIndentGuides_IndentedLines_CountsLevels` (`"a"`, `"  b"`, `"    c"`, indent size 2 → 0, 1, 2)
- [x] `GetLinesIndentGuides_BlankLineInsideABlock_TakesTheBlockLevel`
- [x] `GetLinesIndentGuides_BlankLineAtTheEnd_IsZero`
- [x] `GetLinesIndentGuides_OffSideLanguage_BlankLineBeforeDedent_UsesTheLevelBelow` (use a language whose folding rules have
      `OffSide = true`; P5 tests have a test language for it — reuse it)
- [x] `GetLinesIndentGuides_IndentNotAMultiple_RoundsUp` (3 spaces, indent size 2 → 2)
- [x] `GetLinesIndentGuides_InvalidRange_Throws`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: new row `editor/common/model/guidesTextModelPart.ts` (🔶 P6-28: indent guides)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`getActiveIndentGuide` (P6-29), `getLinesBracketGuides` (P6-30).

## Suggested commit message

`feat(core): compute the indent guides of lines`
