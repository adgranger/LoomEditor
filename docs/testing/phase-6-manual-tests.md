# Phase 6 — manual test list

What unit tests cannot check: how wrapped lines look and behave under a real UI, and the Native AOT build.
Run it on the **Native AOT** build of the Gallery, on Windows 11:

```powershell
dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64
```

and fill the **Result** column (`pass` / `fail` + a note).

- Windows build:
- Date and tester:

## 1. Word wrap (Gallery: the **Word wrap** switch)

| # | Steps | Expected | Result |
|---|---|---|---|
| 1.1 | Load a sample with long lines, turn **Word wrap** on | Long lines wrap at the window width; turning it off restores single lines | |
| 1.2 | Resize the window | The lines rewrap to the new width | |
| 1.3 | Type at the end of a wrapped line | The caret stays visible | |
| 1.4 | Home / End on a wrapped line, then Up / Down | They move by view line | |
| 1.5 | Fold a region whose first line wraps | The fold shows on the wrapped line and unfolds correctly | |
| 1.6 | Look at the gutter | Line numbers show only on the first view line of a wrapped line | |
