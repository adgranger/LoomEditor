# P13-04 — `LoomEditor.CreateDecorationsCollection` for the host

- **Status:** done
- **Depends on:** P13-03
- **Size:** S

**Recommended model:** Sonnet

## Goal

A host highlights lines from its own data: `var blame = editor.CreateDecorationsCollection(); blame.Set([new EditorDecoration(new Range(3, 1, 5, 1), recent)]);`. The phase 13
manual test file exists.

## Sources

Sources: none (specified in this card; [phase-13/README.md](README.md) decisions 4 and 10). Read first: `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs`
(`CreateDecorationsCollection`), `src/LoomEditor.View/Features/EditorContext.cs` (`CreateDecorationsCollection`), `src/LoomEditor.View/Decorations/DecorationsCollection.cs`
(P13-02), `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`, `docs/testing/phase-12-manual-tests.md` (header to copy), `docs/tasks/phase-12/P12-09-editor-public-api.md` (how a
WinUI card writes its manual section).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs` — change
- `src/LoomEditor.WinUI/Controls/LoomEditor.Decorations.cs` — create
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs`, `HideUnchangedRegionsController.cs` — change (the rename)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `docs/testing/phase-13-manual-tests.md` — create (header + section "P13-04")

## Design decisions (already made — do not change)

- **Rename first** (README decision 4): the internal `LoomEditor.CreateDecorationsCollection(IReadOnlyList<ModelDeltaDecoration>?)` becomes
  `CreateModelDecorationsCollection`; update every caller (`grep -rn "CreateDecorationsCollection" src/LoomEditor.WinUI samples`: `LoomDiffEditor.cs`,
  `HideUnchangedRegionsController.cs`). `EditorContext.CreateDecorationsCollection` (View) keeps its name.
- **New file `LoomEditor.Decorations.cs`** (partial of `LoomEditor`):
  ```csharp
  public DecorationsCollection CreateDecorationsCollection(IReadOnlyList<EditorDecoration>? decorations = null)
  ```
  creates `new DecorationsCollection(_context.CreateDecorationsCollection())`, then `Set(decorations)` when it is not null. XML doc: Monaco `createDecorationsCollection`; the
  collection belongs to the caller and lives with this editor's model; it survives `Text` / `ResetText` (the model keeps decorations, collapsed where the text was replaced:
  the host sets them again after loading new text); `<exception cref="ArgumentException">` (an item without options). Threading: UI thread only.
- A side of a `LoomDiffEditor` is a `LoomEditor`: `diffEditor.Modified.CreateDecorationsCollection()` works; nothing is added to `LoomDiffEditor` (README section 2).
- `PublicAPI.Unshipped.txt` (WinUI) gains exactly the method line.
- **`docs/testing/phase-13-manual-tests.md`**: title "Phase 13 — manual test list", a short intro (what unit tests cannot check: the host API seen from an app — drawing, scroll
  sync, zones with content, gutter clicks), **Machine** and **Gallery setup** paragraphs saying "the Gallery controls are added by P13-09", the blank build / date lines copied
  from phase 12, then `## P13-04 — Decorations from the host` with numbered checks, each with a measurable expectation and a "Result:" blank: (1) **Conflict colors**
  (Gallery, P13-09): lines 10–12 get the built-in `line-insert` background and 14–16 `line-delete`; (2) after typing a new line above line 10, the tint moves down one line
  and does not grow when typing at the end of line 12; (3) **Clear decorations** removes every tint; (4) the overview ruler shows one mark per decorated block in the
  host color; (5) the same on the modified side of the diff demo (`Modified.CreateDecorationsCollection()`).

## Tests

- [x] None automated (no WinUI test project; P13-02 covers the collection). The build proves the rename (no ambiguous call left).
- [x] Manual: section "P13-04" written (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `grep -rn "CreateModelDecorationsCollection" src` finds the definition and its callers; `PublicAPI.Unshipped.txt` (WinUI) gains one line
- [x] The manual section is written; the card's Result section says it was not run
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Host styles (P13-03, done), Gallery buttons (P13-09), a change event on the collection.

## Suggested commit message

`feat(winui): let the host create decoration collections on the editor`

## Result

Build 0 warnings, 3780 tests green. The manual section "P13-04" is written and was not run by the agent.
