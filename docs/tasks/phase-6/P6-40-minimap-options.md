# P6-40 — Minimap option and layout types

- **Status:** done
- **Depends on:** P6-02
- **Size:** S

## Goal

The minimap options (Monaco's `editor.minimap.*`) and the minimap part of the layout exist as types, with Monaco's defaults
and validation. Nothing computes or draws them yet.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/config/editorOptions.ts`.

| Lines | What to port |
|---|---|
| 1096–1107 | `ComputeOptionsMemory` (`stableMinimapLayoutInput`, `stableFitMaxMinimapScale`, `stableFitRemainingWidth`) |
| 2495–2499 | `enum RenderMinimap` (`None`, `Text`, `Blocks`) |
| 2588–2600 | `EditorMinimapLayoutInfo` |
| 2648–2662 | `IMinimapLayoutInput` |
| 3405–3482 | `IEditorMinimapOptions` (doc comments of every member) |
| 3484–3623 | `EditorMinimap`: defaults (`enabled: true`, `size: 'proportional'`, `side: 'right'`, `showSlider: 'mouseover'`, `autohide: 'none'`, `renderCharacters: true`, `maxColumn: 120`, `scale: 1`, …) and `validate` (clamps: `maxColumn` 1–10000, `scale` 1–3, …) |

## Files to create or change

- `src/LoomEditor.View/Layout/EditorMinimapOptions.cs` — create (header `editorOptions.ts`): the options record and its enums
  (`MinimapSide { Right, Left }`, `MinimapSize { Proportional, Fill, Fit }`, `MinimapShowSlider { MouseOver, Always }`,
  `MinimapAutohide { None, MouseOver, Scroll }`)
- `src/LoomEditor.View/Layout/EditorMinimapLayoutInfo.cs` — create: `RenderMinimap`, `EditorMinimapLayoutInfo`, `MinimapLayoutInput`,
  `ComputeOptionsMemory`
- `src/LoomEditor.View/Layout/EditorLayoutOptions.cs` — change: `public EditorMinimapOptions Minimap { get; init; } = new() { Enabled = false };`
  (see decisions)
- `tests/LoomEditor.View.Tests/Layout/EditorMinimapOptionsTests.cs` — create

## Design decisions (already made — do not change)

- `EditorMinimapOptions` is an `internal sealed record` with Monaco's defaults **except** that `EditorLayoutOptions` creates it
  with `Enabled = false` until the minimap is drawn (P6-52); P6-54 turns the default back to Monaco's `true` and says so. The
  record's own default stays `Enabled = true` (Monaco's).
- Section header options (`showRegionSectionHeaders`, `showMarkSectionHeaders`, `markSectionHeaderRegex`, `sectionHeaderFontSize`,
  `sectionHeaderLetterSpacing`) are **not** ported: section headers come from the `sectionHeaders` contribution (not in scope).
  List them in the XML remarks.
- `Validated()` returns a copy clamped like Monaco's `validate` (copy every `clamp` / `EditorIntOption.clampedInt` call with its
  bounds from the upstream lines).
- `EditorMinimapLayoutInfo` is an `internal sealed record` with the 11 members of Monaco; `MinimapLayoutInput` a record with the
  13 members of `IMinimapLayoutInput` (the `minimap` member is `EditorMinimapOptions`). `ComputeOptionsMemory` is a small mutable
  `internal sealed class` (Monaco mutates it across layouts).

## Tests

- [x] New: `Defaults_MatchMonaco`
- [x] New: `Validated_ClampsMaxColumnAndScale`
- [x] New: `EditorLayoutOptions_MinimapDisabledUntilDrawn` (documents the temporary default)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`_computeMinimapLayout` (P6-41), drawing (P6-52).

## Suggested commit message

`feat(view): add the minimap options`
