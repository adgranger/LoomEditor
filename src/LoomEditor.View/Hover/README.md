# Hover computation

What a hover shows, without any UI: `ContentHoverComputer` collects the markers of the anchor line (sync) and the answers of
the registered `IHoverProvider`s (async, one `Task`), and `HoverOperation` runs them with Monaco's timings (provider request
after half the hover delay, results after the whole delay, "Loading…" after three times the delay).

- Parts: `MarkerHoverPart` (order 1), `MarkdownHoverPart` (order 3), `LoadingHoverPart` (order 2000).
- Not ported: foreign-element anchors, the `Click` source, verbosity, color picker, code actions, status bar, related
  information, `hoverMessage` decorations.
- A provider hover without a range is kept (Monaco drops it); the anchor range is used.
- The controller is P8-10 and the popup P8-11.
