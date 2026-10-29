# P12-06 — Highlight the lines of a multi-line thread

- **Status:** todo
- **Depends on:** P12-05
- **Size:** S

**Recommended model:** Sonnet

## Goal

The lines of an open multi-line thread get a whole-line background (GitHub's yellow tint), drawn by the existing decoration renderer. Resolving the thread, or a range that
collapses to one line, removes it; reopening brings it back.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decision 7). Read first: `src/LoomEditor.View/Comments/CommentThreadManager.cs` (`AnchorOptions`,
`Add`, `SyncLines`, `Release`), `src/LoomEditor.View/Features/Find/FindDecorations.cs` lines 61–68 (`RangeHighlightDecoration`: a whole-line class decoration),
`src/LoomEditor.View/Layout/DecorationStyles.cs` (the `rangeHighlight` row), `src/LoomEditor.Themes/EditorColorDefaults.cs` (the `loom.selectionAnchor` row: how a LoomEditor-own
color id is declared), `src/LoomEditor.Core/Decorations/IModelDecorationsChangeAccessor.cs` (`ChangeDecorationOptions`), `tests/LoomEditor.View.Tests/Layout/DecorationStylesTests.cs`.

## Files to create or change

- `src/LoomEditor.View/Comments/CommentThreadManager.cs` — change
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — change: one row
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: one row
- `tests/LoomEditor.View.Tests/Comments/CommentThreadManagerTests.cs` — change
- `tests/LoomEditor.View.Tests/Layout/DecorationStylesTests.cs` — change (one test)

## Design decisions (already made — do not change)

- New `static readonly ModelDecorationOptions RangeAnchorOptions = ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "comment-thread-range",
  Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, ClassName = "comment-thread-range", IsWholeLine = true })`.
- **One decoration per thread** (the anchor). `private static ModelDecorationOptions OptionsFor(CommentThread t) => t.StartLineNumber < t.LineNumber && t.State == CommentThreadState.Open ? RangeAnchorOptions : AnchorOptions;`
  `Add` uses `OptionsFor(thread)` for the new decoration. `Entry` remembers the options it last applied (`Entry.Options`).
- **Re-evaluate** with a private `UpdateOptions(IEnumerable<Entry>)` that calls `accessor.ChangeDecorationOptions(id, options)` (inside `_model.ChangeDecorations(_ownerId, …)`,
  with `_busy = true`) only for entries whose `OptionsFor` differs from `Entry.Options`:
  - after `SyncLines` moved entries (a range may have collapsed to one line, or grown from one);
  - on `CommentThread.Changed` with `StateChanged`: the manager subscribes to `thread.Changed` in `Add` and unsubscribes in `Release` (store the handler in `Entry`).
- `DecorationStyles` row, next to `rangeHighlight`, with the comment `// LoomEditor's own class (phase 12): the lines of a multi-line comment thread`:
  `["comment-thread-range"] = new("loom.commentThreadRangeBackground", "editor.rangeHighlightBorder"),`
- `EditorColorDefaults` row, next to `loom.selectionAnchor`, with the comment `// LoomEditor's own id (phase 12): GitHub's tint of the commented lines.`:
  `["loom.commentThreadRangeBackground"] = ("#BB800933", "#D4A72C33", null, null),`
- Switching options must **not** change the tracked range (the tests check `GetDecorationRange` before and after).

## Tests

- [ ] `Highlight_MultiLineOpen_UsesRangeClass` (read the options back with the internal `TextModel.GetDecorationOptions(id)`; the id comes from a new manager method `string? GetDecorationId(CommentThread thread)`, like `GetZoneId`)
- [ ] `Highlight_SingleLine_NoClass`, `Highlight_Resolved_RemovedThenReopened_Restored`
- [ ] `Highlight_RangeCollapsesToOneLine_Removed`, `Highlight_SingleLineGrowsToRange_Added` (`Add(3, 3)`, then insert `"x\n"` at `(3, 2)`: the range becomes 3–4 and is highlighted)
- [ ] `Highlight_SwitchOptions_KeepsTrackedRange`
- [ ] `Remove_UnsubscribesFromThreadChanged` (resolving a removed thread changes nothing and does not throw)
- [ ] `DecorationStyles`: `Get_CommentThreadRange_HasBackgroundAndBorderIds`
- [ ] All existing tests unchanged and green

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

A gutter bar, an overview-ruler mark, a highlight that depends on focus or hover, a second decoration, theme JSON changes.

## Suggested commit message

`feat(view): highlight the lines of a multi-line comment thread`
