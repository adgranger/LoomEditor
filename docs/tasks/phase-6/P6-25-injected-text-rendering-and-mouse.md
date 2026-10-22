# P6-25 — Injected text: its inline decorations in the viewport and the mouse

- **Status:** done
- **Depends on:** P6-24
- **Size:** S

## Goal

Injected text is drawn styled (its `InlineClassName` foreground applies), the mouse hit test normalizes positions that
fall inside injected text and reports `OnInjectedText`, so a double-click on injected text does not select a word.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 883–920 | `getViewportViewLineRenderingData` / `_getViewLineRenderingData`: the view line's own `inlineDecorations` (injected text) are appended to the model inline decorations |
| `src/vs/editor/browser/controller/mouseTarget.ts` | 1094–1113 | `doHitTest`: `getInjectedTextAt`, `normalizePosition(…, PositionAffinity.None)`, `ContentHitTestResult` with the injected text |
| same | 40–50, 874–947 | the `injectedText` detail of a content-text target |
| `src/vs/editor/browser/controller/mouseHandler.ts` | 640–655 | `onInjectedText: target is CONTENT_TEXT && detail.injectedText !== null` |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModel.Decorations.cs` — change: `GetViewportInlineDecorations` appends
  `_lines.GetViewLineData(lineNumber).InlineDecorations` (when not null) after the model ones, like Monaco
- `src/LoomEditor.View/Layout/ViewportData.cs` — change only if it caches inline decorations separately (it must return the merged list)
- `src/LoomEditor.View/Controller/MouseTarget.cs` — change: add `InjectedText? InjectedText = null` (last parameter)
- `src/LoomEditor.View/Controller/MouseTargetFactory.cs` — change: after `GetPositionAtCoordinate`, apply Monaco's `doHitTest`
  normalization (lines 1104–1111)
- `src/LoomEditor.View/Controller/MouseDownOperation.cs`, `MouseDispatchData.cs` — change: `OnInjectedText` is computed, the
  `<param>` doc no longer says "always false"
- `src/LoomEditor.Rendering/TextViewRenderer.Decorations.cs` — change: a one-line comment where inline decoration types are
  filtered, saying that `WidthOnly` is intentionally not drawn (see decisions)
- `tests/LoomEditor.View.Tests/Controller/MouseTargetFactoryInjectedTextTests.cs` — create
- `tests/LoomEditor.Rendering.Tests/TextViewRendererInjectedTextTests.cs` — create

## Design decisions (already made — do not change)

- The text of injected text is already part of the view line content (P6-15), so it is drawn by the normal text pass with the
  default token color; a `Regular` inline decoration on it applies its `DecorationStyles` foreground through the existing
  `GetStyleOverrides`. No new drawing code.
- `WidthOnly` decorations (injected text with empty content and `WidthInEm`) are **not** drawn and reserve no space in the
  drawn layout: DirectWrite has no inline-block element. The line breaker still counts their width (P6-11). Document it in
  PORTING "Behavior differences" and in the `WidthInEm` XML doc ("used for wrapping only").
- The hit test normalization replaces the position by `viewModel.NormalizePosition(pos, PositionAffinity.None)` and keeps the
  injected text in the target, exactly like Monaco (the range stays the collapsed hit position, Monaco does not recompute it).

## Tests

- [x] New (View): `HitTest_InsideInjectedText_NormalizesAndReportsIt`
- [x] New (View): `HitTest_OnModelText_HasNoInjectedText`
- [x] New (View): `DoubleClickOnInjectedText_DoesNotSelectAWord` (through `ViewController` with `OnInjectedText = true`)
- [x] New (View): `ViewportInlineDecorations_IncludeTheInjectedTextOnes`
- [x] New (Rendering): `InjectedText_IsDrawnWithItsInlineClassForeground` (a `DecorationStyles` entry with a foreground for the class)
- [x] New (Rendering): `InjectedText_ShiftsTheFollowingText` (the text after an `After` injection starts after it)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `mouseTarget.ts` row (injected text normalization); remove the injected text row of "Cut points for
      phase 6"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`before` content of plain decorations (`BeforeContentClassName`, still a cut point in `TextViewRenderer.Decorations.cs`);
any feature that produces injected text (inlay hints are phase 8).

## Suggested commit message

`feat(view): style injected text and hit test over it`
