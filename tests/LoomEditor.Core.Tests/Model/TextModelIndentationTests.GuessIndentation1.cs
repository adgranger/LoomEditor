namespace LoomEditor.Core.Tests.Model;

// Monaco: 'guess indentation 1' (one long test, kept apart to keep files short).
public partial class TextModelIndentationTests
{
    // Monaco: 'guess indentation 1'
    [Fact]
    public void GuessIndentation1()
    {
        AssertGuess(null, null,
        [
            "x",
            "x",
            "x",
            "x",
            "x",
            "x",
            "x",
        ], "no clues");

        AssertGuess(false, null,
        [
            "\tx",
            "x",
            "x",
            "x",
            "x",
            "x",
            "x",
        ], "no spaces, 1xTAB");

        AssertGuess(true, 2,
        [
            "  x",
            "x",
            "x",
            "x",
            "x",
            "x",
            "x",
        ], "1x2");

        AssertGuess(false, null,
        [
            "\tx",
            "\tx",
            "\tx",
            "\tx",
            "\tx",
            "\tx",
            "\tx",
        ], "7xTAB");

        AssertGuess(null, 2,
        [
            "\tx",
            "  x",
            "\tx",
            "  x",
            "\tx",
            "  x",
            "\tx",
            "  x",
        ], "4x2, 4xTAB", tabSizeOnlyWithSpaces: true);
        AssertGuess(false, null,
        [
            "\tx",
            " x",
            "\tx",
            " x",
            "\tx",
            " x",
            "\tx",
            " x",
        ], "4x1, 4xTAB");
        AssertGuess(false, null,
        [
            "\tx",
            "\tx",
            "  x",
            "\tx",
            "  x",
            "\tx",
            "  x",
            "\tx",
            "  x",
        ], "4x2, 5xTAB");
        AssertGuess(false, null,
        [
            "\tx",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "  x",
        ], "1x2, 5xTAB");
        AssertGuess(false, null,
        [
            "\tx",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "    x",
        ], "1x4, 5xTAB");
        AssertGuess(false, null,
        [
            "\tx",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "  x",
            "\tx",
            "    x",
        ], "1x2, 1x4, 5xTAB");

        AssertGuess(null, null,
        [
            "x",
            " x",
            " x",
            " x",
            " x",
            " x",
            " x",
            " x",
        ], "7x1 - 1 space is never guessed as an indentation");
        AssertGuess(true, null,
        [
            "x",
            "          x",
            " x",
            " x",
            " x",
            " x",
            " x",
            " x",
        ], "1x10, 6x1");
        AssertGuess(null, null,
        [
            "",
            "  ",
            "    ",
            "      ",
            "        ",
            "          ",
            "            ",
            "              ",
        ], "whitespace lines don't count");
        AssertGuess(true, 3,
        [
            "x",
            "   x",
            "   x",
            "    x",
            "x",
            "   x",
            "   x",
            "    x",
            "x",
            "   x",
            "   x",
            "    x",
        ], "6x3, 3x4");
        AssertGuess(true, 5,
        [
            "x",
            "     x",
            "     x",
            "    x",
            "x",
            "     x",
            "     x",
            "    x",
            "x",
            "     x",
            "     x",
            "    x",
        ], "6x5, 3x4");
        AssertGuess(true, 7,
        [
            "x",
            "       x",
            "       x",
            "     x",
            "x",
            "       x",
            "       x",
            "    x",
            "x",
            "       x",
            "       x",
            "    x",
        ], "6x7, 1x5, 2x4");
        AssertGuess(true, 2,
        [
            "x",
            "  x",
            "  x",
            "  x",
            "  x",
            "x",
            "  x",
            "  x",
            "  x",
            "  x",
        ], "8x2");

        AssertGuess(true, 2,
        [
            "x",
            "  x",
            "  x",
            "x",
            "  x",
            "  x",
            "x",
            "  x",
            "  x",
            "x",
            "  x",
            "  x",
        ], "8x2");
        AssertGuess(true, 2,
        [
            "x",
            "  x",
            "    x",
            "x",
            "  x",
            "    x",
            "x",
            "  x",
            "    x",
            "x",
            "  x",
            "    x",
        ], "4x2, 4x4");
        AssertGuess(true, 2,
        [
            "x",
            "  x",
            "  x",
            "    x",
            "x",
            "  x",
            "  x",
            "    x",
            "x",
            "  x",
            "  x",
            "    x",
        ], "6x2, 3x4");
        AssertGuess(true, 2,
        [
            "x",
            "  x",
            "  x",
            "    x",
            "    x",
            "x",
            "  x",
            "  x",
            "    x",
            "    x",
        ], "4x2, 4x4");
        AssertGuess(true, 2,
        [
            "x",
            "  x",
            "    x",
            "    x",
            "x",
            "  x",
            "    x",
            "    x",
        ], "2x2, 4x4");
        AssertGuess(true, 4,
        [
            "x",
            "    x",
            "    x",
            "x",
            "    x",
            "    x",
            "x",
            "    x",
            "    x",
            "x",
            "    x",
            "    x",
        ], "8x4");
        AssertGuess(true, 2,
        [
            "x",
            "  x",
            "    x",
            "    x",
            "      x",
            "x",
            "  x",
            "    x",
            "    x",
            "      x",
        ], "2x2, 4x4, 2x6");
        AssertGuess(true, 2,
        [
            "x",
            "  x",
            "    x",
            "    x",
            "      x",
            "      x",
            "        x",
        ], "1x2, 2x4, 2x6, 1x8");
        AssertGuess(true, 4,
        [
            "x",
            "    x",
            "    x",
            "    x",
            "     x",
            "        x",
            "x",
            "    x",
            "    x",
            "    x",
            "     x",
            "        x",
        ], "6x4, 2x5, 2x8");
        AssertGuess(true, 4,
        [
            "x",
            "    x",
            "    x",
            "    x",
            "     x",
            "        x",
            "        x",
        ], "3x4, 1x5, 2x8");
        AssertGuess(true, 4,
        [
            "x",
            "x",
            "    x",
            "    x",
            "     x",
            "        x",
            "        x",
            "x",
            "x",
            "    x",
            "    x",
            "     x",
            "        x",
            "        x",
        ], "6x4, 2x5, 4x8");
        AssertGuess(true, 3,
        [
            "x",
            " x",
            " x",
            " x",
            " x",
            " x",
            "x",
            "   x",
            "    x",
            "    x",
        ], "5x1, 2x0, 1x3, 2x4");
        AssertGuess(false, null,
        [
            "\t x",
            " \t x",
            "\tx",
        ], "mixed whitespace 1");
        AssertGuess(false, null,
        [
            "\tx",
            "\t    x",
        ], "mixed whitespace 2");
    }
}
