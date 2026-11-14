namespace BattlegroundsGallery.Data;

/// <summary>
/// Local development database configuration. Override with the
/// BATTLEGROUNDS_DB environment variable in other environments.
/// </summary>
public static class DatabaseOptions
{
    public const string DefaultConnectionString =
        "Host=127.0.0.1;Port=5432;Database=battlegrounds_gallery;" +
        "Username=bg_app;Password=bg_dev_password;";

    public static string GetConnectionString()
        => Environment.GetEnvironmentVariable("BATTLEGROUNDS_DB")
           ?? DefaultConnectionString;
}
