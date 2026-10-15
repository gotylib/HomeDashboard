using Home.Api.Controllers;
using Home.Api.Data;
using Home.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Services;

public class UploadService(IWebHostEnvironment env, IConfiguration config, IServiceScopeFactory scopeFactory)
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

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (bytes.Length == 0)
            throw new InvalidOperationException("Empty file");

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? GuessContentType(ext)
            : file.ContentType;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = new MediaAsset
        {
            Data = bytes,
            ContentType = contentType,
            FileName = Path.GetFileName(file.FileName),
            CreatedAt = DateTime.UtcNow
        };
        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync(ct);

        return (MediaController.PublicPath(asset.Id), contentType);
    }

    public bool TryDelete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        if (MediaController.TryParseId(relativePath, out var mediaId))
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var asset = db.MediaAssets.Find(mediaId);
            if (asset is null)
                return false;
            db.MediaAssets.Remove(asset);
            db.SaveChanges();
            return true;
        }

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

    /// <summary>
    /// Moves leftover disk uploads into Postgres and rewrites service/folder ImagePath values.
    /// </summary>
    public async Task MigrateDiskUploadsToDatabaseAsync(CancellationToken ct = default)
    {
        var root = UploadsRoot;
        if (!Directory.Exists(root))
            return;

        var files = Directory.GetFiles(root);
        if (files.Length == 0)
            return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UploadService>>();

        var services = await db.Services
            .Where(s => s.ImagePath != null && s.ImagePath.Contains("/uploads/"))
            .ToListAsync(ct);
        var folders = await db.Folders
            .Where(f => f.ImagePath != null && f.ImagePath.Contains("/uploads/"))
            .ToListAsync(ct);

        var migrated = 0;
        foreach (var fullPath in files)
        {
            var fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            byte[] bytes;
            try
            {
                bytes = await File.ReadAllBytesAsync(fullPath, ct);
            }
            catch
            {
                continue;
            }

            if (bytes.Length == 0)
                continue;

            var ext = Path.GetExtension(fileName);
            var asset = new MediaAsset
            {
                Data = bytes,
                ContentType = GuessContentType(ext),
                FileName = fileName,
                CreatedAt = DateTime.UtcNow
            };
            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(ct);

            var oldPathSlash = $"/uploads/{fileName}";
            var oldPathPlain = $"uploads/{fileName}";
            var newPath = MediaController.PublicPath(asset.Id);

            foreach (var service in services.Where(s =>
                         string.Equals(s.ImagePath, oldPathSlash, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(s.ImagePath, oldPathPlain, StringComparison.OrdinalIgnoreCase)))
            {
                service.ImagePath = newPath;
            }

            foreach (var folder in folders.Where(f =>
                         string.Equals(f.ImagePath, oldPathSlash, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(f.ImagePath, oldPathPlain, StringComparison.OrdinalIgnoreCase)))
            {
                folder.ImagePath = newPath;
            }

            try
            {
                File.Delete(fullPath);
            }
            catch
            {
                /* keep file if delete fails; DB copy already exists */
            }

            migrated++;
        }

        await db.SaveChangesAsync(ct);
        if (migrated > 0)
            logger.LogInformation("Migrated {Count} disk upload(s) into MediaAssets", migrated);
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

    private static string GuessContentType(string ext) => ext.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        _ => "application/octet-stream"
    };
}
