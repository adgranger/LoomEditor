# P4-01 — Spike: embedded resources and a native DLL in the Native AOT Gallery

- **Status:** todo
- **Depends on:** P3-55
- **Size:** S (throw-away probe + a findings section in ADR 0008)

## Goal

Before any phase 4 code, check two assumptions of ADR 0008 in the **Native AOT** Gallery: (1) an embedded resource of a
library (`LoomEditor.TextMate`) can be read with `GetManifestResourceStream` (ADR 0001, known issue 2, saw it fail once
without investigation); (2) a native DLL shipped by a library (`onig.dll`) is copied to the publish folder and can be
loaded. The agent can do this card alone: the probe writes its results to a file and the app exits by itself.

## Sources

None to port. References: MS Learn "Native AOT deployment" (limitations), `src/native/oniguruma/README.md`,
ADR 0001 section 2.

## Files to create or change

- `src/LoomEditor.TextMate/Probe/probe.json` and an `<EmbeddedResource Include="Probe/probe.json" LogicalName="LoomEditor.TextMate.Probe.probe.json" />` — **temporary**
- `src/LoomEditor.TextMate/Probe/ResourceProbe.cs` (internal static, exposed to the Gallery with a temporary
  `InternalsVisibleTo`) — **temporary**
- `samples/LoomEditor.Gallery/App.xaml.cs` — temporary call when the environment variable `LOOM_PROBE` is set
- `docs/adr/0008-syntax-highlighting.md` — change: add "Findings of the AOT spike (P4-01)" under decision 7

## What the probe does

1. Reads the embedded resource by its `LogicalName` with `typeof(ResourceProbe).Assembly.GetManifestResourceStream`,
   and also lists `GetManifestResourceNames()`.
2. Builds `onig.dll` with `./src/native/oniguruma/build.ps1 -Architecture x64`, adds it to the TextMate project as
   `<None Include="…/artifacts/native/win-x64/onig.dll" Pack="true" PackagePath="runtimes/win-x64/native" CopyToOutputDirectory="PreserveNewest" />`
   (temporary item), and calls `NativeLibrary.TryLoad("onig.dll", typeof(ResourceProbe).Assembly, null, out _)`
   then `onig_version` through a `[LibraryImport]` (temporary).
3. Writes `resource: <length or null>`, the resource names, `onig: <version or error>` to
   `%TEMP%/loom-probe.txt`, then calls `Application.Current.Exit()`.

Run: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64`, then start the published exe with
`LOOM_PROBE=1` and read the file. Also run it once with `dotnet run` (not AOT) to compare.

## Questions to answer (write each answer in the ADR)

- [ ] Under Native AOT, does `GetManifestResourceStream` return the resource? If not: is the resource in the binary
      (`GetManifestResourceNames`)? Does the ILC need `<IlcKeepManifestResources>` or similar? (Check the ILC docs/props
      in the `Microsoft.DotNet.ILCompiler` package before guessing.)
- [ ] Is `onig.dll` in the publish folder? Does `NativeLibrary.TryLoad` find it, and does `onig_version()` return `6.9.10`?
- [ ] Publish warnings: 0?

## Acceptance criteria

- [ ] The ADR section lists the answers, with the exact MSBuild items/properties that made it work
- [ ] If embedded resources cannot work under AOT, **stop** and propose the fallback of ADR 0008 decision 7 to the
      maintainer (changes to P4-34, P4-35, P4-41) before going on
- [ ] The probe files, the temporary items and the temporary call are deleted; `git status` shows only the ADR change
- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings; `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Shipping `onig.dll` for real (P4-02), the interop (P4-03).

## Suggested commit message

`docs(adr): record the findings of the Native AOT resources spike`
