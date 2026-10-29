# P11-08 — Threads in an editor: the `Widget` zone kind and `CommentThreadManager`

- **Status:** todo
- **Depends on:** P11-01
- **Size:** M

## Goal

A `CommentThreadManager` owns the threads of one model: it keeps one tracked decoration per thread so the thread follows its line, one `ViewZone` of the new kind `Widget` after that line (the gap the WinUI element will cover), and the zone height. No UI.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 3 and 4). Concepts kept from Monaco: view zones, tracked ranges. Read first: `src/LoomEditor.View/Layout/ViewZone.cs`, `ViewZones.cs`, `IViewZoneChangeAccessor.cs`, `src/LoomEditor.Rendering/TextViewRenderer.ViewZones.cs` (what the renderer does per `ViewZoneKind`), `src/LoomEditor.View/DiffEditor/InlineViewZones.cs` (a user of zones), `src/LoomEditor.Core/Model/TextModel.Decorations.cs`, `TextModel.DecorationsEmitter.cs`, `src/LoomEditor.View/Features/EditorDecorationsCollection.cs`, and the View tests that build a `ViewModel` with zones (`tests/LoomEditor.View.Tests/Layout/ViewZones*Tests.cs`: copy how they create the objects).

## Files to create or change

- `src/LoomEditor.View/Layout/ViewZone.cs` — change: `ViewZoneKind.Widget` ("a gap covered by a host element; the renderer paints nothing")
- `src/LoomEditor.Rendering/TextViewRenderer.ViewZones.cs` — change only if a `switch` / `if` chain would draw something for an unknown kind (it must draw **nothing** for `Widget`)
- `src/LoomEditor.View/Comments/CommentThreadManager.cs` — create
- `tests/LoomEditor.View.Tests/Comments/CommentThreadManagerTests.cs` — create
- `tests/LoomEditor.Rendering.Tests/TextViewRendererWidgetZoneTests.cs` — create (recording surface: a `Widget` zone draws nothing and still pushes the text down)

## Design decisions (already made — do not change)

- `internal sealed class CommentThreadManager : IDisposable`, ctor `(TextModel model, ViewZones zones)`.
- `CommentThread Add(int lineNumber, string? title = null)`: `ArgumentOutOfRangeException` when the line is not in `1..model.LineCount`; creates `new CommentThread(Guid.NewGuid().ToString("N"), lineNumber, title)` (the internal constructor of P11-01), one decoration, one zone; raises `ThreadsChanged`.
- `bool Remove(CommentThread thread)`: removes the decoration and the zone; `false` when unknown; raises `ThreadsChanged`.
- `IReadOnlyList<CommentThread> Threads` in creation order. `CommentThread? Find(string id)`.
- `string? GetZoneId(CommentThread thread)`; `int GetHeight(CommentThread thread)`; `void SetHeight(CommentThread thread, int heightPx)` (clamped to `>= 0`; no `LayoutZone` call when the height is already that value; otherwise sets `ViewZone.HeightInPx` and calls `ViewZones.Change(a => a.LayoutZone(id))`).
- **Decoration:** the whole line (`line, 1` → `line, maxColumn`), `TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges`, no visible style (no class name, no glyph: it is a pure anchor). Read how `EditorDecorationsCollection` / the model's change accessor create decorations and use the smallest one that works.
- **Following the line:** on `TextModel.DecorationsChanged` the manager re-reads every thread's decoration range; the thread's `LineNumber` becomes `range.StartLineNumber` clamped to `1..LineCount`; when it changed, `thread.LineNumber` is set (raises `LineChanged`), the zone's `AfterLineNumber` is set and `LayoutZone` called once for all moved zones (one `ViewZones.Change`), and `ThreadMoved` (`EventHandler<CommentThread>`) is raised. Guard against re-entrancy (the manager's own decoration changes must not loop).
- **Line text changes:** on `TextModel.ContentChanged`, for each thread whose line **content** differs from the last seen one raise `ThreadLineTextChanged` (`EventHandler<CommentThread>`); the manager caches the line content per thread (used by the suggestion UI to refresh, P11-15).
- **The zone:** `new ViewZone { AfterLineNumber = line, HeightInPx = DefaultHeight (a `const int` of 120), Kind = ViewZoneKind.Widget, SuppressMouseDown = true, ShowInHiddenAreas = false, Ordinal = 20000 + creation counter, Data = thread }` — the `Data` field carries the thread so the host can find the element for a zone.
- When the model is **flushed** (`ModelContentChangedEventArgs.IsFlush`: `SetValue`) the threads stay and clamp to the new line count (never deleted by an edit).
- `Dispose()` removes all decorations and zones and unsubscribes. UI thread only.
- A deleted line: the decoration collapses; the thread stays at the line that took its place; test it.

## Tests

- [ ] Manager (real `TextModel` + `ViewZones`): `Add_CreatesWidgetZoneAfterLine`, `Add_LineOutOfRange_Throws`, `Add_SameLineTwice_TwoZonesInOrder`, `InsertLineAbove_ThreadMovesDown`, `DeleteLineAbove_ThreadMovesUp`, `TypeAtEndOfLine_ThreadStaysOnLine`, `DeleteThreadLine_ThreadStaysAtReplacementLine`, `ReplaceWholeDocument_ClampsToLineCount`, `Moved_UpdatesZoneAfterLineNumber`, `Moved_RaisesThreadMovedOnce`, `SetHeight_UpdatesZoneAndLayout`, `SetHeight_Same_NoLayoutCall`, `SetHeight_Negative_ClampsToZero`, `Remove_RemovesZoneAndDecoration`, `Remove_Unknown_False`, `LineTextChanged_RaisedWhenContentOfThreadLineChanges`, `LineTextChanged_NotRaisedForOtherLines`, `Dispose_RemovesEverything`, `Threads_KeepCreationOrder`
- [ ] Rendering: `WidgetZone_DrawsNothing`, `WidgetZone_ReservesHeight` (the next line is drawn lower by the zone height)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The control API and events (P11-09), the WinUI element and its positioning (P11-10), the diff editor (P11-18), unit-testing against a real WinUI control.

## Suggested commit message

`feat(view): anchor comment threads to a line with decorations and widget view zones`
