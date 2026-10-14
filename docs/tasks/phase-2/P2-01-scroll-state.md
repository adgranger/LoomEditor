# P2-01 — `ScrollState` and `ScrollEvent`

- **Status:** done
- **Depends on:** P1-30
- **Size:** S

## Goal

The immutable scroll state of Monaco (`ScrollState`): viewport size, scrollable size, scroll position, clamping, and the
`ScrollEvent` that describes a change. It is the first brick of Monaco's scrolling model (ADR 0005).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/common/scrollable.ts` | 9–13 | `ScrollbarVisibility` |
| same | 15–41 | `ScrollEvent` |
| same | 43–179 | `ScrollState` (constructor clamping, `equals`, `withScrollDimensions`, `withScrollPosition`, `createScrollEvent`) |
| same | 181–208 | `IScrollDimensions`, `INewScrollDimensions`, `IScrollPosition`, `ISmoothScrollPosition`, `INewScrollPosition` |

## Files to create or change

- `src/LoomEditor.View/Layout/ScrollbarVisibility.cs` — create
- `src/LoomEditor.View/Layout/ScrollEvent.cs` — create
- `src/LoomEditor.View/Layout/ScrollState.cs` — create
- `src/LoomEditor.View/Layout/ScrollDimensions.cs` — create: `NewScrollDimensions`, `ScrollPosition`, `NewScrollPosition`
- `tests/LoomEditor.View.Tests/Layout/ScrollStateTests.cs` — create

## Design decisions (already made — do not change)

- Everything is `internal` (ADR 0005, decision 8). Namespace `LoomEditor.View.Layout`.
- Numbers are `double`, as in Monaco. `forceIntegerValues` truncates toward zero, like JavaScript's `| 0`:
  use `(double)(int)value`. Write a comment that this is the C# form of `| 0`.
- `ScrollState` is a `sealed class` with read-only properties (`RawScrollLeft`, `RawScrollTop`, `Width`, `ScrollWidth`,
  `ScrollLeft`, `Height`, `ScrollHeight`, `ScrollTop`) and `Equals(ScrollState)`. Do not make it a record: Monaco's
  `equals` compares the raw positions too, and a record would also compare `_forceIntegerValues`.
- `ScrollEvent` is a `readonly record struct` with the 19 fields of Monaco, in the same order.
- Optional TypeScript fields (`scrollLeft?: number`) become nullable: `readonly record struct NewScrollPosition(double? ScrollLeft = null, double? ScrollTop = null)`,
  and the same for `NewScrollDimensions(double? Width, double? ScrollWidth, double? Height, double? ScrollHeight)`.
- `IScrollDimensions` / `IScrollPosition` / `ISmoothScrollPosition` become read-only interfaces implemented by
  `ScrollState` (Monaco: `implements IScrollDimensions, IScrollPosition`). `ISmoothScrollPosition` is also
  implemented by a small `readonly record struct SmoothScrollPosition(double ScrollLeft, double ScrollTop, double Width, double Height)`.
  P2-02 needs it.

## Tests

Monaco has no test for `ScrollState`. Write new tests:

- [x] New: the constructor clamps `scrollTop` to `scrollHeight - height` and to 0; same for left/width; negative sizes become 0
- [x] New: `forceIntegerValues` truncates `10.7` → `10` and `-10.7` → `-10` (then clamped)
- [x] New: `WithScrollDimensions(…, useRawScrollPositions: true)` restores a raw position that was clamped before
      (e.g. scrollTop 500 requested while scrollHeight was 100, then scrollHeight grows to 1000 → scrollTop 500)
- [x] New: `WithScrollPosition` keeps the raw value of the axis that is not given
- [x] New: `CreateScrollEvent` sets each `*Changed` flag only for the values that changed
- [x] New: `Equals` is false when only a raw position differs

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: add `base/common/scrollable.ts` → `LoomEditor.View/Layout/` (in progress)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`Scrollable` and smooth scrolling (P2-02); any UI.

## Suggested commit message

`feat(view): port ScrollState and ScrollEvent`
