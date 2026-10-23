// Ported from microsoft/vscode: src/vs/editor/browser/view/viewLayer.ts (RenderedLinesCollection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Events;

namespace LoomEditor.View.Minimap;

/// <summary>A line kept by a <see cref="RenderedLinesCollection{T}"/>. Monaco: <c>ILine</c>.</summary>
internal interface IRenderedLine
{
    /// <summary>Monaco: <c>onContentChanged</c>.</summary>
    void OnContentChanged();

    /// <summary>Monaco: <c>onTokensChanged</c>.</summary>
    void OnTokensChanged();
}

/// <summary>The lines rendered by the last frame, from line number <c>rendLineNumberStart</c>. Monaco: <c>RenderedLinesCollection</c>.</summary>
/// <remarks>
/// Threading: UI thread only. Behavior differences: only what the minimap uses is ported (no <c>getLine</c>, <c>getCount</c> or
/// <c>flush</c>); the line factory is a delegate.
/// </remarks>
internal sealed class RenderedLinesCollection<T>(Func<T> createLine)
    where T : IRenderedLine
{
    private List<T> _lines = [];
    private int _rendLineNumberStart = 1;

    /// <summary>Monaco: <c>_set</c>.</summary>
    public void Set(int rendLineNumberStart, List<T> lines)
    {
        _lines = lines;
        _rendLineNumberStart = rendLineNumberStart;
    }

    /// <summary>Monaco: <c>_get</c>.</summary>
    public (int RendLineNumberStart, List<T> Lines) Get() => (_rendLineNumberStart, _lines);

    /// <summary>Monaco: <c>onLinesDeleted</c>.</summary>
    public void OnLinesDeleted(int deleteFromLineNumber, int deleteToLineNumber)
    {
        if (_lines.Count == 0)
        {
            return;
        }

        int startLineNumber = _rendLineNumberStart;
        int endLineNumber = startLineNumber + _lines.Count - 1;
        if (deleteToLineNumber < startLineNumber)
        {
            // deleting above the viewport
            _rendLineNumberStart -= deleteToLineNumber - deleteFromLineNumber + 1;
            return;
        }

        if (deleteFromLineNumber > endLineNumber)
        {
            // deleted below the viewport
            return;
        }

        int deleteStartIndex = 0;
        int deleteCount = 0;
        for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            int lineIndex = lineNumber - _rendLineNumberStart;
            if (deleteFromLineNumber <= lineNumber && lineNumber <= deleteToLineNumber)
            {
                if (deleteCount == 0)
                {
                    deleteStartIndex = lineIndex;
                    deleteCount = 1;
                }
                else
                {
                    deleteCount++;
                }
            }
        }

        if (deleteFromLineNumber < startLineNumber)
        {
            // something was deleted above
            _rendLineNumberStart -= deleteToLineNumber < startLineNumber
                ? deleteToLineNumber - deleteFromLineNumber + 1
                : startLineNumber - deleteFromLineNumber;
        }

        _lines.RemoveRange(deleteStartIndex, deleteCount);
    }

    /// <summary>Monaco: <c>onLinesChanged</c>.</summary>
    public bool OnLinesChanged(int changeFromLineNumber, int changeCount)
    {
        if (_lines.Count == 0)
        {
            return false;
        }

        int changeToLineNumber = changeFromLineNumber + changeCount - 1;
        int endLineNumber = _rendLineNumberStart + _lines.Count - 1;
        bool someoneNotified = false;
        for (int changedLineNumber = changeFromLineNumber; changedLineNumber <= changeToLineNumber; changedLineNumber++)
        {
            if (changedLineNumber >= _rendLineNumberStart && changedLineNumber <= endLineNumber)
            {
                _lines[changedLineNumber - _rendLineNumberStart].OnContentChanged();
                someoneNotified = true;
            }
        }

        return someoneNotified;
    }

    /// <summary>Monaco: <c>onLinesInserted</c>.</summary>
    public void OnLinesInserted(int insertFromLineNumber, int insertToLineNumber)
    {
        if (_lines.Count == 0)
        {
            return;
        }

        int insertCnt = insertToLineNumber - insertFromLineNumber + 1;
        int endLineNumber = _rendLineNumberStart + _lines.Count - 1;
        if (insertFromLineNumber <= _rendLineNumberStart)
        {
            // inserting above the viewport
            _rendLineNumberStart += insertCnt;
            return;
        }

        if (insertFromLineNumber > endLineNumber)
        {
            // inserting below the viewport
            return;
        }

        if (insertCnt + insertFromLineNumber > endLineNumber)
        {
            // all the remaining lines are pushed outside
            _lines.RemoveRange(insertFromLineNumber - _rendLineNumberStart, endLineNumber - insertFromLineNumber + 1);
            return;
        }

        // push out some lines, but not all the remaining ones
        int insertIndex = insertFromLineNumber - _rendLineNumberStart;
        _lines.RemoveRange(_lines.Count - insertCnt, insertCnt);
        var newLines = new List<T>(insertCnt);
        for (int i = 0; i < insertCnt; i++)
        {
            newLines.Add(createLine());
        }

        _lines.InsertRange(insertIndex, newLines);
    }

    /// <summary>Monaco: <c>onTokensChanged</c>.</summary>
    public bool OnTokensChanged(IReadOnlyList<ViewLineRange> ranges)
    {
        if (_lines.Count == 0)
        {
            return false;
        }

        int startLineNumber = _rendLineNumberStart;
        int endLineNumber = startLineNumber + _lines.Count - 1;
        bool notifiedSomeone = false;
        foreach (ViewLineRange range in ranges)
        {
            if (range.ToLineNumber < startLineNumber || range.FromLineNumber > endLineNumber)
            {
                continue;
            }

            int from = Math.Max(startLineNumber, range.FromLineNumber);
            int to = Math.Min(endLineNumber, range.ToLineNumber);
            for (int lineNumber = from; lineNumber <= to; lineNumber++)
            {
                _lines[lineNumber - _rendLineNumberStart].OnTokensChanged();
                notifiedSomeone = true;
            }
        }

        return notifiedSomeone;
    }
}
