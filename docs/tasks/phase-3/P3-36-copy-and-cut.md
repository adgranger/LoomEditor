# P3-36 — Copy and cut (text to copy, clipboard metadata, `cut`)

- **Status:** todo
- **Depends on:** P3-35
- **Size:** M

## Goal

Compute what Ctrl+C / Ctrl+X put on the clipboard, as Monaco does: the selected text of every cursor (joined with the
EOL, or kept as one text per cursor), the **whole line** when the selection is empty (`emptySelectionClipboard`), and
the metadata that makes the next paste smart (`isFromEmptySelection`, `multicursorText`, `isBlock`). Port the cut
operation. The Windows clipboard itself comes in P3-47: this card is pure logic.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 1005–1056 | `getPlainTextToCopy` |
| same | 1228–1230 | `cut` |
| `src/vs/editor/browser/controller/editContext/clipboardUtils.ts` | 16–45 | `generateDataToCopyAndStoreInMemory`, `storeMetadataInMemory` (without the Firefox branch), `generateDataToCopy` |
| same | 47–79 | `getDataToCopy` — **without** the rich text (HTML) part |
| same | 82–122 | `InMemoryClipboardMetadataManager`, `ClipboardDataToCopy`, `ClipboardStoredMetadata` |
| `src/vs/editor/common/cursor/cursorDeleteOperations.ts` | 235–298 | `DeleteOperations.cut` |
| `src/vs/editor/common/cursor/cursor.ts` | 617–622 | `CursorsController.cut` |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 1930, 1968 | `bug #16740: [editor] Cut line doesn't quite cut the last line`, `issue #128602: When cutting multiple lines (ctrl x), the last line will not be erased` |

## Files to create or change

- `src/LoomEditor.View/Controller/ClipboardData.cs` — create: `ClipboardDataToCopy`, `ClipboardStoredMetadata`,
  `InMemoryClipboardMetadataManager`, `ClipboardUtils.GenerateDataToCopyAndStoreInMemory(ViewModel viewModel)`
- `src/LoomEditor.View/Controller/README.md` — create (what the folder is: the pure logic of Monaco's `browser/controller`)
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `GetPlainTextToCopy`, `Cut`
- `src/LoomEditor.View/Cursors/DeleteOperations.cs` — change: `Cut`
- `src/LoomEditor.View/Cursors/CursorsController.Type.cs` — change: `Cut`
- `tests/LoomEditor.View.Tests/Controller/ClipboardDataTests.cs`, `tests/LoomEditor.View.Tests/Cursors/CutTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `GetPlainTextToCopy(IReadOnlyList<Range> modelRanges, bool emptySelectionClipboard, bool forceCrlf)` returns
  `(IReadOnlyList<Range> SourceRanges, string? SourceText, IReadOnlyList<string>? SourceTexts)`: Monaco returns
  `string | string[]`; exactly one of `SourceText` / `SourceTexts` is non-null. On Windows Monaco passes
  `forceCRLF = isWindows = true`: `ClipboardUtils` always passes `true` (ADR 0006, decision 10).
- `DeleteOperations.cut` sorts the selections **in place** with an inconsistent comparator
  (`Position.compare(a.getStartPosition(), b.getEndPosition())`, line ~238). Do **not** use `List.Sort` (it may throw
  with an inconsistent comparer): copy the list and sort it with a stable **insertion sort** using exactly that
  comparison (conventions §4.3). Write a comment.
- `cut` with an empty selection and `emptySelectionClipboard` deletes the whole line, with the special cases of the
  last line (#16740, #128602): port line by line. It returns an `EditOperationResult` with undo stops before and after.
- `InMemoryClipboardMetadataManager` is a process-wide singleton in Monaco (`INSTANCE`), so that copying in one editor
  and pasting in another keeps the metadata. Keep it as `internal static InMemoryClipboardMetadataManager Instance`
  (UI thread only; say so). `Get(pastedText)` clears the state when the text does not match (port that).
- `ClipboardStoredMetadata`: `int Version` (1), `string? Id` (Monaco: `generateUuid()` → `Guid.NewGuid().ToString()`),
  `bool IsFromEmptySelection`, `bool IsBlock`, `IReadOnlyList<string>? MulticursorText`, `string? Mode` (always `null`
  in phase 3: no rich copy).
- The rich text part (`copyWithSyntaxHighlighting`, `getRichTextToCopy`, HTML) is out of phase 3 (ADR 0006,
  decision 12): write a cut comment.

## Tests

- [ ] Port `cursor.test.ts` › `bug #16740: [editor] Cut line doesn't quite cut the last line` (1930) and
      › `issue #128602: When cutting multiple lines (ctrl x), the last line will not be erased` (1968)
- [ ] New: two cursors on `"ab\ncd"` selecting `a` and `c` → `SourceTexts == ["a", "c"]`, text `"a\r\nc"` (forced CRLF),
      metadata `MulticursorText == ["a", "c"]`
- [ ] New: an empty selection on line 2 of `"a\nbb\nc"` → text `"bb\r\n"`, `IsFromEmptySelection == true`
- [ ] New: with `EmptySelectionClipboard = false`, an empty selection copies nothing
- [ ] New: after a column selection (`ColumnSelect`, P3-28), `IsBlock == true`
- [ ] New: `InMemoryClipboardMetadataManager.Get` returns the metadata for the same text and `null` (and forgets) for another text
- [ ] New: copy an empty selection, then `Paste(text, pasteOnNewLine: true)` (P3-35) inserts the line above the cursor

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map rows `clipboardUtils.ts` (🔶 metadata and text, P3-36), `cursorDeleteOperations.ts` (+ cut);
      behavior difference "no rich (HTML) copy"; tests table `cursor.test.ts` (+2)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The Windows clipboard, the Ctrl+C/X/V commands and keys (P3-47).

## Suggested commit message

`feat(view): port copy text computation, clipboard metadata and cut`
