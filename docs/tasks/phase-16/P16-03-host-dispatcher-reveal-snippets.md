# P16-03 — `HostDispatcher`; share `LoomEditor.Reveal` and `LoomEditor.Snippets`

- **Status:** done
- **Depends on:** P16-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

Each host has `HostDispatcher` (ADR 0017 decision 5), the way shared code posts to the UI thread. `LoomEditor.Reveal.cs` and `LoomEditor.Snippets.cs` exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/LoomEditor.Reveal.cs`, `src/LoomEditor.WinUI/Controls/LoomEditor.Snippets.cs` and their Avalonia twins; `src/LoomEditor.Avalonia/Controls/LoomEditor.cs` (`CreateClipboardService`).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/LoomEditor.Reveal.cs` | 25+45 | `RevealRangeInCenter`, `RevealRange`, `ApplyPendingHorizontalReveal`, `RevealEventHandler`, `HandleEvents` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/LoomEditor.Snippets.cs` | 6+35 | `InsertSnippet`, `InsertWithClipboardAsync` | WinUI same path |

## Files to create or change

- `src/LoomEditor.WinUI/Threading/HostDispatcher.cs`, `src/LoomEditor.Avalonia/Threading/HostDispatcher.cs` — create:
  `internal static class HostDispatcher` with `public static bool Post(Action action)` and `public static bool PostLow(Action action)`.
  WinUI: `DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Normal | Low, () => action())`.
  Avalonia: `Dispatcher.UIThread.Post(action)` / `Dispatcher.UIThread.Post(action, DispatcherPriority.Background)`, then `return true`.
- `src/LoomEditor.WinUI/Controls/LoomEditor.Reveal.cs` → `git mv` to `src/LoomEditor.HostShared/Controls/LoomEditor.Reveal.cs`; the Avalonia twin — delete. The one differing call (`_canvas?.Invalidate()` / `Invalidate()`)
  becomes the host member `private void InvalidateView()` in `LoomEditor.Reveal.Host.cs` of each host (or an existing member of both hosts with that exact role).
- `src/LoomEditor.WinUI/Controls/LoomEditor.Snippets.cs` → `git mv` to `src/LoomEditor.HostShared/Controls/LoomEditor.Snippets.cs`; the Avalonia twin — delete. `InsertWithClipboardAsync` takes an `IClipboardService`
  (View) argument; the caller passes `CreateClipboardService()`, which WinUI gains (`private WindowsClipboardService CreateClipboardService() => new();`, in
  `LoomEditor.Snippets.Host.cs`); Avalonia already has it in `LoomEditor.cs`.

## Design decisions (already made — do not change)

- `HostDispatcher` has no other member. Callers are on the UI thread (each class's threading note says so).
- `HostDispatcher` is not used by any existing file in this card except where Reveal or Snippets need it; later cards switch the controllers to it.

## Tests

- [x] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Other partials of `LoomEditor`.

## Suggested commit message

`refactor(hosts): add HostDispatcher; share the reveal and snippet partials`

## Result

- `HostDispatcher` added to both hosts (unused until the controller cards). `InsertWithClipboardAsync` takes a `Func<Task<string?>>` (the host's `CreateClipboardService().ReadTextAsync`) instead of an `IClipboardService`: with one concrete type per host, CA1859 refuses the interface parameter. WinUI `CreateClipboardService` is `static` (CA1822).
- `View.Tests` failed 1 to 4 tests at random (`CommitCharacterControllerTests`, `TextSelfCheckTests` smart-select) in full solution runs while other builds were running; they pass alone and in a quiet solution run. Flaky under load, unrelated.
