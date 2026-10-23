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

## 2. Indent and bracket pair guides (Gallery: the **Guides** combo box)

| # | Steps | Expected | Result |
|---|---|---|---|
| 2.1 | Load a nested code sample, choose **Indent only** | A 1 px vertical guide at each indent level; the guide of the cursor's block is brighter | |
| 2.2 | Move the cursor into another block | The brighter guide follows the cursor | |
| 2.3 | Choose **Indent + bracket pairs** | Guides take the bracket colors; the active pair shows horizontal lines at its first or last line | |
| 2.4 | Choose **None** | No guide is drawn | |
| 2.5 | Fold a block, then unfold it | The guides follow folding and stay aligned with the text | |
| 2.6 | Turn **Word wrap** on with an indented, long line | The guides follow the wrapped lines and none is drawn past the wrapping column | |
| 2.7 | Scroll horizontally | The guides scroll with the text | |

## 3. Minimap font sampling (scales above 2)

The Win2D font sampler needs a real `CanvasDevice`, which the Rendering tests never create.

| # | Steps | Expected | Result |
|---|---|---|---|
| 3.1 | Call `Win2DMinimapFontSampler.CreateSampleData(device, "Consolas")` (the minimap at `scale: 3`, P6-52) | The array has `16 * 10 * 4 * 96` bytes and the cell of `M` has non-zero alpha pixels | |

## 4. Minimap (Gallery: the **Minimap** toggle)

| # | Steps | Expected | Result |
|---|---|---|---|
| 4.1 | Open a long code sample | The minimap is visible on the right, with the characters of the text | |
| 4.2 | Click in the minimap, then drag the slider | The editor reveals the clicked line; dragging the slider scrolls the editor | |
| 4.3 | Hover the minimap | The slider appears and goes away when the mouse leaves | |
| 4.4 | Set `MinimapSide` to `Left` (code) | The minimap and the slider move to the left; the gutter moves right | |
| 4.5 | Set `MinimapRenderCharacters` to `false` | The minimap shows color blocks | |
| 4.6 | Open find (Ctrl+F) and search a word that occurs often | The matches are marked in the minimap | |
| 4.7 | Select a word, then fold a region | The other occurrences and the folded region are marked in the minimap | |
| 4.8 | Turn the **Minimap** toggle off, then on | The minimap disappears and the text uses the freed width; it comes back | |

## 5. Sticky scroll (Gallery: the **Sticky scroll** toggle)

| # | Steps | Expected | Result |
|---|---|---|---|
| 5.1 | Open a long C# sample and scroll inside a method | The lines that open the enclosing scopes stick at the top and follow the scroll | |
| 5.2 | Scroll inside nested scopes (class, method, block) | The sticky lines stack, at most `StickyScrollMaxLineCount` | |
| 5.3 | Click a sticky line | The editor reveals that line | |
| 5.4 | Click the chevron of a sticky line | The region folds (and unfolds) | |
| 5.5 | Turn the **Sticky scroll** toggle off, then on | The sticky widget disappears, then comes back | |
