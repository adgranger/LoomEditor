# P10-20 — Snippet syntax (2/3): transforms, format strings, variables and `TextmateSnippet`

- **Status:** done
- **Depends on:** P10-19
- **Size:** M

## Goal

The rest of the snippet tree exists: `Transform`, `FormatString`, `Variable`, `IVariableResolver`, the `Walk` helper and `TextmateSnippet` (placeholder info, offsets, enclosing placeholders, variable resolution, `Replace`, `ToTextmateString`, `Clone`).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 318–366 | `Transform` (`regexp`, `resolve`, `_replace`, `toString`, `toTextmateString`, `clone`) |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 368–488 | `FormatString` (`index`, `shorthandName`, `ifValue`, `elseValue`, `resolve` with `upcase`, `downcase`, `capitalize`, `pascalcase`, `camelcase`, `kebabcase`, `snakecase` and the conditional forms, `toTextmateString`, `clone`, `_toKebabCase`, `_toPascalCase`, `_toCamelCase`, `_toSnakeCase`; read lines 380–455 for the exact list) |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 490–528 | `Variable` (`resolve`, `toTextmateString`, `clone`) |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 530–546 | `VariableResolver` interface, `walk` |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 546–646 | `TextmateSnippet` (`placeholderInfo`, `placeholders`, `offset`, `fullLen`, `enclosingPlaceholders`, `resolveVariables`, `appendChild`, `replace`, `toTextmateString`, `clone`, `walk`) |

## Files to create or change

- `src/LoomEditor.Core/Snippets/Transform.cs`, `FormatString.cs`, `Variable.cs`, `IVariableResolver.cs`, `TextmateSnippet.cs` — create
- `src/LoomEditor.Core/Snippets/SnippetMarker.cs` — change: `Snippet` now returns `TextmateSnippet` (remove the `// P10-20` placeholder)
- `tests/LoomEditor.Core.Tests/Snippets/TransformTests.cs`, `FormatStringTests.cs`, `TextmateSnippetTests.cs` — create

## Design decisions (already made — do not change)

- `Transform` holds a .NET `Regex` built at runtime (`new Regex(source, options)`: AOT-safe, interpreted; no `[GeneratedRegex]` possible for user input) plus the original `Source` and the JavaScript flag letters (`Flags`, `"g"`/`"i"` etc.) so `ToTextmateString` and `Clone` reproduce them. Flags: `i` → `RegexOptions.IgnoreCase`, `m` → `Multiline`, `s` → `Singleline`; `g` → replace **all** matches, otherwise only the first (JS `String.replace` semantics: `Regex.Replace(input, evaluator, count: 1)` when not global); `u`, `v`, `y`, `d` are accepted and ignored. Always `RegexOptions.CultureInvariant`. The constructor of a transform with an invalid pattern is the parser's concern (P10-21 catches `ArgumentException` like Monaco's `try/catch`); `Transform` itself has a `public Regex Regexp` settable by the parser.
- `Transform.Resolve(string value)` is Monaco's, including the "no match and some child is a `FormatString` with an `elseValue`" rule. The groups passed to `_replace` are `match.Groups[0..]` as strings (unmatched group → `""`), i.e. what `groups[marker.index] || ''` does.
- `FormatString.Resolve` ports all branches: with `shorthandName` (`upcase`, `downcase`, `capitalize`, `pascalcase`, `camelcase`, `kebabcase`, `snakecase`), with `ifValue` / `elseValue` (and the `+`, `-`, `?` forms through the parser, P10-21), and the plain value. The word-based conversions use the **same regular expressions** as Monaco (`/[\p{L}0-9]+/gu` for pascal and camel case, the longer ones of `_toKebabCase` at lines 405–434, and `_toSnakeCase` at lines 460–464): they are constants, so use `[GeneratedRegex]` (.NET's `\p{L}`, `\p{Lu}`, `\p{Ll}` classes are the same). Use `char.ToUpperInvariant` / `ToLowerInvariant` for case conversion (Monaco's `toUpperCase` is culture-invariant).
- `Variable.Resolve(IVariableResolver)` returns `bool` as in Monaco; `IVariableResolver.Resolve(Variable)` returns `string?` (`null` = unknown, as `undefined`).
- `TextmateSnippet`: `PlaceholderInfo` (a `sealed record PlaceholderInfo(IReadOnlyList<Placeholder> All, Placeholder? Last)`, cached; the algorithm of lines 550–566: `Last` is the placeholder with the highest `Index`, and on a tie the first one met by the walk), `Placeholders`, `Offset(marker)`, `FullLength(marker)`, `EnclosingPlaceholders`, `ResolveVariables` (returns `this`), `Replace`/`AppendChild` invalidate the cached `_placeholders` (set to `null`), `Walk(Func<SnippetMarker, bool>)` and the free function `walk`: one static method `Walk(IEnumerable<SnippetMarker>, Func<SnippetMarker, bool>)` on `SnippetMarker`.
- `<remarks>Threading: not thread-safe.</remarks>`.

## Tests

- [x] Port: none directly (the upstream tests of these classes go through the parser and are ported in P10-21)
- [x] New: `Transform_Resolve_ReplacesFirstMatchWithoutG`, `Transform_Resolve_ReplacesAllWithG`, `Transform_Resolve_IgnoreCase`, `Transform_Resolve_NoMatch_ElseBranch`, `Transform_Resolve_NoMatch_NoElse_ReturnsInput`, `Transform_ToTextmateString_RoundTripsSourceAndFlags`, `Transform_Clone_Independent`
- [x] New: `FormatString_Upcase`, `_Downcase`, `_Capitalize`, `_Pascalcase` (`"foo-bar baz"` → `"FooBarBaz"`), `_Camelcase`, `_Kebabcase`, `_Snakecase`, `_IfValue_WhenGroupMatched`, `_ElseValue_WhenGroupEmpty`, `_PlainGroup`, `_ToTextmateString_AllForms`
- [x] New: `Variable_Resolve_Known_ReplacesChildren`, `Variable_Resolve_Unknown_KeepsDefaultChildren`, `Variable_Resolve_AppliesTransform`, `Variable_ToTextmateString_WithAndWithoutDefault`
- [x] New: `TextmateSnippet_Placeholders_SortedWithFinalLast`, `_Offset_OfNestedPlaceholder`, `_FullLength`, `_EnclosingPlaceholders_Nested`, `_Replace_InvalidatesPlaceholders`, `_Clone_IsDeep`, `_ToTextmateString_RoundTrip` (built by hand with the marker classes)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (no `Regex` analyzer warning left: a dynamic pattern is the documented exception; if the build raises `SYSLIB1045` for the constructor call, the call site is the one in `Transform`, and you suppress it with a one-line `[SuppressMessage]` + comment, never a `NoWarn`)
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: "Behavior differences": snippet transform regexes use .NET regex syntax (JavaScript-only constructs such as `(?<name>)` are the same, `\p{…}` / lookbehind are supported, sticky and unicode-set flags are ignored)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Parsing (P10-21), the variable resolvers (P10-22).

## Suggested commit message

`feat(core): port the snippet transforms, variables and TextmateSnippet`
