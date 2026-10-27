# P9-02 — Sequence contracts: `ISequence`, `SequenceDiff`, `OffsetPair`, timeouts, `LineSequence`

- **Status:** todo
- **Depends on:** P9-01
- **Size:** M

## Goal

The vocabulary shared by the diff algorithms exists: the sequence interface, the diff of two sequences, the timeout abstraction, the result type, and the line sequence used for the first (line) pass.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts` | 13–35 | `IDiffAlgorithm`, `DiffAlgorithmResult` (`diffs`, `hitTimeout`, `trivial`, `trivialTimedOut`) |
| same | 36–126 | `SequenceDiff` (`invert`, `fromOffsetPairs`, `assertSorted`, `delta`, `deltaStart`, `join`, `intersect`, `getStarts`, `getEndExclusives`, `swap`, `toString`) |
| same | 127–171 | `OffsetPair` (`Zero`, `Max`, `delta`, `equals`, `toString`), `ISequence` |
| same | 172–208 | `ITimeout`, `InfiniteTimeout`, `DateTimeout` |
| `src/vs/editor/common/diff/defaultLinesDiffComputer/lineSequence.ts` | 10–45 | `LineSequence` (the `getIndentation` helper included) |
| `src/vs/editor/common/core/ranges/offsetRange.ts` | 16–229 | any `OffsetRange` member that the code above needs and the existing C# type lacks (compare with `src/LoomEditor.Core/Text/OffsetRange.cs`; likely `Join`, `Intersect`, `Intersects`, `Contains`, `Slice`, `Clip`, `ClipCyclic`, `Equals`, `DeltaStart`, `Delta`, `ToString`) |

## Files to create or change

- `src/LoomEditor.Core/Diff/Algorithms/IDiffAlgorithm.cs`, `DiffAlgorithmResult.cs`, `SequenceDiff.cs`, `OffsetPair.cs`, `ISequence.cs`, `ITimeout.cs` (with `InfiniteTimeout`, `DateTimeout`) — create
- `src/LoomEditor.Core/Diff/LineSequence.cs` — create
- `src/LoomEditor.Core/Text/OffsetRange.cs` — change (missing members only)
- `tests/LoomEditor.Core.Tests/Diff/SequenceDiffTests.cs`, `LineSequenceTests.cs`, `TimeoutTests.cs`, `tests/LoomEditor.Core.Tests/Text/OffsetRangeTests.cs` (if it exists, extend it) — create

## Design decisions (already made — do not change)

- `ISequence` members: `int GetElement(int offset)`, `int Length { get; }`, `bool IsStronglyEqual(int offset1, int offset2)` and `int GetBoundaryScore(int length)`; the last one is optional in Monaco (`getBoundaryScore?`): make it a **default interface method** returning `0`, which the callers use as Monaco does when it is absent (read where the heuristics test for it, P9-05).
  Elements are `int` (Monaco: numbers) — `LineSequence` holds the trimmed-line hashes, `LinesSliceCharSequence` (P9-04) the char codes.
- `ITimeout`: `bool IsValid()`. `InfiniteTimeout.Instance`; `DateTimeout(int timeoutMs)` uses `Environment.TickCount64` (no `TimeProvider`: Monaco uses `Date.now()`), and Monaco's `valid` latch (once expired it stays invalid, even if the clock goes back) is kept.
  Tests use a `FakeTimeout : ITimeout` (in `tests/.../Diff/TestHelpers/FakeTimeout.cs`) that turns invalid after N calls.
- `SequenceDiff` is a `sealed class` with `OffsetRange Seq1Range`, `OffsetRange Seq2Range` and the same static helpers as Monaco. `SequenceDiff.AssertSorted` uses `Guard.Assert`.
- `OffsetPair` is a `sealed class` (reference equality is **not** used anywhere; implement `Equals`).
- `DiffAlgorithmResult` is a `sealed record(SequenceDiff[] Diffs, bool HitTimeout)` with the static factories `Trivial(seq1, seq2)` and `TrivialTimedOut(seq1, seq2)` (lines 17–23); Monaco has no other members.
- `LineSequence` constructor: `LineSequence(int[] trimmedHash, string[] lines)`; `GetIndentation` counts leading spaces (1) and tabs (1) like Monaco (read lines 39–45; do not use `char.IsWhiteSpace`).
- `<remarks>Threading: immutable / stateless; DateTimeout is safe to call from the thread that runs the diff.</remarks>`.

## Tests

- [ ] New (comments name the Monaco member): `SequenceDiff_Join_Intersect_Delta_Swap`, `SequenceDiff_Invert_*`, `SequenceDiff_FromOffsetPairs_*`, `SequenceDiff_AssertSorted_Unsorted_Asserts` (Debug only), `OffsetPair_Delta_Equals`, `InfiniteTimeout_AlwaysValid`, `DateTimeout_ExpiresAndStaysInvalid` (use a very small timeout and a spin on `IsValid()` bounded at 5 s), `LineSequence_BoundaryScore_*` (indentation before and after), `LineSequence_IsStronglyEqual_ComparesUntrimmedLines`, and one test per added `OffsetRange` member.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated: `diffAlgorithm.ts`, `lineSequence.ts`, `offsetRange.ts` (members added)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The algorithms (P9-03), `LinesSliceCharSequence` (P9-04).

## Suggested commit message

`feat(core): port the diff sequence contracts and LineSequence`
