using InventoryManagement.DTO;
using Microsoft.Data.SqlClient;

namespace InventoryManagement.DAL;

public sealed class StockDal
{
    public void SaveReceipt(StockReceipt receipt)
    {
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            using var receiptCommand = new SqlCommand("INSERT INTO dbo.PhieuKho (LoaiPhieu, MaNCC, TongTien) OUTPUT INSERTED.MaPhieu VALUES (@Type, @SupplierId, @Total);", connection, transaction);
            receiptCommand.Parameters.Add("@Type", System.Data.SqlDbType.NVarChar, 10).Value = receipt.LoaiPhieu;
            receiptCommand.Parameters.Add("@SupplierId", System.Data.SqlDbType.Int).Value = (object?)receipt.MaNCC ?? DBNull.Value;
            receiptCommand.Parameters.Add("@Total", System.Data.SqlDbType.Decimal).Value = receipt.ChiTiet.Sum(x => x.SoLuong * x.DonGia);
            receiptCommand.Parameters["@Total"].Precision = 18;
            receiptCommand.Parameters["@Total"].Scale = 2;
            var receiptId = Convert.ToInt32(receiptCommand.ExecuteScalar());

            foreach (var line in receipt.ChiTiet)
            {
                using var detailCommand = new SqlCommand("INSERT INTO dbo.ChiTietPhieuKho (MaPhieu, MaVatTu, SoLuong, DonGia) VALUES (@ReceiptId, @Code, @Quantity, @Price);", connection, transaction);
                detailCommand.Parameters.Add("@ReceiptId", System.Data.SqlDbType.Int).Value = receiptId;
                detailCommand.Parameters.Add("@Code", System.Data.SqlDbType.VarChar, 20).Value = line.MaVatTu;
                AddDecimal(detailCommand, "@Quantity", line.SoLuong);
                AddDecimal(detailCommand, "@Price", line.DonGia);
                detailCommand.ExecuteNonQuery();

                const string updateSql = "UPDATE dbo.VatTu WITH (UPDLOCK, ROWLOCK) SET DonGiaTon = CASE WHEN @Type = N'NHAP' AND SoLuongTon + @Quantity > 0 THEN ((SoLuongTon * DonGiaTon) + (@Quantity * @Price)) / (SoLuongTon + @Quantity) ELSE DonGiaTon END, SoLuongTon = SoLuongTon + CASE WHEN @Type = N'NHAP' THEN @Quantity ELSE -@Quantity END WHERE MaVatTu = @Code AND (@Type = N'NHAP' OR SoLuongTon >= @Quantity);";
                using var stockCommand = new SqlCommand(updateSql, connection, transaction);
                stockCommand.Parameters.Add("@Type", System.Data.SqlDbType.NVarChar, 10).Value = receipt.LoaiPhieu;
                stockCommand.Parameters.Add("@Code", System.Data.SqlDbType.VarChar, 20).Value = line.MaVatTu;
                AddDecimal(stockCommand, "@Quantity", line.SoLuong);
                AddDecimal(stockCommand, "@Price", line.DonGia);
                if (stockCommand.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không đủ hàng trong kho");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<StockAlert> GetLowStock(decimal threshold)
    {
        const string sql = "SELECT MaVatTu, TenVatTu, SoLuongTon FROM dbo.VatTu WHERE SoLuongTon < @Threshold ORDER BY SoLuongTon, MaVatTu;";
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = new SqlCommand(sql, connection);
        AddDecimal(command, "@Threshold", threshold);
        connection.Open();
        using var reader = command.ExecuteReader();
        var result = new List<StockAlert>();
        while (reader.Read())
        {
            result.Add(new StockAlert { MaVatTu = reader.GetString(0), TenVatTu = reader.GetString(1), SoLuongTon = reader.GetDecimal(2) });
        }
        return result;
    }

    public decimal GetTotalStockValue()
    {
        const string sql = "SELECT ISNULL(SUM(SoLuongTon * DonGiaTon), 0) FROM dbo.VatTu;";
        using var connection = new SqlConnection(DatabaseSettings.ConnectionString);
        using var command = new SqlCommand(sql, connection);
        connection.Open();
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    private static void AddDecimal(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, System.Data.SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value;
    }
}
