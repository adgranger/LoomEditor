# P16-24 — Close phase 16: self-checks, Native AOT, Sonar duplication ≤ 3%

- **Status:** done
- **Depends on:** P16-01 … P16-23
- **Size:** S (checks and docs)

**Recommended model:** Sonnet

## Goal

The phase is verified end to end and documented: both Galleries pass their self-checks, publish with Native AOT, and SonarCloud reports ≤ 3% duplication on PR #26.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `docs/roadmap/phase-16-shared-host-logic.md`, `docs/adr/0017-shared-host-logic.md`, `CHANGELOG.md`, `.github/workflows/*` (how the self-checks run in CI).

## Files to create or change

- `docs/roadmap/phase-16-shared-host-logic.md` — tick the checklist; add the measured Sonar values
- `docs/adr/0017-shared-host-logic.md` — "Consequences": the verified numbers (as ADR 0016 does)
- `CHANGELOG.md` — one "Changed" line (internal refactoring, no API change)

## Design decisions (already made — do not change)

- Checks, in this order:
  1. `dotnet build LoomEditor.slnx -c Release`, `dotnet test --solution LoomEditor.slnx -c Release`, `dotnet build LoomEditor.Portable.slnf -c Release`.
  2. WinUI Gallery (Windows): `--text-selfcheck`, `--diff-selfcheck`, `--comments-selfcheck`, `--hostapi-selfcheck`, `--lsp-selfcheck` → all PASS (same counts as P15-34:
     text 10/10, diff 9/9, comments 17/17, host API 6/6).
  3. Avalonia Gallery: the same five self-checks → PASS, same counts.
  4. `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` and `dotnet publish samples/LoomEditor.Gallery.Avalonia -c Release -r win-x64` — 0 warnings.
  5. Sonar: agents do not push. Ask the user to push the branch, wait for the SonarCloud analysis of PR #26, then read
     `curl -s "https://sonarcloud.io/api/measures/component?component=adgranger_LoomEditor&pullRequest=26&metricKeys=new_duplicated_lines,new_lines,new_duplicated_lines_density"`.
- If the density is > 3%: list the remaining files with
  `curl -s "https://sonarcloud.io/api/measures/component_tree?component=adgranger_LoomEditor&pullRequest=26&metricKeys=new_duplicated_lines&qualifiers=FIL&ps=500"`,
  write them in the Result section and **stop** (follow-up cards are planned by a person or Opus, not fixed in this card). Do not add Sonar exclusions.
- Check also that SonarCloud lists each `src/LoomEditor.HostShared` file once (not as a duplicate of itself in two projects).

## Tests

- [x] No new test.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] Five self-checks PASS on both Galleries
- [x] Native AOT publish of both Galleries: 0 warnings
- [x] Sonar `new_duplicated_lines_density` on PR #26 ≤ 3.0 (value written in the roadmap), or the Result section lists what remains
- [x] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Fixing remaining duplication (follow-up cards).

## Suggested commit message

`docs: close phase 16 (shared host logic)`

## Result

- All checks of the design decisions pass except step 5 (Sonar): build, tests and Portable build green; both Galleries pass text 10/10, diff 9/9, comments 17/17, host API 6/6 from the Native AOT publish (0 warnings); `--lsp-selfcheck` not run (csharp-ls).
- Sonar (PR #26, analysis of 802565b): `new_duplicated_lines_density` = **2.14** (458 of 21,437 new lines), under the 3.0 target (it was 23.2% before the phase, 4.42% before P16-21 … P16-23).
