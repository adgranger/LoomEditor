# P13-03 — Host decoration styles: `CustomDecorationStyle`, `CustomDecorationStyles`

- **Status:** done
- **Depends on:** P13-02
- **Size:** M

**Recommended model:** Sonnet

## Goal

A host declares its own decoration classes (`"vigo-blame-recent"`, `"vigo-staged"`) with colors for light and dark themes, and the renderer draws them in the text area,
the gutter and the line-decorations strip, like the built-in classes. The built-in table stays internal and unchanged.

## Sources

Sources: none (specified in this card; [phase-13/README.md](README.md) decisions 11 and 12). Read first: `src/LoomEditor.View/Layout/DecorationStyles.cs` (all, `Get`),
`src/LoomEditor.View/Layout/DecorationStyle.cs`, `src/LoomEditor.View/Layout/DecorationColorResolver.cs` (the light / dark rule), `src/LoomEditor.Rendering/TextViewRenderer.Decorations.cs`
(`GetDecorationForeground`, `GetDecorationColor`, `DrawDecorationBox`), `src/LoomEditor.Rendering/TextViewRenderer.Gutter.cs` (`DrawGutter`: margin and lines decorations use
`DrawDecorationBox`), `tests/LoomEditor.View.Tests/Layout/DecorationStylesTests.cs`, `tests/LoomEditor.Rendering.Tests/TextViewRendererDecorationsTests.cs` and
`TextViewRendererGutterTests.cs` (how a drawn rectangle and its color are asserted).

## Files to create or change

- `src/LoomEditor.View/Decorations/CustomDecorationStyle.cs` — create
- `src/LoomEditor.View/Decorations/CustomDecorationStyles.cs` — create
- `src/LoomEditor.View/Decorations/EditorDecorationOptions.cs` — change (`<see cref="CustomDecorationStyles"/>` in the class remarks)
- `src/LoomEditor.View/Layout/DecorationStyle.cs` — change (two resolvers)
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — change (the host table)
- `src/LoomEditor.Rendering/TextViewRenderer.Decorations.cs` — change
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — change
- `tests/LoomEditor.View.Tests/Decorations/CustomDecorationStylesTests.cs` — create
- `tests/LoomEditor.Rendering.Tests/TextViewRendererDecorationsTests.cs`, `TextViewRendererGutterTests.cs` — change

## Design decisions (already made — do not change)

- **`CustomDecorationStyle`**: `public sealed record` with `init` properties (XML doc on each): `ThemeColor? Background`, `ThemeColor? DarkBackground`, `ThemeColor? Border`,
  `ThemeColor? DarkBorder`, `ThemeColor? Foreground`, `ThemeColor? DarkForeground`, `double BorderWidth { get; init; } = 1`, `double LeftBorderWidth { get; init; }`. Dark
  colors are used when the theme type is `Dark` or `HighContrastDark` (the rule of `DecorationColorResolver`); a null dark color falls back to the light one. Threading:
  immutable.
- **`CustomDecorationStyles`**: `public static class`, thread-safe:
  - `public static void Register(string className, CustomDecorationStyle style)` — adds or replaces. `ArgumentNullException` for nulls; `ArgumentException` (param
    `className`) when the name is empty, has a character outside `[A-Za-z0-9_-]` (check with a loop, no `Regex`), or is a built-in class name; `ArgumentOutOfRangeException`
    when `BorderWidth` or `LeftBorderWidth` is negative or not finite.
  - `public static bool Unregister(string className)` — `false` when it was not registered (or is built-in). `ArgumentNullException` for null.
  The class doc: styles are global to the process, like a stylesheet; a change shows at the next repaint; prefix names with the app's name; the built-in names are the
  class names used by the editor (list a few: `rangeHighlight`, `line-insert`, `line-delete`, `char-insert`, `char-delete`, `findMatch`) and cannot be replaced.
- **Internal model.** `DecorationStyle` gains two optional parameters **at the end** (existing call sites keep compiling): `Func<EditorTheme, ThemeColor?>? BackgroundResolver
  = null, Func<EditorTheme, ThemeColor?>? BorderResolver = null` (XML `<param>` docs). `Register` converts a host style once to `new DecorationStyle(BorderWidth: (float)style.BorderWidth,
  LeftBorderWidth: (float)style.LeftBorderWidth, ForegroundResolver: Pick(style.Foreground, style.DarkForeground), BackgroundResolver: Pick(...), BorderResolver: Pick(...))`
  where `Pick(light, dark)` is `null` when both are null, else `theme => IsDark(theme) ? dark ?? light : light`.
- **`DecorationStyles`** gains `internal static bool IsBuiltIn(string className)` (`Table.ContainsKey`), and the host table: a `private static volatile
  IReadOnlyDictionary<string, DecorationStyle> _custom` (empty at start) replaced by a new `Dictionary` copy under a `private static readonly Lock CustomLock` on each write
  (`internal static void SetCustom(string, DecorationStyle)`, `internal static bool RemoveCustom(string)`). `Get`: the whole string in `Table`, then in `_custom`; then for
  each name of the list, `Table` then `_custom` (built-ins win). `ColorIds` is unchanged (host styles have no color ids). Update the class `<remarks>` threading note
  ("the built-in table is immutable; the host table is copy-on-write; safe from any thread").
- **Renderer.** `DrawDecorationBox`: background = `style.BackgroundResolver?.Invoke(Theme) ?? GetDecorationColor(style.BackgroundColorId) ??
  GetDecorationColor(style.BackgroundFallbackColorId)`; border = `style.BorderResolver?.Invoke(Theme) ?? GetDecorationColor(style.BorderColorId)`. Nothing else changes: the
  gutter (`MarginClassName`, `LinesDecorationsClassName`) already goes through `DrawDecorationBox`, and the foreground already uses `ForegroundResolver`.
- `PublicAPI.Unshipped.txt` (View) gains the lines of the two types only.

## Tests

- [x] New (`CustomDecorationStylesTests`; every test uses its own unique class name and unregisters it in a `finally`: the table is global and xUnit runs classes in
  parallel): `Register` then `DecorationStyles.Get(name)` has resolvers that give the light color for a `Light` theme and the dark color for `Dark` and `HighContrastDark`;
  no dark color → the light one in a dark theme; `Get("foo " + name)` finds it in a class list; re-`Register` replaces; `Unregister` → `true`, then `Get` is `null` and a
  second `Unregister` is `false`; `Register("rangeHighlight", …)` → `ArgumentException` and `Get("rangeHighlight")` is the built-in style; names `""`, `"a b"`, `"é"` →
  `ArgumentException`; `BorderWidth = -1` → `ArgumentOutOfRangeException`; `Unregister("rangeHighlight")` → `false`.
- [x] New (`TextViewRendererDecorationsTests`): a whole-line decoration with a host class whose `Background` is red fills the line with red in a light theme and with its
  `DarkBackground` in a dark theme; a host class with `Border` and `BorderWidth = 0`, `LeftBorderWidth = 3` draws a 3-DIP bar on the left edge only.
- [x] New (`TextViewRendererGutterTests`): a decoration whose `MarginClassName` is a host class fills the gutter of its line with the host background.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (existing `DecorationStylesTests` unchanged and green)
- [x] `PublicAPI.Unshipped.txt` (View) gains only the two types' lines
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Per-editor styles, overriding a built-in class, gutter icons, underlines, strike-through, `AfterContentText`, theme color ids in host styles (colors are values), a style
change event.

## Suggested commit message

`feat(view): let the host register its own decoration styles`

## Result

- Added `CustomDecorationStyle` and `CustomDecorationStyles` (View, public); `DecorationStyle` gained the two resolvers, `DecorationStyles` the copy-on-write host table; the renderer uses the resolvers in `DrawDecorationBox`.
- `PublicAPI.Unshipped.txt` (View) gained 28 lines: only these two types.
- Tests: `CustomDecorationStylesTests` (10), 3 rendering tests (whole line light/dark, left bar, gutter margin).
