// Inspired by microsoft/vscode: src/vs/editor/contrib/linesOperations/browser/linesOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.LinesOperations;

/// <summary>Line commands: copy, move, sort, trim, delete, join, transpose and case transforms (all of them).</summary>
/// <remarks>Menus (<c>menuOpts</c>) are not ported. Threading: UI thread only.</remarks>
public sealed class LinesOperationsFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];

    /// <inheritdoc />
    public string Id => "loom.linesOperations";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        void Register(string id, Action run) =>
            // Monaco: precondition `writable`.
            _registrations.Add(context.Commands.Register(new EditorCommand(id, run, () => !viewModel.CursorConfig.ReadOnly)));

        void RegisterCase(string id, Func<string, string> modifyText) =>
            Register(id, () => LinesOperationsActions.TransformCase(viewModel, id, modifyText));

        Register(LinesOperationsCommands.CopyLinesUp, () => LinesOperationsActions.CopyLines(viewModel, false, LinesOperationsCommands.CopyLinesUp));
        Register(LinesOperationsCommands.CopyLinesDown, () => LinesOperationsActions.CopyLines(viewModel, true, LinesOperationsCommands.CopyLinesDown));
        Register(LinesOperationsCommands.DuplicateSelection, () => LinesOperationsActions.DuplicateSelection(viewModel));
        Register(LinesOperationsCommands.MoveLinesUp, () => LinesOperationsActions.MoveLines(viewModel, false, LinesOperationsCommands.MoveLinesUp));
        Register(LinesOperationsCommands.MoveLinesDown, () => LinesOperationsActions.MoveLines(viewModel, true, LinesOperationsCommands.MoveLinesDown));
        Register(LinesOperationsCommands.SortLinesAscending, () => LinesOperationsActions.SortLines(viewModel, false, LinesOperationsCommands.SortLinesAscending));
        Register(LinesOperationsCommands.SortLinesDescending, () => LinesOperationsActions.SortLines(viewModel, true, LinesOperationsCommands.SortLinesDescending));
        Register(LinesOperationsCommands.RemoveDuplicateLines, () => LinesOperationsActions.RemoveDuplicateLines(viewModel));
        Register(LinesOperationsCommands.ReverseLines, () => LinesOperationsActions.ReverseLines(viewModel));
        Register(LinesOperationsCommands.TrimTrailingWhitespace, () => LinesOperationsActions.TrimTrailingWhitespace(viewModel));
        Register(LinesOperationsCommands.DeleteLines, () => LinesOperationsActions.DeleteLines(viewModel));
        Register(LinesOperationsCommands.IndentLines, () => LinesOperationsActions.IndentLines(viewModel));
        Register(LinesOperationsCommands.OutdentLines, () => LinesOperationsActions.OutdentLines(viewModel));
        Register(LinesOperationsCommands.InsertLineBefore, () => LinesOperationsActions.InsertLineBefore(viewModel));
        Register(LinesOperationsCommands.InsertLineAfter, () => LinesOperationsActions.InsertLineAfter(viewModel));
        Register(LinesOperationsCommands.DeleteAllLeft, () => LinesOperationsActions.DeleteAllToBoundary(viewModel, LinesOperationsCommands.DeleteAllLeft, false));
        Register(LinesOperationsCommands.DeleteAllRight, () => LinesOperationsActions.DeleteAllToBoundary(viewModel, LinesOperationsCommands.DeleteAllRight, true));
        Register(LinesOperationsCommands.JoinLines, () => LinesOperationsActions.JoinLines(viewModel));
        Register(LinesOperationsCommands.Transpose, () => LinesOperationsActions.Transpose(viewModel));
        RegisterCase(LinesOperationsCommands.TransformToUppercase, CaseTransforms.ToUpper);
        RegisterCase(LinesOperationsCommands.TransformToLowercase, CaseTransforms.ToLower);
        RegisterCase(LinesOperationsCommands.TransformToTitlecase, CaseTransforms.ToTitleCase);
        RegisterCase(LinesOperationsCommands.TransformToSnakecase, CaseTransforms.ToSnakeCase);
        RegisterCase(LinesOperationsCommands.TransformToCamelcase, CaseTransforms.ToCamelCase);
        RegisterCase(LinesOperationsCommands.TransformToPascalcase, CaseTransforms.ToPascalCase);
        RegisterCase(LinesOperationsCommands.TransformToKebabcase, CaseTransforms.ToKebabCase);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
    }
}
