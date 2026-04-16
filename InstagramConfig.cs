namespace InstagramEmbed;

/// <summary>
/// DSGVO-konformes Instagram-Embed Tool – Konfiguration
/// Alle Werte können per Umgebungsvariablen überschrieben werden.
/// </summary>
public class InstagramConfig
{
    /// <summary>Instagram-Benutzername (ohne @)</summary>
    public string InstagramUser { get; set; } = GetEnv("INSTA_USER", "test_account");

    /// <summary>Port des Webservers</summary>
    public int Port { get; set; } = int.Parse(GetEnv("PORT", "3000"));

    /// <summary>Cache-Dauer in Sekunden</summary>
    public int CacheDuration { get; set; } = int.Parse(GetEnv("CACHE_DURATION", "3600"));

    /// <summary>Bildbreite in Pixeln</summary>
    public int ImageWidth { get; set; } = int.Parse(GetEnv("IMAGE_WIDTH", "600"));

    /// <summary>Consent-Mode: "opt-in" (empfohlen) oder "always"</summary>
    public string ConsentMode { get; set; } = GetEnv("CONSENT_MODE", "opt-in");

    /// <summary>DSGVO-Hinweistext</summary>
    public string ConsentText { get; set; } = GetEnv("CONSENT_TEXT",
        "Um den neuesten Instagram-Beitrag anzuzeigen, werden Daten an Instagram " +
        "(Meta Platforms, Inc., USA) übermittelt. Durch Klicken auf \"Akzeptieren\" " +
        "stimmen Sie der Datenübermittlung zu.");

    /// <summary>Link zur Datenschutzerklärung</summary>
    public string PrivacyPolicyUrl { get; set; } = GetEnv("PRIVACY_POLICY_URL", "/datenschutz");

    /// <summary>Timeout für Scraper in Millisekunden</summary>
    public int ScrapeTimeout { get; set; } = int.Parse(GetEnv("SCRAPE_TIMEOUT", "30000"));

    /// <summary>Ausgabeverzeichnis für Bilder</summary>
    public string OutputDir { get; set; } = GetEnv("OUTPUT_DIR", "./public/images");

    /// <summary>Logging aktivieren</summary>
    public bool Debug { get; set; } = GetEnv("DEBUG", "false") == "true";

    private static string GetEnv(string key, string fallback)
    {
        var val = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrEmpty(val) ? fallback : val;
    }
}
