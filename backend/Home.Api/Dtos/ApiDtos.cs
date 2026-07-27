namespace Home.Api.Dtos;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Username);
public record MeResponse(string Username);

public record DashboardDto(
    WallpaperDto Wallpaper,
    IReadOnlyList<ServiceDto> Services,
    IReadOnlyList<WidgetDto> Widgets);

public record WallpaperDto(string? Path, string Type);

public record ServiceDto(
    Guid Id,
    string Title,
    string Url,
    string? ImagePath,
    string? HealthUrl,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    int SortOrder,
    bool? IsUp,
    DateTime? CheckedAt);

public record CreateServiceRequest(
    string Title,
    string Url,
    string? ImagePath,
    string? HealthUrl,
    int GridX,
    int GridY,
    int GridW,
    int GridH);

public record UpdateServiceRequest(
    string Title,
    string Url,
    string? ImagePath,
    string? HealthUrl,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    int SortOrder);

public record WidgetDto(
    Guid Id,
    string Type,
    string ConfigJson,
    int GridX,
    int GridY,
    int GridW,
    int GridH);

public record CreateWidgetRequest(
    string Type,
    string? ConfigJson,
    int GridX,
    int GridY,
    int GridW,
    int GridH);

public record UpdateWidgetRequest(
    string Type,
    string ConfigJson,
    int GridX,
    int GridY,
    int GridW,
    int GridH);

public record LayoutItemDto(Guid Id, string Kind, int GridX, int GridY, int GridW, int GridH);
public record SaveLayoutRequest(IReadOnlyList<LayoutItemDto> Items);

public record SetWallpaperRequest(string? Path, string Type);

public record UploadResponse(string Path, string ContentType);
