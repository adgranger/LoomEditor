# P17-11 — Gallery: share the main window start-up; keep the Gallery host parts out of Sonar's copy detection

- **Status:** todo
- **Depends on:** P17-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

The two Gallery main windows start the same way from one source, and SonarCloud stops comparing the Gallery `*.Host.cs` files, which are
framework-specific by design (decision 4 of the [roadmap](../../roadmap/phase-17-zero-host-duplication.md)).

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md), the Gallery rules of [phase-16/README.md](../phase-16/README.md) (shared Gallery files live in
`samples/LoomEditor.Gallery/` and are linked into `samples/LoomEditor.Gallery.Avalonia/LoomEditor.Gallery.Avalonia.csproj`, item group
"Shared with the WinUI Gallery"). SonarCloud documentation of `.sonarcloud.properties` (automatic analysis: the analysis scope keys it accepts).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| Avalonia | WinUI | What |
|---|---|---|
| `samples/LoomEditor.Gallery.Avalonia/MainWindow.axaml.cs` 16+20 | `samples/LoomEditor.Gallery/MainWindow.xaml.cs` 11+20 | `_injectedTextIds` and the constructor up to `SelectionChanged` |
| `samples/LoomEditor.Gallery.Avalonia/HostApiDemoWindow.Diff.Host.cs` 20+11 | `samples/LoomEditor.Gallery/HostApiDemoWindow.Diff.Host.cs` 19+11 | `OnAddConflictHeader` (builds two buttons in a panel) |

## Files to create or change

- `samples/LoomEditor.Gallery/MainWindow.Startup.cs` — new shared file (`public sealed partial class MainWindow`, namespace `LoomEditor.Gallery`):
  `_injectedTextIds` (moved from both windows) and two methods taken from the WinUI constructor:
  - `InitializeEditor()` — from `PopulatePickers()` to `Editor.SelectionChanged += (_, _) => RefreshStatus();` (WinUI lines 22–30, comment included);
  - `InitializeChecks()` — `RefreshStatus()`, `InitializeLsp()` and the four `Initialize…SelfCheck()` calls (WinUI lines 34–39).
- `samples/LoomEditor.Gallery/MainWindow.xaml.cs` and `samples/LoomEditor.Gallery.Avalonia/MainWindow.axaml.cs` — the constructor becomes
  `InitializeComponent(); InitializeEditor(); <the host's property-change wiring, unchanged>; InitializeChecks();`; the field goes.
- `samples/LoomEditor.Gallery.Avalonia/LoomEditor.Gallery.Avalonia.csproj` — link `MainWindow.Startup.cs` (`Link="Shared\MainWindow.Startup.cs"`), next
  to the other `MainWindow.*` links.
- `.sonarcloud.properties` — new, at the repository root:

  ```properties
  # The Gallery *.Host.cs parts hold the framework-specific event handlers and controls of each Gallery (ADR 0017, phase 17).
  sonar.cpd.exclusions=samples/**/*.Host.cs
  ```

- `docs/adr/0017-shared-host-logic.md` — "Consequences": one line: phase 17 excludes `samples/**/*.Host.cs` from Sonar's copy detection (the Gallery host
  parts), nothing under `src/`.
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `MainWindow` pair from `KnownDuplicates` (the detector already skips the
  Gallery `*.Host.cs`, P17-01).

## Design decisions (already made — do not change)

- Same calls in the same order as today in each constructor.
- `TabSizeBox.Value = Editor.Model.Options.TabSize` compiles in both Galleries (`NumberBox.Value` is `double`, `NumericUpDown.Value` is `decimal?`): it moves
  to the shared file as is.
- No shared sample for `OnAddConflictHeader`: it builds framework controls, the exclusion covers it.
- Check in the SonarCloud documentation that automatic analysis reads `sonar.cpd.exclusions` from `.sonarcloud.properties`. If it does not, keep the file
  anyway and write in the Result section that the user must set "Duplication Exclusions" = `samples/**/*.Host.cs` in the SonarCloud project settings
  (agents do not change SonarCloud settings).

## Tests

- [ ] No new test.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (both Galleries)
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] The pair is gone from `KnownDuplicates`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Running the Gallery self-checks (P17-13).

## Suggested commit message

`refactor(gallery): share the main window start-up; exclude Gallery host parts from Sonar CPD`
