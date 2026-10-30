# P13-10 — Native AOT self-check and package test of the host API

- **Status:** todo
- **Depends on:** P13-09
- **Size:** M

**Recommended model:** Sonnet

## Goal

Two proofs that the problem VIGO hit cannot come back: (1) `--hostapi-selfcheck` runs the host API logic in the published Native AOT Gallery; (2) the **package test app**,
which references only the NuGet packages, calls every new public member once — if one is not public in the package, the package test fails to compile.

## Sources

Sources: none (specified in this card). Read first: `samples/LoomEditor.Gallery/CommentsSelfCheck.cs` (`Run`, `RunToFile`, `Check`, `Require`, `Names`, the result record),
`MainWindow.CommentsCheck.cs` (the command-line switch), `tests/LoomEditor.View.Tests/Comments/CommentsSelfCheckTests.cs` and the `<Compile Include=…Link=…>` item of
`tests/LoomEditor.View.Tests/LoomEditor.View.Tests.csproj`, `docs/tasks/phase-12/P12-14-native-aot-self-check.md` (publish / run commands, Result section format),
`eng/Test-Packages.ps1`, `eng/package-test/LoomEditor.PackageTest/MainWindow.xaml(.cs)`, `docs/RELEASING.md` (how the package test is run).

## Files to create or change

- `samples/LoomEditor.Gallery/HostApiSelfCheck.cs` — create (WinUI-free)
- `samples/LoomEditor.Gallery/MainWindow.HostApiCheck.cs` — create (the `--hostapi-selfcheck=<file>` switch; call its `Initialize…` where the other self-checks are initialized)
- `tests/LoomEditor.View.Tests/LoomEditor.View.Tests.csproj` — change (link `HostApiSelfCheck.cs` and `HostApiSamples.cs`)
- `tests/LoomEditor.View.Tests/Decorations/HostApiSelfCheckTests.cs` — create
- `eng/package-test/LoomEditor.PackageTest/MainWindow.xaml.cs` — change

## Design decisions (already made — do not change)

- **Self-check** (same shape as `CommentsSelfCheck`: `Run()` → results, `RunToFile(path)` → failures, report title `LoomEditor host API self-check`, last line
  `N of N checks passed`). Five checks, each a private static method returning a one-line success text, failing through `Require`:
  1. `"language"`: `GetLanguageIdByFileName` for `Program.cs` → `csharp`, `src/tsconfig.json` → `jsonc`, `HostApiSamples.FileName` → `csharp`, `Makefile` → `null`.
  2. `"decoration-options"`: an `EditorDecorationOptions` with a class, `IsWholeLine` and an overview ruler color converts to internal options with those values,
     `NeverGrowsWhenTypingAtEdges`, ruler value `#RRGGBBAA`; a second call returns the same instance.
  3. `"decorations-collection"`: over a `TextModel` of `HostApiSamples.Text`, a `DecorationsCollection` (internal constructor; the self-check may use internals, the demo
     window may not) `Set`s the two conflict ranges, an inserted line at the top moves both down, `Clear` empties it.
  4. `"custom-style"`: `CustomDecorationStyles.Register("selfcheck-style", …)` → `DecorationStyles.Get` resolves light and dark backgrounds; a built-in name throws
     `ArgumentException`; `Unregister` → `true` then `false`.
  5. `"blame-data"`: `HostApiSamples.Blame` returns a commit for every line of `HostApiSamples.Text` and the commit changes every 7 lines.
- No reflection, no `IEnumerable<T>` collection expressions (CsWinRT analyzer note of P11-24).
- **Publish and run** as P12-14: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror`, then the published exe with
  `--hostapi-selfcheck=<report file>` (exit code 0, last line `5 of 5 checks passed`), and `--comments-selfcheck`, `--text-selfcheck`, `--diff-selfcheck` (unchanged results).
  Copy the last lines into the card's Result section, verbatim.
- **Package test app**: in `MainWindow`, after the text is set, one private method `UseHostApi()` that touches each new public member once, **without changing what the
  window shows**: `BuiltInLanguages.GetLanguageIdByFileName("Program.cs")` → `Editor.LanguageId`; `CustomDecorationStyles.Register("packagetest-style", new() { Background =
  … })`; `Editor.CreateDecorationsCollection([new EditorDecoration(new Range(1, 1, 1, 1), new EditorDecorationOptions { ClassName = "packagetest-style", IsWholeLine = true })])`
  then `Clear()`; `Editor.ScrollChanged` / `LayoutChanged` / `GutterClicked` subscribed to empty handlers that read `e.ScrollTop`, `Editor.LineHeight`,
  `Editor.GetTopForLineNumber(1, true)`, `e.LineNumber`, `e.Area`; `Editor.LineNumbersEnabled = true`; `Editor.SetHiddenAreas([])`; on `Loaded`:
  `Editor.RemoveViewZone(Editor.AddViewZone(0, 1))` and `Editor.ResetText(Editor.Model.GetValue())`. A comment says why the method exists (phase 13: a member that is not
  public in the package fails this build).
- Run the package test: `./eng/Test-Packages.ps1` (it packs, publishes the app with Native AOT from the packages and smoke-tests it). Copy its last line (`Package test passed:
  <version>`) into the Result section. If the environment cannot run it (no Windows App SDK runtime, for example), say so in the Result section and leave the box unticked —
  never tick it without the run.

## Tests

- [ ] New (`HostApiSelfCheckTests`, as `CommentsSelfCheckTests`): every check passes in-process (`[Theory]` over `HostApiSelfCheck.Names`).
- [ ] Native AOT publish succeeds with `-warnaserror`; the exe prints `5 of 5 checks passed` (result copied in the card)
- [ ] `./eng/Test-Packages.ps1` passes (result copied in the card)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` — succeeds; `--hostapi-selfcheck` exits 0 with `5 of 5 checks passed`
- [ ] `./eng/Test-Packages.ps1` — `Package test passed`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

A WinUI test project, UI automation of the demo window, publishing packages.

## Suggested commit message

`test: Native AOT self-check and package test of the host API`
