# Model

`TextModel`: the text of one document, its end-of-line sequence, and the events raised when it changes.
Monaco source: `src/vs/editor/common/model/textModel.ts`. It is split into `partial` files by concern:

| File | Content |
|---|---|
| `TextModel.cs` | creation, version ids, read API (`GetValue`, `GetLineContent`, `GetOffsetAt`…) |
| `TextModel.Validation.cs` | `ValidatePosition`, `ValidateRange`, `ModifyPosition` |
| `TextModel.Edits.cs` | `ApplyEdits`, `SetValue`, `SetEol`, `ContentChanged` and `RawContentChanged` events |
| `TextModel.Undo.cs` | `PushEditOperations`, `PushEol`, `Undo`, `Redo`, stack elements (see `../Undo/`) |
| `TextModel.Snapshot.cs` | `CreateSnapshot`, `FromSnapshot` |

Other folders: `PieceTree/` (the text storage), `Events/` (raw, line-level change events), `Edits/` (validated edit
operations). `ITextBuffer` and the other buffer types are `internal`; `TextModel` and `TextEdit` are the public API.

Not here yet: decorations and injected text (phase 2+), indentation options and word helpers (phases 4–5).
See docs/PORTING.md for the Monaco → LoomEditor map and the accepted differences.

Threading rule: a `TextModel` is mutated on the UI thread only.
