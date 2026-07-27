namespace Home.Api.Services;

public class UploadService(IWebHostEnvironment env, IConfiguration config)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp4", ".webm", ".svg"
    };

    public string UploadsRoot
    {
        get
        {
            var configured = config["Uploads:Path"];
            var root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(env.ContentRootPath, "uploads")
                : configured;
            Directory.CreateDirectory(root);
            return root;
        }
    }

    public async Task<(string RelativePath, string ContentType)> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("Empty file");

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed");

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(UploadsRoot, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        return ($"/uploads/{fileName}", file.ContentType);
    }

    public bool TryDelete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var normalized = relativePath.Replace('\\', '/').Trim();
        if (!normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return false;

        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
            return false;

        var fullPath = Path.GetFullPath(Path.Combine(UploadsRoot, fileName));
        var root = Path.GetFullPath(UploadsRoot);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!File.Exists(fullPath))
            return false;

        File.Delete(fullPath);
        return true;
    }

    public static string DetectWallpaperType(string? path, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "none";

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".mp4" or ".webm")
            return "video";
        if (ext is ".gif")
            return "gif";
        if (contentType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true)
            return "video";
        return "image";
    }
}
