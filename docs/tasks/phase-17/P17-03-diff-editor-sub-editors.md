# P17-03 — Share the sub-editor configuration of `LoomDiffEditor`

- **Status:** todo
- **Depends on:** P17-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

`CreateSubEditorFeatures` and `ConfigureEditors` (what the diff editor sets on its two editors) exist once.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules). The other shared `LoomDiffEditor.*.cs` files in
`src/LoomEditor.HostShared/Controls/DiffEditor/` (header block, namespace `…Controls`).

## Duplicated block (SonarCloud, PR #26, 2026-11-03)

| Avalonia | WinUI | Members |
|---|---|---|
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.Options.cs` 207+28 | `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.Options.cs` 254+28 | `CreateSubEditorFeatures` (WinUI 254–266), `ConfigureEditors` (WinUI 268–290) |

The only difference: WinUI sets `editor.OverviewRulerEnabled = false;` (a WinUI-only member of `LoomEditor`, see P17-02).

## Files to create or change

- `src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.SubEditors.cs` — new: `_subEditorFeatures` (moved from both `Options.cs`),
  `CreateSubEditorFeatures`, `ConfigureEditors` (WinUI text), and the declaration `partial void ConfigureHostEditor(LoomEditor editor);`.
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.Options.cs` — the two methods and the field go; add the implementation
  `partial void ConfigureHostEditor(LoomEditor editor) => editor.OverviewRulerEnabled = false;` (with a one-line comment: the diff editor draws its own
  overview ruler).
- `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.Options.cs` — the two methods and the field go; no `ConfigureHostEditor` body.
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `LoomDiffEditor.Options.cs` pair from `KnownDuplicates`.

## Design decisions (already made — do not change)

- In the shared `ConfigureEditors`, `ConfigureHostEditor(editor);` takes the place of the `OverviewRulerEnabled` line (after `MinimapEnabled`): same
  order of assignments as today in WinUI.
- A `partial void` method, not an empty host member, because only WinUI has something to do (phase 17 README, rule 2).
- Usings of the new file (unconditional): `LoomEditor.View.Features`, and the namespace of `FoldingFeature` (as in the WinUI `Options.cs`).

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged**.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] The pair is gone from `KnownDuplicates`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The dependency/styled property registrations of `Options.cs` (framework-specific).

## Suggested commit message

`refactor(hosts): share the diff editor's sub-editor configuration`
