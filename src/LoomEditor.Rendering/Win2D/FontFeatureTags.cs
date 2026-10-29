namespace LoomEditor.Rendering.Win2D;

/// <summary>Packs OpenType feature tags the way DirectWrite does (<c>DWRITE_MAKE_FONT_FEATURE_TAG</c>).</summary>
/// <remarks>Threading: stateless, thread-safe.</remarks>
internal static class FontFeatureTags
{
    /// <summary>Packs a four-character ASCII tag, first character in the lowest byte.</summary>
    /// <exception cref="ArgumentException">The tag is not exactly four ASCII characters.</exception>
    public static uint Pack(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (tag.Length != 4)
        {
            throw new ArgumentException("A font feature tag has exactly 4 characters.", nameof(tag));
        }

        uint packed = 0;
        for (int i = 0; i < 4; i++)
        {
            if (tag[i] > 0x7F)
            {
                throw new ArgumentException("A font feature tag is ASCII.", nameof(tag));
            }

            packed |= (uint)tag[i] << (8 * i);
        }

        return packed;
    }
}
