# P6-14 — `InjectedTextInlineDecorationsComputer` and `ViewLineData.InlineDecorations`

- **Status:** done
- **Depends on:** P6-08
- **Size:** S

## Goal

The inline decorations that style injected text (its `InlineClassName`) are computed per output (view) line of a model
line, including width-only injections, and `ViewLineData` can carry them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/inlineDecorations.ts` | 12–25 | `InlineDecorationType.WidthOnly = 4` (the other values exist) |
| same | 194–215 | `IInjectedTextInlineDecorationsComputerContext` |
| same | 217–290 | `InjectedTextInlineDecorationsComputer` |
| `src/vs/editor/common/viewModel.ts` | find `class ViewLineData` (search `export class ViewLineData`) | the `inlineDecorations` member and its doc |
| `src/vs/editor/test/common/viewModel/inlineDecorations.test.ts` | 266–642 | suite `InjectedTextInlineDecorationsComputer` (21 tests) |

## Files to create or change

- `src/LoomEditor.View/Layout/InlineDecorations.cs` — change: add `WidthOnly = 4` to `InlineDecorationType` (with Monaco's
  doc comment) and update the class remark (the injected text computer is now ported)
- `src/LoomEditor.View/Layout/InjectedTextInlineDecorationsComputer.cs` — create (header `inlineDecorations.ts`)
- `src/LoomEditor.View/Layout/ViewLineData.cs` — change: add `IReadOnlyList<InlineDecoration>? InlineDecorations = null` as the
  last positional parameter, and drop the "no `inlineDecorations` member" behavior difference from the remark
- `tests/LoomEditor.View.Tests/Layout/InjectedTextInlineDecorationsComputerTests.cs` — create

## Design decisions (already made — do not change)

- The context interface becomes `internal interface IInjectedTextInlineDecorationsComputerContext` with the five methods
  (`GetInjectionOptions`, `GetInjectionOffsets`, `GetBreakOffsets`, `GetWrappedTextIndentLength`, `GetBaseViewLineNumber`),
  returning `IReadOnlyList<ModelDecorationInjectedTextOptions>?`, `IReadOnlyList<int>?`, `IReadOnlyList<int>`, `int`, `int`.
  The tests implement it with a small private record class `TestContext(...)` (in the test file) taking the five values.
- `GetInlineDecorations(int modelLineNumber)` returns `IReadOnlyList<IReadOnlyList<InlineDecoration>>` (an empty list when there
  are no injections, like Monaco's `[]`).
- `options.widthInEm !== undefined` → `options.WidthInEm is not null`.
- Monaco's `InlineDecoration` is a class compared with `deepStrictEqual`; LoomEditor's is a record: `Assert.Equal` on the
  nested lists works element-wise when you compare `ToArray()`s (or write an `AssertDecorations` helper in the test file).
- The renderer is **not** changed here (P6-25 decides how `WidthOnly` is handled). The renderer's checks on
  `InlineDecorationType` (`TextViewRenderer.Decorations.cs`, `TextViewRenderer.HitTest.cs`) already ignore unknown values: verify
  it and add nothing.

## Tests

- [x] Port: `InjectedTextInlineDecorationsComputer` › all 21 tests (lines 270–642): `no injections returns empty`,
      `single injection with inlineClassName on a single output line`, `injection without inlineClassName produces no inline decorations`,
      `fixed width injection uses its regular inline decoration`, `injection with inlineClassNameAffectsLetterSpacing`,
      `multiple injections on a single output line`, `injection spanning across wrapped lines`,
      `injection with wrappedTextIndentLength on wrapped lines`, `injection starting in later wrapped line`,
      `base view line number offsets correctly`, `range uses view line number, not model line number`,
      `range uses view line number on wrapped lines, not model line number`, and the 9 `spacing-only injection …` tests

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: the `inlineDecorations.ts` map row includes the injected text computer; the
      `inlineDecorations.test.ts` row counts 32 / 33 (only `affectsFont` left)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Using the computer from the projection (P6-16), drawing (P6-25).

## Suggested commit message

`feat(view): compute the inline decorations of injected text`
