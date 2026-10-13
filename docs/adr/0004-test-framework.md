# ADR 0004 — Test framework: xUnit v3 on Microsoft.Testing.Platform

- **Status:** accepted
- **Date:** 2026-10-13

## Options considered

| | xUnit v3 (4.0.2) | MSTest (4.x) | TUnit |
|---|---|---|---|
| Runs on Microsoft.Testing.Platform (the .NET 11 `dotnet test` mode) | yes | yes | yes |
| Familiar to most C# developers | **very** | yes | less |
| Data-driven tests (`[Theory]`) — needed to port Monaco's table-like tests | yes | yes | yes |
| Source-generated discovery | no (reflection) | partly | yes |

## Decision

**xUnit v3** with `Microsoft.Testing.Platform` (test projects are executables, `global.json` selects the runner).

- It is the most widely known .NET test framework: best fit for "lambda" contributors.
- Reflection-based discovery is fine: **test projects are not AOT-compiled**; only `src/` must be AOT-safe.
- Monaco test suites (`suite`/`test` + `assert.strictEqual`) map one-to-one to classes/`[Fact]`/`Assert.Equal`;
  assertion tables map to `[Theory]` + `[InlineData]`.
- Code coverage: `Microsoft.Testing.Extensions.CodeCoverage` (`dotnet test ... --coverage`).

## Conventions

- Test name: `Method_Scenario_ExpectedResult` (underscores allowed in tests only).
- A test ported from Monaco keeps the original name in a comment right above it: `// Monaco: 'swap start and stop'`.
- Test project layout mirrors `src/` (e.g. `tests/LoomEditor.View.Tests/Features/SelectAll/`).
