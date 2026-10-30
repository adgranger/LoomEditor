# Phase 13 — manual test list

What unit tests cannot check: the host API seen from an app — drawing, scroll sync, zones with content, gutter clicks. Fill the **Result** line of each check
(`pass` / `fail` + a note, or `pending`).

**Machine:** Windows 11, a mouse with a wheel.

**Gallery setup:** the Gallery controls of this phase are added by P13-09.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P13-04 — Decorations from the host

1. **Conflict colors** (Gallery, P13-09): lines 10–12 get the built-in `line-insert` background and lines 14–16 the `line-delete` background.
   Result:
2. After typing a new line above line 10, the tint moves down one line and does not grow when typing at the end of line 12.
   Result:
3. **Clear decorations** removes every tint.
   Result:
4. The overview ruler shows one mark per decorated block, in the host color.
   Result:
5. The same on the modified side of the diff demo (`Modified.CreateDecorationsCollection()`).
   Result:
