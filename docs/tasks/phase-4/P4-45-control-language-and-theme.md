# P4-45 — The control: `Language` and `Theme` properties

- **Status:** todo
- **Depends on:** P4-44
- **Size:** M

## Goal

An app colors its code with two properties: `<loom:LoomEditor Language="csharp" Theme="{x:Bind DarkModern}" />`.
Tokens from the background thread arrive on the UI thread and redraw the editor.

## Sources

None to port (WinUI wiring). Read `src/LoomEditor.WinUI/Controls/LoomEditor.cs` and `LoomEditor.Font.cs` (how
dependency properties are declared and forwarded), `AnimationFrameScheduler.cs` (how the control posts to the UI thread).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.Highlighting.cs` — create (`Language`, `Theme` dependency properties)
- `src/LoomEditor.WinUI/Threading/DispatcherQueueUiDispatcher.cs` — create (`IUiDispatcher` over `DispatcherQueue.TryEnqueue`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: give the dispatcher to `EditorContext`; redraw on
  `ViewTokensChangedEvent`; dispose the highlighter on `Unloaded`, recreate it on `Loaded` again if needed
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — via `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`
- `src/LoomEditor.TextMate/` — **public** `BuiltInLanguages.Ids` (`IReadOnlyList<string>`) for the Gallery and apps;
  update its `PublicAPI.Unshipped.txt`

## Design decisions (already made — do not change)

- ADR 0008 decision 10. `Language` is a `string` (default `"plaintext"`), `Theme` an `EditorTheme` (default
  `EditorTheme.Dark`). Changing `Theme` sets the renderer's theme **and** `SyntaxHighlighter.SetTheme`; the background
  of the control follows the theme.
- The worker thread exists only while the control is loaded.
- `TokensChanged` redraws the canvas once per dispatcher turn (coalesce several events in one `Invalidate`).

## Tests

No WinUI test project. Covered by P4-42 … P4-44. **Manual check by the maintainer** (written in the card when done):

- [ ] Gallery with `Language="csharp"`: keywords, strings and comments are colored like VS Code Dark Modern
- [ ] Switching the theme at run time recolors without flicker
- [ ] Typing in a 10 000-line C# file stays fluid; colors below the viewport arrive after a moment

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Public API files updated with the script, diff checked
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The Gallery demo (P4-46).

## Suggested commit message

`feat(winui): add Language and Theme to the editor control`
