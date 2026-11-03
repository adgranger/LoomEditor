# P17-12 — Share the pointer release, the comment zone placement test and the diff editor teardown

- **Status:** done
- **Depends on:** P17-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

Three blocks that the plan missed and `HostTwinDuplicationTests` found (P17-01) exist once: the release logic of `LoomEditor`'s pointer handler, the
"is this widget already placed" test of `CommentZoneHost`, and the teardown of `LoomDiffEditor` when it is unloaded.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules). The shared parts these members join:
`src/LoomEditor.HostShared/Controls/LoomEditor.Pointer.cs`, `src/LoomEditor.HostShared/Controls/Comments/CommentZoneHost.cs`,
`src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.Layout.cs` (header block with host usings).

## Duplicated blocks (`HostTwinDuplicationTests`, commit d3d476b)

Paths under `src/LoomEditor.<Host>/Controls/`.

| Avalonia | WinUI | Code |
|---|---|---|
| `LoomEditor.Pointer.Host.cs` 76–90 | `LoomEditor.Pointer.Host.cs` 87–101 | `OnPointerReleased`: the mouse-up event with the released button, then the zone release |
| `Comments/CommentZoneHost.Host.cs` 41–49 | `Comments/CommentZoneHost.Host.cs` 14–22 | `IsPlaced`, `HasWidth` (and the `IsShown` / `GetWhitespaceViewportData` lines around them) |
| `DiffEditor/LoomDiffEditor.cs` 235–260 | `DiffEditor/LoomDiffEditor.cs` 170–195 | `OnUnloaded` (and the start of `ReflectText` after it) |

The pairs are listed in `KnownDuplicates`, so the test does not print them; to see a block, comment its line out of the list and run the test.

## Files to create or change

### 1. Pointer release

- `src/LoomEditor.HostShared/Controls/LoomEditor.Pointer.cs` — add, after `HandlePointerMove`:
  ```csharp
  // A release. `mouseUp` is the event with the released button, or null when the release is not the text's (no renderer, or over a child).
  // Returns true when the event is handled; the host then releases its pointer capture.
  private bool HandlePointerRelease(EditorMouseEvent release, EditorMouseEvent? mouseUp)
  ```
  Its body is today's code after the released-button lookup, in the same order: `EmitMouseUp` of `mouseUp` (when not null, with
  `CreateMouseTarget(mouseUp.X, mouseUp.Y)`), then the `_capturedZoneId` branch (`HandleZoneMove(release, ViewZonePointerPhase.Released)`), the
  minimap branch, the `_mouseDown` branch; each branch returns `true` where the host today releases the capture; the end returns `false`.
- Both `LoomEditor.Pointer.Host.cs` — `OnPointerReleased` keeps `ThrowIfNull`, `base.…`, the WinUI find-widget/overlay early return, then:
  ```csharp
  EditorMouseEvent release = PointerInput.Create(e, this, 1);
  EditorMouseEvent? mouseUp = null;
  if (_renderer is not null && !IsFromChildControl(e.Source))   // WinUI: !IsFromScrollBar(e.OriginalSource), as today
  {
      // The buttons are already up in a release: the released one is the button of the event.
      PointerUpdateKind kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
      mouseUp = release with
      {
          LeftButton = kind == PointerUpdateKind.LeftButtonReleased,
          MiddleButton = kind == PointerUpdateKind.MiddleButtonReleased,
      };
  }

  if (HandlePointerRelease(release, mouseUp))
  {
      ReleaseCapture();                // WinUI: ReleasePointerCapture(e.Pointer);
      e.Handled = true;
  }
  ```

### 2. Comment zone placement

- `src/LoomEditor.HostShared/Controls/Comments/CommentZoneHost.cs` — add `IsPlaced` and `HasWidth` (WinUI text), with `Canvas.GetLeft(widget)` /
  `Canvas.GetTop(widget)` replaced by the host members below.
- Both `Comments/CommentZoneHost.Host.cs` — `IsPlaced` and `HasWidth` go; add, where they were:
  ```csharp
  private static double GetLeft(CommentThreadWidget widget) => Canvas.GetLeft(widget);

  private static double GetTop(CommentThreadWidget widget) => Canvas.GetTop(widget);
  ```
  `IsShown` stays in the hosts (it names `Visibility` in WinUI).

### 3. Diff editor teardown

- `src/LoomEditor.HostShared/Controls/DiffEditor/LoomDiffEditor.Lifecycle.cs` — new (header block as `LoomDiffEditor.Layout.cs`, with the
  `…Controls.DiffEditor` host usings if the body needs them): `private void DestroyViewModel()` whose body is today's `OnUnloaded` body (WinUI text,
  from `DestroyAccessibleViewer();` to `_decorations = null;`), with the comment
  `// The control leaves the tree: the view model and what was built on it go; Loaded builds them again (EnsureViewModel).`
- Both `DiffEditor/LoomDiffEditor.cs` — `OnUnloaded` becomes `private void OnUnloaded(object? sender, RoutedEventArgs e) => DestroyViewModel();`
  (WinUI: `object sender`, as today). Remove the usings that become unused.

### 4. The gate

- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the three pairs `LoomEditor.Pointer.Host.cs`,
  `Comments/CommentZoneHost.Host.cs` and `DiffEditor/LoomDiffEditor.cs` from `KnownDuplicates`.

## Design decisions (already made — do not change)

- No behavior change. In `OnPointerReleased`, `PointerInput.Create(e, this, 1)` is built once instead of twice (it reads the event only); the
  released-button lookup stays inside the condition, as today; the order "mouse-up event, then zone / minimap / mouse-down release" is kept.
- `EditorMouseEvent` is a `sealed record`: `EditorMouseEvent?` is a nullable reference, no `.Value`.
- `EnsureViewModel` stays in the hosts (it builds the framework dispatchers and reads the control width); only the teardown moves. If the test
  still reports the `LoomDiffEditor.cs` pair after the move, also move `EnsureViewModel` to `LoomDiffEditor.Lifecycle.cs` with three host members
  in each `LoomDiffEditor.cs`: `private double ControlWidth => ActualWidth;` (Avalonia: `Bounds.Width`), `private IUiDispatcher CreateUiDispatcher()`
  and `private IEditorScheduler CreateEditorScheduler()` (today's `new …` expressions).
- `ReflectText` stays in the hosts (its parameter is `DependencyProperty` / `StyledProperty<string>`).
- `LoomEditor.Pointer.cs` stays under ~400 lines (it has room).

## Tests

- [x] No new test. The Avalonia headless tests (pointer, comment zones, diff editor) stay green **unchanged**.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] The three pairs are gone from `KnownDuplicates`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The rest of the pointer handlers (`OnPointerPressed`, `OnPointerMoved`: their child-control tests differ by host), `EnsureViewModel` (unless the
fallback above applies), `OnLoaded`.

## Suggested commit message

`refactor(hosts): share the pointer release, the comment zone placement test and the diff editor teardown`
