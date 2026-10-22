# P6-01 — Wrapping option types: `WrappingIndent`, `WordWrap`, `WordBreak`, `EditorWrappingOptions`

- **Status:** todo
- **Depends on:** P5-96
- **Size:** S

## Goal

The word-wrap settings exist as plain types in `LoomEditor.View/Layout/` (no behavior yet), so the next cards (layout,
line-break computer, view model) share one definition.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/config/editorOptions.ts`.

| Lines | What to port |
|---|---|
| 335–394 | the doc comments and defaults of `wordWrap`, `wordWrapOverride1/2` (not ported, see decisions), `wordWrapColumn`, `wordWrapIndicator`, `wrappingIndent`, `wordWrapBreakBeforeCharacters`, `wordWrapBreakAfterCharacters`, `wordBreak`, `wrapOnEscapedLineFeeds` |
| 5605–5625 | `enum WrappingIndent` (`None`, `Same`, `Indent`, `DeepIndent`) |
| 5624–5665 | `WrappingIndentOption` (`validate`: string → enum, anything else → `Same`; `compute`: accessibility, not ported) |
| 6861–6874 | `wordBreak`: `'normal' | 'keepAll'`, default `normal` |
| 6877–6907 | `wordWrap`: `'off' | 'on' | 'wordWrapColumn' | 'bounded'`, default `off` |
| 6908–6916 | `wordWrapIndicator`, default `false` |
| 6917–6927 | `wordWrapBreakAfterCharacters`, `wordWrapBreakBeforeCharacters`: copy the two default strings **character by character** from these lines (they contain CJK characters) |
| 6928–6939 | `wordWrapColumn`: default `80`, minimum `1` |

## Files to create or change

- `src/LoomEditor.View/Layout/WrappingIndent.cs` — create (header `editorOptions.ts`)
- `src/LoomEditor.View/Layout/WordWrap.cs` — create (`enum WordWrap { Off, On, WordWrapColumn, Bounded }`)
- `src/LoomEditor.View/Layout/WordBreak.cs` — create (`enum WordBreak { Normal, KeepAll }`)
- `src/LoomEditor.View/Layout/EditorWrappingOptions.cs` — create
- `tests/LoomEditor.View.Tests/Layout/EditorWrappingOptionsTests.cs` — create

## Design decisions (already made — do not change)

- All types are `internal` and live in `LoomEditor.View.Layout`, next to `EditorLayoutOptions`.
- `EditorWrappingOptions` is an `internal sealed record` with `init` properties and Monaco's defaults:
  `WordWrap = WordWrap.Off`, `WordWrapColumn = 80`, `WordWrapIndicator = false`, `WrappingIndent = WrappingIndent.Same`,
  `WordBreak = WordBreak.Normal`, `WordWrapBreakBeforeCharacters`, `WordWrapBreakAfterCharacters` (the Monaco strings, as
  `const string` fields of the record's static class part: `DefaultBreakBeforeCharacters`, `DefaultBreakAfterCharacters`),
  `WrapOnEscapedLineFeeds = false`, `UseTwoCellFullwidthCharacters = false`.
- `UseTwoCellFullwidthCharacters` is Monaco's `effectiveFullwidthCharacterWidth === 'twoCells'`: LoomEditor has no
  `fullwidthCharacterWidth` option, only this boolean (document it in the XML doc: "true forces full-width characters into
  two monospace cells; only meaningful for a monospace font").
- `WordWrapColumn` is clamped to `>= 1` by a `Validated()` method that returns a copy (the record itself does not clamp:
  `with` expressions bypass setters).
- **Not ported (document in the record's `<remarks>`):** `wordWrapOverride1/2` (workbench override hooks),
  `wrappingStrategy` (only Monaco's `simple` strategy exists: the editor uses a monospace font; the `advanced` DOM-based
  strategy is not ported), accessibility-driven overrides (`accessibilitySupport`).
- `WrappingIndent.FromString(string?)` is a static method on a small static class `WrappingIndents` (no reflection, no
  `Enum.Parse`): `"none"`, `"same"`, `"indent"`, `"deepIndent"` (case-sensitive like Monaco), anything else → `Same`.
  Same for `WordWraps.FromString` (`"off"`, `"on"`, `"wordWrapColumn"`, `"bounded"`, else `Off`) and `WordBreaks.FromString`
  (`"normal"`, `"keepAll"`, else `Normal`).
- XML doc on every member; threading remark on every class (`Threading: immutable`).

## Tests

- [ ] New: `Defaults_MatchMonaco` (every default listed above, including both break character strings being non-empty and
      containing `'('` / `')'` respectively)
- [ ] New: `FromString_KnownValues_ParseAndUnknownFallsBack` (theory over the three `FromString` helpers, `null` and
      `"INDENT"` fall back)
- [ ] New: `Validated_WordWrapColumnBelowOne_IsClampedToOne`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Reading these options from anywhere (P6-02, P6-20), the line-break computer, any control or gallery wiring.

## Suggested commit message

`feat(view): add the word wrap option types`
