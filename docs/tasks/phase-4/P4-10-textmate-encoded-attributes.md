# P4-10 — vscode-textmate token metadata (`encodedTokenAttributes.ts`)

- **Status:** done
- **Depends on:** P4-09
- **Size:** S

## Goal

The 32-bit token metadata (language id, standard token type, balanced-bracket bit, font style, foreground,
background) can be encoded and read on the vscode-textmate side, with its tests.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/encodedTokenAttributes.ts` | 67–178, 201–246 | `EncodedTokenAttributes` (`toBinaryStr`, `print`, getters, `set`), `EncodedTokenDataConsts`, `StandardTokenType`, `toOptionalTokenType`, `fromOptionalTokenType`, `OptionalStandardTokenType` |
| textmate: `src/encodedTokenAttributes.ts` | 21–63 | **not ported**: `FontAttribute` (ADR 0008, decision 3) |
| textmate: `src/tests/grammar.test.ts` | 34–107 | the metadata tests (list below) |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/EncodedTokenAttributes.cs`, `StandardTokenType.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/EncodedTokenAttributesTests.cs` — create

## Design decisions (already made — do not change)

- Metadata is `uint` (phase-4/README.md, trap 1). `set(…)` takes `int` arguments where upstream passes `-1`/`0` for
  "keep" (font style `NotSet`, color `0`, language id `0`, token type `NotSet`), as upstream.
- `toBinaryStr` pads to 32 characters like upstream (`padStart(32, '0')`).

## Tests

- [x] Port: `'StackElementMetadata works'`, `'StackElementMetadata can overwrite languageId'`, `'… can overwrite tokenType'`,
      `'… can overwrite font style'`, `'… can overwrite font style with strikethrough'`, `'… can overwrite foreground'`,
      `'… can overwrite background'`, `'… can overwrite balanced backet bit'`, `'… can work at max values'` (exact names from the file)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Monaco's `TokenMetadata` in Core (P4-23).

## Suggested commit message

`feat(textmate): port the encoded token attributes`
