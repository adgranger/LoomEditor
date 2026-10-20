# P5-02 — `ModelDecorationOptions`: Monaco's options, `Register` / `CreateDynamic`

- **Status:** todo
- **Depends on:** P5-01
- **Size:** S

## Goal

`ModelDecorationOptions` grows from the phase 3 minimum (stickiness for tracked ranges) to the subset of Monaco's
options that phase 5 draws or uses, with Monaco's normalization (`cleanClassName`, defaults) and its two factories.
The types a decoration API needs (`ModelDeltaDecoration`, `ModelDecoration`) are added. No `TextModel` change except
the tracked-range options. ADR 0009, decisions 3 and 4.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model.ts` | 149–315 | `IModelDecorationOptions` → `ModelDecorationOptionsData` (the subset below) |
| `src/vs/editor/common/model.ts` | 379–410 | `IModelDeltaDecoration` → `ModelDeltaDecoration`, `IModelDecoration` → `ModelDecoration` |
| `src/vs/editor/common/model/textModel.ts` | 2355–2357 | `cleanClassName` |
| `src/vs/editor/common/model/textModel.ts` | 2481–2568 | `ModelDecorationOptions`: `register`, `createDynamic`, the constructor (subset), `EMPTY` |
| `src/vs/editor/common/model/textModel.ts` | 2573–2578 | `TRACKED_RANGE_OPTIONS` (now with Monaco's descriptions) |

## Files to create or change

- `src/LoomEditor.Core/Decorations/ModelDecorationOptionsData.cs` — create
- `src/LoomEditor.Core/Decorations/ModelDecorationOptions.cs` — change (rewrite)
- `src/LoomEditor.Core/Decorations/ModelDeltaDecoration.cs`, `ModelDecoration.cs` — create
- `src/LoomEditor.Core/Model/TextModel.TrackedRanges.cs` — change: `TrackedRangeOptions` built with `Register`
- `src/LoomEditor.Core/Decorations/README.md` — change (one line: phase 5 options)
- `tests/LoomEditor.Core.Tests/Decorations/ModelDecorationOptionsTests.cs` — create

## Design decisions (already made — do not change)

- Everything is **internal** (ADR 0009, decision 3). Namespace `LoomEditor.Core.Decorations`.
- **`ModelDecorationOptionsData`** (Monaco: the `IModelDecorationOptions` object literal) is a `sealed record` with
  `required string Description` and `init` properties, all optional, with Monaco's names: `Stickiness`
  (`TrackedRangeStickiness?`), `ClassName`, `ShouldFillLineOnLineBreak` (`bool?`), `IsWholeLine`, `ShowIfCollapsed`,
  `CollapseOnReplaceEdit`, `ZIndex` (`int?`), `GlyphMarginClassName`, `LinesDecorationsClassName`,
  `FirstLineDecorationClassName`, `MarginClassName`, `InlineClassName`, `InlineClassNameAffectsLetterSpacing`,
  `BeforeContentClassName`, `AfterContentClassName`, `HideInCommentTokens` (`bool?`), `HideInStringTokens` (`bool?`).
- **`ModelDecorationOptions`** (Monaco: the class) is a `sealed class` with a **private** constructor taking a
  `ModelDecorationOptionsData` and get-only properties of the same names (non-nullable where Monaco normalizes:
  `Stickiness` defaults to `AlwaysGrowsWhenTypingAtEdges`, `ZIndex` to 0, `bool?` to `false`; class names stay
  `string?` and go through `CleanClassName`). Plus `AffectsFont` (always `false`: font options are not ported; kept
  because `IntervalNode.SetOptions` reads it).
  - `public static ModelDecorationOptions Register(ModelDecorationOptionsData data)` and
    `public static ModelDecorationOptions CreateDynamic(ModelDecorationOptionsData data)` both return a new instance,
    like Monaco. Features keep the result of `Register` in a `static readonly` field (Monaco's pattern); `CreateDynamic`
    is for options built at run time.
  - `public static ModelDecorationOptions Empty { get; }` = `Register(new() { Description = "empty" })`.
  - Monaco's `_normalizeOptions` (accepts a raw object or an instance) is **not** needed: the decoration API takes
    `ModelDecorationOptions` only (see below).
- **`CleanClassName(string)`** (internal static on `ModelDecorationOptions`): replaces every character that is not
  `a`–`z`, `A`–`Z`, `0`–`9`, `-` or `_` with a space. Write it as a loop over the chars, not a regex (JavaScript's
  `/[^a-z0-9\-_]/gi` is ASCII-only; a .NET `IgnoreCase` class is not).
- **`ModelDeltaDecoration`**: `readonly record struct ModelDeltaDecoration(Range Range, ModelDecorationOptions Options)`.
- **`ModelDecoration`** (Monaco: `IModelDecoration`, which is the interval node itself):
  `readonly record struct ModelDecoration(string Id, int OwnerId, Range Range, ModelDecorationOptions Options)`.
- **Not ported** (write each list in the class XML doc): `blockClassName`, `blockIsAfterEnd`, `blockDoesNotCollapse`,
  `blockPadding` (block decorations); `hoverMessage`, `glyphMarginHoverMessage`, `lineNumberHoverMessage` (hover: phase
  8); `lineHeight`, `fontFamily`, `fontSize`, `fontWeight`, `fontStyle` (font decorations); `overviewRuler`, `minimap`
  (phase 6: no overview ruler or minimap yet); `glyphMargin` lanes; `lineNumberClassName`; `linesDecorationsTooltip`;
  `before` / `after` injected text (ADR 0009, decision 4); `textDirection`.
- `TrackedRangeOptions` in `TextModel.TrackedRanges.cs` becomes Monaco's four `Register` calls with Monaco's
  descriptions (`tracked-range-always-grows-when-typing-at-edges`, …), indexed by stickiness as before.

## Tests

- [ ] New: `Register_Defaults_AreMonacosDefaults` (stickiness `AlwaysGrowsWhenTypingAtEdges`, z-index 0, flags false,
  class names null, `AffectsFont` false)
- [ ] New: `Register_ClassNames_AreCleaned` (`"a.b c#d"` → `"a b c d"` in each of the eight class-name properties)
- [ ] New: `CleanClassName_KeepsLettersDigitsDashAndUnderscore` (`"Find-Match_2"` unchanged; `"é"` → `" "`)
- [ ] New: `Empty_HasTheDescriptionEmpty`
- [ ] New: `CreateDynamic_ReturnsANewInstanceEachTime`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (tracked-range tests of phase 3 untouched)
- [ ] `docs/PORTING.md` map updated (`textModel.ts` `ModelDecorationOptions`, `model.ts` decoration types)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The decoration API of `TextModel` (P5-03) and its event (P5-04).

## Suggested commit message

`feat(core): port Monaco's model decoration options`
