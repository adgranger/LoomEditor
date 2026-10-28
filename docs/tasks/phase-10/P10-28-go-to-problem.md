# P10-28 — Go to next / previous problem (`F8`)

- **Status:** todo
- **Depends on:** P10-27
- **Size:** S

## Goal

`F8` / `Shift+F8` (and `Alt+F8` / `Shift+Alt+F8`) move the caret to the next / previous error, warning or info of the model, scroll it into view and announce its message to screen readers; `Escape` ends the navigation session. The peek-like marker widget of Monaco is not part of phase 10.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/gotoError/browser/gotoError.ts` | 28–173 | `MarkerController`: `_getOrCreateModel` (reuse, the follow-the-cursor reset of the index, the update on marker changes), `close`, `showAtMarker` (reveal), `navigate(next, multiFile)` — **single file, no widget, no related information** |
| `src/vs/editor/contrib/gotoError/browser/gotoErrorWidget.ts` | 214–226, 338–366 | `getAriaLabel` (the message format, with the line content first) and the editor side of `showAtMarker` (position, reveal, focus) |
| `src/vs/editor/contrib/zoneWidget/browser/zoneWidget.ts` | 455–468 | `_showImpl`: `editor.setSelection(where)` |
| `src/vs/editor/contrib/gotoError/browser/gotoError.ts` | 175–282 | `MarkerNavigationAction` and the four actions: ids `editor.action.marker.next` (`Alt+F8`), `editor.action.marker.prev` (`Shift+Alt+F8`), `editor.action.marker.nextInFiles` (`F8`), `editor.action.marker.prevInFiles` (`Shift+F8`), when `editorFocus` |
| `src/vs/editor/contrib/gotoError/browser/gotoError.ts` | 290–304 | the command `closeMarkersNavigation` (`Escape`, `Shift+Escape`, weight `EditorContrib + 50`, when the navigation is visible) |

## Files to create or change

- `src/LoomEditor.View/Features/GotoError/GotoErrorFeature.cs`, `MarkerController.cs`, `GotoErrorCommands.cs`, `README.md` — create (headers: ported from `gotoError.ts`); add to `EditorFeatures.Default`
- `src/LoomEditor.View/Commands/KeybindingContext.cs`, `DefaultKeybindings.cs` — change: field `MarkersNavigationVisible` (`markersNavigationVisible`) and the five rules; fill the field like the others
- `src/LoomEditor.View/Accessibility/AccessibilityMessages.cs` — change: `MarkerNavigation(severity, message, index, total, line, column)` (Monaco's `getAriaLabel`: `<line content>, <Severity>: <message> at <line>:<column>. ` (severity labels `Error`, `Warning`, `Info`, `Hint`), followed by `<index> of <total> problems` (one `problem` when the total is 1); English; one method)
- `tests/LoomEditor.View.Tests/Features/GotoError/MarkerControllerTests.cs` — create
- Gallery: nothing (the existing language server demo of phase 8 and its diagnostics cover it; P10-29 adds a "Problems" sample)

## Design decisions (already made — do not change)

- `F8` and `Alt+F8` have the **same behavior** (one model, so "in files" equals "in file"); all four ids are registered because applications may bind them.
- `Navigate(bool next)`: `GetOrCreateList()`, `list.Move(next, model, primaryPosition)`; if `Selected` is `null` return; then do what `MarkerNavigationWidget.showAtMarker` (`gotoErrorWidget.ts` 338–366) and `ZoneWidget._showImpl` (`zoneWidget.ts` 455–468) do to the editor: the position is the caret when it is already inside the marker's range, otherwise the marker's start; **collapse the selection to that position** (`editor.setSelection(where)`: the text is not selected) and reveal it **in the center if outside the viewport, smooth** (`revealPositionInCenterIfOutsideViewport`: grep how `GotoDefinition` or the find controller reveals a position and use the same call), then focus the editor. Announce via `IEditorAnnouncer.Announce(AccessibilityMessages.MarkerNavigation(...))`. The marker count and index come from `MarkerCoordinate`.
- The session (`MarkersNavigationVisible`, "navigation active") starts at the first navigation and ends on `Escape`/`Shift+Escape`, on model replacement, on feature dispose; while it is active the list follows the cursor: when the caret leaves the selected marker's range, `ResetIndex()` (the listener of lines 70–74 of `gotoError.ts`).
- The list is re-used while the session is active and its `Changed` event only resets the index (no widget to update).
- `Escape` has weight `EditorContrib + 50` **only while** `MarkersNavigationVisible` is true, so it sits above the snippet Escape (`+ 30`, P10-25) and below the suggest and find widget rules (read the existing weights and put it in the same relative position as Monaco).
- Read-only editors work.

## Tests

- [ ] Port: none (no upstream test for the controller without the widget)
- [ ] New: `F8_MovesCaretToNextMarkerAndRevealsIt`, `ShiftF8_GoesToPreviousWrapping`, `F8_NoMarkers_DoesNothing`, `F8_Announces_LineContentSeverityMessagePositionAndIndex`, `F8_CaretInsideMarker_KeepsCaret`, `F8_CaretOutside_MovesToMarkerStart_NoTextSelected`, `AltF8_SameAsF8`, `Escape_EndsSession_AndResetsContextKey`, `CaretMoveOutsideMarker_ResetsIndex`, `MarkersChanged_ResetsIndex`, `Navigation_StartsFromCaretPosition`, `Keybindings_ResolveToTheFourActions`, `Escape_NotInSession_DoesNotResolveToClose`
- [ ] Manual (section "P10-28", with the Gallery language server demo or the diagnostics sample of P10-29): `F8` cycles through the problems with the caret on each one and Narrator reads the message; `Escape` stops.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `gotoError.ts` 🔶 (no widget, no multi-file); `gotoErrorWidget.ts` listed as not ported
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`MarkerNavigationWidget` (zone widget: peek views are a later theme), related information, opening other files, the diagnostics list panel.

## Suggested commit message

`feat(view): add go to next / previous problem (F8)`
