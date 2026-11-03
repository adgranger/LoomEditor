# Phase 15 — manual test list

What the headless Avalonia tests cannot check: the control seen from an app on each operating system, real fonts, real DPI changes, real IME windows and
screen readers. Fill the **Result** line of each check (`pass` / `fail` + a note, or `pending`). Agents write these checks; they do not run them.

**Machines:** Windows 11, macOS 15 or later, Ubuntu 24.04.

**Gallery setup:** run the Avalonia Gallery (P15-31) on each machine.

- Build (Debug / Release / Native AOT):
- Operating system and display scale:
- Date and tester:

## P15-08 — Template, draw loop, font, zoom

1. The Gallery shows a C# file in the editor; the text is drawn on the theme background and the first line is not clipped.
   **Result:** pending
2. The font is the default monospace of the operating system (Cascadia Mono / Consolas on Windows, Menlo on macOS, DejaVu Sans Mono on Linux).
   **Result:** pending
3. Move the window to a display with a 200 % scale (or change the scale in the system settings): the text is re-rendered and stays crisp.
   **Result:** pending
4. Change the font size and the zoom level from the Gallery controls: the line height follows and the text is not blurry.
   **Result:** pending
