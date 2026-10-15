// Ported from microsoft/vscode: src/vs/editor/common/editorCommon.ts (ICommand)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors.Commands;

/// <summary>A command that edits the text and tells where the cursor goes (Monaco: <c>ICommand</c>).</summary>
/// <remarks>
/// <para>Named <c>IEditCommand</c> because the editor also has commands in the sense of "named actions" (ADR 0006, decision 1).</para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal interface IEditCommand
{
    /// <summary>
    /// True when the command inserts whitespace automatically (e.g. indentation after Enter): the model may trim it
    /// later (Monaco: the optional <c>insertsAutoWhitespace</c>).
    /// </summary>
    bool InsertsAutoWhitespace { get; }

    /// <summary>Gets the edit operations of the command, by adding them to <paramref name="builder"/> (Monaco: <c>getEditOperations</c>).</summary>
    void GetEditOperations(TextModel model, IEditOperationBuilder builder);

    /// <summary>Computes the selection of the cursor after the edits (Monaco: <c>computeCursorState</c>).</summary>
    Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper);
}
