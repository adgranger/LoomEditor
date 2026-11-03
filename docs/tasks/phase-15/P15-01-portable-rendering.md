# P15-01 — `LoomEditor.Rendering` becomes `net11.0`; Win2D moves into `LoomEditor.WinUI`

- **Status:** todo
- **Depends on:** P14-06
- **Size:** S (moves; < 150 new lines)

**Recommended model:** Sonnet

## Goal

`LoomEditor.Rendering` builds as plain `net11.0` with no Windows package, so an Avalonia project can reference it. The Win2D backend lives in `LoomEditor.WinUI`.
Behavior of the WinUI control is unchanged.

## Sources

Sources: none (specified in this card; [ADR 0016](../../adr/0016-avalonia-backend.md) decision 2). Read first: `src/LoomEditor.Rendering/LoomEditor.Rendering.csproj`,
`src/LoomEditor.Rendering/Win2D/*.cs`, `src/LoomEditor.WinUI/LoomEditor.WinUI.csproj`, `tests/LoomEditor.Rendering.Tests/LoomEditor.Rendering.Tests.csproj`,
`tests/LoomEditor.Architecture.Tests/DependencyRulesTests.cs`, `PlatformNeutralSourceTests.cs`, `docs/adr/0002-rendering-backend.md`.

## Files to create or change

- `src/LoomEditor.Rendering/Win2D/Win2DBitmapCache.cs`, `Win2DLineLayout.cs`, `Win2DMinimapFontSampler.cs`, `Win2DRenderSurface.cs`, `Win2DTextMeasurer.cs` — **move** (`git mv`) to
  `src/LoomEditor.WinUI/Rendering/Win2D/`, namespace `LoomEditor.WinUI.Rendering.Win2D`
- `src/LoomEditor.Rendering/Win2D/FontFeatureTags.cs` — move to `src/LoomEditor.Rendering/FontFeatureTags.cs`, namespace `LoomEditor.Rendering` (it uses no Win2D type)
- `src/LoomEditor.Rendering/LoomEditor.Rendering.csproj` — `TargetFramework` = `$(LoomNetTargetFramework)`; remove `TargetPlatformMinVersion`, `UseWinUI`, `Win2DWarnNoPlatform`,
  both package references; update the comment and `Description` (`"Rendering of LoomEditor: what to draw, behind IRenderSurface."`)
- `src/LoomEditor.WinUI/LoomEditor.WinUI.csproj` — keep `Microsoft.Graphics.Win2D` (it is already referenced) and the `Win2DWarnNoPlatform` property moved from Rendering
- every `using LoomEditor.Rendering.Win2D;` in `src/LoomEditor.WinUI` and `samples/` — the new namespace
- `tests/LoomEditor.Rendering.Tests/LoomEditor.Rendering.Tests.csproj` — `$(LoomNetTargetFramework)`, no `TargetPlatformMinVersion`; `FontFeatureTagsTests` namespace follows
- `tests/LoomEditor.Architecture.Tests/DependencyRulesTests.cs` — add `"LoomEditor.Rendering"` to `UiIndependentProjects`; update the summary of the class
- `tests/LoomEditor.Architecture.Tests/PlatformNeutralSourceTests.cs` — add `"LoomEditor.Rendering"`; fix the link in the summary to `docs/roadmap/phase-15-avalonia.md`
- `src/LoomEditor.Rendering/IRenderSurface.cs` — XML doc: "the only drawing API used by `TextViewRenderer`; backends live in the control projects (Win2D in `LoomEditor.WinUI`)"
- `docs/ARCHITECTURE.md` — the layer diagram and the table: Rendering is `net11.0`, "`IRenderSurface`, `TextViewRenderer`"; WinUI "… + the Win2D backend"
- `docs/adr/0002-rendering-backend.md` — one line under "Consequences": "Since P15-01 the `Win2D/` folder is in `LoomEditor.WinUI/Rendering/Win2D/` (ADR 0016)."
- `docs/PORTING.md` — any map row naming `LoomEditor.Rendering/Win2D/` gets the new path

## Design decisions (already made — do not change)

- Pure moves: no code change inside the moved files apart from the namespace and `using`s. `git mv` so history follows.
- The `LoomEditor.Rendering` package keeps its id. Its nuspec dependencies lose Win2D and the Windows App SDK; `eng/Test-Packages.ps1` must still pass.
- `InternalsVisibleTo` lists are not changed in this card (P15-05 adds Avalonia).

## Tests

- [ ] Existing: every test still passes; the architecture tests now cover Rendering (they fail if a Windows `using` is left in Rendering)
- [ ] `./eng/Test-Packages.ps1` passes (the WinUI package test app still draws through Win2D)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — succeeds, 0 AOT warnings (Win2D still found at run time)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any Avalonia code, moving other WinUI files (P15-02), CI on other OSes (P15-04).

## Suggested commit message

`refactor(rendering): make LoomEditor.Rendering portable and move the Win2D backend into LoomEditor.WinUI`
