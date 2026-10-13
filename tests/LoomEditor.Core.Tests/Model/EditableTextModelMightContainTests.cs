using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Ported from Monaco <c>editableTextModel.test.ts</c>: suites
/// <c>EditorModel - EditableTextModel.applyEdits updates mightContainRTL</c> and
/// <c>… updates mightContainNonBasicASCII</c>.
/// </summary>
public class EditableTextModelMightContainTests
{
    private static TextEdit EditOp(int startLineNumber, int startColumn, int endLineNumber, int endColumn, params string[] text) =>
        TextEdit.Replace(new Range(startLineNumber, startColumn, endLineNumber, endColumn), string.Join('\n', text));

    private static void TestApplyEditsRtl(string original, TextEdit[] edits, bool before, bool after)
    {
        var model = new TextModel(original);
        model.SetEol(EndOfLineSequence.LF);

        Assert.Equal(before, model.MightContainRtl);

        model.ApplyEdits(edits);
        Assert.Equal(after, model.MightContainRtl);
    }

    private static void TestApplyEditsNonBasicAscii(string original, TextEdit[] edits, bool before, bool after)
    {
        var model = new TextModel(original);
        model.SetEol(EndOfLineSequence.LF);

        Assert.Equal(before, model.MightContainNonBasicAscii);

        model.ApplyEdits(edits);
        Assert.Equal(after, model.MightContainNonBasicAscii);
    }

    // Monaco: 'start with RTL, insert LTR'
    [Fact]
    public void Rtl_StartWithRtl_InsertLtr() =>
        TestApplyEditsRtl("Hello,\nזוהי עובדה מבוססת שדעתו", [EditOp(1, 1, 1, 1, "hello")], true, true);

    // Monaco: 'start with RTL, delete RTL'
    [Fact]
    public void Rtl_StartWithRtl_DeleteRtl() =>
        TestApplyEditsRtl("Hello,\nזוהי עובדה מבוססת שדעתו", [EditOp(1, 1, 10, 10, "")], true, true);

    // Monaco: 'start with RTL, insert RTL'
    [Fact]
    public void Rtl_StartWithRtl_InsertRtl() =>
        TestApplyEditsRtl("Hello,\nזוהי עובדה מבוססת שדעתו", [EditOp(1, 1, 1, 1, "هناك حقيقة مثبتة منذ زمن طويل")], true, true);

    // Monaco: 'start with LTR, insert LTR'
    [Fact]
    public void Rtl_StartWithLtr_InsertLtr() =>
        TestApplyEditsRtl("Hello,\nworld!", [EditOp(1, 1, 1, 1, "hello")], false, false);

    // Monaco: 'start with LTR, insert RTL 1'
    [Fact]
    public void Rtl_StartWithLtr_InsertRtl1() =>
        TestApplyEditsRtl("Hello,\nworld!", [EditOp(1, 1, 1, 1, "هناك حقيقة مثبتة منذ زمن طويل")], false, true);

    // Monaco: 'start with LTR, insert RTL 2'
    [Fact]
    public void Rtl_StartWithLtr_InsertRtl2() =>
        TestApplyEditsRtl("Hello,\nworld!", [EditOp(1, 1, 1, 1, "זוהי עובדה מבוססת שדעתו")], false, true);

    // Monaco: 'start with NON-ASCII, insert ASCII'
    [Fact]
    public void NonBasicAscii_StartWithNonAscii_InsertAscii() =>
        TestApplyEditsNonBasicAscii("Hello,\nZürich", [EditOp(1, 1, 1, 1, "hello", "second line")], true, true);

    // Monaco: 'start with NON-ASCII, delete NON-ASCII'
    [Fact]
    public void NonBasicAscii_StartWithNonAscii_DeleteNonAscii() =>
        TestApplyEditsNonBasicAscii("Hello,\nZürich", [EditOp(1, 1, 10, 10, "")], true, true);

    // Monaco: 'start with NON-ASCII, insert NON-ASCII'
    [Fact]
    public void NonBasicAscii_StartWithNonAscii_InsertNonAscii() =>
        TestApplyEditsNonBasicAscii("Hello,\nZürich", [EditOp(1, 1, 1, 1, "Zürich")], true, true);

    // Monaco: 'start with ASCII, insert ASCII'
    [Fact]
    public void NonBasicAscii_StartWithAscii_InsertAscii() =>
        TestApplyEditsNonBasicAscii("Hello,\nworld!", [EditOp(1, 1, 1, 1, "hello", "second line")], false, false);

    // Monaco: 'start with ASCII, insert NON-ASCII'
    [Fact]
    public void NonBasicAscii_StartWithAscii_InsertNonAscii() =>
        TestApplyEditsNonBasicAscii("Hello,\nworld!", [EditOp(1, 1, 1, 1, "Zürich", "Zürich")], false, true);
}
