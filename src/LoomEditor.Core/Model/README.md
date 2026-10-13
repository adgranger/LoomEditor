# Model

**Phase 1.** Will contain `TextModel`: the text of one document, its end-of-line sequence, its options,
and the events raised when it changes. Monaco source: `src/vs/editor/common/model/textModel.ts`.

Threading rule: a `TextModel` is mutated on the UI thread only.
