# P4-48 — Golden tests (2/2): compare our tokens with vscode-textmate for ten languages

- **Status:** done
- **Depends on:** P4-47
- **Size:** M (test code; fix the port until green)

## Goal

The phase 4 "done" criterion: for each of the ten samples, our scopes and our Dark Modern / Light Modern colors equal
the golden files produced by the real vscode-textmate.

## Sources

The golden files of P4-47. The port under `src/LoomEditor.TextMate/` and `src/LoomEditor.Themes/`.

## Files to create or change

- `tests/LoomEditor.TextMate.Tests/Golden/GoldenTests.cs` — create (a theory, one row per language)
- `tests/LoomEditor.TextMate.Tests/Golden/GoldenFile.cs` — create (reads a `.tokens.json` with `JsonDocument`)
- production code — fix what the tests reveal

## Design decisions (already made — do not change)

- The test uses the product path: `BuiltInLanguages` + `TMGrammarFactory` (P4-34) + `EditorTheme.BuiltIn` → raw
  theme (P4-42's `ThemeToTextMate`, or the same code moved to a place the test can reach) — not test-only shortcuts.
- A failure message names the sample, the line number, the line text, and the first differing token (expected vs actual).
- Never edit a golden file by hand. A difference is a bug of the port until proven otherwise. If it comes from the
  engine version (ADR 0007, decision 6: 6.9.10 vs 6.9.8), **stop** and show the maintainer the line and both outputs;
  only with the maintainer's agreement, list it in `docs/PORTING.md` ("Behavior differences") and in
  `Golden/known-differences.json` (`{ language, line, reason }`), which the test reads to skip exactly those lines.

## Tests

- [x] New: `Golden_Scopes(language)` and `Golden_Colors(language, theme)` for the ten languages and the two themes

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: bugs found by the golden tests, known differences (if any, agreed)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

More languages (one line in `add-a-language.md` says how to add one).

## Suggested commit message

`test(textmate): golden tests for ten languages`
