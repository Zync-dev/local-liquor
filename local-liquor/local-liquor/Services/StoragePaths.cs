namespace local_liquor.Services;

/// <summary>
/// Where the SQLite file and uploaded photos live.
///
/// On Railway the container filesystem is thrown away on every deploy, so both
/// must sit on an attached volume. Railway exposes its mount point as
/// RAILWAY_VOLUME_MOUNT_PATH; we honour that first, then an explicit
/// Storage:DataPath setting, and fall back to App_Data beside the project so a
/// developer machine needs no configuration at all.
/// </summary>
public sealed class StoragePaths
{
    public StoragePaths(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = Environment.GetEnvironmentVariable("RAILWAY_VOLUME_MOUNT_PATH")
                         ?? configuration["Storage:DataPath"];

        Root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data")
            : configured;

        Uploads = Path.Combine(Root, "uploads");
        Keys = Path.Combine(Root, "keys");

        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Uploads);
        Directory.CreateDirectory(Keys);
    }

    /// <summary>Directory holding the database.</summary>
    public string Root { get; }

    /// <summary>Directory holding uploaded photos, served at /media.</summary>
    public string Uploads { get; }

    /// <summary>Data protection key ring — see the note in Program.cs.</summary>
    public string Keys { get; }

    public string DatabaseFile => Path.Combine(Root, "localliquor.db");

    public string ConnectionString => $"Data Source={DatabaseFile}";
}
