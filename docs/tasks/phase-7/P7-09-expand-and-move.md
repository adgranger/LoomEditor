# P7-09 — Expand and move text ranges

- **Status:** done
- **Depends on:** P7-08
- **Size:** M

## Goal

An `AutomationTextRange` can be expanded to a unit and moved by units or by endpoints, exactly as the UIA text range contract describes (README §4): this is what Narrator uses
to read "the next word", "the previous line", and so on.

## Sources

None (specified in `docs/tasks/phase-7/README.md` §4: read the "Operations" list first; the algorithms there are final).

## Files to create or change

- `src/LoomEditor.View/Accessibility/AutomationTextRange.cs` — change (or add `AutomationTextRange.Navigation.cs`, a `partial` file, if `AutomationTextRange.cs` would pass ~400 lines): add
  `void ExpandToEnclosingUnit(AutomationTextUnit unit)`, `int Move(AutomationTextUnit unit, int count)`, `int MoveEndpointByUnit(AutomationTextEndpoint endpoint, AutomationTextUnit unit, int count)`,
  `void MoveEndpointByRange(AutomationTextEndpoint endpoint, AutomationTextRange other, AutomationTextEndpoint otherEndpoint)`
- `tests/LoomEditor.View.Tests/Accessibility/AutomationTextRangeNavigationTests.cs` — create

## Design decisions (already made — do not change)

- Implement the four methods **literally** as README §4 says, on top of `AutomationUnitStarts.Create(Document, unit)` and its `StartAtOrBefore`, `PreviousStart`, `NextStartStrict`, `NextStart`.
  Read the range with the `Range` getter (it validates) and write it with `Set` (it validates).
- `Move` returns the signed count of units really moved; `count == 0` only expands. `MoveEndpointByUnit` returns the signed count; `count == 0` changes nothing and returns `0`.
- `MoveEndpointByRange` with an `other` of another document → `ArgumentException`. Null arguments → `ArgumentNullException`. An undefined `unit` or `endpoint` → `ArgumentOutOfRangeException`.
- None of these methods raises or reads the selection: moving a range never moves the caret (that is `Select`, P7-18).
- The `Range` constructor orders its two positions: build the new range with the explicit `new Range(start, end)` after the repairs of README §4 (they guarantee `start <= end`), never rely on it to swap.
- No special case for hidden (folded) text or injected text: the units already handle them.

## Tests

(Text `T1 = "one two\nthree"`: word starts `(1,1)` `"one "`, `(1,5)` `"two\n"`, `(2,1)` `"three"`; `Rng(sl, sc, el, ec)`, `Pos` from the helpers; `R` = a range over `T1`)

- [x] New — expand: `Expand_Word_Degenerate` (`(1,2)` → `[(1,1),(1,5)) "one "`), `Expand_Word_AtLineEnd_IncludesTheLineBreak` (`(1,8)` → `[(1,5),(2,1)) "two\n"`),
      `Expand_Word_AtDocumentEnd_IsTheLastWord` (`(2,6)` → `[(2,1),(2,6)) "three"`), `Expand_Word_LongerRange_IsShortenedToTheFirstWord` (`[(1,2),(2,3))` → `[(1,1),(1,5))`),
      `Expand_Character`, `Expand_Character_AtLineEnd_IsTheLineBreak` (`(1,8)` → `[(1,8),(2,1)) "\n"`), `Expand_Paragraph`, `Expand_Document`, `Expand_Line_NoWrap_EqualsParagraph`,
      `Expand_Line_Wrapped` (text `"aaaa bbbb cccc"`, `WordWrapColumn = 10`, `(1,12)` → `[(1,11),(1,15)) "cccc"`; assert the view lines first as in P7-08), `Expand_Page`,
      `Expand_EmptyDocument_StaysEmpty`, `Expand_ExactUnit_NoChange`
- [x] New — move: `Move_WordForward` (degenerate `(1,1)` + 1 → `[(1,5),(2,1))`, returns 1), `Move_WordForwardPastTheEnd` (count 5 from `(1,1)` → returns 2, range `[(2,1),(2,6))`),
      `Move_WordAtTheLastWord_ReturnsZero` (range unchanged), `Move_WordBackward` (`[(2,1),(2,6))` − 1 → `[(1,5),(2,1))`, returns -1), `Move_WordBackwardPastTheStart` (− 5 from `[(1,5),(2,1))` → returns -1, `[(1,1),(1,5))`),
      `Move_CharacterAcrossTheLineBreak` (`[(1,7),(1,8))` + 1 → `[(1,8),(2,1))` then + 1 → `[(2,1),(2,2))`), `Move_Zero_OnlyExpands` (returns 0, `(1,2)` becomes `[(1,1),(1,5))` for `Word`),
      `Move_Document_ReturnsZeroAndSelectsEverything`, `Move_Line_Wrapped_GoesByViewLine`, `Move_Paragraph_GoesByModelLine`, `Move_Page`, `Move_ExpandsANonDegenerateRangeFirst` (`[(1,2),(2,3))` + 1 by `Word` → `[(1,5),(2,1))`),
      `Move_SurrogatePair_IsOneCharacter` (`"a😀b"`)
- [x] New — `MoveEndpointByUnit`: `End_Word_Forward` (`[(1,1),(1,5))` End +1 → `[(1,1),(2,1))`, 1), `End_Word_PastTheEnd` (End +5 → `[(1,1),(2,6))`, returns 2: one step to `(2,1)`, one to the document end),
      `Start_Word_Forward_CollapsesWhenCrossing` (`[(1,1),(1,5))` Start +2 → `[(2,1),(2,1))`, returns 2), `Start_Word_Forward_ToTheEnd_IsDegenerate` (Start +1 → `[(1,5),(1,5))`),
      `End_Word_Backward` (`[(1,1),(1,5))` End −1 → `[(1,1),(1,1))`, returns -1), `End_Word_Backward_CrossingCollapsesToTheEnd` (`[(1,5),(2,1))` End −2 → `[(1,1),(1,1))`, returns -2),
      `Start_Character_BackwardAtDocumentStart_ReturnsZero`, `End_Document_Forward_GoesToTheDocumentEnd` (returns 1), `End_Document_Backward_GoesToTheDocumentStart` (from the whole document, returns -1, range degenerate at `(1,1)`),
      `Count_Zero_ReturnsZeroAndChangesNothing`, `MidWordEndpoint_ForwardGoesToTheNextStart` (`[(1,1),(1,3))` End +1 by `Word` → End `(1,5)`)
- [x] New — `MoveEndpointByRange`: `End_ToTheStartOfAnotherRange` (`A = [(1,1),(1,5))`, `B = [(2,1),(2,6))`: `A.End ← B.Start` → `[(1,1),(2,1))`), `Start_ToTheEndOfAnotherRange_CollapsesA` (`A.Start ← B.End` → `[(2,6),(2,6))`),
      `End_ToTheEndOfAnotherRange`, `End_BeforeStart_CollapsesToTheEnd` (`A = [(1,5),(2,1))`, `B = [(1,1),(1,3))`: `A.End ← B.End` → `[(1,3),(1,3))`), `OtherDocument_Throws`
- [x] New: `Move_ModelChangedBetweenCalls_DoesNotThrow` (shrink the model after creating the range, then `Move` and `ExpandToEnclosingUnit` work on the clamped range)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`FindText` (P7-10), selection (P7-18), screen geometry (P7-11).

## Suggested commit message

`feat(view): expand and move automation text ranges`
