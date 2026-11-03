# Phase 17 — read this before any card

Phase 17 removes the last blocks that SonarCloud reports as copied between the WinUI and the Avalonia hosts
([phase-17-zero-host-duplication.md](../../roadmap/phase-17-zero-host-duplication.md)): 458 lines (2.15%) on PR #26; the target is 0.0%.

## The phase 16 rules still apply

Read [phase-16/README.md](../phase-16/README.md) and [ADR 0017](../../adr/0017-shared-host-logic.md) first: no behavior or public API change, start from the
WinUI file, `git mv` when a whole file moves, the header block (`#if !LOOM_AVALONIA … #elif LOOM_AVALONIA … #endif`, see
[src/LoomEditor.HostShared/README.md](../../../src/LoomEditor.HostShared/README.md)), no framework namespace or type in a shared file, host members in
`<File>.Host.cs` with the same signature in both hosts, the Avalonia headless tests green **unchanged**.

## The gate: `HostTwinDuplicationTests` (P17-01)

- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` finds every run of **100 identical tokens** between a WinUI file and an Avalonia
  file (and between the two Galleries), the way Sonar's C# copy detection does. Its `KnownDuplicates` list holds the file pairs that still have a copied
  block.
- A card is done when **its pairs are deleted from `KnownDuplicates`** and the test is green. The test also fails when a listed pair no longer has a
  block, so the list cannot go stale. Run it alone while working:
  `dotnet test --project tests/LoomEditor.Architecture.Tests -c Release`.
- Its failure message gives both files and line ranges of each block. Do not change its tokenizer or its threshold to make a card pass.

## How to remove a block, in this order of preference

1. **Move the logic out of the host**: code that names no framework type goes to the shared file of `src/LoomEditor.HostShared/` (or to
   `LoomEditor.View` when it names no host type either, with a unit test there). This is the real fix; the cards say what moves.
2. **Host members** for the framework calls the moved code needs (phase 16 rule 4), declared in both hosts with the same signature. A C# `partial void`
   method is fine when only one host has something to do (its body exists in that host only).
3. **The framework's own idiom in the host part**, where it means exactly the same and the host already uses it elsewhere. Avalonia:
   `new RowDefinitions("Auto,*")`, `new ColumnDefinitions("48,48,24,*")` (or `$"{Width},*"` as `SuggestionBlockView` does), `new Thickness(8, 4)` for
   `new Thickness(8, 4, 8, 4)`. Never change a value or a default the control relies on.
4. The order of the fields and members **inside one host part** is free (keep a readable order: fields first).

Forbidden: renaming identifiers, padding, dummy statements or comments to break a block; `// NOSONAR`; `sonar.cpd.exclusions` for anything under
`src/`; global usings of `Microsoft.UI.*` / `Avalonia.*` namespaces in the host projects (they would let shared files name framework types); weakening
or editing the detector.

## Done for every card

- `dotnet build LoomEditor.slnx -c Release` — 0 warnings; `dotnet test --solution LoomEditor.slnx -c Release` — all green (the duplication test
  included); `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings.
- The card's pairs are gone from `KnownDuplicates`.
- `docs/PORTING.md` (phase 16 rule 7, for ported files that moved), the card and `docs/tasks/README.md` updated. One commit (`/next-task`), no trailer.
