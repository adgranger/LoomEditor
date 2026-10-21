using LoomEditor.Core.Languages;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Tests.Features.Indentation;

// Monaco: suites 'Auto Indent On Type - Ruby' (the test.skip test is not ported) and 'Auto Indent On Type - PHP'
public partial class AutoIndentOnTypeTests
{
    // Monaco: 'issue #198350: in or when incorrectly match non keywords for Ruby'
    [Fact]
    public void Issue198350_InOrWhenIncorrectlyMatchNonKeywordsForRuby() =>
        Run(TestLanguages.Ruby, string.Empty, EditorAutoIndentStrategy.Full, (_, model, viewModel) =>
        {
            viewModel.Type("def foo\n        i");
            viewModel.Type("n", "keyboard");
            Assert.Equal("def foo\n        in", model.GetValue());
            viewModel.Type(" ", "keyboard");
            Assert.Equal("def foo\nin ", model.GetValue());

            model.SetValue(string.Empty);
            viewModel.Type("  # in");
            Assert.Equal("  # in", model.GetValue());
            viewModel.Type(" ", "keyboard");
            Assert.Equal("  # in ", model.GetValue());
        });

    // Monaco: 'issue #199050: should not indent after { detected in a string'
    [Fact]
    public void Issue199050_ShouldNotIndentAfterBraceDetectedInAString() =>
        Run(
            TestLanguages.PHP,
            "preg_replace('{');",
            EditorAutoIndentStrategy.Full,
            (_, model, viewModel) =>
            {
                SetSelection(viewModel, new Selection(1, 54, 1, 54));
                viewModel.Type("\n", "keyboard");
                Assert.Equal(Join("preg_replace('{');", string.Empty), model.GetValue());
            },
            tokens:
            [
                [
                    new StandardTokenTypeData(0, StandardTokenType.Other),
                    new StandardTokenTypeData(13, StandardTokenType.String),
                    new StandardTokenTypeData(16, StandardTokenType.Other),
                ],
            ]);
}
