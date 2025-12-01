using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Funcy.Data;

public static class DatabaseConnectionFactory
{
    public static string CreateConnectionString(IConfiguration configuration)
    {
        var baseConnectionString = configuration.GetConnectionString("DefaultConnection");
        var builder = new SqliteConnectionStringBuilder(baseConnectionString);

        var fileName = Path.GetFileName(builder.DataSource);
        var baseDirectory = GetBaseDirectory();
        Directory.CreateDirectory(baseDirectory);

        builder.DataSource = Path.Combine(baseDirectory, fileName);
        return builder.ToString();
    }

    static string GetBaseDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "Funcy");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(root, "Funcy");
        }

        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
            return Path.Combine(xdgDataHome, "funcy");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".local", "share", "funcy");
    }
}