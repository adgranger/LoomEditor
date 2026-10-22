# P6-27 — Guide types: `IndentGuide`, `BracketGuideOptions`, `ActiveIndentGuideInfo`

- **Status:** todo
- **Depends on:** P5-96
- **Size:** S

## Goal

The value types that the indent and bracket guides use exist in Core, with the class names of bracket guides.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/textModelGuides.ts` | 8–25 | `IGuidesTextModelPart` (informative: no interface is ported, P6-28 adds a class) |
| same | 27–31 | `IActiveIndentGuideInfo` |
| same | 33–37 | `HorizontalGuidesState` |
| same | 39–43 | `BracketGuideOptions` |
| same | 45–65 | `IndentGuide` (with the constructor check) |
| same | 67–72 | `IndentGuideHorizontalLine` |
| `src/vs/editor/common/model/guidesTextModelPart.ts` | 590–602 | `BracketPairGuidesClassNames` |

## Files to create or change

- `src/LoomEditor.Core/Model/Guides/IndentGuide.cs` — create (header `textModelGuides.ts`): `IndentGuide`, `IndentGuideHorizontalLine`
- `src/LoomEditor.Core/Model/Guides/BracketGuideOptions.cs` — create: `BracketGuideOptions`, `HorizontalGuidesState`
- `src/LoomEditor.Core/Model/Guides/ActiveIndentGuideInfo.cs` — create
- `src/LoomEditor.Core/Model/Guides/BracketPairGuidesClassNames.cs` — create (header `guidesTextModelPart.ts`)
- `tests/LoomEditor.Core.Tests/Model/Guides/IndentGuideTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Core.Model.Guides`; all types `internal`.
- `internal sealed record IndentGuide` with positional members `int VisibleColumn, int Column, string ClassName, IndentGuideHorizontalLine? HorizontalLine, int ForWrappedLinesAfterColumn, int ForWrappedLinesBeforeOrAtColumn`;
  `-1` keeps Monaco's meaning ("not set"). The constructor check (`(visibleColumn != -1) == (column != -1)` → throw) is in an explicit
  constructor body that throws `ArgumentException` (Monaco: `Error()`); keep it a positional record by validating in a
  `private` static helper called from the primary constructor's property initializer, or write the record with an explicit
  constructor — either is fine, but the check must run on every construction.
- `internal sealed record IndentGuideHorizontalLine(bool Top, int EndColumn)`.
- `internal enum HorizontalGuidesState { Disabled, EnabledForActive, Enabled }`.
- `internal sealed record BracketGuideOptions(bool IncludeInactive, HorizontalGuidesState HorizontalGuides, bool HighlightActive)`.
- `internal readonly record struct ActiveIndentGuideInfo(int StartLineNumber, int EndLineNumber, int Indent)`.
- `BracketPairGuidesClassNames` is a `static class` (it has no state): `ActiveClassName = "indent-active"`,
  `GetInlineClassName(int nestingLevel, int nestingLevelOfEqualBracketType, bool independentColorPoolPerBracketType)`,
  `GetInlineClassNameOfLevel(int level)` → `$"bracket-indent-guide lvl-{level % 30}"` (invariant culture).

## Tests

- [ ] New: `IndentGuide_BothColumnsSet_Throws`; `IndentGuide_NeitherColumnSet_Throws`; `IndentGuide_OneColumnSet_IsCreated`
- [ ] New: `GetInlineClassNameOfLevel_WrapsAt30`
- [ ] New: `GetInlineClassName_IndependentPool_UsesTheEqualTypeLevel`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: new row `editor/common/textModelGuides.ts`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Computing guides (P6-28 … P6-30).

## Suggested commit message

`feat(core): add the indent and bracket guide types`
