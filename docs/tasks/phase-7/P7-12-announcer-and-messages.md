# P7-12 — `IEditorAnnouncer` and the announcement messages

- **Status:** todo
- **Depends on:** P7-11
- **Size:** S

## Goal

Features can announce a message to assistive technology through the editor context (`context.GetAnnouncer().Announce("…")`), and the texts Monaco announces
(find results, word highlight navigation, tab focus mode) exist as pure, tested functions.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/find/browser/findWidget.ts` | 491–515 | `_getAriaLabel` (the find result message) |
| `src/vs/editor/contrib/find/browser/findController.ts` | 765–773 | the repeated "no results" message of `MatchFindAction` |
| `src/vs/editor/contrib/wordHighlighter/browser/wordHighlighter.ts` | 436–441 | the message of `moveNext` / `moveBack` |
| `src/vs/editor/contrib/toggleTabFocusMode/browser/toggleTabFocusMode.ts` | 38–45 | the two tab focus mode messages |
| `src/vs/base/browser/ui/aria/aria.ts` | — | `alert` / `status`: the concept only (LoomEditor has one channel, `Announce`) |

## Files to create or change

- `src/LoomEditor.View/Accessibility/IEditorAnnouncer.cs` — create (interface + `NullEditorAnnouncer`)
- `src/LoomEditor.View/Accessibility/ContextAnnouncer.cs` — create
- `src/LoomEditor.View/Accessibility/AccessibilityMessages.cs` — create
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal IEditorAnnouncer Announcer { get; set; } = NullEditorAnnouncer.Instance;`
- `src/LoomEditor.View/Features/EditorContextExtensions.cs` — change: `public static IEditorAnnouncer GetAnnouncer(this IEditorContext context)`
- `tests/LoomEditor.View.Tests/Accessibility/TestHelpers/RecordingAnnouncer.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/AccessibilityMessagesTests.cs`, `EditorAnnouncerTests.cs` — create

## Design decisions (already made — do not change)

- ```csharp
  internal interface IEditorAnnouncer { void Announce(string message); }
  internal sealed class NullEditorAnnouncer : IEditorAnnouncer { public static readonly NullEditorAnnouncer Instance = new(); public void Announce(string message) { } }
  ```
  `Announce` ignores null or empty messages in implementations that have a side effect (the control's, P7-19); the null announcer ignores everything. There is no priority, no "status vs alert" distinction (Monaco's `alert` is assertive, `status` polite: LoomEditor announces
  everything as "important, most recent wins" in WinUI, ADR 0010 decision 7).
- `GetAnnouncer` returns `(context as EditorContext)?.Announcer ?? NullEditorAnnouncer.Instance` (it is `internal static`, like the other extensions in that file).
- `ContextAnnouncer`: `internal sealed class ContextAnnouncer(IEditorContext context) : IEditorAnnouncer` with `Announce(string message) => context.GetAnnouncer().Announce(message);`. It reads the context's announcer **at announcement time**,
  never caches it, because the control assigns `EditorContext.Announcer` (P7-19) after the features were attached. Features hold a `ContextAnnouncer`, never the announcer itself.
- `AccessibilityMessages` is a `static class`; English only (no localization infrastructure exists). Its members (all `internal static string`):
  - `const string NoResults = "No results"` (Monaco `NLS_NO_RESULTS`; `FindMatchesLabel.Format` uses the same literal: make `FindMatchesLabel` use this constant, no behavior change);
  - `FindResult(string label, Range? currentMatch, string searchString, TextModel? model)`: port of `_getAriaLabel`. If `label == NoResults`: `searchString == ""` → `"{label} found"`, else `"{label} found for '{searchString}'"`.
    Else if `currentMatch` is not null: `ariaLabel = "{label} found for '{searchString}', at {startLine}:{startColumn}"`; if `model` is not null and the start line is within `[1, model.LineCount]` return `"{lineContent}, {ariaLabel}"`, else `ariaLabel`.
    Else `"{label} found for '{searchString}'"`. Numbers use `CultureInfo.InvariantCulture`;
  - `FindNoResultsAgain(string searchString)` → `"No results found for '{searchString}'"` (findController.ts 771);
  - `WordHighlightPosition(string lineContent, int index, int count, string word)` → `"{lineContent}, {index} of {count} for '{word}'"` (`index` is 1-based: the caller passes `newIndex + 1`);
  - `TabMovesFocusOn` = `"Pressing Tab will now move focus to the next focusable element"`, `TabMovesFocusOff` = `"Pressing Tab will now insert the tab character"` (const strings).
- `RecordingAnnouncer` (test helper): `List<string> Messages`, `Announce` adds.
- `internal static`/`internal sealed` everywhere; XML doc; the `Threading` remark on the classes with state (none here: static or stateless).

## Tests

- [ ] New (`AccessibilityMessagesTests`; each test carries a `// Monaco: '_getAriaLabel'`-style comment naming the upstream function): `FindResult_NoResultsEmptySearch`, `FindResult_NoResultsWithSearch`, `FindResult_WithCurrentMatch_PrefixesTheLine`
      (`"3 of 5"`, match `(2,7)-(2,10)`, search `"foo"`, model line 2 `"a foo b"` → `"a foo b, 3 of 5 found for 'foo', at 2:7"`), `FindResult_WithCurrentMatch_WithoutModel_NoLinePrefix`,
      `FindResult_CurrentMatchOutsideTheModel_NoLinePrefix`, `FindResult_NoCurrentMatch` (`"? of 5"` → `"? of 5 found for 'foo'"`), `FindNoResultsAgain`, `WordHighlightPosition`, `TabMessages_AreMonacosText`
- [ ] New (`EditorAnnouncerTests`): `GetAnnouncer_DefaultContext_IsTheNullAnnouncer` (an `EditorContext` created with the public constructor), `GetAnnouncer_ReturnsTheAssignedAnnouncer`, `NullAnnouncer_DoesNothing` (no exception),
      `GetAnnouncer_NonEditorContext_IsTheNullAnnouncer` (a minimal fake `IEditorContext`; if no fake exists in `TestHelpers`, create one in the test file),
      `ContextAnnouncer_ForwardsToTheCurrentAnnouncer` (create the forwarder, then assign the recorder to `EditorContext.Announcer`, then announce: the recorder gets the message),
      `ContextAnnouncer_FollowsAReplacedAnnouncer`
- [ ] Existing find label tests still pass (`FindMatchesLabel`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: rows for the four Monaco functions
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Calling the announcer from features (P7-13, P7-14, P7-16), the WinUI implementation (P7-19).

## Suggested commit message

`feat(view): add the editor announcer and Monaco's announcement messages`
