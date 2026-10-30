# P13-06 — `LineNumbersEnabled` and `ResetText`

- **Status:** done
- **Depends on:** P13-05
- **Size:** S

**Recommended model:** Sonnet

## Goal

A host can hide the line numbers (its blame column replaces them) with the dependency property `LineNumbersEnabled`, and reload a file whose text equals the last `Text`
value with `ResetText`. The renderer draws no numbers when they are off.

## Sources

Sources: none (specified in this card; [phase-13/README.md](README.md) table of section 2). Read first: `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs`
(`HideLineNumbers`, `ApplyHostLayoutOptions`, `ApplyText`), `src/LoomEditor.WinUI/Controls/LoomEditor.Minimap.cs` (how a `bool` dependency property is declared and applied),
`src/LoomEditor.WinUI/Controls/LoomEditor.cs` (`OnTextChanged`), `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.Options.cs` (`ApplyText`),
`src/LoomEditor.View/Features/EditorContext.cs` (`SetText`), `src/LoomEditor.Core/Model/TextModel*.cs` (`SetValue`: what it does to the undo stack and the decorations),
`src/LoomEditor.View/Layout/EditorLayoutOptions.cs` (`LineNumbers`), `src/LoomEditor.Rendering/TextViewRenderer.Gutter.cs` (`DrawGutter`),
`tests/LoomEditor.Rendering.Tests/TextViewRendererGutterTests.cs`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.LineNumbers.cs` — create
- `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs`, `LoomEditor.cs` — change
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.Options.cs` — change (`ApplyText` → `ResetText` call)
- `src/LoomEditor.Rendering/TextViewRenderer.Gutter.cs` — change
- `tests/LoomEditor.Rendering.Tests/TextViewRendererGutterTests.cs` — change
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change
- `docs/testing/phase-13-manual-tests.md` — change (section "P13-06")

## Design decisions (already made — do not change)

- **`LineNumbersEnabled`**: `public bool LineNumbersEnabled` with `public static readonly DependencyProperty LineNumbersEnabledProperty`, default `true`, declared like
  `MinimapEnabled` in the new file `LoomEditor.LineNumbers.cs`. Its change callback calls `ApplyHostLayoutOptions()`, which now sets `LineNumbers = LineNumbersEnabled`.
  XML doc: "Whether the line numbers are drawn (Monaco: `lineNumbers: 'on' | 'off'`). Defaults to `true`. When off, the gutter keeps the folding controls and the
  line decorations." **Delete** the internal `HideLineNumbers` and `_hideLineNumbers` (no caller: `grep -rn HideLineNumbers src samples` finds only the definition).
- **Renderer**: `DrawGutter` skips the line-number text when `!viewModel.LayoutOptions.LineNumbers` (today the numbers would be drawn right-aligned at
  `LineNumbersLeft` with a zero width, over the line decorations). The margin decorations, line decorations and the current-line highlight are still drawn.
- **`ResetText(string text)`**: the internal `ApplyText(string)` becomes `public void ResetText(string text)` (`ArgumentNullException` for null). Callers: `OnTextChanged`
  (`ResetText((string?)e.NewValue ?? string.Empty)`) and `LoomDiffEditor.ApplyText`. XML doc: "Replaces the whole text and resets the editor as setting `Text` does, even when
  `text` equals the last value of `Text` (that property is not updated by edits, so setting it to the same string does nothing)"; then state, from reading `SetValue`, what
  happens to the undo stack, the selections and the decorations (do not guess). `Text` is **not** updated by `ResetText` (say so in the doc).
- `PublicAPI.Unshipped.txt` (WinUI) gains exactly: `LineNumbersEnabled.get`, `LineNumbersEnabled.set`, `LineNumbersEnabledProperty`, `ResetText`.
- **Manual section "P13-06"**: (1) **Line numbers** check box (Gallery, P13-09) off → no number is drawn, the gutter is narrower by the numbers' width, folding chevrons and
  decorations still show; on → the numbers come back; (2) **Reload same text** (Gallery, P13-09) after typing restores the original text (the typed characters are gone) although
  `Text` was set to the same string before; (3) in the diff demo, setting `ModifiedText` to its current value after an edit does nothing, while
  `Modified.ResetText(...)` resets it and the diff updates.

## Tests

- [x] New (`TextViewRendererGutterTests`): with `LineNumbers = false` in the layout options, a three-line viewport draws no number text and still draws a line-decoration box;
  with `LineNumbers = true` it draws `1`, `2`, `3` (imitate the existing gutter test that asserts the numbers).
- [x] Manual: section "P13-06" written (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `grep -rn "HideLineNumbers\|ApplyText(string" src` finds nothing (the diff editor's private `ApplyText(LoomEditor?, string?)` stays)
- [x] `PublicAPI.Unshipped.txt` (WinUI) gains exactly the four lines above
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Relative line numbers or a number interval (`lineNumbers: 'relative' | 'interval'`), keeping `Text` in sync with edits, a `LoomDiffEditor.ResetTexts` member.

## Suggested commit message

`feat(winui): let the host hide line numbers and reset the text`

## Result

Build 0 warnings, all tests green (2 new gutter tests). The manual section "P13-06" is written and was not run by the agent. Finding: `SetValue` removes every decoration (as Monaco `setValue`); the P13-04 XML doc of `CreateDecorationsCollection` was corrected accordingly.
