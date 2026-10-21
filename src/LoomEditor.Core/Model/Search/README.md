# Model/Search

The building blocks of text search (Monaco: `common/model/textModelSearch.ts`, `common/model.ts`).

| File | Content |
|---|---|
| `SearchParams.cs` | a search request; `ParseSearchRequest` builds a `SearchData`; `IsMultilineRegexSource`, `LimitFindCount` |
| `SearchData.cs` | compiled regex + word separators + simple search string |
| `FindMatch.cs` | a range and its captured groups (`null` = group that did not participate) |
| `Searcher.cs` | walks the matches of a string with JavaScript `lastIndex` semantics, whole-word checks |

JavaScript flags map to .NET options in `Strings.CreateRegExp`: `i` → `IgnoreCase`, `m` → `Multiline`, always
`CultureInvariant`; never `ECMAScript` or `Compiled`. `g` is the `Searcher`'s own index (`Regex.Match(text, startat)`),
`u` has no equivalent. Nothing here searches a model yet (P5-63).
