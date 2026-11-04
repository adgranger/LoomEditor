# ADR 0017 — Shared host logic: one source for what the WinUI and Avalonia controls have in common

- **Status:** accepted (2026-11-03)
- **Date:** 2026-11-03
- **Amends:** [ADR 0016](0016-avalonia-backend.md) decisions 4 and 5 (public types are no longer copied; the twins no longer repeat their shared logic)

## Context

Phase 15 translated `LoomEditor.WinUI` file by file into `LoomEditor.Avalonia` (ADR 0016 decision 5). Most twins differ only in their namespace
and in a few framework calls: `HideUnchangedRegionsController` differs in 10 lines out of 325, `DiffEditorViewZones` in 11 out of 260,
`LoomDiffEditor.CommentThreads` in 5 out of 165. The Avalonia Gallery repeats the WinUI Gallery the same way.

SonarCloud measures this on PR #26 (`feat/avalonia-control` → `main`): **4,500 duplicated lines out of 19,366 new lines (23.2%)**, in 65 files. The
quality gate asks for ≤ 3%. Every fix made in one control must today be made again in the other, and nothing checks that it was.

| Group | Duplicated lines | Main files |
|---|---|---|
| `LoomDiffEditor` and its controllers | 1,828 | `HideUnchangedRegionsController`, `DiffBlockActionsController`, `DiffEditorViewZones`, `DiffLineCheckBoxesController`, `LoomDiffEditor.*` |
| Comment widgets | 659 | `CommentEventArgs`, `CommentZoneHost`, `CommentThreadWidget.*`, `CommentGutterController` |
| `LoomEditor` partials | 635 | `ViewZonePointer`, `Pointer`, `CommentThreads`, `Scrolling`, `DiffGutter`, `Reveal` |
| Find, suggest, hover widgets | 347 | `FindWidget.*` |
| Avalonia Gallery | 788 | `Lsp/*`, `DiffDemoWindow.*`, `HostApiDemoWindow.*`, `MainWindow.*` |
| Rendering (Win2D and Avalonia twins) | 136 | cluster hit testing, `wght` parsing |
| Avalonia tests | 107 | the diff editor test window setup |

The user chose to factor the code for real: no Sonar exclusion.

Options:

| Option | For | Against |
|---|---|---|
| A. Move every shared piece down into `LoomEditor.View`, behind interfaces that both controls implement | Layer-pure; testable in View | The controllers talk to the control itself (`SetHiddenAreas`, `ViewZones`, `ScrollTop`, `ViewZonePointer`, …): one interface of ~15 members per controller, a redesign of code that works, and the public partials of `LoomEditor` / `LoomDiffEditor` cannot move at all |
| **B. Shared host source**: one `.cs` file compiled into both control assemblies, each in its own namespace | No redesign: the file is the WinUI twin with its framework calls moved out; works for public partials and public types too; the compiler checks both builds | A file that belongs to two projects; a strict file header (below) |
| C. Sonar exclusions (`sonar.cpd.exclusions`) | Nothing to do | Rejected by the user: the duplication stays |

## Decision

1. **Option B for everything that names the control or a host type.** A new source folder **`src/LoomEditor.HostShared/`** (no project file) holds the code that
   `LoomEditor.WinUI` and `LoomEditor.Avalonia` share. Both project files import `src/LoomEditor.HostShared/LoomEditor.HostShared.props`, which compiles every
   `.cs` file of the folder into the project (`<Compile Include … LinkBase="HostShared" />`). The folder mirrors the host-relative paths:
   `src/LoomEditor.HostShared/Controls/DiffEditor/HideUnchangedRegionsController.cs` replaces the two `Controls/DiffEditor/HideUnchangedRegionsController.cs`.
2. **Host symbols.** `LoomEditor.WinUI.csproj` defines `LOOM_WINUI`, `LoomEditor.Avalonia.csproj` defines `LOOM_AVALONIA`. A shared file starts with one header block,
   the **only** conditional code allowed in the folder:

   ```csharp
   using LoomEditor.View.DiffEditor;                       // Core / View / Rendering / Themes usings: unconditional
   #if LOOM_WINUI
   using LoomEditor.WinUI.Controls.Overlay;               // usings of the host's own namespaces only
   namespace LoomEditor.WinUI.Controls.DiffEditor;
   #elif LOOM_AVALONIA
   using LoomEditor.Avalonia.Controls.Overlay;
   namespace LoomEditor.Avalonia.Controls.DiffEditor;
   #else
   #error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
   #endif
   ```

   A file ported from Monaco keeps its two `// Ported from` lines above the header block.
3. **What a shared file may use:** Core, View, Rendering, Themes, TextMate, the BCL; the types and members of its own host (the control, `OverlayHost`,
   `SuggestionBlockView`, named template parts, …) by their simple name, because both hosts spell them the same (ADR 0016 decision 5). It never names an
   Avalonia, WinUI or Windows namespace or type. Members of framework objects that both frameworks spell and mean the same (`Text`, `IsEnabled`, `IsChecked`,
   `Content`, `Children`, `Width`, `Height`, `Tag`) may be used through host-declared fields; anything else (focus, pointer capture, visibility, brushes,
   tool tips, dispatching, invalidation) goes through a **host member**.
4. **Host members.** A type with a shared part is `partial`. Its framework-specific part lives in each host in a file named **`<File>.Host.cs`** next to the
   original path (`Controls/DiffEditor/DiffBlockActionsController.Host.cs`). Both hosts declare the same host members with the same signature
   (for example `private void FocusModified()`). A shared file never contains a host member's body.
5. **UI thread.** Each host gets `Threading/HostDispatcher.cs`: `internal static class HostDispatcher` with `bool Post(Action action)` (normal priority) and
   `bool PostLow(Action action)` (WinUI `DispatcherQueuePriority.Low`, Avalonia `DispatcherPriority.Background`). Shared code posts through it; the controllers stop
   taking a `DispatcherQueue` argument.
6. **Base = the WinUI file.** The WinUI control is the published, reference implementation. A shared file starts from the WinUI twin; where the Avalonia twin
   behaves differently (not only spells differently), the difference stays in the host parts. No behavior of either control changes in phase 16.
7. **Public UI-free types are compiled from one source** (`CommentEventArgs`, `DiffAddCommentRequestedEventArgs`, `DiffSide`, `DiffLine`, the option enums, …).
   Each assembly still declares them in its own namespace, so both public APIs are unchanged (ADR 0016's reason to copy them still holds; the copy goes).
8. **Option A where nothing names a host type.** Pure helpers move down a layer as usual: the cluster hit-test and `wght` parsing helpers of the Win2D and Avalonia
   text layouts go to `src/LoomEditor.Rendering/` (flat, as the rest of the project).
9. **Gallery: the same pattern.** Shared Gallery files stay in `samples/LoomEditor.Gallery/` and are linked into `samples/LoomEditor.Gallery.Avalonia`
   (as phase 15 already does for the UI-free ones). Each Gallery project gets global usings for its control namespace and the alias
   `LoomEditorControl`. Window partials split into a shared part (logic, logging, samples) and `<File>.Host.cs` (event handlers with their framework argument types,
   framework calls).
10. **Checked by tests.** `tests/LoomEditor.Architecture.Tests/HostSharedSourceTests.cs` checks: the header block rule (decision 2) and that it is the only
    `#if` in the folder; the two namespaces are `LoomEditor.WinUI.<X>` / `LoomEditor.Avalonia.<X>` with `<X>` = the folder path; no Avalonia, WinUI or Windows
    namespace in the folder; both host projects import the props file. `PlatformNeutralSourceTests` scans the folder too.

## Why

- **It removes the duplication without new design.** The twins are already the same code; B turns each pair into one file plus two small host parts. A would
  rewrite working, mostly untested-on-WinUI code behind new interfaces.
- **Parity becomes structural.** A fix in a shared file reaches both controls, and a shared file that uses a member only one host has does not build.
- **It respects the layers.** Shared host code is host-layer code compiled into the host assemblies; only code that names no host type goes down to View or
  Rendering, as before.

## Consequences

- New rule for every later card that touches both controls: the host-neutral part is written once in `src/LoomEditor.HostShared/`; the `.Host.cs` parts stay
  small. `CLAUDE.md` and `docs/ARCHITECTURE.md` say so (P16-01).
- The phase 15 rule "a card that finds shared UI-free logic copies it" ([phase-15/README.md](../tasks/phase-15/README.md)) is replaced by this ADR.
- The WinUI control has no automated UI test. Its shared files are exercised by the Avalonia headless tests and by the WinUI Gallery self-checks, which the closing card
  runs (`--text-selfcheck`, `--diff-selfcheck`, `--comments-selfcheck`, `--hostapi-selfcheck`, `--lsp-selfcheck`).
- `docs/PORTING.md` rows that named a WinUI and an Avalonia path name the shared path instead.
- Phase 17 excludes `samples/**/*.Host.cs` from Sonar's copy detection (`.sonarcloud.properties`, the Gallery host parts); nothing under `src/`.
- Verified by P17-13 (2026-11-03): build, tests and Portable build green, 0 warnings; `HostTwins_HaveNoCopiedBlock` green with no allowlist (any block of 100 tokens fails). WinUI Gallery, from the Native AOT publish (0 warnings): text 10/10, diff 9/9, comments 17/17, host API 6/6. Avalonia Gallery: text 10/10, diff 9/9, comments 17/17, host API 6/6 from the framework-dependent build; the Native AOT publish of the Avalonia Gallery fails at start-up (`No precompiled XAML found for LoomEditor.Gallery.App`) and its publish ends with MSB3030 (missing `LoomEditor.Gallery.Avalonia.xml`) - identical at commit 6600f96, before P17-11, so it is not caused by phase 17. Sonar (PR #26, commit 240a778, analysed 2026-11-04): `new_duplicated_lines` = 0 of 22,226 new lines (0.0%), down from 2.14%.
- Target: Sonar "Duplication on New Code" on PR #26 ≤ 3% (≤ about 580 lines). Checked by P16-24.
- Verified by P16-24 (2026-11-03): `dotnet build` / `dotnet test` / Portable build green, 0 warnings. Both Galleries pass `--text-selfcheck` 10/10, `--diff-selfcheck` 9/9, `--comments-selfcheck` 17/17 and `--hostapi-selfcheck` 6/6, from the Native AOT publish (`dotnet publish -r win-x64`, 0 warnings). `--lsp-selfcheck` was not run (needs csharp-ls). Sonar duplication on PR #26: 2.14% (458 of 21,437 new lines).
