// Ported from microsoft/vscode: src/vs/editor/common/model/mirrorTextModel.ts (MirrorTextModel, simplified to a string)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Model.TestHelpers;

/// <summary>
/// Follows a <see cref="TextModel"/> through its <see cref="TextModel.ContentChanged"/> events only:
/// when it equals the model text, the events describe the edits correctly.
/// </summary>
internal sealed class MirrorText
{
    public MirrorText(TextModel model)
    {
        Text = model.GetValue();
        VersionId = model.VersionId;
        model.ContentChanged += (_, e) => Apply(e);
    }

    public string Text { get; private set; }

    public int VersionId { get; private set; }

    private void Apply(ModelContentChangedEventArgs e)
    {
        foreach (ModelContentChange change in e.Changes)
        {
            Text = string.Concat(
                Text.AsSpan(0, change.RangeOffset),
                change.Text,
                Text.AsSpan(change.RangeOffset + change.RangeLength));
        }

        VersionId = e.VersionId;
    }
}
