# Completion list

**Role:** the WinUI side of completion (Monaco: `suggestWidget.ts`, `suggestWidgetRenderer.ts`, look of `suggest.css`).
`SuggestPresenter` implements `ISuggestPresenter` (P8-19): it anchors on the first UIA bounding rectangle of the start of the
typed word, measures the `SuggestWidget`, places it with `OverlayPlacement` and shows it on the overlay canvas
(`Controls/Overlay/OverlayHost`, shared with the hover popup). It listens to `SuggestListState.Changed` and refills the widget.

**Files:** `SuggestWidget.cs` (code-only `UserControl`: 12 pre-created rows and the detail strip), `SuggestWidget.Rows.cs`
(fills a row from a `SuggestRowModel`), `SuggestWidget.Theme.cs` (brushes re-colored by the theme), `SuggestPresenter.cs`;
`LoomEditor.Overlay.cs` is the glue in the control.

**Rules:** no XAML file, no `{Binding}`, no `ListView` (Native AOT, ADR 0011 decision 8).

**Not done (follow-ups):** screen reader support of the list (it is a sighted mouse / keyboard feature in phase 8), kind icons
(two-letter badges instead), the details side pane, resizing, hover-selects-row.
