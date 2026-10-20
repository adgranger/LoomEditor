# P5-95 — Gallery: the phase 5 features demo and the manual test list

- **Status:** todo
- **Depends on:** P5-94
- **Size:** S

## Goal

The Gallery shows every phase 5 feature with a ready-made sample, and the manual checks of phase 5 are written down
for the maintainer.

## Sources

None to port. The samples are short hand-written texts (C# constants, like `HighlightingSamples.cs`), 20–60 lines each.

## Files to create or change

- `samples/LoomEditor.Gallery/MainWindow.xaml` — change: a new row "Features" with a `FeatureSampleBox` combo box
  (filled in code) and a short hint text
- `samples/LoomEditor.Gallery/MainWindow.xaml.cs` — change: loading a sample sets the language, the text, the cursor,
  and the hint
- `samples/LoomEditor.Gallery/FeatureSamples.cs` — create: one entry per sample (name, language id, text, cursor, hint)
- `samples/LoomEditor.Gallery/MainWindow.xaml` header text — change: "phase 5: editor features"
- `docs/testing/phase-5-manual-tests.md` — create (same shape as `phase-4-manual-tests.md`)

## Design decisions (already made — do not change)

- One combo box, no new page: the Gallery stays one window (P4-46). Combo items are filled in code (Native AOT,
  P4-46 notes).
- Samples (the hint says what to try):
  1. **Find / replace** (C#): repeated identifiers, a regex-friendly block; hint: Ctrl+F, Ctrl+H, Alt+C / Alt+W / Alt+R,
     Ctrl+D.
  2. **Folding** (C#): nested classes / methods, `#region` blocks, a `/* */` comment; hint: hover the gutter, click
     the chevrons, Ctrl+Shift+[ / ], Ctrl+K Ctrl+0 / J, Ctrl+K Ctrl+8.
  3. **Folding (off-side)** (Python and YAML, two entries): blank lines inside blocks.
  4. **Brackets** (TypeScript): deep nesting for colorization, a bracket in a string and a comment; hint: Ctrl+Shift+\.
  5. **Auto-closing, auto-indent, comments**, one entry per built-in language (C#, TypeScript, JavaScript, JSON, HTML,
     CSS, XML, Markdown, Python, YAML): an empty block to type into; hint: type `{`, `"`, Enter, Ctrl+/, Shift+Alt+A.
  6. **Line operations** (C#): hint: Alt+Up/Down, Shift+Alt+Up/Down, Ctrl+Shift+K, Ctrl+Enter.
  7. **Multi-cursor** (C#): hint: Ctrl+D, Ctrl+Shift+L, Ctrl+Alt+Up/Down.
  8. **Word highlight** (C#): hint: put the cursor on an identifier, F7 / Shift+F7.
- The large-file buttons stay; folding and word highlight must stay fluid on "Large C# (20 000 lines)".

## Tests

Manual (in `phase-5-manual-tests.md`, a result column for the maintainer; Native AOT build):

- [ ] Find: the widget opens with the word at the cursor, counts "n of m", next / previous wrap, the four toggles,
      find in selection, replace / replace all, Escape closes; matches inside a folded region unfold it
- [ ] Folding: chevrons appear on gutter hover only; a folded line shows `⋯` and the fold background; clicking the
      chevron or the `⋯` toggles; Shift+click folds children first; all folding shortcuts; typing above a fold keeps it;
      moving the cursor into a fold (Ctrl+G is not available: use Find or the arrow keys) unfolds it; Python / YAML
      blocks include their trailing blank lines (off-side)
- [ ] Brackets: pairs are colorized (Dark Modern, Light Modern); the matching bracket is boxed after a short delay;
      Ctrl+Shift+\ jumps
- [ ] Each language: auto-closing pairs and quotes, overtype, auto-surround of a selection, Enter between brackets
      indents, Ctrl+/ toggles line comments (block comments for CSS, HTML, XML), Shift+Alt+A toggles block comments
- [ ] Line operations: move / copy / delete lines, insert line above / below, with several cursors
- [ ] Multi-cursor: Ctrl+D adds the next match, Ctrl+K Ctrl+D moves it, Ctrl+Shift+L selects all occurrences
- [ ] Word highlight: occurrences highlighted after a short delay, F7 / Shift+F7 cycle, typing clears them
- [ ] Indentation: loading the tab-indented sample sets the Tab size box and Tab inserts a tab
- [ ] Large C# file: folding all and word highlight stay fluid; scrolling with folds is smooth

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts and each sample loads
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Closing the phase (P5-96).

## Suggested commit message

`feat(gallery): phase 5 features demo and manual test list`
