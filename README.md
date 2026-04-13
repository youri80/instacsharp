# DSGVO-konformes Instagram-Embed Tool (C# / ASP.NET Core)

Zeigt den neuesten Beitrag eines Instagram-Accounts auf einer Homepage als Bild an.
Durch Klicken des Bildes wird der Nutzer zum Original-Beitrag auf Instagram geleitet.

---

## 🔒 DSGVO-Features

| Feature | Beschreibung |
|---|---|
| **Consent-Banner** | Bild wird erst geladen nach Nutzereinwilligung |
| **Kein Hotlinking** | Bild wird lokal gespeichert, kein direkter Request zu Instagram |
| **EXIF-Entfernung** | Alle Metadaten werden aus dem Bild entfernt (ImageSharp) |
| **CSP-Header** | Content-Security-Policy blockiert externe Ressourcen |
| **Referrer-Policy** | Kein Referrer-Leak an Instagram |
| **Kein Tracking** | Keine Cookies, kein Pixel, keine Analytics |
| **Lokale Einwilligung** | Consent im `localStorage` (30 Tage) |
| **Widerruf möglich** | Einwilligung jederzeit widerrufbar |

---

## ⚙️ Voraussetzungen

- **.NET 9 SDK** (oder .NET 8)
- **Chromium** (für PuppeteerSharp – wird automatisch heruntergeladen)

### Chromium-Abhängigkeiten (Ubuntu/Debian)

```bash
sudo apt install -y chromium-browser libx11-xcb1 libxcomposite1 \
  libxcursor1 libxdamage1 libxi6 libxtst6 libnss3 libcups2 \
  libxrandr2 libasound2 libpangocairo-1.0-0 libgtk-3-0 libgbm1
```

---

## 🚀 Installation & Start

```bash
# 1. In das Verzeichnis wechseln
cd Instagramcsharp

# 2. Konfiguration anpassen
#    Entweder in InstagramConfig.cs (Default-Werte)
#    oder per Umgebungsvariablen (siehe unten)

# 3. Restore & Build
dotnet restore
dotnet build

# 4. Start
dotnet run
# → http://localhost:3000
```

---

## 🔧 Konfiguration

### Umgebungsvariablen

| Variable | Standard | Beschreibung |
|---|---|---|
| `INSTA_USER` | `beispiel_account` | Instagram-Benutzername (ohne @) |
| `PORT` | `3000` | Webserver-Port |
| `CACHE_DURATION` | `3600` | Cache-Dauer in Sekunden |
| `IMAGE_WIDTH` | `600` | Bildbreite in Pixel |
| `CONSENT_MODE` | `opt-in` | `opt-in` oder `always` |
| `CONSENT_TEXT` | *(deutsch)* | DSGVO-Hinweistext |
| `PRIVACY_POLICY_URL` | `/datenschutz` | Link zur Datenschutzerklärung |
| `SCRAPE_TIMEOUT` | `30000` | Timeout in ms |
| `OUTPUT_DIR` | `./wwwroot/images` | Bild-Ausgabeverzeichnis |
| `DEBUG` | `false` | Logging aktivieren |

### Beispiel mit Umgebungsvariablen

```bash
export INSTA_USER=mein_account
export PORT=8080
export CACHE_DURATION=7200
export DEBUG=true
dotnet run
```

---

## 📖 Einbindung in Homepage

### Option 1: Standalone

`http://localhost:3000` direkt aufrufen.

### Option 2: Embed in bestehende Seite

```html
<link rel="stylesheet" href="http://localhost:3000/css/embed.css">
<div id="instagram-embed"></div>
<script>
  window.InstagramEmbed = {
    consentMode: 'opt-in',
    consentText: 'Ihr DSGVO-Hinweistext...',
    privacyPolicyUrl: '/datenschutz',
  };
</script>
<script src="http://localhost:3000/js/embed.js"></script>
```

### Option 3: Reverse Proxy (nginx)

```nginx
location /instagram/ {
    proxy_pass http://127.0.0.1:3000/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

---

## 🔌 API

### GET /api/latest-post

```json
{
  "image": "/images/instagram_meinaccount_1712985600.webp",
  "caption": "Beitragstext...",
  "permalink": "https://www.instagram.com/p/ABC123/",
  "timestamp": "2026-04-13T06:00:00.0000000Z",
  "username": "meinaccount",
  "cached": true
}
```

### GET /api/health

```json
{ "status": "ok", "user": "meinaccount" }
```

---

## 🐳 Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 3000

RUN apt-get update && apt-get install -y \
    chromium --no-install-recommends && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENTRYPOINT ["dotnet", "Instagramcsharp.dll"]
```

```bash
docker build -t instagram-embed-cs .
docker run -d \
  --name instagram-embed-cs \
  -p 3000:3000 \
  -e INSTA_USER=meinaccount \
  --restart unless-stopped \
  instagram-embed-cs
```

---

## ⚠️ DSGVO-Hinweise

1. **Datenschutzerklärung**: Einbindung erwähnen
2. **Rechtsgrundlage**: Art. 6 Abs. 1 lit. a DSGVO (Einwilligung)
3. **Verantwortlicher**: Meta Platforms, Inc. ist Empfänger beim Klick
4. **Widerruf**: Über `localStorage` möglich
5. **Protokollierung**: Server loggt keine Besucher-IPs

### Empfohlener Text für Datenschutzerklärung

> Auf unserer Website wird der neueste Beitrag unseres Instagram-Accounts angezeigt.
> Hierzu wird ein Bild des Beitrags lokal auf unserem Server gespeichert und eingebunden.
> Erst wenn Sie auf das Bild klicken, werden Sie zu Instagram weitergeleitet.
> Dabei werden Daten an Meta Platforms, Inc. (USA) übertragen.
> Die Einbindung erfolgt auf Grundlage Ihrer Einwilligung (Art. 6 Abs. 1 lit. a DSGVO).
> Sie können Ihre Einwilligung jederzeit widerrufen.

---

## 📁 Dateistruktur

```
Instagramcsharp/
├── Program.cs              ← Einstiegspunkt
├── InstagramConfig.cs      ← Konfiguration
├── InstagramScraper.cs     ← Scraper (PuppeteerSharp) + Bildverarbeitung (ImageSharp)
├── InstagramServer.cs       ← ASP.NET Core Server
├── Instagramcsharp.csproj   ← Projektdatei
├── README.md               ← Diese Dokumentation
├── .env.example            ← Umgebungsvariablen-Vorlage
├── .gitignore
└── wwwroot/
    ├── index.html           ← Beispiel-Seite
    ├── css/embed.css        ← Styles
    ├── js/embed.js          ← Client-Skript mit Consent
    └── images/              ← Generierte Bilder
```

---

## 🆚 Vergleich Node.js vs. C#

| Aspekt | Node.js (Instagram/) | C# (Instagramcsharp/) |
|---|---|---|
| Runtime | Node.js | .NET 9 |
| Web-Framework | Express | ASP.NET Core |
| Browser-Automat. | Puppeteer (JS) | PuppeteerSharp |
| Bildverarbeitung | Sharp | ImageSharp |
| Caching | node-cache | IMemoryCache |
| Performance | Gut | Besser (Multi-Core) |
| Speicherbedarf | Niedriger | Höher |
| Deployment | Leicht | Docker empfohlen |