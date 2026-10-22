# P6-04 — `LineInjectedText`: applying injected text and fixed-width ranges

- **Status:** todo
- **Depends on:** P6-03
- **Size:** S

## Goal

Core can turn the decorations that carry injected text into an ordered list of `LineInjectedText` for a line, apply them to
the line text, and list the fixed-width ranges of the resulting text.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/textModelEvents.ts`.

| Lines | What to port |
|---|---|
| 236–256 | `FixedWidthInjectedTextRange`, `occupiesHorizontalSpace` |
| 258–346 | `class LineInjectedText`: `applyInjectedText`, `fromDecorations`, `getFixedWidthInjectedTextRanges`, the constructor, `withText` |

## Files to create or change

- `src/LoomEditor.Core/Model/Events/FixedWidthInjectedTextRange.cs` — create (`readonly record struct`, header `textModelEvents.ts`)
- `src/LoomEditor.Core/Model/Events/LineInjectedText.cs` — create
- `tests/LoomEditor.Core.Tests/Model/Events/LineInjectedTextTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class LineInjectedText(int ownerId, int lineNumber, int column, ModelDecorationInjectedTextOptions options, int order)`
  with get-only properties `OwnerId`, `LineNumber`, `Column`, `Options`, `Order`. `WithText(string)` returns a copy whose options have
  that `Content` (add `ModelDecorationInjectedTextOptions.WithContent(string)` to P6-03's class: one line, `with`-style copy).
  Monaco's spread `{ ...this.options, content: text }`.
- `static string ApplyInjectedText(string lineText, IReadOnlyList<LineInjectedText>? injectedTexts)`.
- `static List<LineInjectedText> FromDecorations(IEnumerable<ModelDecoration> decorations)` uses
  `ModelDecoration.OwnerId`/`.Range`/`.Options.Before`/`.Options.After`. Sort with a stable `List.Sort`-compatible
  comparison **by (line, column, order)** (that triple is total, so stability does not matter).
- `static List<FixedWidthInjectedTextRange> GetFixedWidthInjectedTextRanges(IReadOnlyList<LineInjectedText>? injectedTexts)`:
  `WidthInEm` is `double?`; a range is added when it is not null (Monaco: `!== undefined`).
- `OccupiesHorizontalSpace(options)`: `Content.Length > 0 || (WidthInEm is > 0)`.
- The record `FixedWidthInjectedTextRange(int StartOffset, int EndOffset, double WidthInEm)`.
- `Strings` of C# are UTF-16 like JavaScript's: offsets and `Substring` behave identically.
- Not ported: the `"internal"` annotation (everything is internal anyway).

## Tests

No upstream unit test calls these directly (they run through `modelInjectedText.test.ts`, P6-06, and the projection tests).
All new:

- [ ] `ApplyInjectedText_NullOrEmpty_ReturnsTheLine`
- [ ] `ApplyInjectedText_TwoInjections_InsertsAtColumns` (`"abcdef"`, injections at column 3 and 5)
- [ ] `ApplyInjectedText_TwoInjectionsAtTheSameColumn_KeepsTheOrder`
- [ ] `FromDecorations_BeforeAndAfter_UseStartAndEndOfTheRange` (a range 1,2 → 1,4 with both: columns 2 and 4, orders 0 and 1)
- [ ] `FromDecorations_EmptyContentWithoutWidth_IsIgnored`; `…_EmptyContentWithWidth_IsKept`
- [ ] `FromDecorations_IsSortedByLineColumnThenOrder`
- [ ] `GetFixedWidthInjectedTextRanges_ComputesOffsetsInTheInjectedLine` (two injections, one with `WidthInEm`; the second
      range starts after the first one's content)
- [ ] `GetFixedWidthInjectedTextRanges_WidthOnly_IsAnEmptyRange`
- [ ] `WithText_ReplacesTheContentOnly`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map row of `textModelEvents.ts` mentions `LineInjectedText`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Querying the model (P6-05), the changed-event (P6-06).

## Suggested commit message

`feat(core): add LineInjectedText`
