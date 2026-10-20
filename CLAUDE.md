# LoomEditor — instructions for coding agents

LoomEditor is a C# port of the Monaco editor (`microsoft/vscode`, `src/vs/editor`) as a native, Native AOT-compatible
WinUI 3 control on .NET 11. Read `docs/ARCHITECTURE.md` once; this file is the short version.

## How work is organized

- Work is split into **task cards**: `docs/tasks/phase-<n>/P<n>-<nn>-<name>.md`. Index: `docs/tasks/README.md`.
- **Do one task card at a time**, and only what the card says. If the card is ambiguous or wrong, stop and ask;
  do not redesign. Decisions listed under "Design decisions (already made)" in a card are final.
- When you finish a card: tick its checkboxes, set `Status: done`, update the status column in `docs/tasks/README.md`,
  and update the Monaco → LoomEditor map in `docs/PORTING.md` if you ported a file.
- **Commits:** only commit when running the `/next-task` skill (`.claude/skills/next-task/SKILL.md`), one commit per
  card. Otherwise do not commit: propose ONE commit message. Either way: Conventional Commits, English,
  **no `Co-Authored-By` line and no other trailer** (also enforced by `.claude/settings.json` → `attribution`).
- **Never `git push`**, never bypass hooks or GPG signing (`--no-verify`, `--no-gpg-sign`).

## Language

Everything written in the repository is in **English**: code, comments, docs, test names, commit messages.

## Commands (run from the repository root)

```powershell
dotnet build LoomEditor.slnx -c Release                    # MUST pass: warnings are errors in Release
dotnet test --solution LoomEditor.slnx -c Release          # MUST pass
dotnet test --project tests/LoomEditor.Core.Tests -c Release   # faster loop while working on Core
./eng/Update-PublicApi.ps1 src/LoomEditor.Core             # after adding PUBLIC members (prefer internal!)
dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64   # Native AOT check (only if the card asks)
```

A task is done only when the Release build has **0 warnings** and **all tests pass**.

**rtk:** run shell commands through `rtk` (e.g. `rtk dotnet test ...`). If `rtk dotnet test` crashes (e.g. "Zero tests ran"),
select the VSTest runner and run it again (PowerShell: `$env:DOTNET_TEST_RUNNER = 'VSTest'`; bash: `export DOTNET_TEST_RUNNER=VSTest`):

```powershell
$env:DOTNET_TEST_RUNNER = 'VSTest'
rtk dotnet test --solution LoomEditor.slnx -c Release
```

## Porting rules (see docs/PORTING.md)

- Read Monaco at the pinned commit: `https://raw.githubusercontent.com/microsoft/vscode/0c9c0a4295a9222e12a605537738719455ae3056/<path>`
  (e.g. `<path>` = `src/vs/editor/common/core/range.ts`). Cards give exact files and line numbers. Do not port from memory.
- Header of every ported file (checked by an architecture test):
  ```csharp
  // Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
  // Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
  ```
- **Port the tests first, then the code.** Keep each Monaco test name in a comment above the test: `// Monaco: 'basic insert/delete'`.
- Translate faithfully; keep Monaco's algorithm and concept names. Do not optimize while porting.
- Any deliberate behavior difference: document it in the member's XML doc and in `docs/PORTING.md` ("Behavior differences").

## Code rules

- `internal` by default. Only make something `public` if the card says so.
- File-scoped namespaces; namespace = project + folder (`LoomEditor.Core.Model.PieceTree`).
- Braces always; private fields `_camelCase`; private constants and `static readonly` fields `PascalCase`.
- XML doc on every public member, and a threading note on every class (`<remarks>Threading: ...</remarks>`).
- ~400 lines max per file; split big ports with `partial` files named by concern (`PieceTreeBase.Insert.cs`).
- No reflection, no `dynamic`, no `Activator`, no reflection-based JSON (Native AOT). Banned APIs fail the build.
- `Range` means `LoomEditor.Core.Text.Range` (global alias in each `.csproj`), never `System.Range`.
- Use `Guard.Assert(...)` (Debug-only) for internal invariants; throw `ArgumentException`s for bad arguments.

## Tests

- xUnit v3. Test project mirrors `src/` (`tests/LoomEditor.Core.Tests/Model/PieceTree/...`).
- Names: `Method_Scenario_ExpectedResult` for new tests; ported tests may use a short descriptive name + the `// Monaco:` comment.
- Randomized tests use `new Random(seed)` with the seed in the failure message. Never `Random.Shared`.
- Test helpers go in a `TestHelpers/` folder next to the tests that use them.

## Architecture rules (enforced by tests in tests/LoomEditor.Architecture.Tests)

`WinUI → Rendering → View → (TextMate, Themes) → Core`. Core/TextMate/Themes/View know nothing about Windows or UI.
Features live in `src/LoomEditor.View/Features/<Name>/` and are listed in `EditorFeatures.cs`.
