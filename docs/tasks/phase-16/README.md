# Phase 16 — read this before any card

Phase 16 removes the duplication between the WinUI and Avalonia hosts ([phase-16-shared-host-logic.md](../../roadmap/phase-16-shared-host-logic.md),
[ADR 0017](../../adr/0017-shared-host-logic.md)). SonarCloud counts 4,500 duplicated new lines on PR #26 (23.2%); the target is ≤ 3%.

## This phase is a refactoring, not a port

- **No behavior change** in either control or either Gallery. Same public API (both `PublicAPI.Unshipped.txt` files unchanged, `AvaloniaApiParityTests` green).
- **Start from the WinUI file** (the reference implementation). Where the Avalonia twin behaves differently (not only spells differently), keep the difference in the
  host parts. If a twin looks wrong, do not fix it: stop and write it in the card's Result section.
- Monaco headers (`// Ported from …`, `// Upstream commit: …`) stay on line 1 and 2 of the shared file. No `// Monaco:` comments to add.
- Use `git mv` when a whole file moves, so history follows (WinUI file → `src/LoomEditor.HostShared/...`; the Avalonia twin is deleted or becomes its `.Host.cs`).
- Line numbers in the cards are those of SonarCloud on 2026-11-03; files may have moved a little since. Re-read the blocks of a file with
  `curl -s "https://sonarcloud.io/api/duplications/show?key=adgranger_LoomEditor:<path>&pullRequest=26"` (the twin of each block is listed with it).

## Rules of shared host source (ADR 0017 decisions 1–6)

1. **Where:** `src/LoomEditor.HostShared/<host-relative path>`. The file the shared code comes from had the path `src/LoomEditor.WinUI/Controls/X.cs` →
   `src/LoomEditor.HostShared/Controls/X.cs`. Both hosts compile it through `LoomEditor.HostShared.props` (P16-01): no project file change per card.
2. **Header block** (the only `#if` allowed in the folder; `HostSharedSourceTests` checks it):

   ```csharp
   using LoomEditor.View.DiffEditor;
   #if LOOM_WINUI
   using LoomEditor.WinUI.Controls.Overlay;
   namespace LoomEditor.WinUI.Controls.DiffEditor;
   #elif LOOM_AVALONIA
   using LoomEditor.Avalonia.Controls.Overlay;
   namespace LoomEditor.Avalonia.Controls.DiffEditor;
   #else
   #error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
   #endif
   ```

3. **What shared code may use:** Core, View, Rendering, Themes, TextMate, BCL; the host's own types and members by simple name (the control, `OverlayHost`, named
   template parts, fields of the host part). **Never** an `Avalonia.*`, `Microsoft.UI.*` or `Windows.*` namespace or type. Members of framework objects that both
   frameworks spell and mean the same — `Text`, `IsEnabled`, `IsChecked`, `Content`, `Children`, `Width`, `Height`, `Tag` — may be used; everything else
   (focus, pointer capture, visibility, brushes, colors, tool tips, automation properties, dispatching, invalidation, event-args types) goes through a **host member**.
4. **Host members** live in `<File>.Host.cs` in each host, next to the original path (`src/LoomEditor.Avalonia/Controls/DiffEditor/DiffBlockActionsController.Host.cs`),
   with the **same name and signature** in both hosts. Private by default. One-line members are fine (`private void FocusModified() => _modified.Focus();`).
   The type becomes `partial` in every part. The base class, attributes and interfaces stay on the host part when they are framework types.
5. **UI thread:** `HostDispatcher.Post(Action)` / `HostDispatcher.PostLow(Action)` (P16-03), never `Dispatcher.UIThread` or `DispatcherQueue` in shared code.
6. **Event handlers with framework argument types** (`PointerRoutedEventArgs`, `RoutedEventArgs`, `KeyRoutedEventArgs`, …) stay in the host part. They read
   what they need from the framework argument and call a shared method that takes plain values (`EditorMouseEvent`, `bool`, `double`, View `KeyModifiers`).
7. **`docs/PORTING.md`:** a row that names the WinUI and the Avalonia path of a moved file names the `HostShared` path (plus `.Host.cs` paths if the row lists files).

## Gallery rules (ADR 0017 decision 9)

- Shared Gallery files stay in `samples/LoomEditor.Gallery/` and are linked into `samples/LoomEditor.Gallery.Avalonia/LoomEditor.Gallery.Avalonia.csproj`
  in the existing `<ItemGroup Label="Shared with the WinUI Gallery">` (`Link="Shared\…"`).
- Both Gallery projects have (P16-20) global usings for their control namespace and the alias `LoomEditorControl` (the type `LoomEditor` cannot be named
  inside `namespace LoomEditor.Gallery`).
- A window partial splits into the shared `X.cs` (logic, samples, logging calls) and `X.Host.cs` in each Gallery (event handlers, XAML-specific calls). Same host member
  rules as above.

## Tests

- The Avalonia headless tests (`tests/LoomEditor.Avalonia.Tests`) cover the shared code compiled into Avalonia; they must stay green **without changes**, except the
  test-helper card (P16-19). A card does not delete or weaken a test.
- The WinUI control has no UI test. Each card that moves WinUI code builds the solution in Release (WinUI included); the closing card runs the WinUI Gallery self-checks.
- New tests only where a card says so (arch tests in P16-01, Rendering helpers in P16-18).

## Done for every card

- `dotnet build LoomEditor.slnx -c Release` — 0 warnings; `dotnet test --solution LoomEditor.slnx -c Release` — all green;
  `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings (the HostShared files compiled into Avalonia on the portable filter).
- The members the card lists exist once, in `src/LoomEditor.HostShared/` (or in Rendering / the test helper), and no longer in either host.
- `docs/PORTING.md`, the card and `docs/tasks/README.md` updated. One commit (`/next-task`), no trailer.
