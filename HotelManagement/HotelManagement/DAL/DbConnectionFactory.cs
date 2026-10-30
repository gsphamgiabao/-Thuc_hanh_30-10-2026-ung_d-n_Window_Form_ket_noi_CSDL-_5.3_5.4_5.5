using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace HotelManagement.DAL;

public sealed class DbConnectionFactory
{
    private readonly string connectionString;

    public DbConnectionFactory(string? connectionString = null)
    {
        this.connectionString = connectionString ?? LoadConnectionString();
    }

    public SqlConnection CreateConnection() => new(connectionString);

    private static string LoadConnectionString()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Không tìm thấy tệp cấu hình: {path}");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var value = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("HotelDb")
            .GetString();

        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("ConnectionStrings:HotelDb chưa được cấu hình.")
            : value;
    }
}
