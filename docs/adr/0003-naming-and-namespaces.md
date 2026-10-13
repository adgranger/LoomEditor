# ADR 0003 — Naming and namespaces

- **Status:** accepted
- **Date:** 2026-10-13

## Decisions

1. **Namespace = project name + folder.** `src/LoomEditor.Core/Text/Range.cs` → `LoomEditor.Core.Text`.
   This is the .NET default; a developer can find any type from its namespace alone.
2. **Monaco concept names are kept**: `TextModel`, `Position`, `Range`, `Selection`, `Cursor`, `ViewModel`…
   C# conventions are applied on top (PascalCase, properties, `IDisposable`, .NET events). Method names that are
   awkward in C# are adapted, and the Monaco name is given in a `<remarks>` (e.g. `Range.Plus` ← `Range.plusRange`).
3. **`Range` vs `System.Range`.** Both are imported in most files (`ImplicitUsings` imports `System`), which makes
   `Range` ambiguous. Every project that uses it declares a global alias in its `.csproj`:
   ```xml
   <Using Include="LoomEditor.Core.Text.Range" Alias="Range" />
   ```
   Users of the library add the same line (documented in the README).
4. **The control is named `LoomEditor`**, like the root namespace, because it is the product
   (`<loom:LoomEditor />` in XAML). Inside a `LoomEditor.*` namespace, `LoomEditor.Something` would bind to the
   class, so always use `using` directives (or an alias, as in `LoomEditorAutomationPeer.cs`). CA1724 is suppressed
   on that one class, with a comment.
5. **Commands are `EditorCommand`, not `ICommand`**, to avoid confusion with `System.Windows.Input.ICommand`
   used by WinUI buttons.
6. **Command ids** reuse Monaco ids when the command exists in Monaco (`editor.action.selectAll`), else start with
   `loom.`. **Feature ids** start with `loom.` (`loom.selectAll`).
