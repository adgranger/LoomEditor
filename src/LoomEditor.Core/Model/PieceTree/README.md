# PieceTree

**Phase 1.** The text buffer behind `TextModel`: a *piece tree* (a red-black tree of pieces pointing into
append-only string buffers), which makes edits in huge files fast.

Monaco source: `src/vs/editor/common/model/pieceTreeTextBuffer/`. See docs/PORTING.md for the porting plan
and docs/GLOSSARY.md for what a piece tree is.
