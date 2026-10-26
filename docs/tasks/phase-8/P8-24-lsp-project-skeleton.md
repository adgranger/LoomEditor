# P8-24 — The `LoomEditor.Lsp` project, its test projects and the architecture rules

- **Status:** done
- **Depends on:** P8-23
- **Size:** S

## Goal

The solution has the new project `LoomEditor.Lsp` (references only Core), the test project `LoomEditor.Lsp.Tests`, the empty `LoomEditor.Lsp.FakeServer` console app, and the architecture tests and `ARCHITECTURE.md` know about them. No protocol code yet.

## Monaco sources

Sources: none (specified in this card; ADR 0011 decisions 2 and 10).

## Files to create or change

- `src/LoomEditor.Lsp/LoomEditor.Lsp.csproj`, `PublicAPI.Shipped.txt` (`#nullable enable`), `PublicAPI.Unshipped.txt` (`#nullable enable`), `README.md`, `LspProtocolVersion.cs` — create
- `tests/LoomEditor.Lsp.FakeServer/LoomEditor.Lsp.FakeServer.csproj`, `Program.cs` — create
- `tests/LoomEditor.Lsp.Tests/LoomEditor.Lsp.Tests.csproj`, `TestHelpers/MemoryPipe.cs`, `LspProtocolVersionTests.cs`, `TestHelpers/MemoryPipeTests.cs` — create
- `LoomEditor.slnx` — change: the three projects
- `tests/LoomEditor.Architecture.Tests/DependencyRulesTests.cs`, `PlatformNeutralSourceTests.cs`, `LoomEditor.Architecture.Tests.csproj` — change
- `docs/ARCHITECTURE.md` — change (layer table row, see below)

## Design decisions (already made — do not change)

- **`LoomEditor.Lsp.csproj`** — same shape as `LoomEditor.View.csproj`: `TargetFramework` `$(LoomNetTargetFramework)`, `RootNamespace` `LoomEditor.Lsp`, a `Description` ("Language Server Protocol client of LoomEditor: JSON-RPC over stdio, document sync and the mapping to the editor's language features, independent of any UI framework."), the `Range` using alias
  (`<Using Include="LoomEditor.Core.Text.Range" Alias="Range" />`), **one** `ProjectReference` to `..\LoomEditor.Core\LoomEditor.Core.csproj`, and `<InternalsVisibleTo Include="LoomEditor.Lsp.FakeServer" />` (the test double uses the internal protocol types; the `*.Tests` assembly already gets it from `src/Directory.Build.props`).
  No package references (`System.Text.Json` is part of the shared framework). `src/Directory.Build.props` already adds the AOT analyzers, the public API analyzer, and `BannedSymbols.txt`.
- **`LspProtocolVersion`**: `internal static class LspProtocolVersion { public const string Version = "3.17"; }` with an XML doc "the version of the specification this client is written against" — only so the project has a compilable file and a first test.
- **`LoomEditor.Lsp.FakeServer.csproj`**: `OutputType` `Exe` (set by `tests/Directory.Build.props` already; write it explicitly too), `TargetFramework` `$(LoomNetTargetFramework)`, `RootNamespace` `LoomEditor.Lsp.FakeServer`, `IsAotCompatible` true, `EnableTrimAnalyzer` true, `EnableAotAnalyzer` true, `InvariantGlobalization` true, `ProjectReference` to `LoomEditor.Lsp` and to `LoomEditor.Core`,
  `<IsPackable>false</IsPackable>`. `Program.cs`: `return 0;` with a comment `// P8-28 writes the server loop`. It must build under `PublishAot` with no warning: `dotnet publish tests/LoomEditor.Lsp.FakeServer -c Release -r win-x64 -p:PublishAot=true -warnaserror` is an acceptance command **of P8-27 and P8-28**, not of this card, but run it once here and report (the empty program must publish).
- **`LoomEditor.Lsp.Tests.csproj`**: like `LoomEditor.View.Tests.csproj` (xunit.v3, the two Microsoft.Testing extensions, `Using Xunit`, the `Range` alias) with `ProjectReference`s to `LoomEditor.Lsp`, `LoomEditor.Core` and `..\LoomEditor.Lsp.FakeServer\LoomEditor.Lsp.FakeServer.csproj` (a normal reference: the fake server's assembly is copied next to the tests so that it can be started with `dotnet LoomEditor.Lsp.FakeServer.dll` and used in process).
- **`MemoryPipe`** (test helper, README §6): `internal sealed class MemoryPipe : IDisposable` giving a client and a server two connected streams each: `Stream ClientInput` (what the client reads = what the server writes), `Stream ClientOutput`, `Stream ServerInput`, `Stream ServerOutput`.
  Build it on **anonymous pipes** (`System.IO.Pipes`, part of the shared framework: no package): for each direction one `AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None)` (the writer) and one `AnonymousPipeClientStream(PipeDirection.In, writer.ClientSafePipeHandle)` (the reader); `ClientOutput` + `ServerInput` are one direction, `ServerOutput` + `ClientInput` the other.
  Closing a writer completes the matching reader (a read returns 0). `Dispose` disposes all four. (Do not use `System.IO.Pipelines`: it is not part of the base shared framework and the repository has no package reference for it.) Tests: `ClientWrite_ServerReads` (bytes written to `ClientOutput` appear on `ServerInput`), `ServerWrite_ClientReads`, `CloseClientOutput_ServerReadsZero`, `DisposeCompletesReaders`, `LargeWrite_IsReadInChunks` (1 MiB written by a task while the test reads: no deadlock, all bytes arrive).
- **Architecture tests**: `DependencyRulesTests.AllowedReferences` gets `["LoomEditor.Lsp"] = ["LoomEditor.Core"]`; `UiIndependentProjects` and `PlatformNeutralSourceTests.PortableProjects` get `"LoomEditor.Lsp"`; `LoomEditor.Architecture.Tests.csproj` gets the `ProjectReference` to `LoomEditor.Lsp` (the assembly rule loads it with `Assembly.Load`).
  `ConventionTests.SourceProjects_TrackTheirPublicApi` already iterates every project: the two `PublicAPI.*.txt` files satisfy it. The comment above `AllowedReferences` keeps "you are changing the architecture: update docs/ARCHITECTURE.md and write an ADR": add "(ADR 0011 added LoomEditor.Lsp)".
- **`docs/ARCHITECTURE.md`**: in the layer diagram add a box to the right of View's row? No: **do not redraw the diagram**; add a sentence under it ("`LoomEditor.Lsp` (language server client) references Core only and sits beside View: the Gallery wires it to the control; ADR 0011") and one row to the project table: `| LoomEditor.Lsp | LSP client: JSON-RPC over stdio, document sync, mapping to the language features | Core |`.
  The phase 8 section of the document is written by P8-39.
- `README.md` of the project: role, the folder plan (`JsonRpc/`, `Protocol/`, `Client/`, `Editor/`), rules (Core only, UI-thread rule of ADR 0011, no `await` outside this project's own code), files.

## Tests

- [x] New (`LspProtocolVersionTests`): `Version_Is317`
- [x] New (`MemoryPipeTests`): the five above
- [x] Existing architecture tests: `EverySourceProject_IsListedInTheRules`, `Project_ReferencesOnlyAllowedProjects(LoomEditor.Lsp)`, `UiIndependentProject_TargetsPlainDotNet(LoomEditor.Lsp)`, `UiIndependentProject_DoesNotReferenceUiPackages(LoomEditor.Lsp)`, `UiIndependentAssembly_ReferencesOnlyDotNetAndLoomEditor(LoomEditor.Lsp)`, `PortableProject_SourceFiles_DoNotUseWindowsApis(LoomEditor.Lsp)` run for the new project and pass (they are theories over the lists you changed)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish tests/LoomEditor.Lsp.FakeServer -c Release -r win-x64 -p:PublishAot=true -warnaserror` — succeeds; run the produced exe once, it exits with code 0 (report both)
- [x] `docs/ARCHITECTURE.md` updated as above
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any protocol, JSON or client code (P8-25 …), the fake server's behavior (P8-28), the phase 8 section of `ARCHITECTURE.md` (P8-39).

## Suggested commit message

`feat(lsp): add the LoomEditor.Lsp project, its test projects and the architecture rules`
