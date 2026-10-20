# P5-12 — `CharacterPairSupport`, `OnEnterSupport` and `IndentRulesSupport`

- **Status:** done
- **Depends on:** P5-11
- **Size:** M

## Goal

Core has the three rule engines of a language configuration: auto-closing / surrounding pairs
(`CharacterPairSupport`), on-enter rules (`OnEnterSupport`) and indentation rules (`IndentRulesSupport`).
`EditorAutoIndentStrategy` moves to Core because `OnEnterSupport` needs it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/languages/supports/characterPair.ts` | 8–51 | `CharacterPairSupport` |
| `common/languages/supports/onEnter.ts` | 11–135 | `IOnEnterSupportOptions`, `IProcessedBracketPair`, `OnEnterSupport` |
| `common/languages/supports/indentRules.ts` | 8–83 | `IndentConsts`, `IndentRulesSupport` (`resetGlobalRegex` is not needed) |
| `test/common/modes/supports/onEnterRules.ts` | 8–152 | `javascriptOnEnterRules`, `phpOnEnterRules`, `cppOnEnterRules`, `htmlOnEnterRules`, `vbOnEnterRules` (test helper) |
| `test/common/modes/supports/characterPair.test.ts` | 13–126 | the tests below |
| `test/common/modes/supports/onEnter.test.ts` | 12–192 | the tests below |

## Files to create or change

- `src/LoomEditor.Core/Languages/EditorAutoIndentStrategy.cs` — create: **move** the enum from
  `src/LoomEditor.View/Cursors/EditorCursorEnums.cs` (delete it there); add `using LoomEditor.Core.Languages;` to every
  View and test file that names it (`CursorConfiguration.cs`, `EditorCursorOptions.cs`, `Commands/ShiftCommand.cs`,
  `TypeEditOperations.Enter.cs`, … — the compiler lists them)
- `src/LoomEditor.Core/Languages/Supports/CharacterPairSupport.cs`, `OnEnterSupport.cs`, `IndentRulesSupport.cs` — create
- `tests/LoomEditor.Core.Tests/Languages/TestHelpers/OnEnterRules.cs` — create (`JavascriptOnEnterRules`, `PhpOnEnterRules`, `CppOnEnterRules`, `HtmlOnEnterRules`, `VbOnEnterRules`)
- `tests/LoomEditor.Core.Tests/Languages/Supports/CharacterPairSupportTests.cs`, `OnEnterSupportTests.cs`, `IndentRulesSupportTests.cs` — create

## Design decisions (already made — do not change)

- `CharacterPairSupport(LanguageConfiguration config)`: constants `DefaultAutoCloseBeforeLanguageDefinedQuotes`,
  `DefaultAutoCloseBeforeLanguageDefinedBrackets`, `DefaultAutoCloseBeforeWhitespace`; `GetAutoClosingPairs()` →
  `IReadOnlyList<StandardAutoClosingPairConditional>`, `GetAutoCloseBeforeSet(bool forQuotes)`, `GetSurroundingPairs()`
  → `IReadOnlyList<IAutoClosingPair>`. JavaScript truth values: `config.autoClosingPairs` / `config.brackets` → `is not null`
  (an empty list is truthy); `typeof autoCloseBefore === 'string'` → `is not null`; `surroundingPairs || autoClosingPairs`
  → `??`. The doc comment pair is added when `config.DocComment is not null` (`close || ''`).
- `OnEnterSupport(IReadOnlyList<CharacterPair>? brackets, IReadOnlyList<OnEnterRule>? onEnterRules)`. Missing
  brackets use `OnEnterSupport.DefaultBrackets` = `()`, `{}`, `[]` (in that order, public static, P5-14 needs it).
  `OnEnter(EditorAutoIndentStrategy autoIndent, string previousLineText, string beforeEnterText, string afterEnterText)`
  → `EnterAction?`. `regex.lastIndex = 0` + `test` → `regex.IsMatch(text)`.
- `_createOpenBracketRegExp`: `/\B/.test(str.charAt(0))` is true when the first character is **not** an ASCII word
  character; so prepend `\b` only when `str[0]` is in `[A-Za-z0-9_]`. Same for the last character in
  `_createCloseBracketRegExp`. Use `Strings.EscapeRegExpCharacters` (P5-11). Compile with
  `RegexOptions.CultureInvariant` only (Monaco: `new RegExp(def)`, no flag). `_safeRegExp` catches
  `ArgumentException`, writes it with `Debug.WriteLine` (Monaco: `onUnexpectedError`) and skips the pair.
- `IndentRulesSupport(IndentationRule rules)`: `ShouldIncrease`, `ShouldDecrease`, `ShouldIndentNextLine`,
  `ShouldIgnore`, `GetIndentMetadata` → `int`. `IndentConsts` is a static class of `int` constants
  `IncreaseMask = 0b0001`, `DecreaseMask = 0b0010`, `IndentNextLineMask = 0b0100`, `UnIndentMask = 0b1000`.
- `OnEnterRules.cs`: the regex literals are copied **verbatim** into `new Regex(@"…", RegexOptions.CultureInvariant)`
  (plus `IgnoreCase` for the `i` flag; `g` and `m` of test literals map to nothing and `Multiline`). `\w`, `\d`, `\b`
  keep their .NET meaning: the test texts are ASCII. Actions are `new EnterAction(IndentAction.X, AppendText: …, RemoveText: …)`.
- `characterPair.test.ts` compares `StandardAutoClosingPairConditional` objects with `deepStrictEqual`: compare
  `(Open, Close)` tuples instead.

## Tests

- [x] Port (`characterPair.test.ts`) `CharacterPairSupport`: `only autoClosingPairs`, `only empty autoClosingPairs`,
  `only brackets`, `only empty brackets`, `only surroundingPairs`, `only empty surroundingPairs`,
  `brackets is ignored when having autoClosingPairs`, `shouldAutoClosePair in empty line`,
  `shouldAutoClosePair in not interesting line 1`, `shouldAutoClosePair in not interesting line 2`,
  `shouldAutoClosePair in interesting line 1`, `shouldAutoClosePair in interesting line 2`,
  `shouldAutoClosePair in interesting line 3`
- [x] Port (`onEnter.test.ts`) `OnEnter`: `uses brackets`, `Issue #121125: onEnterRules with global modifier`,
  `uses regExpRules`, `issue #141816`
- [x] New: `GetIndentMetadata_CombinesTheFourMasks`, `ShouldIgnore_WithoutUnIndentedLinePattern_IsFalse`,
  `OnEnter_BelowAdvanced_IgnoresRegExpRules`, `OnEnter_None_ReturnsNull`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (phase 3 cursor tests unchanged)
- [x] `docs/PORTING.md` map updated (`characterPair.ts`, `onEnter.ts`, `indentRules.ts`, `editorOptions.ts` `EditorAutoIndentStrategy` now in Core)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Using them from the cursor (step D); the registry that creates them (P5-14).

## Suggested commit message

`feat(core): port the character pair, on-enter and indentation rule supports`
