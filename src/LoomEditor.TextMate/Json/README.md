# Json

**Phase 4.** `JsonSerializerContext` classes (System.Text.Json source generation) for grammar files.
Never call `JsonSerializer` without a generated context: reflection-based serialization is not AOT-compatible
(and is banned by `src/BannedSymbols.txt`).
