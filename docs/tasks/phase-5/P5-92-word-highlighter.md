# P5-92 — Word highlighter: textual occurrences and their decorations

- **Status:** done
- **Depends on:** P5-91
- **Size:** M

## Goal

When the cursor rests on a word (or a selection inside one word), every occurrence of that word in the model is
highlighted with Monaco's `wordHighlightText` decoration, and the highlights go away when the cursor leaves the word,
when the text changes, or when the editor loses the focus. Only the **textual** provider exists (the semantic providers
of language features are phase 8). Navigation and the feature registration come in P5-93.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/languages.ts` | 1357–1385 | `DocumentHighlightKind` (`Text = 0`, `Read = 1`, `Write = 2`), `DocumentHighlight` |
| `contrib/wordHighlighter/browser/textualHighlightProvider.ts` | 16–49 | `TextualDocumentHighlightProvider.provideDocumentHighlights` |
| `contrib/wordHighlighter/browser/highlightDecorations.ts` | 11–16, 23–90 | the colors, `_WRITE_OPTIONS`, `_TEXT_OPTIONS`, `_REGULAR_OPTIONS`, `getHighlightDecorationOptions` |
| `contrib/wordHighlighter/browser/highlightDecorations.css` | 14–40 | `wordHighlight`, `wordHighlightStrong`, `wordHighlightText`: background + 1 px border colors |
| `contrib/wordHighlighter/browser/wordHighlighter.ts` | 122–176 | `OccurenceAtPositionRequest` (word range, `isValid`) |
| same | 234–433 | `WordHighlighter`: constructor listeners, `hasDecorations`, `restore`, `trigger`, `stop`, `_getSortedHighlights` |
| same | 466–611 | `_removeSingleDecorations`, `_removeAllDecorations`, `_stopSingular`, `_stopAll`, `_onPositionChanged`, `_getWord` |
| same | 672–862 | `_run`, `computeWithModel` (single model), `_beginRenderDecorations`, `renderDecorations`, `dispose` |
| `common/config/editorOptions.ts` | 6553–6573 | `occurrencesHighlight` (`singleFile`), `occurrencesHighlightDelay` (0) |

## Files to create or change

- `src/LoomEditor.View/Features/WordHighlighter/DocumentHighlight.cs` — create (`DocumentHighlightKind`, `DocumentHighlight(Range Range, DocumentHighlightKind? Kind)`)
- `src/LoomEditor.View/Features/WordHighlighter/TextualDocumentHighlightProvider.cs` — create
- `src/LoomEditor.View/Features/WordHighlighter/HighlightDecorations.cs` — create
- `src/LoomEditor.View/Features/WordHighlighter/WordHighlighter.cs` — create
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — change: rows `wordHighlight`, `wordHighlightStrong`, `wordHighlightText`
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: `editor.wordHighlightBackground` (`#575757B8`, `#57575740`, null, null),
  `editor.wordHighlightStrongBackground` (`#004972B8`, `#0e639c40`, null, null), `editor.wordHighlightTextBackground`
  (= `editor.wordHighlightBackground`), `editor.wordHighlightBorder` (null, null, `contrastActiveBorder`, `contrastActiveBorder`),
  `editor.wordHighlightStrongBorder` (same), `editor.wordHighlightTextBorder` (= `editor.wordHighlightBorder`)
- `src/LoomEditor.View/Events/OutgoingViewModelEvent.cs`, `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change:
  `FocusChangedEvent` (outgoing, raised by `SetHasFocus` as Monaco `viewModelImpl.ts` 246–251) and `bool HasFocus`
- `tests/LoomEditor.View.Tests/Features/WordHighlighter/WordHighlighterTests.cs`, `TextualDocumentHighlightProviderTests.cs` — create

## Design decisions (already made — do not change)

- **Single model, no registry.** `WordHighlighter(ViewModel viewModel, IEditorScheduler scheduler, EditorWordHighlightOptions options)`
  calls `TextualDocumentHighlightProvider.ProvideDocumentHighlights(model, position)` directly (Monaco reaches it
  through `LanguageFeatureRegistry`, phase 8). The static `query` / `storedDecorationIDs` (shared between editors, keyed
  by URI) become instance fields; `multiFile`, notebooks, `getOtherModelsToHighlight`, `occurrencesHighlightFromAllProviders`,
  `textModelService` are not ported (written in the class remarks).
- The provider uses step B / F APIs: `model.GetWordAtPosition(position)` and
  `model.FindMatches(word, searchOnlyEditableRange: true, isRegex: false, matchCase: true, WordHelper.UsualWordSeparators, captureMatches: false)`;
  every match is `DocumentHighlightKind.Text`.
- The request is **synchronous** (no worker promise, no cancellation): `workerRequestTokenId` / `workerRequestCompleted`
  keep their meaning for stale results but a request completes inside `_run`.
- **Timers** (ADR 0009, decision 10, with Monaco's real values): `runDelayer` is a `Delayer` of **50 ms** (cursor
  changes and focus); `_beginRenderDecorations(delay)` renders at once when `delay` is 0 (`occurrencesHighlightDelay`,
  default 0) and otherwise through `scheduler.Schedule(delay)`; `restore(250)` is what `editor.action.wordHighlight.trigger`
  uses (P5-93). `lastCursorPositionChangeTime` is not needed: the request completes in the same call.
- Cursor events: the outgoing `CursorStateChangedEvent` (`Source`, `Reason`); `_onPositionChanged` stops when
  `source != "api" && reason != CursorChangeReason.Explicit` (typing).
- Focus: `FocusChangedEvent` gained → run if no request; lost → `_stopAll()` (Monaco `onDidBlurEditorWidget` with no
  focused editor).
- Decorations: an `EditorDecorationsCollection` (step A) plus the delta ids (`storedDecorationIDs`) as Monaco does.
- `DecorationStyles`: background = the color id, border = 1 DIP of the border color id (`box-sizing: border-box`);
  the dotted high-contrast border is drawn solid (behavior difference). `overviewRuler` / `minimap` options are not
  ported (phase 6).
- `EditorWordHighlightOptions` (internal record, ADR 0009 decision 12): `OccurrencesHighlight = SingleFile`
  (`Off`, `SingleFile`; `MultiFile` behaves as `SingleFile`), `OccurrencesHighlightDelay = 0`.

## Tests

`wordHighlighter.test.ts` has 18 tests: **none is ported**. Suite `DocumentHighlightProviders` (15) tests the merging
of several providers through `LanguageFeatureRegistry` and `getOccurrencesAtPosition` (phase 8); suite
`WordHighlighterContribution` (3) needs provider registries, `occurrencesHighlightFromAllProviders`, `setModel` and
the instantiation service. Their textual expectations are reused in new tests:

- [x] New: `TextualProvider_WordWordWord_ReturnsFourTextHighlights` (model `"word word word\nword"`, position (1, 2):
      (1,1–5), (1,6–10), (1,11–15), (2,1–5), kind `Text`; Monaco's `'uses textual fallback only when no other provider matches'`)
- [x] New: `TextualProvider_NotOnAWord_ReturnsEmpty`
- [x] New: `CursorOnWord_After50Ms_HighlightsOccurrences` (`ManualEditorScheduler`: nothing at 49 ms)
- [x] New: `Typing_RemovesHighlights`; `CursorLeavesWord_RemovesHighlights`; `MultiLineSelection_RemovesHighlights`
- [x] New: `CursorMovesToAnotherOccurrence_KeepsHighlights` (`isValid` / "contained in a stored decoration" path)
- [x] New: `FocusLost_RemovesHighlights`
- [x] New: `OccurrencesHighlightOff_NeverHighlights`
- [x] New: `Restore250_RendersAfter250Ms`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: rows `contrib/wordHighlighter/browser/wordHighlighter.ts` (🔶 single model, textual only, P5-92),
      `textualHighlightProvider.ts`, `highlightDecorations.ts` (+ `.css`); "Tests ported": `wordHighlighter.test.ts`
      0 / 18 with the reason above
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The feature class, the commands and their keys (P5-93). `selectionHighlight` (the find-based selection highlighter
belongs to the multi-cursor work of step F).

## Suggested commit message

`feat(word-highlight): highlight textual occurrences of the word at the cursor`
