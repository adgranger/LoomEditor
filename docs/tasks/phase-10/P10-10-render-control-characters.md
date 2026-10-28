# P10-10 — `renderControlCharacters`: visible control characters

- **Status:** done
- **Depends on:** P10-09
- **Size:** S

## Goal

A control character in a line (`U+0001`…`U+001F` except tab, `DEL`) is drawn as its Unicode control picture (`␁`, `␡`…) in white on a dark red cell, instead of nothing or a missing-glyph box; the property `RenderControlCharacters` (default `true`, like Monaco) turns this off. The four characters Monaco always replaces by `�` (BOM, line / paragraph separator, next line) are replaced whatever the option says.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 728–760 | `isControlCharacter` (the `< 32` except tab, `127`; the directional characters of 737–757 are **not** ported: ADR 0013 decision 8) |
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 1178–1204 | the characters produced: `U+0000` → `U+2400`; `c < 32` → `U+2400 + c`; `127` → `U+2421` (9249); BOM `U+FEFF`, `U+2028`, `U+2029`, `U+0085` → `U+FFFD` (always) |
| `src/vs/editor/browser/viewParts/viewLines/viewLines.css` | 17–20 | `.mtkcontrol`: foreground `rgb(255, 255, 255)`, background `rgb(150, 0, 0)` |
| `src/vs/editor/common/config/editorOptions.ts` | 6631–6635 | `renderControlCharacters`, default `true` |

## Files to create or change

- `src/LoomEditor.View/Layout/ControlCharacters.cs` — create: `static string Substitute(string text, bool renderControlCharacters)` and `static bool IsControlPicture(char c)` for the renderer
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: where the line text and its `LineStyle` reach `LineLayoutCache.Get` (grep `LineLayoutCache` / `_layoutCache.Get` in `Render`), substitute first; draw the cell backgrounds and use the white foreground for the substituted offsets (below); property `RenderControlCharacters` (`bool`, default `true`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Whitespace.cs` — change: the `RenderControlCharacters` dependency property (invalidate only)
- `tests/LoomEditor.View.Tests/Layout/ControlCharactersTests.cs`, `tests/LoomEditor.Rendering.Tests/TextViewRendererControlCharactersTests.cs` — create
- Gallery: a switch "Control characters" and a sample line `"a\u0001b\u0007c\u007Fd﻿e"` (in `SampleText.cs` or `FeatureSamples.cs`)

## Design decisions (already made — do not change)

- **1:1 substitution** (ADR 0013 decision 8): `Substitute` returns the **same string instance** when nothing changes (a quick scan; no allocation for normal lines), otherwise a copy of the same length with `c < 32 && c != '\t'` → `(char)(0x2400 + c)` and `127` → `'␡'` (both only when `renderControlCharacters`), and `﻿`, ` `, ` `, `\u0085` → `'�'` (always). Because the length and the offsets are unchanged, columns, caret, selection, hit tests, decorations, find and the model are unaffected: **only the layout text is substituted**, never the model text.
- The renderer draws, for each offset whose **original** character was substituted by a control picture (`IsControlPicture(substituted[i])` for `U+2400…U+241F` and `U+2421`; not the `U+FFFD` ones): a filled rectangle `[GetCharacterX(i), GetCharacterX(i + 1)]` × the line height in `rgb(150, 0, 0)` **before** the text, and the glyph in white through the existing foreground override mechanism (`StyleOverride` in `LineStyle`: read how `TextViewRenderer.Decorations.cs` and `LineStyle.FromTokens` use it; add one override per offset, merged with the existing ones). The two colors are constants in the renderer (Monaco hard-codes them in CSS, in every theme).
- The layout cache key is `(text, style)`: it stays correct because both are the substituted ones.
- `RenderControlCharacters = false` still substitutes the four `U+FFFD` characters and nothing else.
- The substituted text is only for drawing: `ILineLayout.Text` is the substituted one; nothing outside the renderer reads it (check with a grep for `.Text` of layouts in `LoomEditor.WinUI`; if something does, use the view line content instead and report).

## Tests

- [x] New (View): `Substitute_NoControlCharacters_ReturnsSameInstance`, `Substitute_Nul_GivesControlPicture` (`"\0"` → `"␀"`), `Substitute_Us_GivesU241F`, `Substitute_Del_GivesU2421`, `Substitute_Tab_Unchanged`, `Substitute_Bom_GivesReplacementEvenWhenOff`, `Substitute_OptionOff_LeavesControlCharacters`, `Substitute_SameLength`
- [x] New (Rendering, recording surface): `ControlCharacter_DrawsRedCellAndWhiteGlyph`, `OptionOff_NoRedCell`, `Bom_NoRedCell_ButReplacementGlyph`, `Caret_AfterControlCharacter_UsesOriginalOffset`
- [x] Manual (section "P10-10"): the sample line shows `a␁b␇c␡d�e`; the three pictures sit on dark red; the caret moves one character per key press; with the switch off the line looks as before except `�`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `viewLineRenderer.ts` row extended; "Behavior differences": directional formatting characters are not rendered as `[U+202A]`
- [x] Public API file updated; card status and `docs/tasks/README.md` updated

## Out of scope

`[U+202A]`-style rendering of directional characters, unicode highlighting (ambiguous / invisible characters), the minimap and the diff editor's zones.

## Suggested commit message

`feat(rendering): render control characters as control pictures`
