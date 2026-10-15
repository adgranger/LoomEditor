# Phase 2 — Rendering: plain text, virtualized viewport, gutter, caret, scrolling

**Done when:** a 100 MB file scrolls fluidly. ✅ Done: 60 fps, p95 17 ms in a Native AOT build ([docs/performance.md](../performance.md)).

Step-by-step task cards: [docs/tasks/README.md](../tasks/README.md) (P2-01 … P2-23). Design: [ADR 0005](../adr/0005-viewport-rendering.md).

- [x] View lines and viewport (`LoomEditor.View/Layout/`): Monaco's `Scrollable`, `LinesLayout`, `ViewLayout`, `ViewModel` subset — P2-01 … P2-10
- [x] Only visible lines drawn, on a viewport-sized canvas (ADR 0005 replaces `CanvasVirtualControl`); text layout caching — P2-13, P2-14
- [x] Real text layout: tabs, wide characters, proportional fonts, DPI changes — P2-06, P2-12, P2-20
- [x] Gutter, caret (blinking), scrollbars, smooth scrolling — P2-15 … P2-19
- [x] Proof: 100 MB Gallery demo and reference numbers — P2-21, P2-22
