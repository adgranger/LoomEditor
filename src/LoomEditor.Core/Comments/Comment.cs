namespace LoomEditor.Core.Comments;

/// <summary>One comment of a thread.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
public sealed record Comment
{
    /// <summary>Creates a comment.</summary>
    /// <exception cref="ArgumentException"><paramref name="id"/> is null or blank.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="author"/> or <paramref name="body"/> is null.</exception>
    public Comment(string id, CommentAuthor author, string body, DateTimeOffset createdAt, DateTimeOffset? updatedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(author);
        ArgumentNullException.ThrowIfNull(body);
        Id = id;
        Author = author;
        Body = body;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>The identifier, unique inside a thread.</summary>
    public string Id { get; init; }

    /// <summary>The author.</summary>
    public CommentAuthor Author { get; init; }

    /// <summary>The Markdown body; may be empty.</summary>
    public string Body { get; init; }

    /// <summary>When the comment was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the comment was last edited, or <see langword="null"/>.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Creates a comment with a new random <see cref="Id"/>; <paramref name="createdAt"/> defaults to now (UTC).</summary>
    public static Comment Create(CommentAuthor author, string body, DateTimeOffset? createdAt = null) =>
        new(Guid.NewGuid().ToString("N"), author, body, createdAt ?? DateTimeOffset.UtcNow);
}
