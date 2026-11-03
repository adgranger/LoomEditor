# P15-20 — Comment threads: event args, gutter "+" button, editor wiring

- **Status:** todo
- **Depends on:** P15-19
- **Size:** M

**Recommended model:** Sonnet

## Goal

The public comment-thread API of `LoomEditor` exists on Avalonia, and the gutter "+" appears on hover and raises `AddCommentRequested`. The thread widget itself comes in
P15-21 … P15-23.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs`, `CommentGutterButton.cs`, `CommentGutterController.cs`, `Controls/Gutter/GutterButtonTheme.cs`,
`Controls/LoomEditor.CommentThreads.cs`. Also read [ADR 0014](../../adr/0014-inline-comments.md), `docs/how-to/use-comment-threads.md`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/Comments/CommentEventArgs.cs` — copy (public, [README](README.md) decision 3)
- `src/LoomEditor.Avalonia/Controls/Comments/CommentGutterButton.cs`, `CommentGutterController.cs`, `Controls/Gutter/GutterButtonTheme.cs`, `Controls/LoomEditor.CommentThreads.cs` —
  create (twins)
- `src/LoomEditor.Avalonia/Themes/Icons.axaml` — `plus` path
- `tests/LoomEditor.Avalonia.Tests/Controls/CommentGutterTests.cs` — create
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- The "+" is a `Button` with a `PathIcon` (`plus`), not a tab stop, with the same automation name and tooltip. `GutterButtonTheme.Apply(Button, EditorTheme)` keeps its
  WinUI signature with the Avalonia `Button`.
- Until P15-23 the thread widget does not exist: `LoomEditor.CommentThreads.cs` compiles with the zone host calls behind a `// P15-23` comment and an internal no-op
  `ICommentZoneHost` stub. P15-23 deletes the stub.

## Tests

- [ ] New: `PointerOverLine_ShowsPlus_AtThatLine`; `ClickPlus_RaisesAddCommentRequested_WithLine`; `PointerLeaves_HidesPlus`; `Theme_Change_RecolorsPlus`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

The thread widget (P15-21, P15-22) and its zone (P15-23).

## Suggested commit message

`feat(avalonia): comment thread events and the gutter add button`
