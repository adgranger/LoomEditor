# Phase 15 — read this before any card

Phase 15 adds **`LoomEditor.Avalonia`**: the `LoomEditor` and `LoomDiffEditor` controls on Avalonia, so that LoomEditor runs on **Windows, macOS and Linux**
([phase-15-avalonia.md](../../roadmap/phase-15-avalonia.md), [ADR 0016](../../adr/0016-avalonia-backend.md)). The WinUI control stays and does not change for its hosts.

## This phase is not a port of Monaco, it is a translation of `LoomEditor.WinUI`

- The **source of every card is the WinUI twin**: the file(s) of `src/LoomEditor.WinUI/` with the same relative path and name. Read them first, then translate one to one:
  same type and member names, same member order, same partial-file split, same XML doc (adapted to Avalonia), same threading note.
- A twin keeps the **header of its WinUI file**. If the WinUI file has a `// Ported from microsoft/vscode: …` header, copy it unchanged; if it has none, add none.
  No `// Monaco:` test comments for new tests. In `docs/PORTING.md`, a row that names the WinUI file gets the Avalonia path added (`…/LoomEditor.WinUI/X.cs`, `…/LoomEditor.Avalonia/X.cs`).
- **Do not improve, merge or redesign while translating.** If a WinUI file is wrong, stop and report: the fix belongs in its own card, for both controls.
- A card that finds a UI-free piece of logic that both controls need, and that P15-02 did not move, **copies it** and lists it in its Result section (a later card can share it).

## Decisions (approved 2026-11-03 — do not change)

1. **Layers:** `LoomEditor.Avalonia → LoomEditor.Rendering → LoomEditor.View → (TextMate, Themes) → Core`. `LoomEditor.Rendering` becomes `net11.0` (P15-01). `LoomEditor.Avalonia`
   is `net11.0`, no Windows API (checked by `PlatformNeutralSourceTests`), no WinUI package (checked by `DependencyRulesTests`). The one exception is the screen-reader
   P/Invoke of P15-14 (`Interop/`, behind `OperatingSystem.IsWindows()`). It is listed with its reason in the test.
2. **Text and drawing: Avalonia's own stack** (ADR 0016 option A). `DrawingContext` for shapes and bitmaps, `Avalonia.Media.TextFormatting.TextLayout` for text, with
   `DefaultIncrementalTab = tabSize × FontInfo.SpaceWidth` (the DirectWrite `IncrementalTabStop` model), `FontFeatureCollection` for font features, the font weight from
   `EditorFontOptions`, letter spacing on the layout. No SkiaSharp code in `src/`. The editor canvas is a `Control` whose `Render(DrawingContext)` calls `TextViewRenderer`.
3. **Public API = WinUI's, translated** (ADR 0016 decision 4). Namespace `LoomEditor.Avalonia.Controls`. The substitutions are exactly these:

   | WinUI | Avalonia |
   |---|---|
   | `DependencyProperty` (read-write) | `StyledProperty<T>` (`AvaloniaProperty.Register<TOwner, T>`) |
   | `DependencyProperty` (read-only, set by the control) | `DirectProperty<TOwner, T>` |
   | `VirtualKey` | `Avalonia.Input.Key` (`VirtualKey.S` → `Key.S`) |
   | `UIElement` / `FrameworkElement` (host content) | `Control` |
   | `Windows.UI.Color` | `Avalonia.Media.Color` |
   | `ElementTheme` | `Avalonia.Styling.ThemeVariant` |
   | `TypedEventHandler<TSender, TArgs>` | `EventHandler<TArgs>` |

   UI-free public types (enums, event args, `DiffLine`, `DiffSide`, `GutterArea`, …) are **copied** with the same name, members and XML doc into the Avalonia namespace.
   `LoomEditor.Avalonia/PublicAPI.Unshipped.txt` is kept by `./eng/Update-PublicApi.ps1 src/LoomEditor.Avalonia`.
4. **API parity is a test** (`tests/LoomEditor.Architecture.Tests/AvaloniaApiParityTests.cs`, P15-05). It reduces every line of both `PublicAPI.Unshipped.txt` files to a key
   `Type.Member` (namespace swapped, signature dropped). Every WinUI key must exist in Avalonia unless it is listed in
   `tests/LoomEditor.Architecture.Tests/AvaloniaParityExceptions.txt` (one key per line + `# reason`) or in `AvaloniaParityPending.txt`.
   P15-05 writes the full pending list. **Each card removes the keys it implements**; a key that is pending but already implemented also fails the test, so the
   list stays exact. P15-34 checks that the pending list is empty.
5. **Styles:** the library ships `LoomEditor.Avalonia.LoomEditorStyles : Styles` (control themes in `Themes/Generic.axaml`). A host adds `<loom:LoomEditorStyles />` to
   `Application.Styles` after its own theme (as with Avalonia's `DataGrid`). The controls work with `FluentTheme` and `SimpleTheme`. Their own colors come from the
   `EditorTheme`, as in WinUI, never from Fluent resources.
6. **Icons:** built-in icons (find widget, comment widget, diff gutter) are `PathIcon`s with geometries in `Themes/Icons.axaml`. Each card that needs one writes
   simple path data (chevrons, close, plus, arrows, check). No icon font and no external icon set. Host glyph strings (`PrimaryBlockActionGlyph`, …) keep their WinUI type
   `string` and are drawn with the styled property `GlyphFontFamily` (default `"Segoe Fluent Icons, Segoe MDL2 Assets"`), which the host can set to its own font. This is
   documented in the XML doc and in "Behavior differences".
7. **macOS keys:** `Input/KeyMapping.cs` maps `KeyModifiers.Meta` (Cmd) → View `KeyModifiers.Ctrl` and `KeyModifiers.Control` → View `KeyModifiers.Meta` when
   `OperatingSystem.IsMacOS()`. Nothing else changes in View. Monaco's `mac:` keybindings are out of scope.
8. **Default fonts:** `BareFontInfo` gains `DefaultMacFontFamily` and `DefaultLinuxFontFamily`, ported from the Monaco constants next to the one
   `DefaultWindowsFontFamily` already ports (its XML doc names the file). The Avalonia control's default `FontFamily` is the one for the current OS (P15-08).
9. **Threading:** UI thread = `Dispatcher.UIThread`. `DispatcherQueueEditorScheduler` → `DispatcherEditorScheduler`, `AnimationFrameScheduler` uses
   `TopLevel.RequestAnimationFrame`. DPI = `TopLevel.RenderScaling`.
10. **No reflection:** compiled bindings by default (`<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`), no `{Binding}` without
    `x:DataType`, no `ReflectionBinding`. The banned-API list of the repository applies to the Avalonia project.

## Tests: unlike WinUI, the Avalonia control is tested

`tests/LoomEditor.Avalonia.Tests` (P15-05) uses **Avalonia.Headless** with the Skia renderer (`UseHeadlessDrawing = false`), which draws real frames into a bitmap.
xUnit v3 has no Avalonia attribute, so tests run inside a session started by a helper: `TestHelpers/HeadlessSession.cs`
(`HeadlessUnitTestSession.StartNew(typeof(TestApp))`, one per assembly, `session.Dispatch(…)` in each test). Every card of steps B to E adds tests there:

- the surface and the text: pixels and widths (P15-06, P15-07);
- the control: typing, keys, clipboard, pointer selection, IME composition, scrolling, widgets opening and closing, through `HeadlessWindowExtensions`
  (`KeyPress`, `KeyTextInput`, `MouseDown`, `CaptureRenderedFrame`).

What headless cannot check (real IME windows, screen readers, real fonts on each OS, the native look) goes to `docs/testing/phase-15-manual-tests.md` (P15-08 creates it from the
header of `phase-14-manual-tests.md`; machines: Windows 11, macOS 15 or later, Ubuntu 24.04). Each UI card adds a `## P15-xx` section. Agents do not run the Gallery UI; a card's
Result section says the manual checks were written, not run.

## Order of the cards

- **A — Make the shared layers portable** (P15-01 … P15-04): Rendering to `net11.0`, shared helpers, Oniguruma on Linux and macOS, CI on three OSes.
- **B — Project and drawing** (P15-05 … P15-07): the project, tests and parity test; text measurement; surface, bitmaps, minimap.
- **C — The `LoomEditor` control** (P15-08 … P15-16).
- **D — Widgets** (P15-17 … P15-23): find, hover, suggest, comment threads.
- **E — `LoomDiffEditor`** (P15-24 … P15-30).
- **F — Ship** (P15-31 … P15-34): Gallery, Native AOT on three OSes, packages, close.

Take the cards in table order. Each card's "Depends on" is satisfied by the cards above it.

**Recommended model:** Sonnet, except **Opus** for P15-06 (text measurement: everything else is placed from these numbers), P15-13 (IME) and P15-14 (accessibility).
Each card is about 400 lines of production code or less (XAML not counted). If a card does not fit in one session, stop and report a proposed split.

## Every card also

- Keeps `dotnet build LoomEditor.slnx -c Release` at 0 warnings and `dotnet test --solution LoomEditor.slnx -c Release` green on Windows.
- From P15-04 on, keeps `dotnet build LoomEditor.Portable.slnf -c Release` and `dotnet test --solution LoomEditor.Portable.slnf -c Release` green. CI runs them on Linux and
  macOS; locally, Windows is enough.
- Removes its keys from `AvaloniaParityPending.txt` (from P15-05 on).

## Out of scope for phase 15

Monaco's mac-only keybindings, a macOS native menu bar integration, mobile and browser (WASM) targets, font variation axes other than `wght`, replacing or changing the
WinUI control, sharing the control code between WinUI and Avalonia beyond P15-02 (a later refactor once both exist), Skia-level drawing (ADR 0016 option B).
