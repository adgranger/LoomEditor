# P7-02 — `TextModel.IsDominatedByLongLines`

- **Status:** done
- **Depends on:** P7-01
- **Size:** S

## Goal

The text model can tell whether most of its characters are in very long lines (Monaco's `isDominatedByLongLines`), which P7-03 uses to
force word wrap in screen reader mode.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/textModel.ts` | 126 | `LONG_LINE_BOUNDARY = 10000` |
| `src/vs/editor/common/model/textModel.ts` | 649–669 | `isDominatedByLongLines` |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.LongLines.cs` — create (`internal bool IsDominatedByLongLines()` in the `partial class TextModel`; header comment names the Monaco file)
- `tests/LoomEditor.Core.Tests/Model/TextModelLongLinesTests.cs` — create

## Design decisions (already made — do not change)

- Same algorithm as Monaco: returns `false` when `IsTooLargeForTokenization` (Monaco's `isTooLargeForTokenization()`); otherwise sums, over all lines,
  `_buffer.GetLineLength(line)` into `longLineCharCount` (line length `>= 10000`) or `smallLineCharCount`, and returns `longLineCharCount > smallLineCharCount`.
- The member is `internal` (`LoomEditor.Core.csproj` already has `InternalsVisibleTo` for View, WinUI and the test projects; do not add a public member, so `PublicAPI` does not change). The boundary is a `private const int LongLineBoundary = 10000;`.
- It throws `ObjectDisposedException` like the other `TextModel` members if the model has a disposed check (look at `GetLineContent`); if `TextModel` has no disposed
  state, no check.
- Not cached (Monaco does not cache it; P7-04 calls it once per `Text` change).

## Tests

- [x] New: `IsDominatedByLongLines_EmptyModel_ReturnsFalse`
- [x] New: `IsDominatedByLongLines_ShortLinesOnly_ReturnsFalse`
- [x] New: `IsDominatedByLongLines_OneLongLineAndLittleElse_ReturnsTrue` (a line of 10 000 `a` and five lines of `"short"`)
- [x] New: `IsDominatedByLongLines_LineOfExactlyTheBoundary_CountsAsLong` (10 000 characters → long; 9 999 → not long, with no other text)
- [x] New: `IsDominatedByLongLines_LongAndShortCharactersEqual_ReturnsFalse` (strict `>`: a 10 000-character line plus ten lines of 1 000 characters)
- [x] New: `IsDominatedByLongLines_ManyShortCharsOutweighOneLongLine_ReturnsFalse` (a 10 000-character line plus twenty lines of 1 000 characters)
- [x] New: `IsDominatedByLongLines_TooLargeForTokenization_ReturnsFalse` (only if a model can be made "too large" cheaply in tests: look at how `IsTooLargeForTokenization` is
      tested; if that needs a huge text, skip this test and say so in the commit body)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Using it in the layout (P7-03), recomputing it when the text changes (P7-04).

## Suggested commit message

`feat(core): add TextModel.IsDominatedByLongLines`
