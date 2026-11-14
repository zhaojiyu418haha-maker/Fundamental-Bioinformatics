using System.Text.Json;

namespace MiniHttpServer;

public static class ConfigurationManager
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ServerSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("settings.json was not found.", path);
        }

        var settings = JsonSerializer.Deserialize<ServerSettings>(
            File.ReadAllText(path),
            Options);

        if (settings is null)
        {
            throw new InvalidOperationException("settings.json is empty or invalid.");
        }

        if (settings.Prefixes.Length == 0)
        {
            throw new InvalidOperationException("At least one HTTP prefix is required.");
        }

        return settings;
    }
}
