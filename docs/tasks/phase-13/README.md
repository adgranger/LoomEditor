# Phase 13 — read this before any card

Phase 13 opens a **public API for host applications** ([phase-13-host-api.md](../../roadmap/phase-13-host-api.md)). It comes from a real integration: VIGO (a
git client with blame, diff and merge conflicts) tried the published packages `0.1.0-alpha.1` and could not build its features, because every member it needed is
`internal`. VIGO found those members in the packages' XML docs (the C# compiler writes the XML doc of internal members too), which is why the request names
internal members.

The rule of this phase: **a small, well-named public facade on `LoomEditor`, plus a few public DTO / option types — never a blanket `internal` → `public` of the
internals.** Every Monaco internal that VIGO asked for drags a large type graph (decoration options have 25 fields, injected text, an `object` payload, stickiness
enums, ids; view zones are mutable objects with an untyped `Data` and a change-accessor protocol). Section 2 says, need by need, what becomes public and what stays
internal, and why.

## This phase is not a port

Same as phases 11 and 12: no Monaco source, no Monaco header on new files, no `// Monaco:` test comments, no row in the Monaco → LoomEditor map. Each card says
`Sources: none (specified in this card)` and lists the **existing LoomEditor files to read first**. Write the card's tests before or with the code. Where a public member
mirrors a Monaco API, its XML doc names it (`Monaco: <c>getTopForLineNumber</c>`), as the existing members do.

## Order of the cards

Take the cards in table order of [../README.md](../README.md) (each card's "Depends on" is satisfied by the cards above it). Steps:

- **A — Logic, no UI** (P13-01 … P13-03): the language of a file name, the host decoration types, host decoration styles. Unit tested.
- **B — Public API of `LoomEditor`** (P13-04 … P13-08): decorations on the control, scroll and layout sync, line numbers and text reset, hidden areas and gutter clicks,
  view zones with host content.
- **C — Prove it and close** (P13-09 … P13-11): Gallery demo and manual plan, Native AOT self-check and package test, close the phase (docs, CHANGELOG).

There is still **no WinUI test project**. Each WinUI card (P13-04 … P13-08) adds a `##` section "P13-nn" with **measurable** manual checks to
`docs/testing/phase-13-manual-tests.md` (P13-04 creates the file from the header of `docs/testing/phase-12-manual-tests.md`; P13-09 completes it). The Gallery controls those
checks use are added by P13-09: a WinUI card's manual section names them as "(Gallery, P13-09)". Agents do not run the Gallery UI; a WinUI card's Result section says the
manual checks were not run.

**Recommended model:** every card is written for **Sonnet**: the public surface, the names, the files and the edge cases are decided below and in each card; each card is
≤ ~400 lines of production code (most are far smaller). No card is marked Opus: the only delicate WinUI piece (an element over a view zone, focus and keys between it and
the editor) already exists for comment threads (P11-12 … P11-14) and P13-08 reuses it, with the P11-14 check list as its manual checks. If a card does not fit in one
session, stop and report with a proposed split.

## Out of scope for phase 13

Making Monaco's internal types public (`ModelDecorationOptions`, `ModelDeltaDecoration`, `EditorDecorationsCollection`, `ViewZone`, `ViewZones`, `LanguageRegistry`,
`DecorationStyle`, `DecorationStyles`); a pointer event for view zones (host content receives its own pointer events); host view zones and host hidden areas on the sides of
a `LoomDiffEditor` (the diff alignment and "hide unchanged regions" own them — the members exist on each side, documented as unsupported there); registering new languages
or grammars from the host; per-editor decoration styles (styles are global, like CSS); hover messages, injected text, glyph-margin icons, stickiness or z-index options on
host decorations; a "visible lines" API; removing internal members from the packages' XML docs (a packaging follow-up, see "Open risks" of the roadmap); a WinUI test
project. Anything the manual plan finds is a follow-up card.

## 1. Concepts (new in phase 13)

| Concept | LoomEditor |
|---|---|
| the host | the application that embeds `LoomEditor` / `LoomDiffEditor` from the NuGet packages (VIGO is the first one) |
| a host decoration | `EditorDecoration(Range, EditorDecorationOptions)` (View, public), set through a `DecorationsCollection` from `LoomEditor.CreateDecorationsCollection` |
| a decoration class | a name in an `EditorDecorationOptions` field (`ClassName`, `InlineClassName`, `MarginClassName`, `LinesDecorationsClassName`): a built-in class (`line-insert`, `rangeHighlight`, …) or a host class |
| a host decoration style | `CustomDecorationStyle` (colors for light and dark themes, border widths) registered under a class name with `CustomDecorationStyles.Register` — the "stylesheet" of the host |
| line metrics | `LineHeight`, `GetTopForLineNumber`, `ScrollTop` (already public) — what a column drawn beside the editor (blame) needs |
| a host view zone | a gap after a model line, created by `AddViewZone`, optionally covered by a host `FrameworkElement` placed in the existing zone-widget layer |
| host hidden areas | ranges of lines hidden by the host (`SetHiddenAreas`), merged with folding's |

## 2. Design decisions (already made — do not change)

### Need by need (VIGO's list → public surface)

| VIGO step and member it asked for | Decision | Card |
|---|---|---|
| Blame: `HostScrollChanged` | renamed **`ScrollChanged`**, public, with a public `EditorScrollChangedEventArgs` (replaces the internal record `HostScrollEventArgs`; same data) | P13-05 |
| Blame: `HostLayoutChanged` | renamed **`LayoutChanged`**, public; also raised when the font (so the line height) changes | P13-05 |
| Blame: `LineHeight` | made public as is (`double`, DIPs, read-only) | P13-05 |
| Blame: `GetTopForLineNumber` | made public as is; the line number is clamped to the model (Monaco validates the position), never throws | P13-05 |
| Blame: `HideLineNumbers` | **replaced** by a public dependency property `LineNumbersEnabled` (default `true`, like `MinimapEnabled`); the internal property is unused and deleted; the renderer stops drawing numbers when they are off (today it would draw them over the gutter) | P13-06 |
| Blame: `ApplyText` | made public as **`ResetText(string)`**: replaces the text even when it equals the last `Text` value (`Text` is not kept in sync with edits). `Model.SetValue` is already public but skips indentation detection and the long-lines flag | P13-06 |
| Highlighting: `CreateDecorationsCollection`, `ModelDecorationOptions`, `ModelDeltaDecoration`, `EditorDecorationsCollection` | **facade**: public `EditorDecoration`, `EditorDecorationOptions`, `DecorationsCollection` in `LoomEditor.View.Decorations`, and `LoomEditor.CreateDecorationsCollection(IReadOnlyList<EditorDecoration>?)`. The Monaco types stay internal | P13-02, P13-04 |
| Highlighting: the host's own styles (`DecorationStyles` is frozen) | public `CustomDecorationStyle` + `CustomDecorationStyles.Register / Unregister` (global, thread-safe, copy-on-write); `DecorationStyles` and `DecorationStyle` stay internal | P13-03 |
| Language: `LanguageRegistry.GetLanguageIdByFileName` | public static **`BuiltInLanguages.GetLanguageIdByFileName(string)`** next to `BuiltInLanguages.Ids`; `LanguageRegistry` stays internal | P13-01 |
| Unified diff: `ViewZones`, `ViewZone` | **facade**: `AddViewZone(int afterLineNumber, double height, FrameworkElement? content = null)` → id, `RemoveViewZone(string id)`. The content is placed over the gap by the zone-widget layer of phase 11 | P13-08 |
| Unified diff: `ViewZonePointer` | **not exposed**: a host zone's content is a WinUI element and receives its own pointer events; the internal event stays for the diff editor | — |
| Unified diff: `SetHiddenAreas` | public **`SetHiddenAreas(IReadOnlyList<Range>)`**, one host source per editor, merged with folding; the internal `(ranges, source)` overload stays | P13-07 |
| Staging: a margin click to tick a line | new public event **`GutterClicked`** (`GutterClickedEventArgs`: `LineNumber`, `Area`, `Handled`), raised on a left press in the gutter; `Handled = true` cancels the line selection / fold toggle | P13-07 |

Already public, nothing to add: `ScrollTop` (get / set), `Model` (`TextModel`: `LineCount`, `GetLineContent`, `SetValue`, `ContentChanged`, …), `LanguageId`, `IsReadOnly`,
`Theme`, and on `LoomDiffEditor` the two sides `Original` / `Modified` (each a `LoomEditor`, so every member above works on a side). **No member is added to
`LoomDiffEditor`.**

### Rules for every card

1. **Where the public types live.** Logic types that unit tests must reach live in View: namespace `LoomEditor.View.Decorations`, folder `src/LoomEditor.View/Decorations/`
   (new). Event args and enums of the control live in WinUI: namespace `LoomEditor.WinUI.Controls`, one file each in `src/LoomEditor.WinUI/Controls/`. The language lookup
   lives on `BuiltInLanguages` (TextMate). Core gets **no** new public member.
2. **Public API files.** Every new public member goes to the project's `PublicAPI.Unshipped.txt` with `./eng/Update-PublicApi.ps1 src/<project>`. A card lists the exact lines
   it adds; review the diff — no other line may change.
3. **XML doc** on every public member, `<exception>` for each thrown exception, and `<remarks>Threading: …</remarks>` on every public type: UI thread only, except
   `CustomDecorationStyles` and `BuiltInLanguages` (thread-safe) and the immutable DTOs (immutable; thread-safe).
4. **Names.** `LoomEditor` already has `CreateDecorationsCollection(IReadOnlyList<ModelDeltaDecoration>?)` (internal): P13-04 renames that one
   `CreateModelDecorationsCollection` (else `CreateDecorationsCollection()` is ambiguous) and updates its callers. The public collection type is `DecorationsCollection`
   (Monaco: `IEditorDecorationsCollection`), **not** `EditorDecorationsCollection`, which is the internal port and keeps Monaco's name.
5. **Lines are 1-based model line numbers; ranges are `LoomEditor.Core.Text.Range`** (public). Heights and offsets are DIPs (`double`).
6. **Validation.** Bad arguments throw `ArgumentNullException` / `ArgumentOutOfRangeException` / `ArgumentException` from the public member; positions and ranges inside the
   model are **clamped** (Monaco validates them), never rejected — except `AddViewZone`, whose `afterLineNumber` must be `0 … LineCount` (a zone after a line that does not
   exist is a host bug).
7. **No reflection, no `dynamic`, no JSON, no `x:Bind`** (Native AOT); WinUI elements are built in code.

### Decorations (P13-02, P13-03)

8. **`EditorDecorationOptions` is a sealed class, not a record** (a record would compare the cached internal options): immutable `init` properties `ClassName`,
   `IsWholeLine`, `InlineClassName`, `MarginClassName`, `LinesDecorationsClassName` (`string?` / `bool`), `OverviewRulerColor`, `OverviewRulerDarkColor`
   (`ThemeColor?`). It converts itself once, lazily, to an internal `ModelDecorationOptions` (`Description = "host-decoration"`, stickiness
   `NeverGrowsWhenTypingAtEdges`, overview ruler in `OverviewRulerLane.Center` with the colors as `#RRGGBBAA` hex values). Hosts reuse one options object for many
   decorations (one per style), as Monaco's `ModelDecorationOptions.register`.
9. **`EditorDecoration` is a `readonly record struct (Range Range, EditorDecorationOptions Options)`.** A decoration with `Options == null` (the `default` struct) is an
   `ArgumentException` naming its index.
10. **`DecorationsCollection`** (sealed class, internal constructor around the internal `EditorDecorationsCollection`): `Count`, `Set`, `Append`, `Clear`, `GetRange(int)` →
    `Range?`, `GetRanges()`. Methods return nothing (the internal ids stay internal). No change event (YAGNI).
11. **Styles are global, like CSS.** `CustomDecorationStyles.Register(className, style)` adds or replaces a host class; `Unregister(className)` → `bool`. A name must match
    `[A-Za-z0-9_-]+` (decoration class names are cleaned to that set) and must not be a built-in class (`ArgumentException`). The renderer asks the built-in table first, then
    the host table. A change shows at the next repaint of each editor; register styles before setting decorations. Thread-safe: writers take a lock and swap a new
    dictionary; readers read the current one.
12. **`CustomDecorationStyle`** (sealed record): `Background`, `Border`, `Foreground` (`ThemeColor?`) with `DarkBackground`, `DarkBorder`, `DarkForeground` (used for `Dark` and
    `HighContrastDark` themes; `null` = the light color), `BorderWidth` (`double`, default 1: a box border when `Border` is set) and `LeftBorderWidth` (`double`, default 0: a
    bar on the left edge, in the border color). A host class works in every field of `EditorDecorationOptions`: the background and border draw in the text area
    (`ClassName`), the gutter (`MarginClassName`) and the line-decorations strip (`LinesDecorationsClassName`); the foreground colors text (`InlineClassName`).

### The control (P13-04 … P13-08)

13. **Scroll and layout events** keep the data and the raise points of the internal events they replace: `ScrollChanged` on every scroll position or scroll height change,
    `LayoutChanged` on a new layout info **and** after a font change. A column beside the editor redraws on both (the how-to of P13-11 shows the loop).
14. **`GutterClicked`** is raised only for a **left** press on a model line (not below the last line), in `GlyphMargin`, `LineNumbers` or `LineDecorations`, before the editor
    acts on the press. Not for the comment "+" button (an overlay element).
15. **Host view zones** are `Widget` zones (the renderer paints nothing) with `SuppressMouseDown = true`; their content is stretched over the content area (from the left of
    the text to the right of the content, like a comment box without its inset) and sized to the zone. Without the template part `PART_ZoneWidgetCanvas` a zone is a plain
    gap. `RemoveViewZone` only removes ids returned by `AddViewZone` (comment and diff zones are refused: `false`). If the focus is inside the removed content, the editor
    takes the focus first (rule of P11-14).
16. **Hidden areas** from the host use one source object per editor; `SetHiddenAreas([])` shows them again; folding's areas are untouched.
