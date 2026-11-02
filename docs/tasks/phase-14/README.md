# Phase 14 — read this before any card

Phase 14 gives `LoomDiffEditor` the **building blocks for line- and block-level actions** in the side-by-side view
([phase-14-diff-actions.md](../../roadmap/phase-14-diff-actions.md)). It comes from VIGO (a WinUI 3 git client): in its side-by-side diff a user cannot
stage one line or one small block ("I only want to stage one line, but I have to tick both boxes on the left; Fork has a button for this").

The rule of this phase: **`LoomDiffEditor` knows nothing about git.** It exposes the diff structure, a line selection and action buttons, and raises events; the
host maps line ranges to its own hunks and patches. No text editing, no patch building, no new dependency, AOT-safe (no reflection, no runtime-generated code).

## This phase is not a port

Same as phases 11–13: no Monaco header on new files, no `// Monaco:` test comments, no row in the Monaco → LoomEditor map. Monaco has close relatives that stay
**not ported** (ADR 0012): `revertButtonsFeature.ts` (an arrow per block) and `gutterFeature.ts` (a toolbar column per block). Each card says
`Sources: none (specified in this card)` and lists the existing LoomEditor files to read first.

## Decisions (approved 2026-11-02 — do not change)

1. **Where the controls live: XAML overlay controls in a glyph margin with lanes.** When a phase 14 feature is on, both editors of a side-by-side
   `LoomDiffEditor` get a glyph margin (`EditorLayoutOptions.GlyphMargin`) whose lane count is the number of phase 14 lanes in use. Lane width is Monaco's: one
   line height per lane (`EditorLayoutInfoComputer`), so it follows the font, the line height and the zoom. Lanes, left to right, each present only when used:
   1. **select** — the line checkbox (`LineSelectionEnabled`);
   2. **line action** — the per-line hover button (`LineSelectionEnabled` and `LineActionGlyph` not null);
   3. **primary block action** (`BlockActionsVisibility != Off` and `PrimaryBlockActionGlyph` not null);
   4. **secondary block action** (same, with `SecondaryBlockActionGlyph`).

   The controls are real WinUI controls placed through the editor's `OverlayHost`, exactly as the comment "+" (`CommentGutterController`): pooled, created only for
   **visible** lines, re-placed after every draw (`LoomEditor.Arrange` path), not tab stops, with an automation name and a tooltip. The comment "+" keeps its place at
   `LineNumbersLeft` (right of the glyph margin); the overview ruler is untouched.
2. **Selection lives in a View-layer model**: `DiffLineSelection` (`src/LoomEditor.View/DiffEditor/`, internal, unit tested). A set of `(side, line)` plus the Shift+click
   anchor. On a new diff it is **kept** (minus lines that are no longer changed lines) when the version ids of both models are the ones of the previous diff, and
   **cleared** otherwise.
3. **Pair gesture: click toggles one line, Ctrl+click toggles the pair.** A pair is "same index in the same block": in a block with original lines `o..o+n-1` and
   modified lines `m..m+k-1`, original line `o+i` pairs with modified line `m+i` when `i < min(n, k)`. Side-by-side shows them next to each other (the alignment of a
   block is top-aligned). Shift+click extends from the anchor on the same side.
4. **Side-by-side only.** With `RenderSideBySide` off (or the inline fallback of a narrow control: `RenderSideBySideEffective`), no lane, no checkbox, no button; the
   `Changes` API and `SelectedLines` still work.
5. **Reuse, not a parallel infrastructure:** `DiffChanged` (already raised when a new diff is published) is the change notification for `Changes` — no
   `ChangesChanged` event. `GoToNextChange` / `GoToPreviousChange` (Alt+F5 / Shift+Alt+F5 — **not** Alt+Up/Down, which is "move lines") walk the same
   `Diff.Mappings` list `Changes` is built from, so they are consistent by construction.
6. **Line numbers in the public API are 1-based model lines of that side.** A block with an empty side (count 0) is a pure insertion or a pure deletion; its start line
   on the empty side is the line **before which** the other side's lines sit (Monaco `LineRange` semantics; it can be `LineCount + 1`). The XML doc says so, with the
   git-hunk conversion (`git` uses "the line after which", i.e. `start - 1`).
7. Blocks follow the diff options: with `IgnoreTrimWhitespace` on (the default), a whitespace-only change is **not** a block. The XML doc of `Changes` says so.

## Public API of the phase (the names are final)

```csharp
// LoomEditor.View.DiffEditor (public: built and unit tested in View)
public sealed class DiffChangeBlock            // immutable
{
    public int OriginalStartLineNumber { get; }
    public int OriginalLineCount { get; }       // 0 = pure insertion
    public int ModifiedStartLineNumber { get; }
    public int ModifiedLineCount { get; }       // 0 = pure deletion
    public IReadOnlyList<DiffInnerChange> InnerChanges { get; }   // empty when the algorithm gave none
}
public readonly record struct DiffInnerChange(Range OriginalRange, Range ModifiedRange);

// LoomEditor.WinUI.Controls
public readonly record struct DiffLine(DiffSide Side, int LineNumber);
public enum DiffBlockActionsVisibility { Off, OnHover, Always }
public enum DiffBlockAction { Primary, Secondary }
public sealed class DiffBlockActionInvokedEventArgs : EventArgs { DiffChangeBlock Block; DiffBlockAction Action; }
public sealed class DiffLineActionInvokedEventArgs : EventArgs { IReadOnlyList<DiffLine> Lines; }

partial class LoomDiffEditor
{
    IReadOnlyList<DiffChangeBlock> Changes { get; }                       // P14-01
    DiffBlockActionsVisibility BlockActionsVisibility { get; set; }       // P14-03, DP, default Off
    string? PrimaryBlockActionGlyph / PrimaryBlockActionToolTip           // P14-03, DPs
    string? SecondaryBlockActionGlyph / SecondaryBlockActionToolTip       // P14-03, DPs
    event EventHandler<DiffBlockActionInvokedEventArgs>? BlockActionInvoked;   // P14-03
    bool LineSelectionEnabled { get; set; }                               // P14-04, DP, default false
    IReadOnlyList<DiffLine> SelectedLines { get; }                        // P14-02
    void SetSelection(IEnumerable<DiffLine> lines);                       // P14-02
    void ClearSelection();                                                // P14-02
    event EventHandler? SelectionChanged;                                 // P14-02
    string? LineActionGlyph / LineActionToolTip                           // P14-04, DPs
    VirtualKey LineActionKey { get; set; }                                // P14-04, DP, default VirtualKey.S
    event EventHandler<DiffLineActionInvokedEventArgs>? LineActionInvoked;     // P14-04
}
```

As shipped (checked against both `PublicAPI.Unshipped.txt` files in P14-06): every name above is unchanged. Also public: a `…Property` field for each
dependency property above, the constructors `DiffBlockActionInvokedEventArgs(DiffChangeBlock block, DiffBlockAction action)` and
`DiffLineActionInvokedEventArgs(IReadOnlyList<DiffLine> lines)` (throws on an empty list), and `DiffChangeBlock.ToString()` (`"3,1 -> 3,2"`). `DiffChangeBlock` has no
public constructor. `DiffSide` (`Original`, `Modified`) is the existing enum of the diff comment threads (P11-22).

## Order of the cards

- **A — Logic, no UI** (P14-01, P14-02): change blocks, the selection model and its public facade. Unit tested in `LoomEditor.View.Tests`.
- **B — The gutter** (P14-03, P14-04): glyph lanes and block buttons; line checkboxes and the line action.
- **C — Prove it and close** (P14-05, P14-06): Gallery demo and manual plan; package test, docs, CHANGELOG.

There is still **no WinUI test project**. P14-03 and P14-04 each add a `##` section with measurable manual checks to `docs/testing/phase-14-manual-tests.md`
(P14-03 creates it from the header of `phase-13-manual-tests.md`). Agents do not run the Gallery UI; a WinUI card's Result section says the manual checks were not run.

**Recommended model:** Sonnet for every card; each is ≤ ~400 lines of production code. If a card does not fit in one session, stop and report with a proposed split.

## Out of scope for phase 14

Git logic, patch building, editing text from an action, the inline (unified) view, a keyboard-only way to reach the block buttons (the host can use `Changes` with the
caret, or the line action key), drawing glyph-margin decorations in the renderer, a highlight of selected lines beyond the checkbox, Monaco's `revertButtonsFeature` and
`gutterFeature`.
