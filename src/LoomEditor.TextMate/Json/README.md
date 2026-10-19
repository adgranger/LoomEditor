# Json

**Phase 4.** Grammar files are **not** read with `JsonSerializer` or a `JsonSerializerContext`: a raw TextMate grammar
has no fixed shape (`applyEndPatternLast` is a boolean or a number, captures are objects keyed by numbers).
`Parsing/RawGrammarReader.cs` parses the text with `JsonNode.Parse` (comments and trailing commas allowed) and walks
the nodes by hand; the plist reader builds the same `JsonNode` tree, so there is one mapping for both formats
(ADR 0008, decision 3).

Never call `JsonSerializer` without a generated context: reflection-based serialization is not AOT-compatible
(and is banned by `src/BannedSymbols.txt`).
