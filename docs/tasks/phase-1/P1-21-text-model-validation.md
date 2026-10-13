# P1-21 — `TextModel`: position and range validation

- **Status:** todo
- **Depends on:** P1-20
- **Size:** S

## Goal

Turn any position or range (out of bounds, column past the end of the line, in the middle of a surrogate pair)
into the nearest valid one. Every public `TextModel` method that receives a position or range uses this.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/textModel.ts` | 925–1161 | `_validateRangeRelaxedNoAllocations`, `_isValidPosition`, `_validatePosition`, `validatePosition`, `isValidRange`, `_isValidRange`, `validateRange` |
| same | 1162–1167 | `modifyPosition` |
| same | search `StringOffsetValidationType` | the enum (`Relaxed`, `SurrogatePairs`) |
| `src/vs/editor/test/common/model/textModel.test.ts` | 779–998 | from suite `Editor Model - TextModel`: `validatePosition`, `validatePosition around high-low surrogate pairs 1` and `2`, `validateRange around high-low surrogate pairs 1` and `2`, `modifyPosition` |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.Validation.cs` — create
- `src/LoomEditor.Core/Model/TextModel.cs` — make the read methods of P1-20 validate their inputs where Monaco does
- `tests/LoomEditor.Core.Tests/Model/TextModelValidationTests.cs` — create

## Design decisions (already made — do not change)

- Our `Position`/`Range` cannot hold values below 1, but Monaco's tests pass `new Position(0, 0)`. So the public API is:
  - `Position ValidatePosition(Position position)` and `Position ValidatePosition(int lineNumber, int column)`
  - `Range ValidateRange(Range range)` and `Range ValidateRange(int startLineNumber, int startColumn, int endLineNumber, int endColumn)`
  - `bool IsValidRange(Range range)`, `Position ModifyPosition(Position position, int offset)`
  Ported tests that use 0 or negative values call the `int` overloads.
- Monaco tests `validatePosition handle NaN.`, `issue #71480: validatePosition handle floats`,
  `issue #71480: validateRange handle floats` do **not** apply (C# `int`): do not port them; list them in the class
  XML doc as "not applicable".

## Tests

- [ ] `validatePosition`
- [ ] `validatePosition around high-low surrogate pairs 1`, `… 2`
- [ ] `validateRange around high-low surrogate pairs 1`, `… 2`
- [ ] `modifyPosition`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Edits (P1-22).

## Suggested commit message

`feat(core): add position and range validation to TextModel`
