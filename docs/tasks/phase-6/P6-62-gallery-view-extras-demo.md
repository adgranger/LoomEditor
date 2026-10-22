# P6-62 — Gallery: the phase 6 samples and the manual test list

- **Status:** todo
- **Depends on:** P6-61, P6-54, P6-35, P6-26
- **Size:** S

## Goal

The Gallery has ready-made samples for word wrap, injected text, guides, the minimap and sticky scroll, and the phase 6
manual test list is complete for the maintainer.

## Sources

None to port. Samples are short hand-written texts (C# constants in `FeatureSamples.cs`, like P5-95).

## Files to create or change

- `samples/LoomEditor.Gallery/FeatureSamples.cs` — change: the new entries
- `samples/LoomEditor.Gallery/ModelDemoActions.cs` (or `MainWindow.xaml.cs`) — change: an "Inject text" demo action that adds a decoration
  with `After = new InjectedTextOptions { Content = ": int", InlineClassName = … }` at the cursor (a styled class registered in
  `DecorationStyles` with a dim foreground), and "Clear injected text"
- `samples/LoomEditor.Gallery/MainWindow.xaml` — change: the header text "phase 6: view extras"
- `docs/testing/phase-6-manual-tests.md` — change: complete it (created by P6-26 and extended by P6-35, P6-54, P6-61)

## Design decisions (already made — do not change)

- Samples (the hint says what to try):
  1. **Word wrap** (Markdown): long paragraphs, an indented list with long items, a line with tabs; hint: toggle "Word wrap", resize
     the window, Home / End / Up / Down on wrapped lines.
  2. **Guides** (C#): deep nesting with mixed brackets; hint: choose "Indent + bracket pairs", move the cursor between scopes.
  3. **Sticky scroll** (C# and Python, two entries): a 300-line file with nested classes / methods; hint: scroll slowly, click a sticky line,
     fold from a sticky chevron.
  4. **Minimap** (the large C# button already exists): hint: drag the slider, click the minimap, find a word and see the matches.
  5. **Injected text** (TypeScript): hint: put the cursor after a variable and click "Inject text"; move the cursor across it.
- The large-file buttons stay; word wrap, the minimap and sticky scroll must stay fluid on "Large C# (20 000 lines)" (add that check).

## Tests

Manual (in `phase-6-manual-tests.md`, a result column for the maintainer; Native AOT build):

- [ ] Word wrap: wraps at the window width, rewraps on resize, wrapped indent follows `Same`, line numbers only on the first view line,
      selection across wraps, cursor up / down by view line, folding a wrapped region
- [ ] Injected text: shown and styled, the cursor jumps over it, double-click on it selects nothing, it wraps
- [ ] Guides: indent guides on every level, the active one highlighted, bracket pair guides with horizontal lines on the active pair
- [ ] Overview ruler: find matches, word highlights and the matching bracket are marked; the cursor mark moves
- [ ] Minimap: characters at 100 % and 200 % DPI, the slider on hover, click and drag, left side, blocks mode
- [ ] Sticky scroll: lines stack and push each other, click reveals, chevron folds, the toggle hides it
- [ ] Performance: the 20 000-line file scrolls fluidly with all of the above on

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — succeeds
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any new editor behavior.

## Suggested commit message

`feat(gallery): phase 6 view extras demo and manual test list`
