// Ported from microsoft/vscode: src/vs/base/common/strings.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

internal static partial class Strings
{
    private const int Zwj = 0x200D;

    /// <summary>Variation Selector-16 (VS16).</summary>
    private const int EmojiVariantSelector = 0xFE0F;

    /// <summary>Combining Enclosing Keycap.</summary>
    private const int EnclosingKeyCap = 0x20E3;

    /// <summary>Treats wide characters as 2 columns (Monaco: <c>isFullWidthCharacter</c>).</summary>
    public static bool IsFullWidthCharacter(int charCode)
        => (charCode >= 0x2E80 && charCode <= 0xD7AF)
            || (charCode >= 0xF900 && charCode <= 0xFAFF)
            || (charCode >= 0xFF01 && charCode <= 0xFF5E)
            || (charCode >= 0xFFE0 && charCode <= 0xFFE6);

    /// <summary>A fast, imprecise emoji check (Monaco: <c>isEmojiImprecise</c>).</summary>
    public static bool IsEmojiImprecise(int x)
        => (x >= 0x1F1E6 && x <= 0x1F1FF) || x == 8986 || x == 8987 || x == 9200
            || x == 9203 || (x >= 9728 && x <= 10175) || x == 11088 || x == 11093
            || (x >= 127744 && x <= 128591) || (x >= 128640 && x <= 128764)
            || (x >= 128992 && x <= 129008) || (x >= 129280 && x <= 129535)
            || (x >= 129648 && x <= 129782);

    /// <summary>The offset after a left delete (Backspace) at <paramref name="offset"/>, honoring emoji rules (Monaco: <c>getLeftDeleteOffset</c>).</summary>
    public static int GetLeftDeleteOffset(int offset, string str)
    {
        if (offset == 0)
        {
            return 0;
        }

        var emojiOffset = GetOffsetBeforeLastEmojiComponent(offset, str);
        if (emojiOffset is { } value)
        {
            return value;
        }

        var iterator = new CodePointIterator(str, offset);
        iterator.PrevCodePoint();
        return iterator.Offset;
    }

    /// <summary>Monaco: <c>getOffsetBeforeLastEmojiComponent</c> (<c>number | undefined</c>).</summary>
    internal static int? GetOffsetBeforeLastEmojiComponent(int initialOffset, string str)
    {
        // See https://www.unicode.org/reports/tr51/tr51-14.html#EBNF_and_Regex for the structure of emojis.
        var iterator = new CodePointIterator(str, initialOffset);
        var codePoint = iterator.PrevCodePoint();

        while (IsEmojiModifier(codePoint) || codePoint == EmojiVariantSelector || codePoint == EnclosingKeyCap)
        {
            if (iterator.Offset == 0)
            {
                return null;
            }

            codePoint = iterator.PrevCodePoint();
        }

        if (!IsEmojiImprecise(codePoint))
        {
            return null;
        }

        var resultOffset = iterator.Offset;
        if (resultOffset > 0)
        {
            var optionalZwjCodePoint = iterator.PrevCodePoint();
            if (optionalZwjCodePoint == Zwj)
            {
                resultOffset = iterator.Offset;
            }
        }

        return resultOffset;
    }

    /// <summary>True for the skin tone modifiers U+1F3FB..U+1F3FF (Monaco: <c>isEmojiModifier</c>).</summary>
    internal static bool IsEmojiModifier(int codePoint) => 0x1F3FB <= codePoint && codePoint <= 0x1F3FF;
}
