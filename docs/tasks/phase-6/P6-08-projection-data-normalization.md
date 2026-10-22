# P6-08 — `ModelLineProjectionData`: normalizing positions around injected text

- **Status:** todo
- **Depends on:** P6-07
- **Size:** S

## Goal

`ModelLineProjectionData` normalizes an output position so the cursor never stops inside injected text (honoring
`InjectedTextCursorStops`) or at the seam of a wrapped line, and returns the injected text at an output position.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/modelLineProjectionData.ts`.

| Lines | What to port |
|---|---|
| 169–192 | `normalizeOutputPosition` |
| 194–200 | `outputPositionToOffsetInInputWithInjections` |
| 202–254 | `normalizeOffsetInInputWithInjectionsAroundInjections` (all affinities) |
| 256–265 | `getInjectedText` |
| 267–297 | `getInjectedTextAtOffset` |
| 300–307 | `hasRightCursorStop`, `hasLeftCursorStop` |
| 309–311 | `class InjectedText` |

## Files to create or change

- `src/LoomEditor.View/Layout/ModelLineProjectionData.Injections.cs` — create (second partial; same header)
- `src/LoomEditor.View/Layout/InjectedText.cs` — create: `internal sealed record InjectedText(ModelDecorationInjectedTextOptions Options)`
- `tests/LoomEditor.View.Tests/Layout/ModelLineProjectionDataTests.cs` — change (add tests)

## Design decisions (already made — do not change)

- `getInjectedTextAtOffset` returns `(int InjectedTextIndex, int OffsetInInputWithInjections, int Length)?` (a nullable
  value tuple) instead of an object or `undefined`.
- `assertNever(affinity)` → `throw new ArgumentOutOfRangeException(nameof(affinity))` after the last branch.
- The cursor-stop helpers are `private static`; a `null` cursor stop means `Both` (Monaco: `null || undefined` → `true`).
- `GetInjectedText(int outputLineIndex, int outputOffset)` returns `InjectedText?`.

## Tests

- [ ] Port: `Editor ViewModel - LineBreakData` › `Injected Text 1` › `normalization`
      (`src/vs/editor/test/common/viewModel/lineBreakData.test.ts` 151–187, into `LineBreakDataTests.cs`)

New (upstream also covers this through `viewModelImpl.test.ts` › `normalizePosition with multiple touching injected text`, P6-22):

- [ ] `NormalizeOutputPosition_NoInjections_LeftAffinityAtWrapSeam_GoesToThePreviousLineEnd`
- [ ] `NormalizeOutputPosition_NoInjections_RightAffinityAtLineEnd_GoesToTheNextLineStart`
- [ ] `NormalizeOutputPosition_InsideInjectedText_NoneAffinity_GoesLeft` (cursor stops `Both`)
- [ ] `NormalizeOutputPosition_AtInjectedTextEnd_WithRightStop_StaysRight`
- [ ] `NormalizeOutputPosition_TouchingInjections_RightAffinity_SkipsAll` / `…LeftAffinity_SkipsAll`
- [ ] `NormalizeOutputPosition_CursorStopsNone_NoneAffinity_WalksBackOverTouchingInjections`
- [ ] `GetInjectedText_OnInjectedText_ReturnsItsOptions`; `GetInjectedText_OnModelText_ReturnsNull`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map row of `modelLineProjectionData.ts` updated (all of the class); `lineBreakData.test.ts` 8 / 8
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`ILineBreaksComputer*` interfaces (P6-09).

## Suggested commit message

`feat(view): normalize projected positions around injected text`
