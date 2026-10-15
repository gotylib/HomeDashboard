namespace Home.Api.Models;

public class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
