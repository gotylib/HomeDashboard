using Home.Api.Data;
using Home.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace Home.Api.Services;

public class HealthCheckWorker(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<HealthCheckWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly bool RunningInContainer =
        string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase)
        || File.Exists("/.dockerenv");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Health check cycle failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckAllAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var services = await db.Services.AsNoTracking().ToListAsync(ct);
        var client = httpClientFactory.CreateClient("health");

        foreach (var service in services)
        {
            var url = string.IsNullOrWhiteSpace(service.HealthUrl) ? service.Url : service.HealthUrl!;
            var isUp = await ProbeAsync(client, url, ct);

            var status = await db.HealthStatuses.FindAsync([service.Id], ct);
            if (status is null)
            {
                db.HealthStatuses.Add(new HealthStatus
                {
                    ServiceId = service.Id,
                    IsUp = isUp,
                    CheckedAt = DateTime.UtcNow
                });
            }
            else
            {
                status.IsUp = isUp;
                status.CheckedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public static async Task<bool> ProbeAsync(HttpClient client, string rawUrl, CancellationToken ct)
    {
        if (!TryNormalizeUri(rawUrl, out var uri))
            return false;

        // Inside Docker, localhost points at the container — check the host instead.
        if (RunningInContainer && IsLoopbackHost(uri.Host))
        {
            uri = new UriBuilder(uri) { Host = "host.docker.internal" }.Uri;
        }

        if (!await CanConnectAsync(uri, ct))
            return false;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var code = (int)response.StatusCode;
            // Reachable service: OK family, redirects, or auth-required.
            return code is >= 200 and < 400 or 401 or 403;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryNormalizeUri(string rawUrl, out Uri uri)
    {
        uri = null!;
        var value = rawUrl.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!value.Contains("://", StringComparison.Ordinal))
            value = "http://" + value;

        if (!Uri.TryCreate(value, UriKind.Absolute, out uri!))
            return false;

        return uri.Scheme is "http" or "https";
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> CanConnectAsync(Uri uri, CancellationToken ct)
    {
        var port = uri.IsDefaultPort
            ? (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80)
            : uri.Port;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            using var client = new TcpClient();
            await client.ConnectAsync(uri.IdnHost, port, cts.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
