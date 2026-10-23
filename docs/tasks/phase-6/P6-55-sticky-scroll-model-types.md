# P6-55 — Sticky scroll options and model types

- **Status:** done
- **Depends on:** P5-96
- **Size:** S

## Goal

The sticky scroll options and the data types of its model (ranges, elements, candidates, widget state) exist, with
Monaco's defaults and validation. Nothing computes them yet.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | 3171–3195 | `IEditorStickyScrollOptions` (doc comments) |
| same | 3197–3240 | `EditorStickyScroll`: defaults (`enabled: true`, `maxLineCount: 5`, `defaultModel: 'outlineModel'`, `scrollWithEditor: true`) and `validate` (`maxLineCount` clamped 1–20) |
| `src/vs/editor/contrib/stickyScroll/browser/stickyScrollElement.ts` | 8–41 | `StickyRange`, `StickyElement`, `StickyModel` |
| `src/vs/editor/contrib/stickyScroll/browser/stickyScrollProvider.ts` | 20–27 | `StickyLineCandidate` |
| `src/vs/editor/contrib/stickyScroll/browser/stickyScrollWidget.ts` | 25–44 | `StickyScrollWidgetState` (`equals`, `Empty`) |

## Files to create or change

- `src/LoomEditor.View/Features/StickyScroll/EditorStickyScrollOptions.cs` — create (header `editorOptions.ts`)
- `src/LoomEditor.View/Features/StickyScroll/StickyScrollElement.cs` — create (header `stickyScrollElement.ts`)
- `src/LoomEditor.View/Features/StickyScroll/StickyLineCandidate.cs` — create (header `stickyScrollProvider.ts`)
- `src/LoomEditor.View/Features/StickyScroll/StickyScrollWidgetState.cs` — create (header `stickyScrollWidget.ts`)
- `src/LoomEditor.View/Features/StickyScroll/README.md` — create (one paragraph, like the other feature folders' READMEs)
- `tests/LoomEditor.View.Tests/Features/StickyScroll/StickyScrollTypesTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.View.Features.StickyScroll`; all types `internal`.
- `EditorStickyScrollOptions` record: `Enabled = true`, `MaxLineCount = 5`, `DefaultModel = StickyScrollModel.OutlineModel`
  (`enum StickyScrollModel { OutlineModel, FoldingProviderModel, IndentationModel }`), `ScrollWithEditor = true`, `Validated()` clamps.
  The outline and folding-provider models need language features (phase 8): P6-56 falls back to the indentation model (as Monaco does
  when no provider exists).
- `StickyRange(int StartLineNumber, int EndLineNumber)` record; `StickyElement` a class with `StickyRange? Range`,
  `List<StickyElement> Children`, `StickyElement? Parent`; `StickyModel(int Version, StickyElement? Element)` — Monaco's `uri` and
  `outlineProviderId` are dropped (one model per editor, no outline).
- `StickyLineCandidate(int StartLineNumber, int EndLineNumber, int Top, int Height)` record.
- `StickyScrollWidgetState(IReadOnlyList<int> StartLineNumbers, IReadOnlyList<int> EndLineNumbers, int LastLineRelativePosition, int? ShowEndForLine = null)`
  with `Equals` comparing the lists element-wise (write it: a record compares list references) and `static Empty`.

## Tests

- [x] New: `Options_Defaults_MatchMonaco`; `Options_Validated_ClampsMaxLineCount`
- [x] New: `WidgetState_Equals_ComparesContent`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: rows for `stickyScrollElement.ts` and the parts above
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Computing the model (P6-56), the feature (P6-58).

## Suggested commit message

`feat(view): add the sticky scroll types`
