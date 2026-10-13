# How to add a feature

A **feature** is a piece of editor behavior: Find, Folding, Bracket matching, Toggle comment…
Each feature lives in **one folder** and is enabled by **one line** in `EditorFeatures.cs`.
You should not need to touch any other file.

This guide builds a complete example: **Duplicate line** (`editor.action.copyLinesDownAction` in Monaco).
For the smallest real feature, look at `src/LoomEditor.View/Features/SelectAll/`.

## 1. Create the folder

```
src/LoomEditor.View/Features/DuplicateLine/
├─ DuplicateLineFeature.cs     # implements IEditorFeature
├─ DuplicateLineCommands.cs    # command ids
├─ DuplicateLineModel.cs       # optional: the pure logic
└─ README.md
```

## 2. Declare the command ids

```csharp
namespace LoomEditor.View.Features.DuplicateLine;

/// <summary>Ids of the commands exposed by <see cref="DuplicateLineFeature"/>.</summary>
public static class DuplicateLineCommands
{
    /// <summary>Copies the current line below itself. Same id as in Monaco.</summary>
    public const string CopyLinesDown = "editor.action.copyLinesDownAction";
}
```

Reuse the Monaco id if the command exists in Monaco (search `id: 'editor.action.` in `src/vs/editor/contrib`).
Otherwise use `loom.<something>`.

## 3. Write the logic as a pure function

Pure = takes values, returns values, no events, no UI. It is trivial to test.

```csharp
// Ported from microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/copyLinesCommand.ts
// Upstream commit: <sha>  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.DuplicateLine;

/// <summary>Computes the edit that duplicates a line.</summary>
internal static class DuplicateLineModel
{
    /// <summary>Returns the edit that inserts a copy of <paramref name="lineNumber"/> below it.</summary>
    public static TextEdit ComputeEdit(IReadOnlyList<string> lines, int lineNumber)
    {
        string line = lines[lineNumber - 1];
        return TextEdit.Insert(new Position(lineNumber, line.Length + 1), "\n" + line);
    }
}
```

## 4. Write the feature

The feature only **wires** things: it registers commands and subscribes to events in `Attach`, and undoes
everything in `Dispose`.

```csharp
using LoomEditor.View.Commands;

namespace LoomEditor.View.Features.DuplicateLine;

/// <summary>Adds the "duplicate line" command.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class DuplicateLineFeature : IEditorFeature
{
    private IDisposable? _registration;

    /// <inheritdoc />
    public string Id => "loom.duplicateLine";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _registration = context.Commands.Register(new EditorCommand(
            DuplicateLineCommands.CopyLinesDown,
            execute: () =>
            {
                TextEdit edit = DuplicateLineModel.ComputeEdit(context.Model, context.Selection.PositionLineNumber);
                context.Model.ApplyEdits([edit]);
            }));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registration?.Dispose();
        _registration = null;
    }
}
```

Rules:
- **Never** keep a reference to the context after `Dispose`.
- If you subscribe to an event (`context.SelectionChanged += OnSelectionChanged;`), unsubscribe in `Dispose`.
- Things that are slow (search in a large file…) run in the background with a `CancellationToken`, and their
  results are applied on the UI thread.

## 5. Enable it: one line

`src/LoomEditor.View/Features/EditorFeatures.cs`:

```csharp
public static IReadOnlyList<Func<IEditorFeature>> Default { get; } =
[
    () => new SelectAllFeature(),
    () => new DuplicateLineFeature(),   // ← new
];
```

## 6. Write the README (10 lines)

```markdown
# DuplicateLine

**Role:** duplicates the current line (Shift+Alt+Down in VS Code).
**Monaco source:** `src/vs/editor/contrib/linesOperations/browser/copyLinesCommand.ts`.
**Files:** `DuplicateLineFeature.cs` (wiring), `DuplicateLineModel.cs` (logic), `DuplicateLineCommands.cs` (ids).
**Tests:** `tests/LoomEditor.View.Tests/Features/DuplicateLine/`.
**Known limits:** single cursor only.
```

## 7. Test it

`tests/LoomEditor.View.Tests/Features/DuplicateLine/DuplicateLineFeatureTests.cs`:

```csharp
using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.DuplicateLine;

namespace LoomEditor.View.Tests.Features.DuplicateLine;

public class DuplicateLineFeatureTests
{
    [Fact]
    public void ComputeEdit_InsertsCopyBelow()
    {
        TextEdit edit = DuplicateLineModel.ComputeEdit(["abc", "def"], lineNumber: 1);

        Assert.Equal(TextEdit.Insert(new Position(1, 4), "\nabc"), edit);
    }

    [Fact]
    public void Attach_RegistersCommand_DisposeRemovesIt()
    {
        var context = new EditorContext();
        var feature = new DuplicateLineFeature();

        feature.Attach(context);
        Assert.True(context.Commands.Contains(DuplicateLineCommands.CopyLinesDown));

        feature.Dispose();
        Assert.False(context.Commands.Contains(DuplicateLineCommands.CopyLinesDown));
    }
}
```

If you ported Monaco tests, keep their names in comments (`// Monaco: 'copy lines down'`).

## 8. Check

```powershell
dotnet build LoomEditor.slnx -c Release          # warnings are errors in Release
dotnet test --solution LoomEditor.slnx -c Release
./eng/Update-PublicApi.ps1 src/LoomEditor.View   # if you added public types: review the diff!
```

The architecture tests check that your folder has the three standard files and is listed in `EditorFeatures.cs`.

## Optional: let users opt out

Users choose features with the control's `Features` property (set it before the control is loaded).
The list is explicit, like `EditorFeatures.Default`:

```csharp
editor.Features =
[
    () => new SelectAllFeature(),
    // DuplicateLineFeature left out on purpose
];
```
