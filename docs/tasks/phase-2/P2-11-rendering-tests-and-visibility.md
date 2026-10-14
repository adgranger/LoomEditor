# P2-11 — Rendering test project and internal visibility

- **Status:** done
- **Depends on:** P2-10
- **Size:** S

## Goal

Two preparations before the renderer is rewritten. First, a test project for `LoomEditor.Rendering`, with a fake
`IRenderSurface` that records draw calls, and characterization tests of today's renderer. Second, the visibility
rules of ADR 0005 (decision 8): the layout and rendering classes become `internal`, and our own assemblies see them
through `InternalsVisibleTo`.

## Monaco sources

None.

## Files to create or change

- `src/LoomEditor.View/LoomEditor.View.csproj` — add `<InternalsVisibleTo Include="LoomEditor.Rendering" />` and `<InternalsVisibleTo Include="LoomEditor.WinUI" />`
- `src/LoomEditor.Rendering/LoomEditor.Rendering.csproj` — add `<InternalsVisibleTo Include="LoomEditor.WinUI" />`
- `src/LoomEditor.Rendering/**/*.cs` — `public` → `internal` for `IRenderSurface`, `FontMetrics`, `TextViewRenderer`, `Win2DFont`, `Win2DRenderSurface`
- `src/LoomEditor.Rendering/PublicAPI.Unshipped.txt` — remove every line except `#nullable enable`
- `tests/LoomEditor.Rendering.Tests/LoomEditor.Rendering.Tests.csproj` — create; add it to `LoomEditor.slnx` (folder `/tests/`)
- `tests/LoomEditor.Rendering.Tests/TestHelpers/RecordingRenderSurface.cs` — create
- `tests/LoomEditor.Rendering.Tests/TextViewRendererTests.cs` — create
- `docs/ARCHITECTURE.md` — rule 7: add one sentence: "`LoomEditor.View` and `LoomEditor.Rendering` share internals with the assemblies above them (ADR 0005)"

## Design decisions (already made — do not change)

- The test project targets `$(LoomWindowsTargetFramework)` and references `LoomEditor.Rendering` and xUnit v3, like
  `LoomEditor.View.Tests` (same `Using` items, including the `Range` alias). It gets the internals of Rendering through
  the existing `$(MSBuildProjectName).Tests` rule of `src/Directory.Build.props`.
- Tests must not create any Win2D object (no graphics device in CI). They use `RecordingRenderSurface`, which stores
  every call as a record (`FillRectangle(x, y, w, h, color)`, `DrawText(text, x, y, color)` …) in a list.
- If the test project needs extra MSBuild properties to build or run (e.g. `Win2DWarnNoPlatform`, a platform, or
  `WindowsAppSDKSelfContained`), add the minimum and explain each one in an XML comment. If the tests cannot run
  under `dotnet test --solution LoomEditor.slnx -c Release` on an AnyCPU solution build, **stop** and report: the card
  must then change (plan B: move the renderer's drawing decisions to `LoomEditor.View`).
- No behavior change in this card. The Gallery must look exactly the same.

## Tests

Characterization tests of the phase 0 renderer, so that P2-12…P2-16 change behavior on purpose only:

- [x] New: `Render` clears with `Theme.Background` first
- [x] New: 3 lines → 3 line numbers right-aligned and 3 text draws at `y = 0, LineHeight, 2 × LineHeight`
- [x] New: an empty selection draws no selection rectangle; a two-line selection draws two rectangles
- [x] New: `GetGutterWidth(1000 lines, CharacterWidth 10)` is `(4 + 3) × 10`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green, and the new project runs in it
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts and looks the same
- [x] Architecture tests still green (they check project references, not visibility)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any change to what is drawn (P2-12 onwards).

## Suggested commit message

`test(rendering): add the rendering test project and share internals`
