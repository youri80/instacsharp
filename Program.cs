using InstagramEmbed;

var config = new InstagramConfig();

var server = new InstagramServer(config);

// Graceful Shutdown
Console.CancelKeyPress += async (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("[Instagram-Embed] Server wird heruntergefahren...");
    await server.Stop();
    Environment.Exit(0);
};

AppDomain.CurrentDomain.ProcessExit += async (_, _) =>
{
    await server.Stop();
};

try
{
    await server.Start();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Instagram-Embed] Fataler Fehler: {ex.Message}");
    Environment.Exit(1);
}