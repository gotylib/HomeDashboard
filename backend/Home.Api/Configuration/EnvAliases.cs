namespace Home.Api.Configuration;

/// <summary>
/// Maps flat Dokploy-friendly env names onto ASP.NET hierarchical keys (__).
/// Existing hierarchical vars always win.
/// </summary>
public static class EnvAliases
{
    public static void Apply()
    {
        SetIfMissing("Admin__Username", "ADMIN_USERNAME");
        SetIfMissing("Admin__Password", "ADMIN_PASSWORD");
        SetIfMissing("Jwt__Key", "JWT_KEY");
        SetIfMissing("Jwt__Issuer", "JWT_ISSUER");
        SetIfMissing("Jwt__Audience", "JWT_AUDIENCE");
        SetIfMissing("Jwt__ExpireHours", "JWT_EXPIRE_HOURS");
        SetIfMissing("Uploads__Path", "UPLOADS_PATH");
        SetIfMissing("ConnectionStrings__Default", "CONNECTION_STRING", "DATABASE_URL");

        if (Environment.GetEnvironmentVariable("ConnectionStrings__Default") is null)
        {
            var host = Get("DATABASE_HOST", "POSTGRES_HOST") ?? "localhost";
            var port = Get("DATABASE_PORT", "POSTGRES_PORT") ?? "5432";
            var db = Get("DATABASE_NAME", "POSTGRES_DB") ?? "home_dashboard";
            var user = Get("DATABASE_USER", "POSTGRES_USER") ?? "home";
            var password = Get("DATABASE_PASSWORD", "POSTGRES_PASSWORD") ?? "home";

            // Build only when at least one DB_* / POSTGRES_* var is present (Dokploy style).
            if (Get("DATABASE_HOST", "POSTGRES_HOST", "DATABASE_USER", "POSTGRES_USER",
                    "DATABASE_PASSWORD", "POSTGRES_PASSWORD", "DATABASE_NAME", "POSTGRES_DB") is not null)
            {
                Environment.SetEnvironmentVariable(
                    "ConnectionStrings__Default",
                    $"Host={host};Port={port};Database={db};Username={user};Password={password}");
            }
        }
    }

    private static void SetIfMissing(string target, params string[] sources)
    {
        if (Environment.GetEnvironmentVariable(target) is not null)
            return;

        foreach (var source in sources)
        {
            var value = Environment.GetEnvironmentVariable(source);
            if (!string.IsNullOrWhiteSpace(value))
            {
                Environment.SetEnvironmentVariable(target, value);
                return;
            }
        }
    }

    private static string? Get(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
