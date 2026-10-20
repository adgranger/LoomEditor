# Phase 4 — manual test list

What unit tests cannot check: how the colors look, the background thread under a real UI, the Native AOT build.
Run it on the **Native AOT** build of the Gallery, on Windows 11:

```powershell
dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64
```

and fill the **Result** column (`pass` / `fail` + a note).

- Windows build:
- Date and tester:

The Gallery has a **Language** picker (plain text and the built-in languages), a **Theme** picker (the ten VS Code
default themes) and a **Large C# (20 000 lines)** button. Choosing a language loads a short sample of it.

## 1. Languages and themes

| # | Steps | Expected | Result |
|---|---|---|---|
| 1.1 | Pick each language in turn with the theme **Dark Modern** | Keywords, strings, comments, numbers and types have different colors, like in VS Code Dark Modern | |
| 1.2 | Same, with **Light Modern** | Same, with the light palette; the text stays readable | |
| 1.3 | HTML: look at the `<style>` and `<script>` blocks | The CSS and the JavaScript inside are colored as CSS and JavaScript | |
| 1.4 | Markdown: look at the fenced `csharp` block | The code is colored as C# | |
| 1.5 | Pick **plaintext** | No colors: all text has the default foreground | |
| 1.6 | Switch the theme while a C# sample is shown, through all ten themes | The colors change at once, without flicker; the editor background, the line numbers, the selection and the caret follow the theme | |
| 1.7 | **Dark High Contrast** and **Light High Contrast** | Strong colors; bold or italic where the theme asks for it | |

## 2. Background tokenization

| # | Steps | Expected | Result |
|---|---|---|---|
| 2.1 | Click **Large C# (20 000 lines)**, then drag the scrollbar to the end | Colors are there at once at the end (the visible lines are tokenized first); the rest fills in a moment later | |
| 2.2 | In the large file, go to the top and type `/*` at the very beginning | The whole file is recolored as a comment, without freezing the UI (scroll and type stay responsive) | |
| 2.3 | Delete the `/*` again | The colors come back | |
| 2.4 | Type quickly in the middle of the large file | Typing stays fluid; the typed line is colored | |
| 2.5 | Select another language while the large file is still filling in | No crash; the new language is applied | |

## 3. Native AOT

| # | Steps | Expected | Result |
|---|---|---|---|
| 3.1 | Start the published Gallery; repeat 1.1, 1.6 and 2.1 | Same results as the other builds; `onig.dll` is next to the executable | |
