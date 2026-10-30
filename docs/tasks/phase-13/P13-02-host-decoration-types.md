# P13-02 — Host decorations: `EditorDecoration`, `EditorDecorationOptions`, `DecorationsCollection`

- **Status:** done
- **Depends on:** P13-01
- **Size:** M

**Recommended model:** Sonnet

## Goal

The public decoration types a host uses exist in View and are unit tested: a host describes a decoration with a `Range` and a reusable `EditorDecorationOptions`, and a
`DecorationsCollection` sets, appends, clears and reads them on the model — without any Monaco internal type in the public API. (The control's factory method comes in
P13-04.)

## Sources

Sources: none (specified in this card; [phase-13/README.md](README.md) decisions 1–3, 8–10). Read first: `src/LoomEditor.View/Features/EditorDecorationsCollection.cs`
(the internal port this card wraps), `src/LoomEditor.Core/Decorations/ModelDecorationOptions.cs`, `ModelDecorationOptionsData.cs`, `ModelDecorationOverviewRulerOptions.cs`,
`DecorationColor.cs`, `OverviewRulerLane.cs`, `src/LoomEditor.Core/Model/TrackedRangeStickiness.cs`, `src/LoomEditor.Themes/ThemeColor.cs`,
`tests/LoomEditor.View.Tests/Features/EditorDecorationsCollectionTests.cs` (how a collection is built over a `TextModel` in tests).

## Files to create or change

- `src/LoomEditor.View/Decorations/EditorDecoration.cs` — create
- `src/LoomEditor.View/Decorations/EditorDecorationOptions.cs` — create
- `src/LoomEditor.View/Decorations/DecorationsCollection.cs` — create
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.View`)
- `tests/LoomEditor.View.Tests/Decorations/DecorationsCollectionTests.cs` — create
- `tests/LoomEditor.View.Tests/Decorations/EditorDecorationOptionsTests.cs` — create

## Design decisions (already made — do not change)

- **`EditorDecoration`**: `public readonly record struct EditorDecoration(Range Range, EditorDecorationOptions Options);` XML doc on the type and both parameters
  ("Monaco: `IModelDeltaDecoration`"). Threading remark: immutable.
- **`EditorDecorationOptions`**: `public sealed class` (not a record, README decision 8) with `init` properties and XML doc on each:
  - `string? ClassName` — "The class of the decorated range: its background and border (Monaco: `className`)."
  - `bool IsWholeLine` — "The decoration covers whole lines, from the left of the text to the right edge (Monaco: `isWholeLine`)."
  - `string? InlineClassName` — "The class that colors the decorated text (Monaco: `inlineClassName`)."
  - `string? MarginClassName` — "The class drawn across the whole gutter of the decorated lines (Monaco: `marginClassName`)."
  - `string? LinesDecorationsClassName` — "The class drawn in the narrow strip between the line numbers and the text (Monaco: `linesDecorationsClassName`)."
  - `ThemeColor? OverviewRulerColor`, `ThemeColor? OverviewRulerDarkColor` — "The mark in the overview ruler, in light / dark themes; no mark when `OverviewRulerColor` is null."
  The class `<remarks>`: a class name is a built-in class or one registered with `CustomDecorationStyles` (P13-03; write the `<see cref>` in P13-03, plain text here);
  unknown names draw nothing; reuse one instance for many decorations; immutable, thread-safe.
- **Conversion** (internal): `internal ModelDecorationOptions ToModelOptions()` creates on first call, then returns the same instance (a private field; a race creates two
  equal objects, harmless — say so in a comment). It calls `ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "host-decoration", Stickiness =
  TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, ClassName, IsWholeLine, InlineClassName, MarginClassName, LinesDecorationsClassName, OverviewRuler = … })`. The overview
  ruler is `null` when `OverviewRulerColor` is null, else `new ModelDecorationOverviewRulerOptions(DecorationColor.FromValue(Hex(light)), dark is null ? null :
  DecorationColor.FromValue(Hex(dark)), OverviewRulerLane.Center)` with `Hex(c) = $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}"` (invariant culture; VS Code's `#RRGGBBAA`).
- **`DecorationsCollection`**: `public sealed class`, `internal DecorationsCollection(EditorDecorationsCollection inner)`. Members (XML doc, Monaco names in the doc):
  - `int Count { get; }` — `inner.Count`;
  - `void Set(IReadOnlyList<EditorDecoration> decorations)` — replaces all (Monaco: `set`);
  - `void Append(IReadOnlyList<EditorDecoration> decorations)` — adds (Monaco: `append`);
  - `void Clear()`;
  - `Range? GetRange(int index)` — the current range, `null` for a bad index or a removed decoration;
  - `IReadOnlyList<Range> GetRanges()`.
  `Set` / `Append`: `ArgumentNullException` for a null list; `ArgumentException` (param `decorations`, message names the index) when an item has `Options == null`; then one
  `ModelDeltaDecoration(range, options.ToModelOptions())` per item, in order. Ranges are passed as given: the model validates them (README decision 6). Threading remark: UI
  thread only. Class doc says "Monaco: `IEditorDecorationsCollection`; named `DecorationsCollection` so it does not clash with the internal port (README decision 4)".
- `PublicAPI.Unshipped.txt` (View) gains the lines of these three types only (the record struct's generated members included). Paste them in the card's Result section.

## Tests

- [x] New (`EditorDecorationOptionsTests`): every field maps to the internal options (class names, `IsWholeLine`); `Description == "host-decoration"`; stickiness is
  `NeverGrowsWhenTypingAtEdges`; `ToModelOptions()` twice returns the same instance; `OverviewRulerColor = new ThemeColor(0x80, 0x12, 0x34, 0x56)` → color value
  `"#12345680"`, dark color `null`, lane `Center`; with a dark color both are set; no ruler color → `OverviewRuler == null`.
- [x] New (`DecorationsCollectionTests`, model `"a\nb\nc\nd"`): `Set` two decorations → `Count == 2`, the model has two decorations with those options; `Set` again with one →
  `Count == 1`; `Append` one → `Count == 2`, `GetRanges()` in insertion order; `Clear` → `Count == 0`; insert a line at the top → `GetRange(0)` moved down one line; typing at
  the end of a decorated range does not grow it; `GetRange(-1)` and `GetRange(Count)` → `null`; `Set(null!)` → `ArgumentNullException`; `Set([default])` →
  `ArgumentException` whose message contains `0`; two decorations sharing one options object share one internal options instance.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` (View) gains only the three types' lines; Core's file is unchanged (no Monaco type became public)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`LoomEditor.CreateDecorationsCollection` (P13-04), host styles (P13-03), a change event, hover messages, injected text, glyph-margin icons, z-index, minimap marks, stickiness
as an option.

## Suggested commit message

`feat(view): add public host decoration types`

## Result

- Added `EditorDecoration`, `EditorDecorationOptions`, `DecorationsCollection` (`src/LoomEditor.View/Decorations/`), 12 tests.
- `PublicAPI.Unshipped.txt` (View) gained 37 lines: only these three types (the record struct's generated members included); Core's file is unchanged.
- `DecorationsCollection` carries a justified `SuppressMessage` for CA1711 (suffix `Collection`): the name is fixed by README decision 4.
