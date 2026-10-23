using LoomEditor.View.Accessibility;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationTextRangeTests
{
    private const string Text = "abc\ndef\nghi";

    [Fact]
    public void ForDocument_IsTheDocumentRange() =>
        AutomationTestHelpers.WithDocument(Text, d => Assert.Equal(d.DocumentRange, AutomationTextRange.ForDocument(d).Range));

    [Fact]
    public void Clone_IsEqualButIndependent() =>
        AutomationTestHelpers.WithDocument(Text, d =>
        {
            var original = new AutomationTextRange(d, new Range(1, 1, 1, 3));
            AutomationTextRange clone = original.Clone();
            Assert.True(original.Compare(clone));
            clone.Set(new Range(2, 1, 2, 2));
            Assert.Equal(new Range(1, 1, 1, 3), original.Range);
        });

    [Fact]
    public void Compare_SameEndpoints_ReturnsTrue() =>
        AutomationTestHelpers.WithDocument(Text, d =>
            Assert.True(new AutomationTextRange(d, new Range(1, 1, 2, 2)).Compare(new AutomationTextRange(d, new Range(1, 1, 2, 2)))));

    [Fact]
    public void Compare_DifferentEndpoints_ReturnsFalse() =>
        AutomationTestHelpers.WithDocument(Text, d =>
            Assert.False(new AutomationTextRange(d, new Range(1, 1, 2, 2)).Compare(new AutomationTextRange(d, new Range(1, 1, 2, 3)))));

    [Fact]
    public void Compare_DifferentDocuments_ReturnsFalse() =>
        AutomationTestHelpers.WithDocument(Text, d1 => AutomationTestHelpers.WithDocument(Text, d2 =>
            Assert.False(AutomationTextRange.ForDocument(d1).Compare(AutomationTextRange.ForDocument(d2)))));

    [Fact]
    public void CompareEndpoints_StartAgainstStart_Signs() =>
        AutomationTestHelpers.WithDocument(Text, d =>
        {
            var r = new AutomationTextRange(d, new Range(1, 2, 1, 3));
            var before = new AutomationTextRange(d, new Range(1, 1, 1, 3));
            var after = new AutomationTextRange(d, new Range(2, 1, 2, 2));
            Assert.Equal(1, r.CompareEndpoints(AutomationTextEndpoint.Start, before, AutomationTextEndpoint.Start));
            Assert.Equal(0, r.CompareEndpoints(AutomationTextEndpoint.Start, r.Clone(), AutomationTextEndpoint.Start));
            Assert.Equal(-1, r.CompareEndpoints(AutomationTextEndpoint.Start, after, AutomationTextEndpoint.Start));
        });

    [Fact]
    public void CompareEndpoints_EndAgainstStart_Signs() =>
        AutomationTestHelpers.WithDocument(Text, d =>
        {
            var r = new AutomationTextRange(d, new Range(1, 1, 1, 3));
            Assert.Equal(1, r.CompareEndpoints(AutomationTextEndpoint.End, new AutomationTextRange(d, new Range(1, 2, 1, 3)), AutomationTextEndpoint.Start));
            Assert.Equal(0, r.CompareEndpoints(AutomationTextEndpoint.End, new AutomationTextRange(d, new Range(1, 3, 1, 4)), AutomationTextEndpoint.Start));
            Assert.Equal(-1, r.CompareEndpoints(AutomationTextEndpoint.End, new AutomationTextRange(d, new Range(2, 1, 2, 2)), AutomationTextEndpoint.Start));
        });

    [Fact]
    public void CompareEndpoints_OtherDocument_Throws() =>
        AutomationTestHelpers.WithDocument(Text, d1 => AutomationTestHelpers.WithDocument(Text, d2 =>
            Assert.Throws<ArgumentException>(() => AutomationTextRange.ForDocument(d1)
                .CompareEndpoints(AutomationTextEndpoint.Start, AutomationTextRange.ForDocument(d2), AutomationTextEndpoint.Start))));

    [Fact]
    public void Range_AfterTheModelShrinks_IsClamped() =>
        TestCodeEditor.WithTestCodeEditor("1\n2\n3\n4\n5 long", editor =>
        {
            var d = new AutomationTextDocument(editor.ViewModel);
            var r = new AutomationTextRange(d, new Range(5, 2, 5, 6));
            editor.Model.SetValue("x");
            Assert.Equal(new Range(1, 2, 1, 2), r.Range);
        });

    [Fact]
    public void IsDegenerate_StartEqualsEnd() =>
        AutomationTestHelpers.WithDocument(Text, d =>
        {
            Assert.True(new AutomationTextRange(d, new Range(1, 2, 1, 2)).IsDegenerate);
            Assert.False(AutomationTextRange.ForDocument(d).IsDegenerate);
        });

    [Fact]
    public void GetText_ReadsTheRange() =>
        AutomationTestHelpers.WithDocument(Text, d => Assert.Equal("bc\nd", new AutomationTextRange(d, new Range(1, 2, 2, 2)).GetText()));
}
