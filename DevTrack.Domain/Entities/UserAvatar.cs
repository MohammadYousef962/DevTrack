namespace DevTrack.Domain.Entities;

// A user's profile picture, kept in its own table so it isn't loaded along with every user query.
public class UserAvatar
{
    public int UserId { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}