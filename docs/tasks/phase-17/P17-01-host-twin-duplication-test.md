# P17-01 — A test that finds blocks copied between the two hosts

- **Status:** done
- **Depends on:** P16-24
- **Size:** M

**Recommended model:** Sonnet

## Goal

A test of the solution finds, without SonarCloud, every block of code copied between the WinUI and the Avalonia hosts (and between the two Galleries).
Today's copies are listed as known; the next cards empty the list, and from then on a new copy fails the build.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md), [phase-16/README.md](../phase-16/README.md). Existing architecture tests:
`tests/LoomEditor.Architecture.Tests/HostSharedSourceTests.cs` (style, `[GeneratedRegex]`), `RepositoryLayout.cs` (`Root`, `Source`).
Reference result (Sonar `api/duplications/show`, PR #26, analysis of 46c21e2): the blocks of [the roadmap](../../roadmap/phase-17-zero-host-duplication.md).

## Files to create or change

- `tests/LoomEditor.Architecture.Tests/TestHelpers/CopyDetector.cs` — new: the tokenizer and the block finder (internal, static).
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — new: the tests and `KnownDuplicates`.
- `tests/LoomEditor.Architecture.Tests/RepositoryLayout.cs` — add `Samples` (the `samples/` folder) next to `Source`, if needed.

## Design decisions (already made — do not change)

- **What is compared** (each side against the other only, never a file against its own host):
  - every `*.cs` under `src/LoomEditor.WinUI/` against every `*.cs` under `src/LoomEditor.Avalonia/`;
  - every `*.cs` under `samples/LoomEditor.Gallery/` against every `*.cs` under `samples/LoomEditor.Gallery.Avalonia/`, **except `*.Host.cs`** (they are
    excluded from Sonar's copy detection by P17-11; the shared Gallery files live in `samples/LoomEditor.Gallery/` only and are linked, so they are
    not copies).
  - Skip `bin/` and `obj/` folders. `src/LoomEditor.HostShared/` is not scanned (one source).
- **Tokens** (the rule of Sonar's C# copy detection):
  - Ignored: whitespace, `//` and `/* */` comments (XML doc included), preprocessor lines (`#if`, `#pragma`, …), and `using` directives
    (a line matching `^\s*(global\s+)?using\s+(static\s+)?[\w.]+(\s*=\s*[^;]+)?;\s*$`; `using var x = …;` and `using (…)` statements are kept).
  - String literals of any form (`"…"`, `@"…"`, `$"…"`, `$@"…"`, `"""…"""`) → one token `$str` (an interpolated string is one token, holes included);
    character literals → `$char`; numeric literals (`48`, `0.7`, `0x33`, `1_000`, `2d`) → `$num`.
  - Identifiers and keywords: their text (`@name` without the `@`). Operators: the longest of `??= ?. ?? => == != <= >= && || ++ -- += -= *= /= %= &= |= ^= << ::`,
    else one character (`>>` is two tokens, generics stay correct).
  - Each token keeps its 1-based line number.
- **Block**: a run of at least **100** consecutive identical tokens in a WinUI-side file and an Avalonia-side file (`MinimumTokens = 100`, a `const`).
  Implementation: intern each token text to an `int`; index every 100-token window of the WinUI side with a rolling hash (unchecked `ulong`); for
  each window of the Avalonia side, confirm a hash hit by comparing the 100 ints; merge consecutive matching windows (same file pair, consecutive
  offsets on both sides) into one block reported as `avalonia/path.cs:first-last ↔ winui/path.cs:first-last` (repository-relative paths, `/`).
- **Tests** in `HostTwinDuplicationTests`:
  - `HostTwins_HaveNoUnlistedCopiedBlock` — every block found belongs to a pair of `KnownDuplicates`; the message lists each unlisted block with both
    line ranges.
  - `KnownDuplicates_AreStillCopied` — every pair of `KnownDuplicates` still has a block; message: "… is no longer copied: delete it from
    KnownDuplicates".
  - Detector unit tests (no files): `Tokenize_NormalizesLiteralsAndSkipsCommentsAndUsings`, `FindBlocks_ReportsA100TokenRun`,
    `FindBlocks_IgnoresA99TokenRun`.
- **`KnownDuplicates`** (`private static readonly HashSet<string>`, one `"<Avalonia path> <-> <WinUI path>"` per line, ordinal comparison), starting with
  the 13 pairs Sonar reports today (the `HostApiDemoWindow.Diff.Host.cs` pair is out of scope, see above):

  ```text
  src/LoomEditor.Avalonia/Controls/LoomEditor.HostHooks.Host.cs <-> src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.Host.cs
  src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.Options.cs <-> src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.Options.cs
  src/LoomEditor.Avalonia/Controls/DiffEditor/AccessibleDiffViewer.Host.cs <-> src/LoomEditor.WinUI/Controls/DiffEditor/AccessibleDiffViewer.Host.cs
  src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs
  src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Theme.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Theme.cs
  src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Header.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Header.cs
  src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Composer.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Composer.cs
  src/LoomEditor.Avalonia/Controls/Comments/CommentComposerToolbar.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentComposerToolbar.cs
  src/LoomEditor.Avalonia/Controls/Comments/SuggestionBlockView.cs <-> src/LoomEditor.WinUI/Controls/Comments/SuggestionBlockView.cs
  src/LoomEditor.Avalonia/Controls/Hover/HoverWidget.cs <-> src/LoomEditor.WinUI/Controls/Hover/HoverWidget.cs
  src/LoomEditor.Avalonia/Controls/Suggest/SuggestWidget.cs <-> src/LoomEditor.WinUI/Controls/Suggest/SuggestWidget.cs
  src/LoomEditor.Avalonia/Controls/Suggest/SuggestWidget.Rows.cs <-> src/LoomEditor.WinUI/Controls/Suggest/SuggestWidget.Rows.cs
  samples/LoomEditor.Gallery.Avalonia/MainWindow.axaml.cs <-> samples/LoomEditor.Gallery/MainWindow.xaml.cs
  ```

- **Calibration**: run the detector with an empty list first. It must find exactly these 13 pairs, at about Sonar's line ranges (the roadmap table).
  If it finds a pair Sonar does not report, or misses one, fix the tokenizer against the rules above, not the list. If one difference remains after
  that, **stop** and write it in the Result section (the token rule is then wrong somewhere and Opus decides).
- No production code changes in this card.

## Tests

- [x] The two file tests and the three detector unit tests above; all green with the 13-pair list.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings (the architecture tests build on Linux and macOS too)
- [x] The detector finds exactly the 13 pairs above with an empty list (write the block count and the run time of the file test in the Result section) — 16 pairs, 3 extra ones added by decision; removed by a later card
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Removing any block (P17-02 … P17-11); copies inside one host; `tests/` folders.

## Suggested commit message

`test(arch): detect code copied between the WinUI and Avalonia hosts`

## Result

Calibration with an empty `KnownDuplicates`: **16 pairs / 24 blocks**. The 13 listed pairs match Sonar's line ranges (±1). Three more pairs are real copies
that Sonar does not report; by decision they are listed in `KnownDuplicates` and a later card removes them:

- `Controls/LoomEditor.Pointer.Host.cs` Avalonia 76-90 ↔ WinUI 87-101
- `Controls/Comments/CommentZoneHost.Host.cs` Avalonia 41-49 ↔ WinUI 14-22
- `Controls/DiffEditor/LoomDiffEditor.cs` Avalonia 235-260 ↔ WinUI 170-195

Extra blocks inside listed pairs: `CommentComposerToolbar.cs` 16-33 ↔ 16-33; `AccessibleDiffViewer.Host.cs` 103-111 ↔ 99-107; `HoverWidget.cs` is three blocks
(10-25, 27-49, 52-75); `MainWindow` 399-412 ↔ 314-327. Run time of the file tests: about 1.4 s for the five tests.
