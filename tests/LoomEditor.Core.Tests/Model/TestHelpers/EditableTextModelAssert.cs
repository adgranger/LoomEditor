// Ported from microsoft/vscode: src/vs/editor/test/common/model/editableTextModelTestUtils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.TestHelpers;

/// <summary>Monaco: <c>editableTextModelTestUtils.ts</c>. <c>MirrorTextModel</c> is replaced by <see cref="MirrorText"/>.</summary>
internal static class EditableTextModelAssert
{
    /// <summary>Monaco: <c>testApplyEditsWithSyncedModels</c>.</summary>
    public static void TestApplyEditsWithSyncedModels(
        string[] original,
        TextEdit[] edits,
        string[] expected,
        bool inputEditsAreInvalid = false)
    {
        string originalStr = string.Join('\n', original);
        string expectedStr = string.Join('\n', expected);

        AssertSyncedModels(originalStr, (model, assertMirrorModels) =>
        {
            // Apply edits & collect inverse edits
            IReadOnlyList<TextEdit> inverseEdits = model.ApplyEdits(edits, computeUndoEdits: true);

            // Assert edits produced expected result
            Assert.Equal(expectedStr, model.GetValue(EndOfLinePreference.LF));

            assertMirrorModels();

            // Apply the inverse edits
            IReadOnlyList<TextEdit> inverseInverseEdits = model.ApplyEdits(inverseEdits, computeUndoEdits: true);

            // Assert the inverse edits brought back model to original state
            Assert.Equal(originalStr, model.GetValue(EndOfLinePreference.LF));

            if (!inputEditsAreInvalid)
            {
                // Assert the inverse of the inverse edits are the original edits
                Assert.Equal(edits, inverseInverseEdits);
            }

            assertMirrorModels();
        });
    }

    /// <summary>Monaco: <c>assertSyncedModels</c>.</summary>
    public static void AssertSyncedModels(string text, Action<TextModel, Action> callback, Action<TextModel>? setup = null)
    {
        var model = TestTextModels.Create(text);
        model.SetEol(EndOfLineSequence.LF);
        AssertLineMapping(model, "model");

        if (setup is not null)
        {
            setup(model);
            AssertLineMapping(model, "model");
        }

        var mirror = new MirrorText(model);

        void AssertMirrorModels()
        {
            AssertLineMapping(model, "model");
            Assert.Equal(model.GetValue(), mirror.Text);
            Assert.Equal(model.VersionId, mirror.VersionId);
        }

        callback(model, AssertMirrorModels);
    }

    // Monaco: assertLineMapping (both directions)
    private static void AssertLineMapping(TextModel model, string msg)
    {
        AssertOneDirectionLineMapping(model, offsetToPosition: false, msg);
        AssertOneDirectionLineMapping(model, offsetToPosition: true, msg);
    }

    // Monaco: assertOneDirectionLineMapping
    private static void AssertOneDirectionLineMapping(TextModel model, bool offsetToPosition, string msg)
    {
        string allText = model.GetValue();

        int line = 1;
        int column = 1;
        bool previousIsCarriageReturn = false;
        for (int offset = 0; offset <= allText.Length; offset++)
        {
            // The position coordinate system cannot express the position between \r and \n
            var position = new Position(line, column + (previousIsCarriageReturn ? -1 : 0));

            if (offsetToPosition)
            {
                Position actualPosition = model.GetPositionAt(offset);
                Assert.True(
                    position.ToString() == actualPosition.ToString(),
                    $"{msg} - GetPositionAt mismatch for offset {offset}: expected {position}, got {actualPosition}");
            }
            else
            {
                // The position coordinate system cannot express the position between \r and \n
                int expectedOffset = offset + (previousIsCarriageReturn ? -1 : 0);
                int actualOffset = model.GetOffsetAt(position);
                Assert.True(
                    expectedOffset == actualOffset,
                    $"{msg} - GetOffsetAt mismatch for position {position}: expected {expectedOffset}, got {actualOffset}");
            }

            char c = offset < allText.Length ? allText[offset] : '\0';
            if (c == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }

            previousIsCarriageReturn = c == '\r';
        }
    }
}
