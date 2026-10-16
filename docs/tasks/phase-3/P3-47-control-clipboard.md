# P3-47 — Clipboard: copy, cut and paste with the Windows clipboard

- **Status:** done
- **Depends on:** P3-46
- **Size:** S

## Goal

Ctrl+C / Ctrl+X / Ctrl+V (and Ctrl+Insert / Shift+Delete / Shift+Insert) work with the Windows clipboard, with
Monaco's multi-cursor behavior: the text and metadata of P3-36 on copy and cut, and the metadata given back to
`Paste` (P3-35) when the clipboard still holds what the editor copied (ADR 0006, decision 10).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/clipboard/browser/clipboard.ts` | 41–150 | the three actions: ids `editor.action.clipboardCutAction`, `editor.action.clipboardCopyAction`, `editor.action.clipboardPasteAction` (keys already in P3-39) |
| same | 211–262 | `registerExecCommandImpl` for cut/copy: "no selection and `emptySelectionClipboard` off → do nothing" (lines 225–230); copy writes the clipboard; cut copies then runs `Handler.Cut` |
| same | 279–340 | paste: the **web** path (lines ~313–335): read the text, look up `InMemoryClipboardMetadataManager`, compute `pasteOnNewLine = emptySelectionClipboard && metadata.isFromEmptySelection`, then `Handler.Paste` |

## Files to create or change

- `src/LoomEditor.View/Controller/IClipboardService.cs` — create: `void WriteText(string text)`, `Task<string?> ReadTextAsync()`
- `src/LoomEditor.View/Controller/ClipboardCommands.cs` — create: `Copy`, `Cut`, `PasteAsync` and `Register(CommandRegistry, ViewModel, IClipboardService)`
- `src/LoomEditor.WinUI/Input/WindowsClipboardService.cs` — create: `Windows.ApplicationModel.DataTransfer.Clipboard`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: register the clipboard commands after creating the `EditorContext`
- `tests/LoomEditor.View.Tests/Controller/ClipboardCommandsTests.cs` — create (with a fake `IClipboardService`)

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `ClipboardCommands.Copy(ViewModel vm, IClipboardService clipboard)`: if every selection is empty and
  `!CursorConfig.EmptySelectionClipboard` → return; otherwise `ClipboardUtils.GenerateDataToCopyAndStoreInMemory(vm)`
  (P3-36, it stores the metadata) and `clipboard.WriteText(data.Text)`.
- `Cut`: the same copy, then `vm.Cut("keyboard")` (P3-36). Read-only: `Cut` copies but does not delete (the cursor
  refuses the edit and raises `ReadOnlyEditAttemptEvent`).
- `PasteAsync`: `text = await clipboard.ReadTextAsync()`; if `null` or empty → return; metadata =
  `InMemoryClipboardMetadataManager.Instance.Get(text)`; then `vm.Paste(text, pasteOnNewLine, metadata?.MulticursorText, "keyboard", metadata?.IsBlock ?? false)`.
  The `EditorCommand` registered for paste is synchronous: it starts `PasteAsync` and returns `true` (fire-and-forget
  on the UI thread; exceptions are written with `Debug.WriteLine` and swallowed — a failed clipboard read must not crash
  the app; say so in a comment).
- `WindowsClipboardService`: `WriteText` → a `DataPackage` with `SetText(text)` and `Clipboard.SetContent(package)`;
  `ReadTextAsync` → `Clipboard.GetContent()`, `Contains(StandardDataFormats.Text)`, `await GetTextAsync()`. Catch the
  `COMException` thrown when another process holds the clipboard and return `null` / do nothing.
- No custom clipboard format and no JSON (Native AOT): metadata survive only inside this process.

## Tests

The clipboard metadata suite of Monaco (`columnSelectionPaste.test.ts`, suite `clipboard metadata`) uses browser
events and is not ported (P3-35). New tests with a fake clipboard:

- [x] New: copy two cursors' selections → the fake holds `"a\r\nc"`; paste with two cursors spreads `a` and `c`
- [x] New: copy an empty selection → the whole line with CRLF; paste on another line inserts it above that line
- [x] New: copy, then the clipboard is changed by "another app" (the fake returns other text) → paste ignores the metadata
- [x] New: cut an empty selection removes the line; with `EmptySelectionClipboard = false` nothing happens
- [x] New: `Commands.Execute("editor.action.clipboardPasteAction")` returns `true` and the text appears after the task completes
- [ ] Manual (Gallery, AOT build): Ctrl+C / Ctrl+V between the editor and Notepad in both directions; Ctrl+X on an empty _(left to the maintainer)_
      line; multi-cursor copy/paste; column selection copy then paste as a block

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, manual checks pass in the AOT app _(publish OK; manual checks left to the maintainer)_
- [x] `docs/PORTING.md`: map row `contrib/clipboard/browser/clipboard.ts` (🔶 cut/copy/paste, P3-47); behavior difference
      "clipboard metadata is kept in memory only (no custom format)"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Rich (HTML) copy, paste-as / paste providers (`CopyPasteController`), drag-and-drop.

## Suggested commit message

`feat(winui): copy, cut and paste with the Windows clipboard`
