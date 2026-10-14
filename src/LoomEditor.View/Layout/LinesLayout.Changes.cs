// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/linesLayout.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.View.Layout;

// Change accessors and the pending whitespace changes committed after a ChangeWhitespace callback.
internal sealed partial class LinesLayout
{
    private sealed class LineHeightChangeAccessor(LineHeightsManager manager) : ILineHeightChangeAccessor
    {
        public bool HadAChange { get; private set; }

        public void InsertOrChangeCustomLineHeight(string decorationId, int startLineNumber, int endLineNumber, int lineHeight)
        {
            HadAChange = true;
            manager.InsertOrChangeCustomLineHeight(decorationId, startLineNumber, endLineNumber, lineHeight);
        }

        public void RemoveCustomLineHeight(string decorationId)
        {
            HadAChange = true;
            manager.RemoveCustomLineHeight(decorationId);
        }
    }

    private sealed class WhitespaceChangeAccessor(LinesLayout layout) : IWhitespaceChangeAccessor
    {
        public bool HadAChange { get; private set; }

        public string InsertWhitespace(int afterLineNumber, int ordinal, int heightInPx, int minWidth)
        {
            HadAChange = true;
            var id = layout._instanceId + (++layout._lastWhitespaceId).ToString(CultureInfo.InvariantCulture);
            layout._pendingChanges.Insert(new EditorWhitespace(id, afterLineNumber, ordinal, heightInPx, minWidth));
            return id;
        }

        public void ChangeOneWhitespace(string id, int newAfterLineNumber, int newHeight)
        {
            HadAChange = true;
            layout._pendingChanges.Change(new PendingChange(id, newAfterLineNumber, newHeight));
        }

        public void RemoveWhitespace(string id)
        {
            HadAChange = true;
            layout._pendingChanges.Remove(new PendingRemove(id));
        }
    }

    internal sealed record PendingChange(string Id, int NewAfterLineNumber, int NewHeight);

    internal sealed record PendingRemove(string Id);

    private sealed class PendingChanges
    {
        private bool _hasPending;
        private List<EditorWhitespace> _inserts = [];
        private List<PendingChange> _changes = [];
        private List<PendingRemove> _removes = [];

        public void Insert(EditorWhitespace x)
        {
            _hasPending = true;
            _inserts.Add(x);
        }

        public void Change(PendingChange x)
        {
            _hasPending = true;
            _changes.Add(x);
        }

        public void Remove(PendingRemove x)
        {
            _hasPending = true;
            _removes.Add(x);
        }

        public void Commit(LinesLayout linesLayout)
        {
            if (!_hasPending)
            {
                return;
            }

            var inserts = _inserts;
            var changes = _changes;
            var removes = _removes;

            _hasPending = false;
            _inserts = [];
            _changes = [];
            _removes = [];

            linesLayout.CommitPendingChanges(inserts, changes, removes);
        }
    }
}
