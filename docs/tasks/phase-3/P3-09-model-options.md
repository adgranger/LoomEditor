# P3-09 — Model options (`tabSize`, `indentSize`, `insertSpaces`, `trimAutoWhitespace`) and indent helpers

- **Status:** done
- **Depends on:** P3-08
- **Size:** S

## Goal

Tab, Enter and Backspace depend on the indentation options of the **model** (in Monaco they belong to the
`TextModel`, not to the editor). Add them with `UpdateOptions` and an `OptionsChanged` event, plus the two small model
helpers the cursor needs: `GetLineIndentColumn` and `NormalizePosition`. The control's `TabSize` property (P2-20)
now writes the model option (ADR 0006, decision 5).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

If a function is not exactly at the given line, search for its name.

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model.ts` | 566–640 | `class TextModelResolvedOptions` (constructor clamping, `originalIndentSize`, `equals`, `createChangeEvent`) |
| same | 654–660 | `interface ITextModelUpdateOptions` (without `bracketColorizationOptions`) |
| same | 1461–1486 | `const enum PositionAffinity` |
| `src/vs/editor/common/core/misc/textModelDefaults.ts` | 6–12 | `EDITOR_MODEL_DEFAULTS` (tabSize 4, indentSize 4, insertSpaces true, trimAutoWhitespace true) |
| `src/vs/editor/common/model/textModel.ts` | 193–203 | `DEFAULT_CREATION_OPTIONS` (`detectIndentation: false`) |
| same | 677–716 | `getOptions`, `updateOptions` |
| same | 2117–2128 | `normalizePosition` (returns the position: no injected text), `getLineIndentColumn` |
| same | 2166–2176 | `indentOfLine` |
| `src/vs/editor/common/textModelEvents.ts` | search `IModelOptionsChangedEvent` | the four booleans |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModelOptions.cs` — create: **public** `sealed class TextModelOptions` (Monaco: `TextModelResolvedOptions`)
- `src/LoomEditor.Core/Model/TextModelUpdateOptions.cs` — create: **public** `readonly record struct TextModelUpdateOptions`
- `src/LoomEditor.Core/Model/ModelOptionsChangedEventArgs.cs` — create: **public**
- `src/LoomEditor.Core/Model/PositionAffinity.cs` — create: `internal enum`
- `src/LoomEditor.Core/Model/TextModel.Options.cs` — create (partial): `Options`, `UpdateOptions`, `OptionsChanged`, `GetLineIndentColumn`, `NormalizePosition`
- `src/LoomEditor.Core/Model/TextModel.cs` — change: constructor gets an optional `TextModelOptions? options = null`
- `src/LoomEditor.Core/PublicAPI.Unshipped.txt` — update with `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Font.cs` — change: `TabSize` → `Model.UpdateOptions(new() { TabSize = value })`
- `tests/LoomEditor.Core.Tests/Model/TextModelOptionsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `public sealed class TextModelOptions`:
  - constructor `TextModelOptions(int tabSize = 4, int? indentSize = null, bool insertSpaces = true, bool trimAutoWhitespace = true, DefaultEndOfLine defaultEol = DefaultEndOfLine.LF)`;
    `indentSize == null` means Monaco's `'tabSize'` (indent size follows the tab size).
  - properties `TabSize`, `IndentSize`, `InsertSpaces`, `TrimAutoWhitespace`, `DefaultEol`, `IndentSizeIsTabSize`;
    `TabSize = Math.Max(1, tabSize)`, `IndentSize = Math.Max(1, indentSize ?? TabSize)`.
  - `static TextModelOptions Default { get; }` = Monaco's `DEFAULT_CREATION_OPTIONS` (4, 4, true, true, LF);
    note: `indentSize` is **4, not `'tabSize'`**, in `EDITOR_MODEL_DEFAULTS`.
  - `bool Equals(TextModelOptions other)` like Monaco's `equals` (implement `IEquatable<TextModelOptions>` and
    `GetHashCode`).
- `public readonly record struct TextModelUpdateOptions(int? TabSize = null, int? IndentSize = null, bool IndentSizeIsTabSize = false, bool? InsertSpaces = null, bool? TrimAutoWhitespace = null)`.
  `IndentSizeIsTabSize = true` is Monaco's `indentSize: 'tabSize'`.
- `UpdateOptions` is a line-by-line port of lines 689–716: keep the old value for each `null`; when the result equals
  the current options, do nothing; otherwise replace them and raise `OptionsChanged` with
  `ModelOptionsChangedEventArgs(bool TabSize, bool IndentSize, bool InsertSpaces, bool TrimAutoWhitespace)`.
- No indentation guessing (`detectIndentation` is false; guessing comes in phase 4–5).
- `internal int GetLineIndentColumn(int lineNumber)` = number of leading `' '`/`'\t'` + 1 (Monaco's `indentOfLine`
  iterates code points with `for…of`; spaces and tabs are single code units, so a plain `for` over `char` is identical).
- `internal Position NormalizePosition(Position position, PositionAffinity affinity)` returns `position` (no injected
  text in the model). Keep the parameter: the cursor calls it.
- The control: the `TabSize` dependency property keeps its validation (P2-20). Its changed callback calls
  `Model.UpdateOptions(new TextModelUpdateOptions(TabSize: value))`. The control subscribes to `Model.OptionsChanged`
  and calls `RecreateTextMeasurer()` when `TabSize` changed, reading the tab size from `Model.Options.TabSize`
  (single source of truth: the model). Do not call `RecreateTextMeasurer` twice for one change.

## Tests

- [x] New: `new TextModel("x").Options` is 4 / 4 / true / true; `IndentSizeIsTabSize` is false
- [x] New: `UpdateOptions(TabSize: 8)` raises `OptionsChanged` once with only `TabSize == true`; the same call again raises nothing
- [x] New: `UpdateOptions(IndentSizeIsTabSize: true)` then `UpdateOptions(TabSize: 2)` → `IndentSize == 2`
- [x] New: `TabSize: 0` is clamped to 1 (Monaco clamps; no exception in the model)
- [x] New: `GetLineIndentColumn` of `"\t  x"` is 4; of `""` is 1; of `"   "` is 4
- [x] Existing tests pass (the control's `TabSize` behavior is checked by hand: tab size 4 → 8 in the Gallery still moves tab-indented text)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` of `LoomEditor.Core`: only `TextModelOptions`, `TextModelUpdateOptions`,
      `ModelOptionsChangedEventArgs`, `TextModel.Options`, `TextModel.UpdateOptions`, `TextModel.OptionsChanged` and the new constructor parameter
- [x] `docs/PORTING.md`: map row `textModel.ts` mentions options (P3-09); behavior difference "no indentation guessing yet"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Indentation guessing, `getFormattingOptions`, bracket pair colorization options, trimming auto whitespace (P3-11).

## Suggested commit message

`feat(core): add tab size, indent size, insert spaces and trim auto whitespace model options`
