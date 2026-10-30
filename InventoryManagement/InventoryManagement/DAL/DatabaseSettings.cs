using System.Text.Json;

namespace InventoryManagement.DAL;

public static class DatabaseSettings
{
    private static readonly Lazy<Configuration> Current = new(Load);

    public static string ConnectionString => Current.Value.ConnectionString;
    public static decimal SafetyStockThreshold => Current.Value.SafetyStockThreshold;

    private static Configuration Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("Không tìm thấy appsettings.json.", path);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var connectionString = root.GetProperty("ConnectionStrings").GetProperty("InventoryDb").GetString();
        var threshold = root.GetProperty("Inventory").GetProperty("SafetyStockThreshold").GetDecimal();
        return new Configuration(connectionString ?? throw new InvalidOperationException("Thiếu connection string."), threshold);
    }

    private sealed record Configuration(string ConnectionString, decimal SafetyStockThreshold);
}
