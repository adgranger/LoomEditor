# P5-94 — Detect the indentation when the text is set (`detectIndentation`)

- **Status:** todo
- **Depends on:** P5-93
- **Size:** S

## Goal

Setting the text of an editor guesses its indentation (tab size, spaces or tabs), as VS Code does when it opens a
file: the `detectIndentation` editor option (default `true`) is wired to `TextModel.DetectIndentation` of P5-19.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/core/misc/textModelDefaults.ts` | 6–17 | `EDITOR_MODEL_DEFAULTS`: `tabSize` 4, `insertSpaces` true, `detectIndentation` true |
| `common/model/textModel.ts` | 205–215 | `resolveOptions`: with `detectIndentation`, the guessed tab size and `insertSpaces` replace the defaults |
| `common/services/modelService.ts` | 153–156, 254–256 | how the option is read and applied (`model.detectIndentation(insertSpaces, tabSize)` with the defaults) |

## Files to create or change

- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal bool DetectIndentation { get; set; } = true`;
  `SetText` calls `Model.DetectIndentation(insertSpaces: true, tabSize: 4)` after `Model.SetValue(text)` when it is true
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — no change expected (the `Text` property goes through `SetText`); check it
- `samples/LoomEditor.Gallery/MainWindow.xaml.cs` — change: after loading a sample, the **Tab size** box shows the
  model's tab size (the user's change still calls `UpdateOptions`, which wins until the next load)
- `tests/LoomEditor.View.Tests/Features/EditorContextTests.cs` — change or create

## Design decisions (already made — do not change)

- Detection runs on `SetText` only (Monaco: model creation and option changes). Edits never re-detect.
- The defaults passed are Monaco's `EDITOR_MODEL_DEFAULTS` (`insertSpaces: true`, `tabSize: 4`), not the current model
  options, so a previous detection does not leak into the next document.
- No public property in phase 5 (ADR 0009, decision 12).

## Tests

- [ ] New: `SetText_TabIndentedText_UsesTabs` (`InsertSpaces == false`)
- [ ] New: `SetText_TwoSpaceIndentedText_TabSizeTwo`
- [ ] New: `SetText_DetectIndentationOff_KeepsTheOptions`
- [ ] New: `SetText_NoIndentation_KeepsTheDefaults` (tab size 4, spaces)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: row `common/services/modelService.ts` (`detectIndentation` on `SetText` only, P5-94)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

A public `DetectIndentation` property on the control.

## Suggested commit message

`feat(view): detect the indentation when the text is set`
