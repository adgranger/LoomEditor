# P7-14 — Word highlight navigation announces the occurrence

- **Status:** todo
- **Depends on:** P7-13
- **Size:** S

## Goal

F7 / Shift+F7 (next / previous occurrence of the word under the cursor) announces "line text, 2 of 5 for 'word'", like Monaco.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/wordHighlighter/browser/wordHighlighter.ts` | 426–464 | `moveNext` / `moveBack`: the `alert(...)` after the cursor moved |
| `src/vs/editor/contrib/wordHighlighter/browser/wordHighlighter.ts` | 597–620 | `_getWord`: the word at the (new) selection start |

## Files to create or change

- `src/LoomEditor.View/Features/WordHighlighter/WordHighlighter.cs` — change: constructor parameter `IEditorAnnouncer? announcer = null` (last, optional) stored in a field (`NullEditorAnnouncer.Instance` when null); `MoveBy` (lines ~135–163) announces
- `src/LoomEditor.View/Features/WordHighlighter/WordHighlighterFeature.cs` — change: pass `new ContextAnnouncer(context)` (from P7-12) as the new constructor argument (line 58)
- `tests/LoomEditor.View.Tests/Features/WordHighlighter/WordHighlighterAnnouncementTests.cs` — create (follow `WordHighlighterFeatureTests.cs` for the setup)

## Design decisions (already made — do not change)

- In `MoveBy`, inside the `try` after the selection is set and the reveal requested (Monaco order): `Selection selection = _viewModel.GetSelection()`; `word = _viewModel.Model.GetWordAtPosition(new Position(selection.StartLineNumber, selection.StartColumn))`
  (as line 287 of the same file does); if `word is { } w`: `_announcer.Announce(AccessibilityMessages.WordHighlightPosition(_viewModel.Model.GetLineContent(dest.StartLineNumber), newIndex + 1, highlights.Count, w.Word))`.
  If there is no word at the position nothing is announced (Monaco: `if (word)`).
- Remove the sentence "the screen reader alert is not ported" from the comment above `MoveBy`.
- No behavior change when the announcer is the null one.

## Tests

- [ ] New: `MoveNext_AnnouncesLineAndPosition` (text `"foo bar\nfoo baz"`, cursor on the first `foo`, highlights computed, `MoveNext` → the cursor is on line 2 and the message is `"foo baz, 2 of 2 for 'foo'"`; use the scheduler the existing tests use to run the highlight work)
- [ ] New: `MoveBack_Wraps_AnnouncesTheLastOccurrence` (from the first of 3 occurrences → `"…, 3 of 3 for 'foo'"`)
- [ ] New: `MoveNext_NoWordAtThePosition_AnnouncesNothing` (if the highlights exist but the new position has no word: only if constructible; otherwise omit and say so in the commit body)
- [ ] New: `MoveNext_WithoutAnnouncer_Works` (the default constructor path)
- [ ] New: `Feature_AnnouncesThroughTheContextAnnouncer` (attach `WordHighlighterFeature`, then assign the recorder to `EditorContext.Announcer`, then `MoveNext` → the recorder gets the message)
- [ ] Existing word highlighter and find announcement tests still pass

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Other announcements Monaco makes in features phase 5 does not have (rename, code actions, go to error), the WinUI announcer (P7-19).

## Suggested commit message

`feat(word-highlight): announce the occurrence when navigating`
