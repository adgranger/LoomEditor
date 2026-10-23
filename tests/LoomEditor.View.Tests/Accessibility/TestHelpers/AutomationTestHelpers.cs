using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Accessibility.TestHelpers;

internal static class AutomationTestHelpers
{
    public static void WithDocument(string text, Action<AutomationTextDocument> test, TestCodeEditorOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(text, editor => test(new AutomationTextDocument(editor.ViewModel)), options);

    public static Position Pos(int line, int column) => new(line, column);

    public static Range Rng(int sl, int sc, int el, int ec) => new(sl, sc, el, ec);
}
