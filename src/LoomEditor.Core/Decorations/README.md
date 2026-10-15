# Decorations

**Phase 1–2.** Decorations are ranges with styling attached (squiggles, highlights, breakpoints…).
They are stored in an interval tree so that they move correctly when text is edited.

Monaco source: `src/vs/editor/common/model/intervalTree.ts`.

**Phase 3 (P3-05).** `IntervalNode`, `IntervalTree` (insert, delete, rotations, `maxEnd`, delta normalization, searches, `ResolveNode`),
`NodeColor` and a minimal `ModelDecorationOptions` are ported. Edits (P3-07) come next.
