# P9-10 — Monaco's diff fixture tests

- **Status:** todo
- **Depends on:** P9-09
- **Size:** M
- **Recommended model:** Opus (a mismatch against a fixture means a port bug somewhere in P9-02 … P9-09: finding it needs reading the two algorithms side by side. On failure — a fixture still differs after you fixed what you could prove wrong — stop, do **not** edit the expected files, do not commit, and report the fixture names, a unified diff of expected vs actual and what you checked.)

## Goal

The whole diff stack is proven against Monaco's own expected results: every fixture folder (`1.txt`, `2.txt`, `advanced.expected.diff.json`) of the upstream tests gives exactly the same line changes, inner character changes and moves. Bugs found are fixed in the card that owns the code.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/test/node/diffing/fixtures.test.ts` | 20–111 | the runner (`runTest`, `getDiffs`, `formatRange`, the `ignoreTrimWhitespace = folder contains 'trimws'` rule, `computeMoves: true`, `maxComputationTimeMs` huge) |
| same | 112–158 | `assertDiffCorrectness`, `rangeMappingsToTextEdit` (applying all inner changes to the original must give the modified text; only when `!ignoreTrimWhitespace`) |
| `src/vs/editor/test/node/diffing/fixtures/<folder>/` | each folder: `1.*`, `2.*`, `advanced.expected.diff.json` | the data. Folders at the pinned commit: list them with `gh api "repos/microsoft/vscode/contents/src/vs/editor/test/node/diffing/fixtures?ref=0c9c0a4295a9222e12a605537738719455ae3056" --jq '.[].name'` (about 40: `bracket-aligning`, `class-replacement`, `deletion`, `difficult-move`, `equals`, `false-positive-move`, `fuzzy-matching`, `import-shifting`, `indentation`, `intra-block-align`, `invalid-diff-bug`, `invalid-diff-trimws`, `invalid-ranges`, `issue-*`, `json-brackets`, `just-whitespace`, `method-splitting`, `minimal-diff-character`, `move-1`, `noise-*`, `noisy-move1`, `penalize-fragmentation`, `random-match-*`, …) |

## Files to create or change

- `tests/LoomEditor.Core.Tests/Diff/Fixtures/<folder>/1.<ext>`, `2.<ext>`, `advanced.expected.diff.json` — create (copied byte for byte with `curl -fsSL https://raw.githubusercontent.com/microsoft/vscode/0c9c0a4295a9222e12a605537738719455ae3056/src/vs/editor/test/node/diffing/fixtures/<folder>/<file> -o …`; **skip** `legacy.expected.diff.json`; if a folder has no `advanced.expected.diff.json` skip the folder and say so in the card)
- `tests/LoomEditor.Core.Tests/Diff/DiffFixtureTests.cs` — create
- `tests/LoomEditor.Core.Tests/Diff/TestHelpers/DiffFixtureFormat.cs` — create (`FormatRange`, the result → JSON-comparable model, the correctness check)
- `tests/LoomEditor.Core.Tests/LoomEditor.Core.Tests.csproj` — change: `<Content Include="Diff\Fixtures\**\*" CopyToOutputDirectory="PreserveNewest" />` (and `<None Remove="…">` if the default globs complain)

## Design decisions (already made — do not change)

- One `[Theory]` with `MemberData` listing the folders found under `AppContext.BaseDirectory/Diff/Fixtures`; the test name is the folder. A missing folder directory fails the test (never "zero tests pass").
- The expected file is read with `System.Text.Json.JsonDocument` (the tests are not AOT-published, but no reflection serialization either); compare in the same shape as Monaco: `diffs[]` (`originalRange`, `modifiedRange` as the `LineRange.ToString()` text, `innerChanges[]` of `{originalRange, modifiedRange}` as `[l,c -> l,c]` with the ` EOL` suffix when the end column is the line length + 1) and the optional `moves[]` (`originalRange`, `modifiedRange`, `changes[]`). A fixture with no moves has no `moves` key: compare `null` / empty the same way.
- The text files are normalized: `\r\n` and `\r` → `\n`, then `Split('\n')` (Monaco `split(/\n/)`), exactly like the runner; the file read is UTF-8.
- **The expected files are never edited** (Monaco's runner rewrites them on mismatch: do **not** port that). A mismatch fails the test with the first differing JSON path and both values.
- `ComputeDiff(…, new LinesDiffComputerOptions(IgnoreTrimWhitespace: folder.Contains("trimws"), MaxComputationTimeMs: int.MaxValue, ComputeMoves: true))`.
- If one fixture cannot pass and the cause is a deliberate behavior difference you can justify, list it in `docs/PORTING.md` "Behavior differences" **and** skip that one folder through an explicit `s_knownDifferences` dictionary (`folder → reason`) in the test; never skip silently. The expectation is an empty dictionary.

## Tests

- [ ] Port: `diffing fixtures` › `<folder>-advanced` for every folder (comment: `// Monaco: 'diffing fixtures' › '<folder>-advanced'` on the theory).
- [ ] New: `Fixtures_AreAllPresent` (the folder count equals the number listed in the card's result section — write the number in the card when done), `FormatRange_ToLastChar_AddsEol`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the fixture theory included (write the number of fixtures run in the card's "Result" section)
- [ ] Any bug fix is made in the file of the card that owns it, with a regression test there
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The `legacy` fixtures and the legacy computer; performance (P9-31).

## Suggested commit message

`test(core): port Monaco's diff fixture tests`
