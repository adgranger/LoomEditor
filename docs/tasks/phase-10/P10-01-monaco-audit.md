# P10-01 — Audit: what of Monaco's editor is not in LoomEditor yet

- **Status:** todo
- **Depends on:** P9-33
- **Size:** S (documentation only, no code)

## Goal

Section G of the backlog ("To verify before writing cards") is done: three tables in `docs/roadmap/phase-10-audit.md` say, for every Monaco `contrib` folder, every editor option and every core command, whether it is **ported**, **deferred** (with the reason) or **out of scope**. No source file changes.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

Sources: the folder listing of the pinned commit (one request, then work offline):

```powershell
Invoke-RestMethod "https://api.github.com/repos/microsoft/vscode/git/trees/0c9c0a4295a9222e12a605537738719455ae3056?recursive=1" |
    Select-Object -ExpandProperty tree | Where-Object { $_.path -like 'src/vs/editor/contrib/*' -and $_.type -eq 'tree' } | Select-Object -ExpandProperty path
```

| File | Lines | What to read |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | the `EditorOption` enum (search `export const enum EditorOption`) and the `EditorOptions` object (search `export const EditorOptions = {`) | the list of option names |
| `src/vs/editor/browser/coreCommands.ts` | whole file | the registered command ids |

## Files to create or change

- `docs/roadmap/phase-10-audit.md` — create
- `docs/roadmap/phase-10-editor-completeness.md` — change: tick or annotate backlog items the audit shows are already done, add the items it finds (section "Found by the audit"), correct the statement of section A, first bullet (see below)

## Design decisions (already made — do not change)

- **Table 1 — `contrib` folders.** One row per folder of `src/vs/editor/contrib/` (about 60). Columns: folder, status (`ported` / `partial` / `deferred` / `out of scope`), where (the `docs/PORTING.md` row or the `src/LoomEditor.*` folder), reason when not `ported`. A folder is `ported` only if `docs/PORTING.md` has a row for a file of it **and** the code exists (`Get-ChildItem src -Recurse -Filter <Name>*.cs`). Seed list found while writing the phase, **to confirm**: not ported at all — `anchorSelect`, `caretOperations`, `cursorUndo`, `dnd`, `gotoError`, `inPlaceReplace`, `insertFinalNewLine`, `lineSelection`, `longLinesHelper`, `middleScroll`, `smartSelect`, `snippet`, `codeAction`, `codelens`, `colorPicker`, `format`, `inlayHints`, `inlineCompletions`, `links`, `linkedEditing`, `parameterHints`, `peekView`, `quickAccess`, `rename`, `semanticTokens`, `unicodeHighlighter`, `documentSymbols`, `gotoSymbol`, `contextmenu`; `fontZoom` is in phase 10 (P10-06).
- **Table 2 — options.** One row per `EditorOption` (about 150). Columns: option, status (`exposed` = a control property or `Editor*Options` field exists / `internal default` = Monaco's default is hard-coded / `missing`), where, the phase-10 card or "later". Search the code with `Select-String -Path src -Pattern <Option>` (the C# name is the PascalCase of the option).
- **Table 3 — commands.** One row per command id of `coreCommands.ts` and per `registerEditorAction` id of the ported contribs: registered (`CommandRegistry` id exists) or missing.
- Every `missing` row gets either a phase-10 card id, or "later" with a reason, or "out of scope" with a reason. The audit does **not** create cards.
- **Correction to write in the backlog (section A, first bullet):** the sentence "Monaco itself splits the line into runs and disables ligatures while the caret is inside one" is **wrong** at the pinned commit: `src/vs/editor/browser/viewParts/viewLines/viewLine.ts` has no such code (the only ligature reads are `options.fontLigatures` when building the `RenderLineInput`, line 170, and `splitLargeTokens(…, !input.isBasicASCII || input.fontLigatures)`, `viewLineRenderer.ts` line 524). Monaco turns ligatures on or off with `font-feature-settings` and measures positions from the DOM. Replace the sentence by: "Monaco sets `font-feature-settings` and reads positions from the DOM (`FontMeasurements`: `isMonospace` is false when ligatures are on, `fontMeasurements.ts` line 219); there is no caret special case".

## Tests

- [ ] None (documentation). `docs/roadmap/phase-10-audit.md` renders: every table has the same number of columns on every row.

## Acceptance criteria

- [ ] The three tables exist and cover every folder, option and command (counts stated at the top of the file, e.g. "61 folders, 148 options, 74 commands")
- [ ] Every `missing` / `deferred` row has a card id, "later" or "out of scope", each with a reason
- [ ] The backlog file is corrected as above
- [ ] `dotnet build LoomEditor.slnx -c Release` is **not** required (docs only); `git status` shows only `docs/` files
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Writing cards, porting anything, changing code.

## Suggested commit message

`docs: audit Monaco's editor against LoomEditor for phase 10`
