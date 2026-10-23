# Hover

**Role:** shows a hover (the markers of the line and the answers of the hover providers) when the mouse rests on text,
hides it when the mouse leaves, the text or the selection changes, the view scrolls or `Esc` is pressed, and shows it at
the cursor with `Ctrl+K Ctrl+I` (`editor.action.showHover`, `editor.action.hideHover`).

**Monaco sources:** `src/vs/editor/contrib/hover/browser/` (`contentHoverController.ts`, `contentHoverWidgetWrapper.ts`, `hoverActions.ts`).

**Files:**
- `HoverFeature.cs` — creates the controller, registers the two commands.
- `HoverCommands.cs` — command ids.
- The logic is in `src/LoomEditor.View/Hover/` (`HoverController`, `IHoverPresenter`, `HoverContent`).

**Tests:** `tests/LoomEditor.View.Tests/Hover/HoverControllerTests.cs`, `tests/LoomEditor.View.Tests/Features/Hover/`.

**Known limits:** the popup itself is the control's `IHoverPresenter` (P8-11); no sticky geometry, focus or verbosity
actions, keyboard modifier rule, context menu pause or color picker.
