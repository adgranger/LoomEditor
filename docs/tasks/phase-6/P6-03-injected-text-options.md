# P6-03 — `InjectedTextOptions` and the `before` / `after` decoration options

- **Status:** done
- **Depends on:** P5-96
- **Size:** S

## Goal

A decoration can carry injected text: `ModelDecorationOptionsData.Before` / `.After` accept an `InjectedTextOptions`, and
`ModelDecorationOptions.Before` / `.After` expose the normalized `ModelDecorationInjectedTextOptions`. Nothing reads them
yet (P6-04 …).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model.ts` | 282–292 | the doc comments of `before` / `after` |
| same | 329–367 | `interface InjectedTextOptions` (members listed in the decisions) |
| same | 369–374 | `enum InjectedTextCursorStops` (`Both`, `Right`, `Left`, `None`) |
| `src/vs/editor/common/model/textModel.ts` | 2454–2479 | `ModelDecorationInjectedTextOptions` (`from`, the constructor normalization incl. the `widthInEm` rule) |
| same | 2520–2523, 2559–2562 | the `after` / `before` fields of `ModelDecorationOptions` and how the constructor fills them |
| `src/vs/editor/test/common/model/modelDecorations.test.ts` | 116–135 | `injected text width must be finite and non-negative` |

## Files to create or change

- `src/LoomEditor.Core/Decorations/InjectedTextCursorStops.cs` — create (header `model.ts`)
- `src/LoomEditor.Core/Decorations/InjectedTextOptions.cs` — create: `internal sealed record InjectedTextOptions` (the raw input)
- `src/LoomEditor.Core/Decorations/ModelDecorationInjectedTextOptions.cs` — create (header `textModel.ts`): the normalized class
- `src/LoomEditor.Core/Decorations/ModelDecorationOptionsData.cs` — change: add `Before`, `After` (type `InjectedTextOptions?`), and
  update its `<remarks>` (the `before` and `after` injected text are no longer in the "Not ported" list)
- `src/LoomEditor.Core/Decorations/ModelDecorationOptions.cs` — change: add `After`, `Before` (type `ModelDecorationInjectedTextOptions?`)
- `tests/LoomEditor.Core.Tests/Decorations/ModelDecorationInjectedTextOptionsTests.cs` — create
- the test file that holds the P5-03 ports of `modelDecorations.test.ts` (find it with
  `grep -rln "Monaco: 'decorations" tests/LoomEditor.Core.Tests`) — change: add the ported test

## Design decisions (already made — do not change)

- `InjectedTextOptions` members (all `init`): `required string Content`, `string? InlineClassName`,
  `bool InlineClassNameAffectsLetterSpacing`, `double? WidthInEm`, `object? AttachedData`, `InjectedTextCursorStops? CursorStops`.
  **Not ported:** `tokens` (a `TokenArray` of pre-tokenized injected text: not needed; `GetLineTokensWithInjections`, P6-16,
  always gives injected text the default token metadata). Say so in the XML doc.
- `ModelDecorationInjectedTextOptions` is an `internal sealed class` with a private constructor and
  `From(InjectedTextOptions)`. It normalizes like Monaco: `Content ?? ""` (never null), `InlineClassName` empty → `null`,
  `WidthInEm` kept only when finite and `>= 0` (else `null`), `AttachedData` and `CursorStops` kept as they are.
  `InlineClassName` is stored raw (`options.inlineClassName || null`, unlike the class names of the decoration itself): do
  **not** run `ModelDecorationOptions.CleanClassName` on it.
- `ModelDecorationOptions` builds `After` / `Before` with `From` when the data has them (Monaco: `options.after ? … : null`).
- An `InjectedTextOptions` is "injected text" only when it exists; whether it takes space is decided by `LineInjectedText` (P6-04).
- The Monaco test asserts `widthInEm` values `-1, NaN, Infinity` → `undefined`, `0` and `1.5` kept; port it as a theory.

## Tests

- [x] Port: `Editor Model - Decorations` › `injected text width must be finite and non-negative` (`[Theory]` over the inputs; read
      the expected array at lines 126–132)
- [x] New: `From_NullContent_BecomesEmpty`, `From_EmptyInlineClassName_BecomesNull`, `From_KeepsAttachedDataAndCursorStops`
- [x] New: `Register_WithBeforeAndAfter_ExposesNormalizedOptions`, `Register_WithoutInjectedText_HasNullBeforeAndAfter`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: create the section **"Tests ported in phase 6"** (same table format as "Tests ported in phase 5",
      placed above it; every later phase 6 card adds or updates its rows there) with the row `modelDecorations.test.ts`
      (the "injected text width" gap is closed); update the phase 5 row of `modelDecorations.test.ts` to point to it
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`LineInjectedText`, reading injected text from the model, events, rendering.

## Suggested commit message

`feat(core): add injected text options to model decorations`
