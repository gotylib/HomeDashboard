namespace Home.Api.Models;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public string? WallpaperPath { get; set; }
    public string WallpaperType { get; set; } = "none"; // none | image | gif | video
    public byte[]? WallpaperData { get; set; }
    public string? WallpaperContentType { get; set; }
    public DateTime? WallpaperUpdatedAt { get; set; }
}
