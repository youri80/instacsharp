using PuppeteerSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace InstagramEmbed;

/// <summary>
/// Instagram-Scraper – Holt den neuesten Beitrag ohne Instagram-API
/// 
/// DSGVO-relevant:
/// - Keine Anmeldung an Instagram nötig
/// - Kein API-Key erforderlich
/// - Scrapt nur öffentlich zugängliche Profilseiten
/// - Nur serverseitiger Abruf – kein Client-Tracking
/// </summary>
public class InstagramScraper
{
    private readonly InstagramConfig _config;
    private IBrowser? _browser;

    public InstagramScraper(InstagramConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Startet den Browser (Chromium)
    /// </summary>
    public async Task InitBrowser()
    {
        // Chromium herunterladen falls nicht vorhanden
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        _browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--disable-extensions",
            },
        });

        if (_config.Debug)
            Console.WriteLine("[Scraper] Browser gestartet");
    }

    /// <summary>
    /// Schließt den Browser
    /// </summary>
    public async Task CloseBrowser()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }
    }

    /// <summary>
    /// Holt den neuesten Beitrag eines Instagram-Profils
    /// </summary>
    /// <returns>Beitragsdaten mit Bild-URL, Caption, Permalink und Zeitstempel</returns>
    public async Task<InstagramPost?> FetchLatestPost(string username)
    {
        if (_browser == null)
            throw new InvalidOperationException("Browser nicht initialisiert. InitBrowser() zuerst aufrufen.");

        await using var page = await _browser.NewPageAsync();

        try
        {
            await page.SetUserAgentAsync(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

            await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 800 });

            // DSGVO: Cookies ablehnen (kein Tracking)
            await page.SetCookieAsync(new CookieParam
            {
                Name = "d_prefs",
                Value = "MToxLGNvbnNlbnRfdmVyc2lvbjoyLHNpZ25hbF9yZW1pbmRlcjox",
                Domain = ".instagram.com",
                Path = "/",
            });

            var url = $"https://www.instagram.com/{username}/";

            if (_config.Debug)
                Console.WriteLine($"[Scraper] Rufe auf: {url}");

            await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2},
                Timeout = _config.ScrapeTimeout,
            });

            // Warten auf Beiträge
            await page.WaitForSelectorAsync("article", new WaitForSelectorOptions
            {
                Timeout = _config.ScrapeTimeout,
            });

            // Ersten Beitrag extrahieren
            var postData = await page.EvaluateFunctionAsync<InstagramPostData?>(@"
                () => {
                    const article = document.querySelector('article');
                    if (!article) return null;

                    const img = article.querySelector('img[srcset]');
                    const imageUrl = img ? img.src : null;
                    const caption = img ? img.alt : '';

                    const link = article.querySelector('a[href*=""/p/""]') || 
                                 article.querySelector('a[href*=""/reel/""]');
                    const href = link ? link.getAttribute('href') : null;
                    const permalink = href ? 'https://www.instagram.com' + href : null;

                    return { imageUrl, caption, permalink };
                }
            ");

            if (postData == null || string.IsNullOrEmpty(postData.ImageUrl))
                throw new InvalidOperationException(
                    "Kein Beitrag auf der Profilseite gefunden. Profil möglicherweise privat.");

            var result = new InstagramPost
            {
                ImageUrl = postData.ImageUrl,
                Caption = postData.Caption ?? string.Empty,
                Permalink = postData.Permalink ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                Username = username,
            };

            if (_config.Debug)
                Console.WriteLine($"[Scraper] Beitrag gefunden: {result.Permalink}");

            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Scraper] Fehler: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Lädt ein Bild herunter und verarbeitet es lokal
    /// - Entfernt EXIF-Daten (DSGVO!)
    /// - Skaliert auf konfigurierte Breite
    /// - Speichert lokal (kein Hotlinking = DSGVO-konform)
    /// </summary>
    public async Task<string> ProcessImage(string imageUrl, string filename)
    {
        if (_browser == null)
            throw new InvalidOperationException("Browser nicht initialisiert.");

        await using var page = await _browser.NewPageAsync();

        try
        {
            // Bild über Browser herunterladen
            var response = await page.GoToAsync(imageUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = _config.ScrapeTimeout,
            });

            var imageBuffer = await response.BufferAsync();

            // Ausgabeverzeichnis erstellen
            var outputDir = Path.GetFullPath(_config.OutputDir);
            Directory.CreateDirectory(outputDir);

            // Als WebP speichern (Dateiendung ändern)
            var webpFilename = Path.GetFileNameWithoutExtension(filename) + ".webp";
            var outputPath = Path.Combine(outputDir, webpFilename);

            // Bild verarbeiten mit SixLabors.ImageSharp:
            // - EXIF-Daten entfernen (DSGVO!)
            // - Auf konfigurierte Breite skalieren
            // - Als WebP speichern
            using (var image = SixLabors.ImageSharp.Image.Load(imageBuffer))
            {
                // EXIF-Metadaten entfernen
                image.Metadata.ExifProfile = null;

                // Skalieren
                if (image.Width > _config.ImageWidth)
                {
                    var ratio = (double)_config.ImageWidth / image.Width;
                    var newHeight = (int)(image.Height * ratio);
                    image.Mutate(x => x.Resize(_config.ImageWidth, newHeight));
                }

                // Als WebP speichern
                await image.SaveAsWebpAsync(outputPath);
            }

            var relativePath = $"/images/{webpFilename}";

            if (_config.Debug)
                Console.WriteLine($"[Scraper] Bild gespeichert: {outputPath}");

            return relativePath;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Scraper] Bildverarbeitung fehlgeschlagen: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Räumt alte Instagram-Bilder auf (behält nur das neueste)
    /// </summary>
    public void CleanOldImages(string username, string currentFilename)
    {
        var outputDir = Path.GetFullPath(_config.OutputDir);
        if (!Directory.Exists(outputDir)) return;

        var prefix = $"instagram_{username}_";
        var currentBase = Path.GetFileNameWithoutExtension(currentFilename);

        foreach (var file in Directory.GetFiles(outputDir))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith(prefix) && !fileName.StartsWith(currentBase))
            {
                try
                {
                    File.Delete(file);
                    if (_config.Debug)
                        Console.WriteLine($"[Scraper] Altes Bild gelöscht: {fileName}");
                }
                catch { /* Ignorieren */ }
            }
        }
    }

    // Hilfsklasse für JavaScript-Ergebnis
    private class InstagramPostData
    {
        public string? ImageUrl { get; set; }
        public string? Caption { get; set; }
        public string? Permalink { get; set; }
    }
}

/// <summary>
/// Datenmodell für einen Instagram-Beitrag
/// </summary>
public class InstagramPost
{
    public string ImageUrl { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Permalink { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Username { get; set; } = string.Empty;
}

/// <summary>
/// API-Response mit lokalem Bildpfad
/// </summary>
public class InstagramPostResponse
{
    public string Image { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Permalink { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool Cached { get; set; }
}