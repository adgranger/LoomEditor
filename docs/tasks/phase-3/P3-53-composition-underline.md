# P3-53 — IME: underline the text being composed

- **Status:** todo
- **Depends on:** P3-52
- **Size:** S

## Goal

While composing, the IME tells which parts of the composed text are being converted (thick underline) and which are
not (thin underline). Draw these underlines like Monaco: a 1-DIP line for "thin", a 2-DIP line for "thick", in the
`editor.compositionBorder` color, and remove them when the composition ends.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What |
|---|---|---|
| `src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts` | 39–43 | `CompositionClassName` (`none`, `secondary`, `primary`) |
| same | 504–536 | `_handleTextFormatUpdate`: edit-context offsets → model range; `thin` → secondary, `thick` → primary, anything else → none |
| `src/vs/editor/browser/controller/editContext/native/nativeEditContext.css` | 31–42 | secondary = `border-bottom: 1px`, primary = `border-bottom: 2px`, color `--vscode-editor-compositionBorder` |
| `src/vs/platform/theme/common/colors/editorColors.ts` | 141–143 | `editor.compositionBorder` (`{ light: '#000000', dark: '#ffffff', hcLight: '#000000', hcDark: '#ffffff' }`) |

## Files to create or change

- `src/LoomEditor.View/Controller/CompositionFormat.cs` — create: `CompositionUnderline { None, Secondary, Primary }`,
  `internal readonly record struct CompositionFormat(Range Range, CompositionUnderline Underline)`
- `src/LoomEditor.View/Controller/EditContextModel.cs` — change: `IReadOnlyList<CompositionFormat> ToCompositionFormats(TextModel model, IReadOnlyList<(int RangeStart, int RangeEnd, string Thickness)> formats)` (port of 504–536)
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `IReadOnlyList<CompositionFormat> CompositionFormats { get; }` + `SetCompositionFormats(...)`
- `src/LoomEditor.Themes/EditorTheme.cs` — change: `CompositionBorder` (dark `#FFFFFF`, light `#000000`)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: draw the underlines after the text
- `src/LoomEditor.WinUI/Input/ImeInput.cs` — change: `FormatUpdating` → formats; clear them on `CompositionCompleted`
- `tests/LoomEditor.View.Tests/Controller/CompositionFormatTests.cs`, `tests/LoomEditor.Rendering.Tests/TextViewRendererCompositionTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- **No model decorations** (ADR 0006: no decorations API in phase 3). The formats are kept on the view model and drawn
  by the renderer. Document this as a behavior difference.
- `FormatUpdating` gives `args.Range` (offsets in the edit-context text) and `args.UnderlineType`
  (`UnderlineType.Thin` → `"thin"`, `Thick` → `"thick"`, others → none). Keep the offset arithmetic of lines 512–516
  (offset of the start of the edit-context text + range start, then `Model.GetPositionAt`).
- The renderer draws, for each visible format that is not `None`, a filled rectangle under the text:
  `y = line top + lineHeight - thickness`, height 1 (secondary) or 2 (primary), from `GetCharacterX(start)` to
  `GetCharacterX(end)`, color `Theme.CompositionBorder`. After every format change, invalidate the canvas.

## Tests

- [ ] New: `ToCompositionFormats` maps offsets on line 2 (edit-context text = lines 2–2) to the right model range
- [ ] New: renderer draws a 2-DIP rectangle for a primary format and a 1-DIP rectangle for a secondary one, nothing for none
- [ ] Manual (Japanese IME): while converting, the segment being converted has a thick underline and the rest a thin one;
      the underlines disappear after Enter

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `nativeEditContext.ts` (+ formats, P3-53); behavior difference "composition underlines
      are view-model state, not model decorations"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Text colors and backgrounds requested by the IME (`TextColor`, `BackgroundColor` of the format), high-contrast borders.

## Suggested commit message

`feat(rendering): underline the IME composition`
