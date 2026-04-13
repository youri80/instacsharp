using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace InstagramEmbed;

/// <summary>
/// DSGVO-konformer ASP.NET Core Server
/// Stellt den neuesten Instagram-Beitrag als Bild bereit.
/// Beinhaltet Consent-Management und keine Client-seitigen Third-Party-Requests.
/// </summary>
public class InstagramServer
{
    private readonly InstagramConfig _config;
    private readonly InstagramScraper _scraper;
    private readonly IMemoryCache _cache;

    public InstagramServer(InstagramConfig config)
    {
        _config = config;
        _scraper = new InstagramScraper(config);
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public async Task Start()
    {
        // Browser initialisieren
        await _scraper.InitBrowser();

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMemoryCache();

        var app = builder.Build();

        // Statische Dateien
        app.UseStaticFiles();

        // DSGVO-Security-Header
        app.Use(async (context, next) =>
        {
            // CSP: Keine Verbindungen zu Instagram/Meta vom Client
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; img-src 'self'; script-src 'self' 'unsafe-inline'; " +
                "style-src 'self' 'unsafe-inline'; connect-src 'self';";

            // Kein Referrer an Instagram
            context.Response.Headers["Referrer-Policy"] = "no-referrer-when-downgrade";

            await next();
        });

        // API: Neuester Beitrag
        app.MapGet("/api/latest-post", async (HttpContext ctx) =>
        {
            var cacheKey = $"latest_{_config.InstagramUser}";

            if (_cache.TryGetValue(cacheKey, out InstagramPostResponse? cached))
            {
                if (cached != null)
                {
                    cached.Cached = true;
                    if (_config.Debug)
                        Console.WriteLine("[Server] Cache-Treffer");
                    await ctx.Response.WriteAsJsonAsync(cached);
                    return;
                }
            }

            try
            {
                // Neuesten Beitrag scrapen
                var post = await _scraper.FetchLatestPost(_config.InstagramUser);

                if (post == null)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsJsonAsync(new { error = "Kein Beitrag gefunden" });
                    return;
                }

                // Bild herunterladen und lokal verarbeiten
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var filename = $"instagram_{_config.InstagramUser}_{timestamp}.jpg";
                var localImagePath = await _scraper.ProcessImage(post.ImageUrl, filename);

                // Alte Bilder aufräumen
                _scraper.CleanOldImages(_config.InstagramUser, filename);

                var response = new InstagramPostResponse
                {
                    Image = localImagePath,
                    Caption = string.IsNullOrEmpty(post.Caption)
                        ? $"Neuester Beitrag von @{_config.InstagramUser}"
                        : post.Caption,
                    Permalink = post.Permalink,
                    Timestamp = post.Timestamp.ToString("o"),
                    Username = _config.InstagramUser,
                    Cached = false,
                };

                // Cache speichern
                _cache.Set(cacheKey, response, TimeSpan.FromSeconds(_config.CacheDuration));

                if (_config.Debug)
                    Console.WriteLine($"[Server] Neuer Beitrag geladen: {response.Permalink}");

                await ctx.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Server] Fehler: {ex.Message}");
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    error = "Beitrag konnte nicht geladen werden",
                    details = _config.Debug ? ex.Message : (string?)null,
                });
            }
        });

        // Health-Check
        app.MapGet("/api/health", () => new { status = "ok", user = _config.InstagramUser });

        // Server starten
        Console.WriteLine($"[Instagram-Embed] Server läuft auf Port {_config.Port}");
        Console.WriteLine($"[Instagram-Embed] Account: @{_config.InstagramUser}");
        Console.WriteLine($"[Instagram-Embed] Cache: {_config.CacheDuration}s");
        Console.WriteLine($"[Instagram-Embed] Consent-Mode: {_config.ConsentMode}");
        Console.WriteLine("[Instagram-Embed] DSGVO-Modus: Aktiv");

        app.Run($"http://0.0.0.0:{_config.Port}");
    }
}