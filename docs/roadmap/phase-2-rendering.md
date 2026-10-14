# Phase 2 — Rendering: plain text, virtualized viewport, gutter, caret, scrolling

**Done when:** a 100 MB file scrolls fluidly.

Step-by-step task cards: [docs/tasks/README.md](../tasks/README.md) (P2-01 … P2-23). Design: [ADR 0005](../adr/0005-viewport-rendering.md).

- [ ] View lines and viewport (`LoomEditor.View/Layout/`): Monaco's `Scrollable`, `LinesLayout`, `ViewLayout`, `ViewModel` subset — P2-01 … P2-10
- [ ] Only visible lines drawn, on a viewport-sized canvas (ADR 0005 replaces `CanvasVirtualControl`); text layout caching — P2-13, P2-14
- [ ] Real text layout: tabs, wide characters, proportional fonts, DPI changes — P2-06, P2-12, P2-20
- [ ] Gutter, caret (blinking), scrollbars, smooth scrolling — P2-15 … P2-19
- [ ] Proof: 100 MB Gallery demo and reference numbers — P2-21, P2-22
