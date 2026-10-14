# P2-13 — Line layout cache and very long lines

- **Status:** done
- **Depends on:** P2-12
- **Size:** S

## Goal

Creating a DirectWrite layout for every visible line on every frame is too slow for fluid scrolling. Layouts are kept
in an LRU cache keyed by the line's text. Very long lines (minified files, 1 MB on one line) are cut at
`stopRenderingLineAfter` = 10,000 characters, and a "Show more (…)" label is drawn after them, as in Monaco.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | 6783–6786 | `stopRenderingLineAfter` default (10000) |
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 474–490 | how `resolveRenderLineInput` computes `len` and `overflowingCharCount` |
| same | 1244–1248 | the overflow label `Show more ({0})` |
| same | 1259–1266 | `renderOverflowingCharCount` (`chars` / `KB` / `MB`) |

## Files to create or change

- `src/LoomEditor.Rendering/LineLayoutCache.cs` — create
- `src/LoomEditor.Rendering/OverflowLabel.cs` — create (`static string Format(int overflowingCharCount)`, ported from `renderOverflowingCharCount` + `showMore`)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — use the cache; get the line text with a length limit (see below)
- `tests/LoomEditor.Rendering.Tests/LineLayoutCacheTests.cs`, `OverflowLabelTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class LineLayoutCache : IDisposable`, constructor `(ITextMeasurer measurer, int capacity)`.
  `ILineLayout Get(string text)` returns the cached layout or creates one; evicted layouts are disposed.
  `Clear()` disposes everything (font or DPI change, P2-20). Key = the text, compared with `StringComparer.Ordinal`.
  LRU = `Dictionary<string, LinkedListNode<…>>` + `LinkedList<…>`.
- Capacity: `Math.Max(256, 4 × visible line count)`. The renderer grows the cache when the viewport gets taller
  (`EnsureCapacity(int)`), and never shrinks it.
- Getting the text: if `model.GetLineLength(line) > StopRenderingLineAfter`, read only the first 10,000 characters
  with `GetValueInRange(new Range(line, 1, line, 10_001))`. Never call `GetLineContent` on such a line during a frame
  (a 100 MB single-line file would allocate 200 MB per frame).
- Cutting at 10,000 can split a surrogate pair. Keep Monaco's behavior (it cuts at a UTF-16 index too). Write a test
  that shows it does not crash.
- The overflow label is drawn right after the cut text, with `Theme.LineNumberForeground`. It is not clickable in
  phase 2 (mouse handling is phase 3). The label text is English, inline, as in the rest of the code base.
- `StopRenderingLineAfter` is a constant (`10000`) in `TextViewRenderer` for now, named and commented after Monaco's option.

## Tests

- [x] New: `Get` twice with the same text creates one layout; a capacity of 2 and 3 texts disposes the least recently used one
- [x] New: `Clear` disposes every layout
- [x] New: `OverflowLabel.Format(500) == "Show more (500 chars)"`, `Format(2048) == "Show more (2.0 KB)"`, `Format(3 * 1024 * 1024) == "Show more (3.0 MB)"`
- [x] New: rendering a 25,000-character line with `RecordingRenderSurface` + `FakeTextMeasurer` draws a 10,000-character layout and the label for 15,000 chars (`"Show more (14.6 KB)"`)
- [x] New: drawing the same 40 lines twice creates 40 layouts, not 80

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `viewLineRenderer.ts` → only `stopRenderingLineAfter` and the overflow label (the HTML renderer is not ported, ADR 0005)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Scrolling (P2-14), clicking "Show more" (phase 3 or later), whitespace rendering (phase 6).

## Suggested commit message

`perf(rendering): cache line layouts and stop rendering very long lines`
