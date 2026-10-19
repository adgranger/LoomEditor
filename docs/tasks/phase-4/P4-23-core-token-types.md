# P4-23 — Core tokens: metadata, tokenization interfaces, null tokenizer, language ids, registry

- **Status:** done
- **Depends on:** P4-22
- **Size:** M

## Goal

`LoomEditor.Core/Tokens/` has Monaco's token vocabulary: the 32-bit metadata and its getters, `IState`,
`ITokenizationSupport` (encoded tokens only), the background tokenizer interfaces, the null tokenizer, the
language id codec, and a tokenization registry **instance**.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/encodedTokenAttributes.ts` | 9–124, 171–193 | `LanguageId`, `FontStyle`, `ColorId`, `StandardTokenType`, the bit-layout comment, `MetadataConsts`, `TokenMetadata` getters and `getPresentationFromMetadata`, `ITokenPresentation` |
| vscode: same file | 125–170 | **not ported**: `getClassNameFromMetadata`, `getInlineStyleFromMetadata` (CSS) |
| vscode: `src/vs/editor/common/languages.ts` | 35–38, 82–97, 116–175 | `ILanguageIdCodec`, `EncodedTokenizationResult` (without `fontInfo`), `ITokenizationSupport` (without the string `tokenize`, **and without `createBackgroundTokenizer`**: it needs `ITextModel` and `ContiguousMultilineTokens`, which come later; added in P4-29 with `IBackgroundTokenizer` and `IBackgroundTokenizationStore`), `IState` |
| vscode: same file | 2504–2507, 2546–2596 | `ITokenizationSupportChangedEvent`, `ITokenizationRegistry` (without the lazy-factory members) |
| vscode: `src/vs/editor/common/tokenizationRegistry.ts` | 12–113 | `TokenizationRegistry` (`handleChange`, `register`, `get`, `setColorMap`, `getColorMap`, `getDefaultBackground`) — **not** `registerFactory`, `getOrCreate`, `isResolved`, `TokenizationSupportFactoryData` |
| vscode: `src/vs/editor/common/languages/nullTokenize.ts` | 9–34 | `NullState`, `nullTokenize` (not ported: string tokens), `nullTokenizeEncoded` |
| vscode: `src/vs/editor/common/services/languagesRegistry.ts` | 19, 32–64 | `NULL_LANGUAGE_ID`, `LanguageIdCodec` |

## Files to create or change

- `src/LoomEditor.Core/Tokens/TokenMetadata.cs`, `MetadataConsts.cs`, `LanguageId.cs`, `FontStyle.cs`, `ColorId.cs`, `StandardTokenType.cs`, `TokenPresentation.cs` — create
- `src/LoomEditor.Core/Tokens/IState.cs`, `ITokenizationSupport.cs`, `EncodedTokenizationResult.cs` — create (`IBackgroundTokenizer.cs` and `IBackgroundTokenizationStore.cs` move to P4-29)
- `src/LoomEditor.Core/Tokens/NullTokenize.cs`, `LanguageIdCodec.cs`, `TokenizationRegistry.cs`, `TokenizationSupportChangedEventArgs.cs` — create
- `tests/LoomEditor.Core.Tests/Tokens/TokenMetadataTests.cs`, `LanguageIdCodecTests.cs`, `TokenizationRegistryTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008 decision 4: `TokenizationRegistry` is an instance, one per editor. `onDidChange` → `event EventHandler<TokenizationSupportChangedEventArgs>? Changed`.
- Core does not know `LoomEditor.Themes`, so Monaco's color map (`Color[]`) becomes `IReadOnlyList<uint>` in Core
  (ARGB, index 0 unused, as Monaco). View converts it from the theme's `ThemeColor`s (P4-42).
- Metadata is `uint`; token arrays `uint[]` (phase-4/README.md, trap 1).
- `IState` is `IState Clone()` and `bool Equals(IState? other)`; `NullState.Instance` is a singleton.
- `LanguageIdCodec` registers `vs.editor.nullLanguage` = 0 and `plaintext` = 1 in its constructor, as upstream.
- Everything is `internal`.

## Tests

No upstream unit tests for these files. New:

- [x] `TokenMetadata_Getters_ReadEachField` (build metadata with the `MetadataConsts` offsets)
- [x] `TokenMetadata_GetPresentation_FontStyles`
- [x] `NullTokenizeEncoded_OneTokenWithDefaultMetadata`
- [x] `LanguageIdCodec_UnknownLanguage_EncodesToNull_AndDecodesToNullLanguage`
- [x] `TokenizationRegistry_RegisterAndSetColorMap_RaiseChanged`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated; "Behavior differences": registry instance, no lazy factories, no CSS class names
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`LineTokens` (P4-24). vscode-textmate's own attributes (done in P4-10).

## Suggested commit message

`feat(core): add Monaco's token types and tokenization registry`
