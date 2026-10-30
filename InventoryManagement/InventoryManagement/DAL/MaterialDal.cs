using InventoryManagement.DTO;
using Microsoft.Data.SqlClient;

namespace InventoryManagement.DAL;

public sealed class MaterialDal
{
    public List<Material> GetAll(string? search = null)
    {
        const string sql = "SELECT MaVatTu, TenVatTu, DVT, SoLuongTon FROM dbo.VatTu WHERE @Search = N'' OR MaVatTu LIKE @Pattern OR TenVatTu LIKE @Pattern ORDER BY MaVatTu;";
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = new SqlCommand(sql, connection);
        var value = search?.Trim() ?? string.Empty;
        command.Parameters.Add("@Search", System.Data.SqlDbType.NVarChar, 150).Value = value;
        command.Parameters.Add("@Pattern", System.Data.SqlDbType.NVarChar, 150).Value = $"%{value}%";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<Material>();
        while (reader.Read())
        {
            result.Add(new Material
            {
                MaVatTu = reader.GetString(0),
                TenVatTu = reader.GetString(1),
                DVT = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                SoLuongTon = reader.GetDecimal(3)
            });
        }
        return result;
    }

    public void Add(Material material)
    {
        Execute("INSERT INTO dbo.VatTu (MaVatTu, TenVatTu, DVT) VALUES (@Code, @Name, @Unit);", material);
    }

    public void Update(Material material)
    {
        Execute("UPDATE dbo.VatTu SET TenVatTu = @Name, DVT = @Unit WHERE MaVatTu = @Code;", material);
    }

    public void Delete(string code)
    {
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = new SqlCommand("DELETE FROM dbo.VatTu WHERE MaVatTu = @Code;", connection);
        command.Parameters.Add("@Code", System.Data.SqlDbType.VarChar, 20).Value = code;
        connection.Open();
        command.ExecuteNonQuery();
    }

    private static void Execute(string sql, Material material)
    {
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Code", System.Data.SqlDbType.VarChar, 20).Value = material.MaVatTu;
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 150).Value = material.TenVatTu;
        command.Parameters.Add("@Unit", System.Data.SqlDbType.NVarChar, 20).Value = material.DVT;
        connection.Open();
        command.ExecuteNonQuery();
    }
}
