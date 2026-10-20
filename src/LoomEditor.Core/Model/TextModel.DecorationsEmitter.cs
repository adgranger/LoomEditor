// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts (DidChangeDecorationsEmitter)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model.Events;

namespace LoomEditor.Core.Model;

// The decorations emitter: its state lives in the model, apart from the content emitter (TextModel.DeferredEmit.cs),
// like the two emitters of Monaco. Not ported: the lines affected by injected text, line heights and fonts, and
// handleBeforeFire (none of these exist here). Monaco never resets _affectsLineNumber and nothing sets it here
// (lineNumberClassName is not ported), so it is always false.
public sealed partial class TextModel
{
    private int _decorationsDeferredCount;
    private bool _decorationsShouldFireDeferred;
    private bool _decorationsAffectsMinimap;
    private bool _decorationsAffectsOverviewRuler;
    private bool _decorationsAffectsGlyphMargin;

    /// <summary>Raised when decorations are added, changed or removed and when an edit moves them (Monaco: <c>onDidChangeDecorations</c>).</summary>
    /// <remarks>Raised once per operation: the events of a deferred operation are merged. <c>SetValue</c> and <c>SetEol</c> alone do not raise it.</remarks>
    internal event EventHandler<ModelDecorationsChangedEventArgs>? DecorationsChanged;

    // Monaco: DidChangeDecorationsEmitter.beginDeferredEmit
    private void BeginDecorationsDeferredEmit() => _decorationsDeferredCount++;

    // Monaco: DidChangeDecorationsEmitter.endDeferredEmit
    private void EndDecorationsDeferredEmit()
    {
        _decorationsDeferredCount--;
        if (_decorationsDeferredCount == 0 && _decorationsShouldFireDeferred)
        {
            DoFireDecorationsChanged();
        }
    }

    // Monaco: DidChangeDecorationsEmitter.checkAffectedAndFire (only the glyph margin of the four flags can be set)
    private void CheckAffectedAndFireDecorations(ModelDecorationOptions options)
    {
        _decorationsAffectsGlyphMargin |= options.GlyphMarginClassName is not null;
        TryFireDecorationsChanged();
    }

    // Monaco: DidChangeDecorationsEmitter.fire
    private void FireDecorationsChanged()
    {
        _decorationsAffectsMinimap = true;
        _decorationsAffectsOverviewRuler = true;
        _decorationsAffectsGlyphMargin = true;
        TryFireDecorationsChanged();
    }

    private void TryFireDecorationsChanged()
    {
        if (_decorationsDeferredCount == 0)
        {
            DoFireDecorationsChanged();
        }
        else
        {
            _decorationsShouldFireDeferred = true;
        }
    }

    private void DoFireDecorationsChanged()
    {
        var args = new ModelDecorationsChangedEventArgs(
            _decorationsAffectsMinimap, _decorationsAffectsOverviewRuler, _decorationsAffectsGlyphMargin, affectsLineNumber: false);
        _decorationsShouldFireDeferred = false;
        _decorationsAffectsMinimap = false;
        _decorationsAffectsOverviewRuler = false;
        _decorationsAffectsGlyphMargin = false;
        DecorationsChanged?.Invoke(this, args);
    }
}
