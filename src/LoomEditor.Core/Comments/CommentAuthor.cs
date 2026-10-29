namespace LoomEditor.Core.Comments;

/// <summary>Who wrote a comment.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
public sealed record CommentAuthor
{
    /// <summary>Creates an author.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or blank.</exception>
    public CommentAuthor(string name, string? avatarUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        AvatarUrl = avatarUrl;
    }

    /// <summary>The display name; never blank.</summary>
    public string Name { get; init; }

    /// <summary>The URL of the avatar image, or <see langword="null"/> to show initials.</summary>
    public string? AvatarUrl { get; init; }
}
