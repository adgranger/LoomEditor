using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Ported from Monaco <c>textModel.test.ts</c>, suite <c>TextModel.createSnapshot</c>, plus a round trip through
/// <see cref="TextModel.FromSnapshot"/>.
/// </summary>
public class TextModelSnapshotTests
{
    private const string Bom = "﻿";

    // Monaco: 'empty file'
    [Fact]
    public void CreateSnapshot_EmptyFile_ReadsNull()
    {
        var model = new TextModel("");

        ITextSnapshot snapshot = model.CreateSnapshot();

        Assert.Null(snapshot.Read());
    }

    // Monaco: 'file with BOM'
    [Fact]
    public void CreateSnapshot_FileWithBom_PrefixesBomOnlyWhenPreserved()
    {
        var model = new TextModel(Bom + "Hello");
        Assert.Equal("Hello", model.GetLineContent(1));

        ITextSnapshot snapshot = model.CreateSnapshot(preserveBom: true);

        Assert.Equal(Bom + "Hello", snapshot.Read());
        Assert.Null(snapshot.Read());
        Assert.Equal("Hello", model.CreateSnapshot().Read());
    }

    // Monaco: 'regular file'
    [Fact]
    public void CreateSnapshot_RegularFile_ReadsWholeTextThenNull()
    {
        const string text = "My First Line\n\t\tMy Second Line\n    Third Line\n\n1";
        var model = new TextModel(text);

        ITextSnapshot snapshot = model.CreateSnapshot();

        Assert.Equal(text, snapshot.Read());
        Assert.Null(snapshot.Read());
    }

    // Monaco: 'large file'
    [Fact]
    public void CreateSnapshot_LargeFile_ReadsAtMostTwoChunks()
    {
        string text = string.Join('\n', Enumerable.Repeat("Just some text that is a bit long such that it can consume some memory", 1000));
        var model = new TextModel(text);

        ITextSnapshot snapshot = model.CreateSnapshot();
        string actual = snapshot.Read()!;
        Assert.NotNull(actual);

        // 70999 length => at most 2 read calls are necessary
        string? second = snapshot.Read();
        if (second is not null)
        {
            actual += second;
            Assert.Null(snapshot.Read());
        }

        Assert.Equal(text, actual);
    }

    // Monaco: 'issue #119632: invalid range'
    // Monaco passes undefined line numbers; Range cannot hold them, so 0 (below the first line) stands for them.
    [Fact]
    public void ValidateRange_CoordinatesBelowTheText_ReturnsFirstPosition()
    {
        var model = new TextModel("hello world!");

        Range actual = model.ValidateRange(0, 0, 0, 1);

        Assert.Equal(new Range(1, 1, 1, 1), actual);
    }

    [Fact]
    public void CreateSnapshot_AfterEdit_DoesNotSeeLaterEdits()
    {
        var model = new TextModel("abc");
        ITextSnapshot snapshot = model.CreateSnapshot();

        model.SetValue("changed");

        Assert.Equal("abc", snapshot.Read());
        Assert.Null(snapshot.Read());
    }

    [Fact]
    public void FromSnapshot_SnapshotOfModel_HasSameValue()
    {
        var model = new TextModel("one\r\ntwo\r\n\r\nthree");

        TextModel copy = TextModel.FromSnapshot(model.CreateSnapshot());

        Assert.Equal(model.GetValue(), copy.GetValue());
        Assert.Equal(model.Eol, copy.Eol);
        Assert.Equal(model.LineCount, copy.LineCount);
    }

    [Fact]
    public void FromSnapshot_SnapshotWithBom_KeepsBomOutOfTheText()
    {
        var model = new TextModel(Bom + "Hello\nWorld");

        TextModel copy = TextModel.FromSnapshot(model.CreateSnapshot(preserveBom: true));

        Assert.Equal("Hello\nWorld", copy.GetValue());
        Assert.Equal(Bom + "Hello\nWorld", copy.GetValue(preserveBom: true));
    }

    [Fact]
    public void FromSnapshot_NullSnapshot_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TextModel.FromSnapshot(null!));
    }
}
