# Phase 17 — Zero duplication between the WinUI and Avalonia hosts

**Done when:** SonarCloud's "Duplication on New Code" on PR #26 (`feat/avalonia-control` → `main`) is **0.0%**, and a test of the solution fails
when a block of code is copied between the two hosts again. Neither control changes behavior or public API.

Planned 2026-11-03. Decision: [ADR 0017](../adr/0017-shared-host-logic.md) (unchanged; phase 17 applies it to the last blocks). Rules:
[docs/tasks/phase-17/README.md](../tasks/phase-17/README.md). Task cards: [docs/tasks/phase-17/](../tasks/phase-17/README.md) (13 cards).

Phase 16 left **458 duplicated lines out of 21,332 new lines (2.15%)**, all between `src/LoomEditor.WinUI` and `src/LoomEditor.Avalonia` and
between the two Galleries (Sonar `api/duplications/show`, PR 26, analysis of 46c21e2). Most of them are UI construction code that both
frameworks spell the same (a `Border` with the same thickness, radius and padding) and the theme logic around it.

## Where the duplicated blocks are, and which card removes them

Avalonia file:first line+size ↔ WinUI file:first line+size (Sonar). Paths under `src/LoomEditor.<Host>/Controls/` unless stated.

| Card | Blocks | Sonar lines |
|---|---|---|
| P17-01 | (the cross-host duplication test; no block removed) | — |
| P17-02 | `LoomEditor.HostHooks.Host.cs` 38+33 ↔ 44+33 | 66 |
| P17-03 | `DiffEditor/LoomDiffEditor.Options.cs` 207+28 ↔ 254+28 | 28 |
| P17-04 | `DiffEditor/AccessibleDiffViewer.Host.cs` 49+14 ↔ 48+14, 73+23 ↔ 69+23 | 74 |
| P17-05 | `Comments/CommentThreadWidget.cs` 28+25 ↔ 27+25 | 25 |
| P17-06 | `Comments/CommentThreadWidget.Theme.cs` 9+33 ↔ 9+33 | 33 |
| P17-07 | `Comments/CommentThreadWidget.Header.cs` 25+27 ↔ 24+27 | 27 |
| P17-08 | `Comments/CommentThreadWidget.Composer.cs` 76+18 ↔ 82+18, 105+17 ↔ 115+17 | 35 |
| P17-09 | `Comments/CommentComposerToolbar.cs` 63+32 ↔ 58+32; `Comments/SuggestionBlockView.cs` 67+17 ↔ 70+17, 105+16 ↔ 110+16 | 65 |
| P17-10 | `Hover/HoverWidget.cs` 28+22 ↔ 28+22; `Suggest/SuggestWidget.cs` 16+20 ↔ 14+20; `Suggest/SuggestWidget.Rows.cs` 57+21 ↔ 58+21 | 63 |
| P17-11 | `samples/LoomEditor.Gallery.Avalonia/MainWindow.axaml.cs` 16+20 ↔ `samples/LoomEditor.Gallery/MainWindow.xaml.cs` 11+20; `samples/*/HostApiDemoWindow.Diff.Host.cs` 20+11 ↔ 19+11 | 42 |
| P17-12 | found by the P17-01 detector, not in Sonar's list: `LoomEditor.Pointer.Host.cs` 76–90 ↔ 87–101; `Comments/CommentZoneHost.Host.cs` 41–49 ↔ 14–22; `DiffEditor/LoomDiffEditor.cs` 235–260 ↔ 170–195 (first–last line) | — |
| P17-13 | close: build, tests, self-checks, Sonar = 0% | — |
| **Total** | (Sonar counts the lines of both sides of the `.Host.cs` pairs) | **458** |

## Decisions

1. **Same technique as phase 16** (ADR 0017): framework-free code moves to `src/LoomEditor.HostShared/` (or down to `LoomEditor.View` when it names
   no host type); framework calls stay in `<File>.Host.cs`. Shared files still name no framework type.
2. **A local gate instead of waiting for Sonar.** P17-01 adds `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs`: a token-based copy
   detector (the same rule as Sonar's C# CPD: 100 identical tokens, literals normalized, comments and `using` directives ignored) between the two
   hosts and between the two Galleries. It starts with an allowlist of today's pairs; each card deletes its pairs from the list; P17-13 leaves it
   empty, so a new copy fails the build from then on.
3. **Pure UI construction that both frameworks spell the same** (the remaining host code) is not forced into shared files. A host part may use its
   framework's own idiom where it means exactly the same (Avalonia: `new RowDefinitions("Auto,*")`, `new ColumnDefinitions("…")`,
   `new Thickness(horizontal, vertical)`), which the Avalonia host already does elsewhere (`SuggestionBlockView`). Rejected: global usings of the
   framework namespaces in the hosts, which would let shared files name `Grid` or `Border` (against ADR 0017 decision 3); renaming or padding code to
   defeat the detector.
4. **Galleries.** `MainWindow`'s constructor logic is shared (no framework type in it). The Gallery `*.Host.cs` files are framework-specific by design
   (event handlers with framework arguments and the controls they build) and are excluded from Sonar's copy detection by a new
   `.sonarcloud.properties` (`sonar.cpd.exclusions=samples/**/*.Host.cs`); a shared sample for the conflict-header buttons would need host factories
   for `Button` and `StackPanel`, heavier than the 11 lines it removes. No exclusion under `src/`.

## Checklist

- [x] Cross-host duplication test with today's pairs as its allowlist (P17-01)
- [x] `LoomEditor` and `LoomDiffEditor` leftovers shared (P17-02, P17-03)
- [x] Accessible diff viewer, comment, hover and suggest widgets free of copied blocks (P17-04 … P17-10)
- [x] Galleries: shared main window start-up, `.sonarcloud.properties` (P17-11)
- [x] Pointer release, comment zone placement test, diff editor teardown shared (P17-12)
- [x] Allowlist empty; build, tests, self-checks green; Sonar duplication on PR #26 = 0.0% (P17-13)

## Result

- Measured locally (2026-11-03): build, tests and Portable build green, 0 warnings; `HostTwins_HaveNoCopiedBlock` green with no allowlist (any block of 100 tokens fails). WinUI Gallery, from the Native AOT publish (0 warnings): text 10/10, diff 9/9, comments 17/17, host API 6/6. Avalonia Gallery: text 10/10, diff 9/9, comments 17/17, host API 6/6 from the framework-dependent build; the Native AOT publish of the Avalonia Gallery fails at start-up (`No precompiled XAML found for LoomEditor.Gallery.App`) and its publish ends with MSB3030 (missing `LoomEditor.Gallery.Avalonia.xml`) - identical at commit 6600f96, before P17-11, so it is not caused by phase 17. Sonar (PR #26, commit 240a778, analysed 2026-11-04): `new_duplicated_lines` = 0 of 22,226 new lines (0.0%), down from 2.14%.
