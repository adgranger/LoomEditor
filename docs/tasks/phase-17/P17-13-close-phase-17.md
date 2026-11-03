# P17-13 — Close phase 17: build, tests, self-checks, Sonar duplication 0%

- **Status:** in progress
- **Depends on:** P17-01 … P17-12
- **Size:** S (checks and docs)

**Recommended model:** Sonnet

## Goal

The phase is verified end to end: no known copy is left, the detector guards both hosts for good, both Galleries pass their self-checks, and
SonarCloud reports 0.0% duplication on PR #26.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md). `docs/roadmap/phase-17-zero-host-duplication.md`, `docs/adr/0017-shared-host-logic.md`,
[P16-24](../phase-16/P16-24-close-phase-16.md) (how the self-checks and the Sonar check ran), `.github/workflows/*`.

## Files to create or change

- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — `KnownDuplicates` must be empty: delete it and `KnownDuplicates_AreStillCopied`, and
  rename `HostTwins_HaveNoUnlistedCopiedBlock` to `HostTwins_HaveNoCopiedBlock` (any block fails).
- `src/LoomEditor.HostShared/README.md` and `CLAUDE.md` ("Architecture rules") — one line each: no block of 100 tokens may be copied between the two hosts
  or the two Galleries (`HostTwinDuplicationTests`); move the code to `src/LoomEditor.HostShared/` instead.
- `docs/roadmap/phase-17-zero-host-duplication.md` — tick the checklist; add a "Result" section with the measured values.
- `docs/adr/0017-shared-host-logic.md` — "Consequences": the verified numbers of phase 17.

## Design decisions (already made — do not change)

- Checks, in this order:
  1. `dotnet build LoomEditor.slnx -c Release`, `dotnet test --solution LoomEditor.slnx -c Release`, `dotnet build LoomEditor.Portable.slnf -c Release`.
  2. WinUI Gallery: `--text-selfcheck`, `--diff-selfcheck`, `--comments-selfcheck`, `--hostapi-selfcheck` → PASS with P16-24's counts (text 10/10, diff 9/9,
     comments 17/17, host API 6/6). `--lsp-selfcheck` is not required (needs csharp-ls).
  3. Avalonia Gallery: the same four self-checks, same counts.
  4. `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` and `dotnet publish samples/LoomEditor.Gallery.Avalonia -c Release -r win-x64` —
     0 warnings (run the self-checks of steps 2 and 3 from these publishes, as P16-24 did).
  5. Sonar: agents do not push. Ask the user to push the branch, wait for the SonarCloud analysis of PR #26, then read
     `curl -s "https://sonarcloud.io/api/measures/component?component=adgranger_LoomEditor&pullRequest=26&metricKeys=new_duplicated_lines,new_lines,new_duplicated_lines_density"`.
- If `new_duplicated_lines` > 0: list the blocks with
  `curl -s "https://sonarcloud.io/api/measures/component_tree?component=adgranger_LoomEditor&pullRequest=26&metricKeys=new_duplicated_lines&qualifiers=FIL&ps=500"`
  and `api/duplications/show` for each file, write them in the Result section, say whether `HostTwinDuplicationTests` misses them (a tokenizer gap) or
  they come from a scope the detector does not scan, and **stop** (follow-up cards are planned by Opus). No exclusion under `src/`.
- Kill every Gallery instance started by the self-checks.

## Tests

- [x] `HostTwins_HaveNoCopiedBlock` green with no allowlist.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] Four self-checks PASS on both Galleries, from the Native AOT publish (0 warnings)
- [ ] Sonar `new_duplicated_lines` on PR #26 = 0 (values written in the roadmap), or the Result section lists what remains
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Fixing remaining duplication (follow-up cards).

## Suggested commit message

`docs: close phase 17 (zero host duplication)`

## Result

- build, tests and Portable build green, 0 warnings; `HostTwins_HaveNoCopiedBlock` green with no allowlist (any block of 100 tokens fails). WinUI Gallery, from the Native AOT publish (0 warnings): text 10/10, diff 9/9, comments 17/17, host API 6/6. Avalonia Gallery: text 10/10, diff 9/9, comments 17/17, host API 6/6 from the framework-dependent build; the Native AOT publish of the Avalonia Gallery fails at start-up (`No precompiled XAML found for LoomEditor.Gallery.App`) and its publish ends with MSB3030 (missing `LoomEditor.Gallery.Avalonia.xml`) - identical at commit 6600f96, before P17-11, so it is not caused by phase 17. Sonar (PR #26): not measured yet (needs a push).
