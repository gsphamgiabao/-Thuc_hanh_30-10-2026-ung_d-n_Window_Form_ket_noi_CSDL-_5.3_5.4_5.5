using InventoryManagement.DTO;
using Microsoft.Data.SqlClient;

namespace InventoryManagement.DAL;

public sealed class SupplierDal
{
    public List<Supplier> GetAll(string? search = null)
    {
        const string sql = "SELECT MaNCC, TenNCC, DienThoai, DiaChi FROM dbo.NhaCungCap WHERE @Search = N'' OR TenNCC LIKE @Pattern OR DienThoai LIKE @Pattern ORDER BY TenNCC;";
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = new SqlCommand(sql, connection);
        var value = search?.Trim() ?? string.Empty;
        command.Parameters.Add("@Search", System.Data.SqlDbType.NVarChar, 150).Value = value;
        command.Parameters.Add("@Pattern", System.Data.SqlDbType.NVarChar, 150).Value = $"%{value}%";
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<Supplier>();
        while (reader.Read())
        {
            result.Add(new Supplier
            {
                MaNCC = reader.GetInt32(0),
                TenNCC = reader.GetString(1),
                DienThoai = reader.IsDBNull(2) ? null : reader.GetString(2),
                DiaChi = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }
        return result;
    }

    public int Add(Supplier supplier)
    {
        const string sql = "INSERT INTO dbo.NhaCungCap (TenNCC, DienThoai, DiaChi) OUTPUT INSERTED.MaNCC VALUES (@Name, @Phone, @Address);";
        return ExecuteScalar(sql, supplier);
    }

    public void Update(Supplier supplier)
    {
        const string sql = "UPDATE dbo.NhaCungCap SET TenNCC = @Name, DienThoai = @Phone, DiaChi = @Address WHERE MaNCC = @Id;";
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = CreateCommand(connection, sql, supplier);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = supplier.MaNCC;
        connection.Open();
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = new SqlCommand("DELETE FROM dbo.NhaCungCap WHERE MaNCC = @Id;", connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;
        connection.Open();
        command.ExecuteNonQuery();
    }

    private static int ExecuteScalar(string sql, Supplier supplier)
    {
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = CreateCommand(connection, sql, supplier);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string sql, Supplier supplier)
    {
        var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 150).Value = supplier.TenNCC;
        command.Parameters.Add("@Phone", System.Data.SqlDbType.VarChar, 15).Value = (object?)supplier.DienThoai ?? DBNull.Value;
        command.Parameters.Add("@Address", System.Data.SqlDbType.NVarChar, 255).Value = (object?)supplier.DiaChi ?? DBNull.Value;
        return command;
    }
}
