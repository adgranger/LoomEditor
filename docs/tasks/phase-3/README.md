# Phase 3 — read this before any card

Phase 3 ports Monaco's cursor and editing. The design is in
[ADR 0006](../../adr/0006-editing-and-text-input.md). This page lists the conventions that **every** phase 3 card
relies on. A card never repeats them.

## 1. How to read a card

- **Line numbers** are at the pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`. If a function is not exactly
  at the given line, search the file for its **name**: the name is authoritative, the line number is a hint.
- **Port only what the card lists.** A Monaco function that is not listed is either already ported (search the code
  base for its C# name) or belongs to another card.
- **"Cut point"** means: Monaco calls the language configuration (or tokens) here. Do not port that call. Write
  `// Phase 4–5 cut point: <what Monaco does here>` and use the value the card gives (usually: "no language
  configuration", which is what Monaco itself does for a plain-text model).
- **Tests first.** Port the Monaco tests listed in the card before the code. Keep `// Monaco: '<test name>'` above each
  ported test. Never change an expected value of a ported test. If a ported test fails and you believe Monaco's
  expectation cannot hold in C#, **stop** and report it.

## 2. Where things go

| Monaco | LoomEditor | Namespace |
|---|---|---|
| `base/common/strings.ts`, `editor/common/core/*` (cursorColumns, characterClassifier, wordCharacterClassifier, wordHelper) | `src/LoomEditor.Core/Text/` | `LoomEditor.Core.Text` |
| `editor/common/model/intervalTree.ts` | `src/LoomEditor.Core/Decorations/` | `LoomEditor.Core.Decorations` |
| `editor/common/model/textModel.ts` additions | `src/LoomEditor.Core/Model/TextModel.*.cs` | `LoomEditor.Core.Model` |
| `editor/common/cursorCommon.ts`, `editor/common/cursor/*` | `src/LoomEditor.View/Cursors/` | `LoomEditor.View.Cursors` |
| `editor/common/commands/*` (replace, shift, column-selection paste) and `ICommand` | `src/LoomEditor.View/Cursors/Commands/` | `LoomEditor.View.Cursors.Commands` |
| `editor/common/viewModel/coordinatesConverter.ts`, `viewModelImpl.ts` (cursor part) | `src/LoomEditor.View/Layout/` | `LoomEditor.View.Layout` |
| `editor/common/viewModelEventDispatcher.ts`, `editor/common/viewEvents.ts` (subset) | `src/LoomEditor.View/Events/` | `LoomEditor.View.Events` |
| `editor/browser/coreCommands.ts`, keybindings | `src/LoomEditor.View/Commands/` | `LoomEditor.View.Commands` |
| `editor/browser/view/viewController.ts`, `controller/mouseHandler.ts`, `mouseTarget.ts`, `dragScrolling.ts` (pure logic) | `src/LoomEditor.View/Controller/` | `LoomEditor.View.Controller` |
| `editor/browser/controller/editContext/*` (pure logic) | `src/LoomEditor.View/Controller/` | `LoomEditor.View.Controller` |
| WinUI wiring (keyboard, pointer, clipboard, IME) | `src/LoomEditor.WinUI/Input/`, `Controls/LoomEditor.*.cs` | `LoomEditor.WinUI.*` |

Tests mirror `src/`: `tests/LoomEditor.Core.Tests/Text/…`, `tests/LoomEditor.View.Tests/Cursors/…`, and so on.
Everything is `internal` unless the card says **public**.

## 3. Monaco API → C# API (the existing types)

`Position`, `Range`, `Selection` are `readonly record struct`s in `LoomEditor.Core.Text` (phase 1). Their API is not
Monaco's word for word:

| Monaco (TypeScript) | C# |
|---|---|
| `new Position(l, c)`, `new Range(a, b, c, d)` | same — but they **throw** `ArgumentOutOfRangeException` when a value is < 1 |
| `range.getStartPosition()`, `range.getEndPosition()` | `range.Start`, `range.End` |
| `range.isEmpty()` | `range.IsEmpty` |
| `Range.fromPositions(a)` | `Range.Collapsed(a)` |
| `Range.fromPositions(a, b)` / `new Range(a…, b…)` with `b < a` | `new Range(a, b)` (swaps, like Monaco) |
| `range.equalsRange(other)`, `position.equals(other)` | `==` |
| `a.isBefore(b)`, `a.isBeforeOrEqual(b)` | `a < b`, `a <= b` (or `IsBefore`, `IsBeforeOrEqual`) |
| `Position.compare(a, b)` | `a.CompareTo(b)` |
| `Range.compareRangesUsingStarts(a, b)`, `…UsingEnds` | `Range.CompareUsingStarts(a, b)`, `Range.CompareUsingEnds(a, b)` |
| `range.containsPosition(p)`, `range.containsRange(r)` | `range.Contains(p)`, `range.Contains(r)` |
| `range.intersectRanges(r)` | `range.Intersect(r)` (returns `Range?`) |
| `range.plusRange(r)` | `range.Plus(r)` |
| `Selection` **extends** `Range` in Monaco | `Selection` is **not** a `Range`: use `selection.Range.StartLineNumber`, `selection.Range.Start`, `selection.Range.IsEmpty`… |
| `selection.getSelectionStart()`, `selection.getPosition()` | `selection.SelectionStart`, `selection.Position` |
| `selection.selectionStartLineNumber` etc. | `selection.SelectionStartLineNumber`, `SelectionStartColumn`, `PositionLineNumber`, `PositionColumn` |
| `Selection.fromPositions(start, end)` | `new Selection(start, end)` |
| `Selection.createWithDirection(…)`, `getDirection()` | `Selection.FromRange(range, direction)`, `selection.Direction` |
| `selection.isEmpty()` | `selection.IsEmpty` |
| `Selection.liftSelection(s)` | `s` (already a `Selection`) |

**Columns or lines equal to 0.** Monaco sometimes builds a position with column 0 or line 0 on purpose (for example
`moveWordRight` with `WordAccessibility`, or `restoreState` defaults). A C# `Position` cannot hold 0. In those places,
keep plain `int lineNumber, int column` variables instead of a `Position`, and say so in a comment.

## 4. JavaScript traps (check every port against this list)

1. **`Math.round(x)`** rounds .5 up (`Math.round(2.5) == 3`, `Math.round(-2.5) == -2`). C# `Math.Round` rounds .5 to
   even. Use `(int)Math.Floor(x + 0.5)`.
2. **`charCodeAt(i)` out of range returns `NaN`.** C# throws. Guard the index; when Monaco relies on `NaN`, the card
   says what to use (usually `-1`, which every classifier treats as "default").
3. **`array.sort` is stable.** `List<T>.Sort` and `Array.Sort` are **not**. When Monaco sorts, use a stable sort:
   `list = list.OrderBy(x => x, comparer).ToList()` (LINQ `OrderBy` is stable) or an insertion sort. A comparer that
   is not consistent (Monaco's `cut` does this) must be used with an insertion sort, never with `List.Sort` (it can throw).
4. **`a | 0`**, **`Math.floor` on positive numbers** → integer division / `(int)` cast. On negative numbers
   `Math.floor(-1.5) == -2` but `(int)-1.5 == -1`: use `Math.Floor`.
5. **Truthiness**: `if (x)` is false for `0`, `''`, `null`, `undefined`, `NaN`. Write the C# condition explicitly.
6. **`str.substr(start, length)`** vs **`str.substring(start, end)`**: they differ. C# `Substring(start, length)` is `substr`.
7. **Arrays with holes / `null` entries**: Monaco's command arrays can contain `null` (`Array<ICommand | null>`):
   use `IEditCommand?[]` / `List<IEditCommand?>` and keep the `null` checks.
8. **Strings are UTF-16** in both languages: offsets are the same. Do **not** use `StringInfo`, `Rune` enumeration or
   `char.IsWhiteSpace`: use the ported helpers of `Strings` (P3-01, P3-02). Whitespace for Monaco's regex `\s` is not
   .NET's `\s`; the cards say which helper to use.
9. **Optional parameters with `undefined`**: map to C# optional parameters or nullable types, as the card says.
10. **Global mutable state** (Monaco's `InputMode`, caches): never `static` mutable in C# unless the card says so.

## 5. Test helpers shared by many cards

| Helper | Created by | Port of |
|---|---|---|
| `tests/LoomEditor.View.Tests/Cursors/TestHelpers/CommandTester.cs` (`TestCommand`, `GetEditOperation`) | P3-14 | `src/vs/editor/test/browser/testCommand.ts` |
| `tests/LoomEditor.View.Tests/TestHelpers/TestCodeEditor.cs` (`WithTestCodeEditor`, `CreateTextModel`) | P3-20 | `src/vs/editor/test/browser/testCodeEditor.ts` (subset) |
| `tests/LoomEditor.View.Tests/Cursors/TestHelpers/CursorTestHelpers.cs` (`MoveTo`, `MoveLeft`…, `AssertCursor`) | P3-27 | `cursor.test.ts` lines 36–125 |
| `tests/LoomEditor.View.Tests/Cursors/TestHelpers/WordTestUtils.cs` | P3-29 | `src/vs/editor/contrib/wordOperations/test/browser/wordTestUtils.ts` |

## 6. Commands and commit

Same as every card (see `CLAUDE.md`): Release build with 0 warnings, all tests green, one commit per card through
`/next-task`, Conventional Commits, no trailer.
