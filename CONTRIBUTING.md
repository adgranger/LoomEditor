# Contributing to LoomEditor

Thank you! This project is designed so that **an intermediate C# developer can contribute without knowing Monaco,
DirectWrite or balanced trees**. If something is hard to understand, that is a bug in the code or the docs:
please open an issue.

## Getting started (10 minutes)

1. Install the .NET 11 SDK (version in `global.json`) and Visual Studio 2026 / Rider / VS Code with C# Dev Kit.
2. Build and test:
   ```powershell
   dotnet build LoomEditor.slnx -c Release
   dotnet test --solution LoomEditor.slnx -c Release
   ```
3. Run the demo: `dotnet run --project samples/LoomEditor.Gallery -p:Platform=x64`.
4. Read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) (5 minutes) and skim [docs/GLOSSARY.md](docs/GLOSSARY.md).

## Picking something to work on

Work is split into small **task cards** in [docs/tasks/](docs/tasks/README.md): each one says which Monaco lines to
port, which files to create, which decisions are already made and which tests to write. Pick the next `todo` card
whose dependencies are `done`. Coding agents follow [CLAUDE.md](CLAUDE.md).

## Where is…?

See the "Where do I find…?" table in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Most changes touch **one folder**:
a feature folder in `src/LoomEditor.View/Features/`, or one folder of `LoomEditor.Core`.

## Rules

### Code style
- Enforced by `.editorconfig` and analyzers. **Release builds treat warnings as errors** (so does CI):
  run `dotnet build -c Release` before pushing.
- File-scoped namespaces, braces always, private fields `_camelCase`, `var` only when the type is obvious.
- **Short methods; one class = one responsibility; about 400 lines per file at most** (direct ports of big Monaco
  files may be longer — split them with `partial` by concern).
- **No obscure abbreviations** in names (`lineNumber`, not `ln`), except terms from the glossary.
- **Non-obvious optimizations** (pooling, `Span<T>`, `unsafe`, caching) must have a comment saying *why*, and a
  benchmark in `tests/LoomEditor.Benchmarks` that proves it.
- Every public member has an XML doc comment (`<summary>`), including its **threading contract** for classes.

### Architecture
- Respect the dependency direction `WinUI → Rendering → View → Core` (checked by tests).
- **No run-time reflection** (`dynamic`, `Activator`, assembly scanning, reflection-based JSON). Use source
  generators. Native AOT must keep working: CI publishes the Gallery with `PublishAot=true` and fails on any warning.
- Everything is `internal` unless it must be public. New public API is listed in `PublicAPI.Unshipped.txt`:
  run `./eng/Update-PublicApi.ps1 src/<Project>` and **review the diff**.

### Porting from Monaco
Follow [docs/PORTING.md](docs/PORTING.md): file header with the upstream commit, port the tests (keep their names),
then the code; update the Monaco → LoomEditor map.

### Tests
- Every bug fix comes with a test that fails without the fix.
- Test names: `Method_Scenario_ExpectedResult`.

## Commits and pull requests

- **Small, atomic commits** using [Conventional Commits](https://www.conventionalcommits.org/):
  `feat(core): add Position and Range`, `fix(view): keep selection direction`, `docs: explain view lines`,
  `test(core): port range.test.ts`, `build: pin Windows App SDK 2.5.1`, `ci: ...`, `refactor(...)`, `perf(...)`.
  Scopes: `core`, `textmate`, `themes`, `view`, `rendering`, `winui`, `gallery`, `native`, `docs`, `ci`, `build`.
- Code, comments, documentation and commit messages are in **English**.
- One pull request = one topic. Fill in the pull request template.
- Big or architectural changes: open an issue first, and write an ADR in `docs/adr/` (copy an existing one).

## License

By contributing, you agree that your contributions are licensed under the MIT License.
Code ported from Monaco stays under its original MIT license and copyright (see `NOTICE.md`).
