# P16-01 — `src/LoomEditor.HostShared`: shared host source, its tests, the comment event args

- **Status:** todo
- **Depends on:** P15-34
- **Size:** M

**Recommended model:** Sonnet

## Goal

A source folder compiled into both control assemblies exists, with its props file, the `LOOM_WINUI` / `LOOM_AVALONIA` symbols and the architecture tests of
ADR 0017 decision 10. The first two shared files are the public comment event args, which were copied in phase 15.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/LoomEditor.WinUI.csproj`, `src/LoomEditor.Avalonia/LoomEditor.Avalonia.csproj`, `tests/LoomEditor.Architecture.Tests/*.cs` (style of
`ConventionTests` and `PlatformNeutralSourceTests`), `samples/LoomEditor.Gallery.Avalonia/LoomEditor.Gallery.Avalonia.csproj` (how linked files are written today).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/Comments/CommentEventArgs.cs` | 7+106 | `CommentThreadEventArgs`, `CommentSubmittedEventArgs`, `AddCommentRequestedEventArgs`, `SuggestionAppliedEventArgs` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/DiffAddCommentRequestedEventArgs.cs` | 6+48 | `DiffAddCommentRequestedEventArgs` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/LoomEditor.HostShared.props` — create:
  ```xml
  <Project>
    <!-- Shared host source (ADR 0017): compiled into LoomEditor.WinUI and LoomEditor.Avalonia, each in its own namespace. -->
    <ItemGroup>
      <Compile Include="$(MSBuildThisFileDirectory)**\*.cs" Exclude="$(MSBuildThisFileDirectory)bin\**;$(MSBuildThisFileDirectory)obj\**" LinkBase="HostShared" />
    </ItemGroup>
  </Project>
  ```
- `src/LoomEditor.HostShared/README.md` — create: what the folder is, the header block, the host-member rule, a link to ADR 0017 (≤ 20 lines)
- `src/LoomEditor.WinUI/LoomEditor.WinUI.csproj` — `<Import Project="..\LoomEditor.HostShared\LoomEditor.HostShared.props" />`;
  `<DefineConstants>$(DefineConstants);LOOM_WINUI</DefineConstants>`
- `src/LoomEditor.Avalonia/LoomEditor.Avalonia.csproj` — the same with `LOOM_AVALONIA`
- `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs` → `git mv` to `src/LoomEditor.HostShared/Controls/Comments/CommentEventArgs.cs`, header block of the phase README; `src/LoomEditor.Avalonia/Controls/Comments/CommentEventArgs.cs` — delete
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffAddCommentRequestedEventArgs.cs` → `git mv` to `src/LoomEditor.HostShared/Controls/DiffEditor/DiffAddCommentRequestedEventArgs.cs`; the Avalonia twin — delete
- `tests/LoomEditor.Architecture.Tests/HostSharedSourceTests.cs` — create (tests below)
- `tests/LoomEditor.Architecture.Tests/PlatformNeutralSourceTests.cs` — add `"LoomEditor.HostShared"` to `PortableProjects`
- `docs/ARCHITECTURE.md` — a short "Shared host source" section (where, header block, host members, link to ADR 0017)
- `CLAUDE.md` — one line under "Architecture rules": "Code both controls need and that names a host type lives once in `src/LoomEditor.HostShared/` (ADR 0017); the
  framework-specific part goes to `<File>.Host.cs` in each host."

## Design decisions (already made — do not change)

- The folder has **no project file**; `RepositoryLayout.SourceProjects` (csproj under `src/`) does not see it. If another architecture test enumerates folders
  of `src/` and fails on it, make that test skip `LoomEditor.HostShared` with a one-line comment, and say so in the Result section.
- Header block exactly as in the phase README (`#if LOOM_WINUI` / `#elif LOOM_AVALONIA` / `#else` + `#error` / `#endif`). Inside it: only `using LoomEditor.WinUI…;`,
  `using LoomEditor.Avalonia…;` and the two `namespace` lines.
- The two files keep the WinUI XML doc (the Avalonia twins only changed `…` into `...`). The local `using Range = …` alias goes: both projects have the global alias.
- Both `PublicAPI.Unshipped.txt` files stay unchanged (each assembly still declares the types in its own namespace).

## Tests

- [ ] New (`HostSharedSourceTests`): `HostSharedFiles_HaveOneHeaderBlock` (the `#if LOOM_WINUI … #endif` block exists once, before the first type declaration, and
  holds only the allowed lines); `HostSharedFiles_HaveNoOtherConditionalCode` (no other `#if`, `#elif`, `#define`); `HostSharedNamespaces_MatchTheFolder`
  (`Controls/DiffEditor/X.cs` → `LoomEditor.WinUI.Controls.DiffEditor` and `LoomEditor.Avalonia.Controls.DiffEditor`); `HostSharedFiles_UseNoFrameworkNamespace`
  (regex `(?<![\w.])(Avalonia|Microsoft\.UI|Windows)\.` finds nothing, so `global::Avalonia.` is caught and `LoomEditor.Avalonia.` is not);
  `HostProjects_ImportHostSharedProps` (both csproj import the props file and define their symbol)
- [ ] New: `HeaderBlockChecker_RejectsAStatementInsideTheBlock`, `HeaderBlockChecker_AcceptsTheReadmeExample` (the checker is a small static method tested on inline
  strings, as `PortedHeader_*` in `ConventionTests`)
- [ ] The Avalonia headless tests stay green unchanged

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `HostSharedSourceTests` green; the two event-args files exist once, under `src/LoomEditor.HostShared/`
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any other shared file (P16-02 onwards). Sonar exclusions. Galleries.

## Suggested commit message

`build: add src/LoomEditor.HostShared, compiled into both controls (ADR 0017)`
