# How to use the host API

The host API lets an application that references only the NuGet packages build features like a git client's (blame, diff, conflicts, staging) on top of `LoomEditor`.
It is a small public facade, not the editor's internals. The design is in [ADR 0015](../adr/0015-public-host-api.md); a complete example is the Gallery window
**Host API demo** (`samples/LoomEditor.Gallery/HostApiDemoWindow*.cs`, public members only).

## Language of a file

```csharp
editor.LanguageId = BuiltInLanguages.GetLanguageIdByFileName("src/Program.cs") ?? "plaintext";   // "csharp"
```

## Highlight lines

Reuse one `EditorDecorationOptions` for many decorations (one per style). Built-in classes that are useful: `line-insert`, `line-delete`, `char-insert`, `char-delete`,
`rangeHighlight`.

```csharp
var added = new EditorDecorationOptions { ClassName = "line-insert", IsWholeLine = true, OverviewRulerColor = ThemeColor.FromRgb(0x20, 0x80, 0x40) };
DecorationsCollection marks = editor.CreateDecorationsCollection();
marks.Set((EditorDecoration[])[new EditorDecoration(new Range(10, 1, 12, 1), added)]);   // follows edits
marks.Clear();
```

Loading new text (`Text`, `ResetText`) removes every decoration: set them again.

## Your own styles

Styles are global, like CSS: register them once, before use, under a prefixed name (`[A-Za-z0-9_-]+`, not a built-in name).

```csharp
CustomDecorationStyles.Register("vigo-staged", new CustomDecorationStyle
{
    Border = ThemeColor.FromRgb(0x2E, 0x9E, 0x4F), DarkBorder = ThemeColor.FromRgb(0x4E, 0xC9, 0x70),
    BorderWidth = 0, LeftBorderWidth = 3,   // a bar on the left edge
});
```

The dark colors are used by dark and high-contrast dark themes; `null` falls back to the light color. Use the class in `ClassName` (text area), `MarginClassName` (gutter)
or `LinesDecorationsClassName` (line-decorations strip); `InlineClassName` colors text.

## A column beside the editor

Redraw on both events; a line's top in the editor is `GetTopForLineNumber(line, includeViewZones: true) - ScrollTop`.

```csharp
editor.ScrollChanged += (_, _) => Redraw();
editor.LayoutChanged += (_, _) => Redraw();   // also raised when the font (so LineHeight) changes
editor.LineNumbersEnabled = false;            // if your column replaces the numbers
```

Lines hidden by folding or `SetHiddenAreas` answer with the top of the nearest visible line above: skip rows that are less than one `LineHeight` below the previous one
(the Gallery's `HostApiDemoWindow.Blame.cs` does).

## Reload a file

`Text` is not kept in sync with edits, so setting it to the same string does nothing. `ResetText(text)` always replaces the text.

## Unified diff and staging

```csharp
editor.SetHiddenAreas((Range[])[new Range(20, 1, 40, 1)]);   // lines 20-40 hidden; SetHiddenAreas(Array.Empty<Range>()) shows them; folding is untouched
editor.GutterClicked += (_, e) =>
{
    if (e.Area == GutterArea.LineDecorations) { Toggle(e.LineNumber); e.Handled = true; }   // Handled: no line selection, no fold toggle
};
```

`GutterClicked` is raised for a left press on a model line, in `GlyphMargin`, `LineNumbers` or `LineDecorations`. A staged bar is a decoration with a style like `vigo-staged`.

## Content between lines

```csharp
string id = editor.AddViewZone(afterLineNumber: 8, height: editor.LineHeight, content: new Button { Content = "Accept current" });
editor.RemoveViewZone(id);   // false when the id is unknown or already removed
```

The content is sized to the zone and gets its own pointer and keyboard input. Its theme and its focus handling are the host's job; removing a zone whose content has the focus
gives the focus back to the editor.

## The diff editor

`LoomDiffEditor` has no new member: use `Original` and `Modified`, which are `LoomEditor`s, so every member above works on a side. Host view zones and hidden areas are
**not supported** on a diff side (the diff alignment and "hide unchanged regions" own them).

## What stays internal and why

Monaco's internals (`ModelDecorationOptions`, `ViewZone`, `LanguageRegistry`, `DecorationStyles`) drag large type graphs (25-field options, untyped payloads, change-accessor
protocols); making them public would freeze them. The facade above covers the needs of a host and can grow member by member. The XML docs inside the packages still describe
internal members (the compiler writes them): do not rely on a member that is not in `PublicAPI.Shipped.txt`.
