namespace LoomEditor.TextMate.Tests.Grammar;

// Ported from microsoft/vscode-textmate: src/tests/tokenization.test.ts, run on test-cases/suite1/whileTests.json.
public class WhileTests
{
    public static IEnumerable<TheoryDataRow<string, int>> Cases() => TokenizationSuiteRunner.Rows("suite1/whileTests.json");

    // vscode-textmate: test(tst.desc) — one row per case, named by its desc.
    [Theory]
    [MemberData(nameof(Cases))]
    public void Tokenizes(string testsFile, int index) => TokenizationSuiteRunner.Run(testsFile, index);
}
