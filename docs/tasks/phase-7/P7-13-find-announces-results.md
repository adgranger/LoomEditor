# P7-13 — Find announces its results

- **Status:** done
- **Depends on:** P7-12
- **Size:** S

## Goal

When the find widget is open and the matches count, the current match or the search string change, a screen reader hears "3 of 12 found for 'foo', at 20:5" (with the line text);
an explicit F3 with no result repeats "No results found for 'foo'".

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/find/browser/findWidget.ts` | 426–431, 457–486 | `_onStateChanged` → `_updateMatchesCount`: the `alertFn(this._getAriaLabel(...))` call when `searchString`, `matchesCount` or `matchesPosition` changed |
| `src/vs/editor/contrib/find/browser/findController.ts` | 730, 765–773 | `MatchFindAction`: re-announce "no results" on an explicit navigation when the widget was visible |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindController.cs` — change: `internal IEditorAnnouncer Announcer { get; set; } = NullEditorAnnouncer.Instance;` and the subscription below
- `src/LoomEditor.View/Features/Find/FindFeature.cs` — change: `controller.Announcer = new ContextAnnouncer(context);` after the controller is created (`ContextAnnouncer` is from P7-12: it reads the context's announcer at announcement time)
- `src/LoomEditor.View/Features/Find/FindActions.cs` — change: `MatchFindAction` (lines 46–82)
- `tests/LoomEditor.View.Tests/Features/Find/FindAnnouncementTests.cs` — create (follow `FindControllerTests.cs` for the setup)
- `src/LoomEditor.WinUI/Controls/Find/FindWidget.Replace.cs` — **no change** (the widget keeps showing the label; it must not announce)

## Design decisions (already made — do not change)

- In `FindController` (constructor or `Start`, wherever `State.Changed` is already subscribed: `OnStateChanged`, line 63): when `e.SearchString || e.MatchesCount || e.MatchesPosition` **and** `State.IsRevealed`,
  call `Announcer.Announce(AccessibilityMessages.FindResult(FindMatchesLabel.Format(State.MatchesPosition, State.MatchesCount), State.CurrentMatch, State.SearchString, ViewModel.Model))`.
  `IsRevealed` is required because the Monaco widget only exists once opened and the match info also updates while the widget is closed (decorations, background search); announcing then would be noise.
- Identical consecutive messages are **not** suppressed (Monaco does not either).
- `FindFeature` sets `controller.Announcer = new ContextAnnouncer(context)` (never the announcer value itself: the control assigns `EditorContext.Announcer` after the features were attached).
- `MatchFindAction`: when `!RunMatch()` and, after `controller.Start(...)` and the second `RunMatch()`, the match still failed, and `wasFindWidgetVisible && controller.State.MatchesCount == 0 && controller.State.SearchString.Length > 0`:
  `controller.Announcer.Announce(AccessibilityMessages.FindNoResultsAgain(controller.State.SearchString))`. (Monaco 765–773: only when the first `runMatch` fails, then `start`, then no match.)
- No change to `FindMatchesLabel` output; no new public API.

## Tests

- [x] New: `Announces_WhenTheMatchesCountChanges` (reveal the find with a search string over `"foo bar foo"`: the recorder's last message is `"foo bar foo, 1 of 2 found for 'foo', at 1:1"` — adapt the position/label to what the state gives; assert by building the expected string with `AccessibilityMessages` from the same state)
- [x] New: `Announces_WhenTheCurrentMatchMoves` (`MoveToNextMatch` → the message mentions the new position)
- [x] New: `Announces_NoResults_ForASearchWithoutMatch` (`"No results found for 'zzz'"`)
- [x] New: `Announces_NoResultsFound_ForAnEmptySearchString` (widget revealed with an empty search string: the message is `"No results found"`; if the state never announces for an empty string because `SearchString` did not change, set it to `"x"` then to `""` in the test)
- [x] New: `DoesNotAnnounce_WhenTheWidgetIsNotRevealed` (change the search string with `IsRevealed` false → no message)
- [x] New: `NextMatchWithoutResult_RepeatsNoResults` (widget revealed, search `"zzz"`, `FindActions.NextMatch` → the last message is `FindNoResultsAgain("zzz")`)
- [x] New: `UsesTheAnnouncerAssignedLater` (the feature attached first, `EditorContext.Announcer` assigned afterwards → messages reach the recorder)
- [x] Existing find tests still pass

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Replace announcements ("Replaced N occurrences": Monaco has none in the widget), the find widget's own accessibility names (XAML `AutomationProperties.Name` are checked in P7-25), the WinUI announcer (P7-19).

## Suggested commit message

`feat(find): announce the find results to screen readers`
