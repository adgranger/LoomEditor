# Hover popup

**Role:** the WinUI side of the hover (Monaco: `contentHoverWidget.ts` layout, `hover.css` look). `HoverPresenter` implements
`IHoverPresenter` (P8-10): it anchors on the first UIA bounding rectangle of the range, measures the `HoverWidget`, places it
with `OverlayPlacement` (below, above when there is no room) and shows it on the overlay canvas (`Controls/Overlay/OverlayHost`,
template part `PART_OverlayCanvas`, reused by the completion list in P8-22).

**Files:** `HoverWidget.cs` (code-only `UserControl`: rows in a scroll viewer), `HoverWidget.Markdown.cs` (marker and Markdown rows as
`RichTextBlock`s), `HoverWidget.Theme.cs` (brushes re-colored by the theme), `HoverPresenter.cs`; `LoomEditor.Hover.cs` is the glue in the control.

**Rules:** no XAML file, no `{Binding}` (Native AOT). Pointer events from the overlay do not reach the text (`IsFromOverlay`).

**Not done (follow-ups):** screen reader announcement of the popup, resizing, status bar, clickable links, focus into the popup.
