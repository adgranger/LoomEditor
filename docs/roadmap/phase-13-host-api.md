# Phase 13 — Public API for host applications

**Done when:** an application that references only the NuGet packages can build VIGO's features (a git client: blame, diff, conflicts, staging) with public members:
it finds the language of a file from its name; highlights lines with built-in classes or **its own decoration styles** (light and dark colors); draws a column beside the
editor that stays aligned with the lines (scroll, layout, line height, line tops) and hides the line numbers; reloads a text equal to the previous one; hides ranges of
lines; reacts to a click in the gutter; and puts its own WinUI element in a gap between two lines. The package test app calls every new member, so a member that is
not public breaks the package test; the Native AOT self-check passes.

Origin: VIGO tried `0.1.0-alpha.1` and found every member it needed `internal` (it had read them in the packages' XML docs, which list internal members too). Not a port.
Design: [docs/tasks/phase-13/README.md](../tasks/phase-13/README.md) (section 2: the decision per need, and what stays internal). Task cards:
[docs/tasks/phase-13/](../tasks/phase-13/README.md) (11 cards).

## Checklist

- [x] `BuiltInLanguages.GetLanguageIdByFileName` (P13-01)
- [x] Public decoration types: `EditorDecoration`, `EditorDecorationOptions`, `DecorationsCollection` (P13-02)
- [x] Host decoration styles: `CustomDecorationStyle`, `CustomDecorationStyles` (P13-03)
- [x] `LoomEditor.CreateDecorationsCollection` (P13-04)
- [x] `ScrollChanged`, `LayoutChanged`, `LineHeight`, `GetTopForLineNumber` (P13-05)
- [x] `LineNumbersEnabled`, `ResetText` (P13-06)
- [x] `SetHiddenAreas`, `GutterClicked` (P13-07)
- [x] `AddViewZone`, `RemoveViewZone` with WinUI content (P13-08)
- [x] Gallery "Host API" demo and manual test plan (P13-09)
- [x] Native AOT self-check and package test of the host API (P13-10)
- [x] How-to, CHANGELOG, ADR 0015; close the phase (P13-11)

## Status of the done-when line

- **Automated:** build 0 warnings and all tests green at the close (counted with `dotnet test --list-tests`: Core 2214, TextMate 739, Lsp 476, View 3787, Rendering 242, Architecture 90).
- **Native AOT self-check and package test (P13-10):** the published Native AOT Gallery prints `5 of 5 checks passed` for `--hostapi-selfcheck` (the comments, text and diff checks are unchanged:
  17 of 17, 10 of 10, 9 passed). `./eng/Test-Packages.ps1` printed `Package test passed: 0.1.0-alpha`, run with a **placeholder** arm64 `onig.dll` (a copy of the x64 one, deleted afterwards;
  the MSVC ARM64 tools are not installed locally and CI builds the real one).
- **Not verified:** the manual plan [phase-13-manual-tests.md](../testing/phase-13-manual-tests.md) has not been run (no WinUI test project; agents do not run the Gallery UI), nor Narrator, high contrast,
  a host `TextBox` in a zone, or VIGO itself.

## Not in phase 13

Making Monaco's internal types public (decoration options, view zones, the language registry, the style table); a pointer event for view zones; host zones and hidden
areas on the sides of a `LoomDiffEditor`; registering languages from the host; per-editor styles; hover messages, injected text, glyph-margin icons on host decorations; a
"visible lines" API; any new member on `LoomDiffEditor` (its sides `Original` / `Modified` are public `LoomEditor`s).

## Open risks

- The packages' XML docs still describe internal members (the compiler writes them), which is how VIGO was misled. Stripping them is a packaging follow-up.
- Host decoration styles are global to the process: two editors cannot draw the same class name differently.
- A host view zone on one side of a `LoomDiffEditor` may break the alignment of the two sides (not supported, documented).
