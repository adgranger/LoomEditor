// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;
using static LoomEditor.View.Tests.Layout.TestHelpers.LineBreaksTestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class MonospaceLineBreaksComputerIncrementalTests
{
    private static MonospaceLineBreaksComputerFactory DefaultFactory() =>
        new(EditorWrappingOptions.DefaultBreakBeforeCharacters, EditorWrappingOptions.DefaultBreakAfterCharacters);

    // Monaco: 'MonospaceLineBreaksComputer incremental 1'
    [Fact]
    public void Incremental1()
    {
        MonospaceLineBreaksComputerFactory factory = DefaultFactory();


        AssertIncrementalLineBreaks(
            factory, "just some text and more", 4,
            10, "just some |text and |more",
            15, "just some text |and more"
        );

        AssertIncrementalLineBreaks(
            factory, "Cu scripserit suscipiantur eos, in affert pericula contentiones sed, cetero sanctus et pro. Ius vidit magna regione te, sit ei elaboraret liberavisse. Mundi verear eu mea, eam vero scriptorem in, vix in menandri assueverit. Natum definiebas cu vim. Vim doming vocibus efficiantur id. In indoctum deseruisse voluptatum vim, ad debitis verterem sed.", 4,
            47, "Cu scripserit suscipiantur eos, in affert |pericula contentiones sed, cetero sanctus et |pro. Ius vidit magna regione te, sit ei |elaboraret liberavisse. Mundi verear eu mea, |eam vero scriptorem in, vix in menandri |assueverit. Natum definiebas cu vim. Vim |doming vocibus efficiantur id. In indoctum |deseruisse voluptatum vim, ad debitis verterem |sed.",
            142, "Cu scripserit suscipiantur eos, in affert pericula contentiones sed, cetero sanctus et pro. Ius vidit magna regione te, sit ei elaboraret |liberavisse. Mundi verear eu mea, eam vero scriptorem in, vix in menandri assueverit. Natum definiebas cu vim. Vim doming vocibus efficiantur |id. In indoctum deseruisse voluptatum vim, ad debitis verterem sed."
        );

        AssertIncrementalLineBreaks(
            factory, "An his legere persecuti, oblique delicata efficiantur ex vix, vel at graecis officiis maluisset. Et per impedit voluptua, usu discere maiorum at. Ut assum ornatus temporibus vis, an sea melius pericula. Ea dicunt oblique phaedrum nam, eu duo movet nobis. His melius facilis eu, vim malorum temporibus ne. Nec no sale regione, meliore civibus placerat id eam. Mea alii fabulas definitionem te, agam volutpat ad vis, et per bonorum nonumes repudiandae.", 4,
            57, "An his legere persecuti, oblique delicata efficiantur ex |vix, vel at graecis officiis maluisset. Et per impedit |voluptua, usu discere maiorum at. Ut assum ornatus |temporibus vis, an sea melius pericula. Ea dicunt |oblique phaedrum nam, eu duo movet nobis. His melius |facilis eu, vim malorum temporibus ne. Nec no sale |regione, meliore civibus placerat id eam. Mea alii |fabulas definitionem te, agam volutpat ad vis, et per |bonorum nonumes repudiandae.",
            58, "An his legere persecuti, oblique delicata efficiantur ex |vix, vel at graecis officiis maluisset. Et per impedit |voluptua, usu discere maiorum at. Ut assum ornatus |temporibus vis, an sea melius pericula. Ea dicunt oblique |phaedrum nam, eu duo movet nobis. His melius facilis eu, |vim malorum temporibus ne. Nec no sale regione, meliore |civibus placerat id eam. Mea alii fabulas definitionem |te, agam volutpat ad vis, et per bonorum nonumes |repudiandae."
        );

        AssertIncrementalLineBreaks(
            factory, "\t\t\"owner\": \"vscode\",", 4,
            14, "\t\t\"owner|\": |\"vscod|e\",",
            16, "\t\t\"owner\":| |\"vscode\"|,",
            WrappingIndent.Same
        );

        AssertIncrementalLineBreaks(
            factory, "🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇&👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬", 4,
            51, "🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇&|👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬",
            50, "🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇|&|👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬🌖🌞🏇🍼🐇👬",
            WrappingIndent.Same
        );

        AssertIncrementalLineBreaks(
            factory, "🐇👬&🌞🌖", 4,
            5, "🐇👬&|🌞🌖",
            4, "🐇👬|&|🌞🌖",
            WrappingIndent.Same
        );

        AssertIncrementalLineBreaks(
            factory, "\t\tfunc('🌞🏇🍼🌞🏇🍼🐇&👬🌖🌞👬🌖🌞🏇🍼🐇👬', WrappingIndent.Same);", 4,
            26, "\t\tfunc|('🌞🏇🍼🌞🏇🍼🐇&|👬🌖🌞👬🌖🌞🏇🍼🐇|👬', |WrappingIndent.|Same);",
            27, "\t\tfunc|('🌞🏇🍼🌞🏇🍼🐇&|👬🌖🌞👬🌖🌞🏇🍼🐇|👬', |WrappingIndent.|Same);",
            WrappingIndent.Same
        );

        AssertIncrementalLineBreaks(
            factory, "factory, \"xtxtfunc(x\"🌞🏇🍼🌞🏇🍼🐇&👬🌖🌞👬🌖🌞🏇🍼🐇👬x\"", 4,
            16, "factory, |\"xtxtfunc|(x\"🌞🏇🍼🌞🏇🍼|🐇&|👬🌖🌞👬🌖🌞🏇🍼|🐇👬x\"",
            17, "factory, |\"xtxtfunc|(x\"🌞🏇🍼🌞🏇🍼🐇|&👬🌖🌞👬🌖🌞🏇🍼|🐇👬x\"",
            WrappingIndent.Same
        );
    }

    // Monaco: 'issue #95686: CRITICAL: loop forever on the monospaceLineBreaksComputer'
    [Fact(Timeout = 10000)]
    public async Task Issue95686_LoopForever()
    {
        await Task.Delay(0, TestContext.Current.CancellationToken);
        MonospaceLineBreaksComputerFactory factory = DefaultFactory();
        AssertIncrementalLineBreaks(
            factory,
            "						<tr dmx-class:table-danger=\"(alt <= 50)\" dmx-class:table-warning=\"(alt <= 200)\" dmx-class:table-primary=\"(alt <= 400)\" dmx-class:table-info=\"(alt <= 800)\" dmx-class:table-success=\"(alt >= 400)\">",
            4,
            179, "						<tr dmx-class:table-danger=\"(alt <= 50)\" dmx-class:table-warning=\"(alt <= 200)\" dmx-class:table-primary=\"(alt <= 400)\" dmx-class:table-info=\"(alt <= 800)\" |dmx-class:table-success=\"(alt >= 400)\">",
            1, "	|	|	|	|	|	|<|t|r| |d|m|x|-|c|l|a|s|s|:|t|a|b|l|e|-|d|a|n|g|e|r|=|\"|(|a|l|t| |<|=| |5|0|)|\"| |d|m|x|-|c|l|a|s|s|:|t|a|b|l|e|-|w|a|r|n|i|n|g|=|\"|(|a|l|t| |<|=| |2|0|0|)|\"| |d|m|x|-|c|l|a|s|s|:|t|a|b|l|e|-|p|r|i|m|a|r|y|=|\"|(|a|l|t| |<|=| |4|0|0|)|\"| |d|m|x|-|c|l|a|s|s|:|t|a|b|l|e|-|i|n|f|o|=|\"|(|a|l|t| |<|=| |8|0|0|)|\"| |d|m|x|-|c|l|a|s|s|:|t|a|b|l|e|-|s|u|c|c|e|s|s|=|\"|(|a|l|t| |>|=| |4|0|0|)|\"|>",
            WrappingIndent.Same
        );
    }

    // Monaco: 'issue #110392: Occasional crash when resize with panel on the right'
    [Fact]
    public void Issue110392_CrashWhenResizeWithPanelOnTheRight()
    {
        MonospaceLineBreaksComputerFactory factory = DefaultFactory();
        AssertIncrementalLineBreaks(
            factory,
            "你好 **hello** **hello** **hello-world** hey there!",
            4,
            15, "你好 **hello** |**hello** |**hello-world**| hey there!",
            1, "你|好| |*|*|h|e|l|l|o|*|*| |*|*|h|e|l|l|o|*|*| |*|*|h|e|l|l|o|-|w|o|r|l|d|*|*| |h|e|y| |t|h|e|r|e|!",
            WrappingIndent.Same,
            1.6605405405405405
        );
    }

    [Fact]
    public void Finalize_WithPreviousDataAndInjectedText_UsesTheFullComputation()
    {
        MonospaceLineBreaksComputerFactory factory = DefaultFactory();
        const string text = "abcdefghij";
        var injected = new[] { Injected(3, "XY") };
        ModelLineProjectionData? previous = GetLineBreakData(factory, 4, 5, 2, WrappingIndent.None, WordBreak.Normal, false, text, null);

        ModelLineProjectionData? fromPrevious = GetLineBreakData(factory, 4, 6, 2, WrappingIndent.None, WordBreak.Normal, false, text, previous, injected);
        ModelLineProjectionData? direct = GetLineBreakData(factory, 4, 6, 2, WrappingIndent.None, WordBreak.Normal, false, text, null, injected);

        Assert.NotNull(direct!.InjectionOptions);
        AssertLineBreakDataEqual(fromPrevious, direct);
        Assert.Equal(direct.InjectionOffsets, fromPrevious!.InjectionOffsets);
    }
}
