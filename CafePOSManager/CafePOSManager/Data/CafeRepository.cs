using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace CafePOSManager.Data;

public sealed class CafeRepository
{
    private static string? s_cachedConnectionString;
    private readonly string _connectionString;

    public string ConnectionString => _connectionString;

    public CafeRepository(string? connectionString = null)
    {
        _connectionString = connectionString ?? ResolveConnectionString();
        EnsureDatabaseInitialized(_connectionString);
    }

    private static string ResolveConnectionString()
    {
        if (!string.IsNullOrEmpty(s_cachedConnectionString))
        {
            return s_cachedConnectionString;
        }

        var envCs = Environment.GetEnvironmentVariable("CAFEPOS_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envCs) && TestConnection(envCs))
        {
            s_cachedConnectionString = envCs;
            return envCs;
        }

        var appsettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(appsettingsPath))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(appsettingsPath));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var csSection) &&
                    csSection.TryGetProperty("CafePOSDB", out var csElement))
                {
                    var configuredCs = csElement.GetString();
                    if (!string.IsNullOrWhiteSpace(configuredCs) && TestConnection(configuredCs))
                    {
                        s_cachedConnectionString = configuredCs;
                        return configuredCs;
                    }
                }
            }
            catch
            {
                // Fall back to candidates
            }
        }

        string[] candidateServers =
        [
            @"(localdb)\MSSQLLocalDB",
            @"localhost\SQLEXPRESS",
            @"localhost\SQL",
            @".\SQLEXPRESS",
            @".\SQL",
            @"localhost",
            @"."
        ];

        foreach (var server in candidateServers)
        {
            var cs = $"Server={server};Database=CafePOSDB;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=2;";
            if (TestConnection(cs))
            {
                s_cachedConnectionString = $"Server={server};Database=CafePOSDB;Integrated Security=True;TrustServerCertificate=True;";
                return s_cachedConnectionString;
            }

            var masterCs = $"Server={server};Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=2;";
            if (TestConnection(masterCs))
            {
                s_cachedConnectionString = $"Server={server};Database=CafePOSDB;Integrated Security=True;TrustServerCertificate=True;";
                return s_cachedConnectionString;
            }
        }

        s_cachedConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=CafePOSDB;Integrated Security=True;TrustServerCertificate=True;";
        return s_cachedConnectionString;
    }

    private static bool TestConnection(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureDatabaseInitialized(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.BanAn;", connection);
            cmd.ExecuteScalar();
            return;
        }
        catch
        {
            // Database or table might not exist yet. Try creating via master.
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master",
                ConnectTimeout = 5
            };

            string[] possiblePaths =
            [
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "CafePOSDB.sql"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CafePOSDB.sql"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Database", "CafePOSDB.sql")
            ];

            string? sqlFile = possiblePaths.FirstOrDefault(File.Exists);
            if (sqlFile == null) return;

            string script = File.ReadAllText(sqlFile, System.Text.Encoding.UTF8);
            var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            using var masterConn = new SqlConnection(builder.ConnectionString);
            masterConn.Open();
            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                using var cmd = new SqlCommand(trimmed, masterConn);
                cmd.ExecuteNonQuery();
            }
        }
        catch
        {
            // If creation fails, subsequent queries will throw descriptive error
        }
    }

    public DataTable GetTables()
    {
        return LoadTable("SELECT MaBan, TenBan, TrangThai FROM dbo.BanAn ORDER BY MaBan;");
    }

    public DataTable GetCategories()
    {
        return LoadTable("SELECT MaDanhMuc, TenDanhMuc FROM dbo.DanhMuc ORDER BY TenDanhMuc;");
    }

    public DataTable GetMenu()
    {
        return LoadTable("""
            SELECT m.MaMon, m.TenMon, m.MaDanhMuc, d.TenDanhMuc, m.DonGia
            FROM dbo.MonAn AS m
            INNER JOIN dbo.DanhMuc AS d ON d.MaDanhMuc = m.MaDanhMuc
            ORDER BY d.TenDanhMuc, m.TenMon;
            """);
    }

    public DataTable GetOrderDetails(int tableId)
    {
        return LoadTable("""
            SELECT m.MaMon, m.TenMon, c.SoLuong, c.DonGia,
                   CAST(c.SoLuong * c.DonGia AS DECIMAL(18,2)) AS ThanhTien
            FROM dbo.HoaDon AS h
            INNER JOIN dbo.ChiTietHoaDon AS c ON c.MaHoaDon = h.MaHoaDon
            INNER JOIN dbo.MonAn AS m ON m.MaMon = c.MaMon
            WHERE h.MaBan = @MaBan AND h.TrangThai = 0
            ORDER BY m.TenMon;
            """, new SqlParameter("@MaBan", tableId));
    }

    public void AddItem(int tableId, int productId, int quantity)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            int invoiceId;
            using (var findInvoice = new SqlCommand("""
                SELECT MaHoaDon FROM dbo.HoaDon WITH (UPDLOCK, HOLDLOCK)
                WHERE MaBan = @MaBan AND TrangThai = 0;
                """, connection, transaction))
            {
                findInvoice.Parameters.AddWithValue("@MaBan", tableId);
                var result = findInvoice.ExecuteScalar();
                if (result is null)
                {
                    using var createInvoice = new SqlCommand("""
                        INSERT INTO dbo.HoaDon (MaBan) VALUES (@MaBan);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);
                        """, connection, transaction);
                    createInvoice.Parameters.AddWithValue("@MaBan", tableId);
                    invoiceId = (int)createInvoice.ExecuteScalar()!;
                }
                else
                {
                    invoiceId = Convert.ToInt32(result);
                }
            }

            using (var updateDetail = new SqlCommand("""
                UPDATE dbo.ChiTietHoaDon
                SET SoLuong = SoLuong + @SoLuong
                WHERE MaHoaDon = @MaHoaDon AND MaMon = @MaMon;
                """, connection, transaction))
            {
                updateDetail.Parameters.AddWithValue("@SoLuong", quantity);
                updateDetail.Parameters.AddWithValue("@MaHoaDon", invoiceId);
                updateDetail.Parameters.AddWithValue("@MaMon", productId);
                if (updateDetail.ExecuteNonQuery() == 0)
                {
                    using var insertDetail = new SqlCommand("""
                        INSERT INTO dbo.ChiTietHoaDon (MaHoaDon, MaMon, SoLuong, DonGia)
                        SELECT @MaHoaDon, MaMon, @SoLuong, DonGia
                        FROM dbo.MonAn WHERE MaMon = @MaMon;
                        """, connection, transaction);
                    insertDetail.Parameters.AddWithValue("@MaHoaDon", invoiceId);
                    insertDetail.Parameters.AddWithValue("@MaMon", productId);
                    insertDetail.Parameters.AddWithValue("@SoLuong", quantity);
                    if (insertDetail.ExecuteNonQuery() == 0)
                    {
                        throw new InvalidOperationException("The selected menu item no longer exists.");
                    }
                }
            }

            using (var updateTable = new SqlCommand(
                "UPDATE dbo.BanAn SET TrangThai = N'Có khách' WHERE MaBan = @MaBan;",
                connection, transaction))
            {
                updateTable.Parameters.AddWithValue("@MaBan", tableId);
                updateTable.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public decimal Pay(int tableId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            int invoiceId;
            decimal total;
            using (var getInvoice = new SqlCommand("""
                SELECT h.MaHoaDon, COALESCE(SUM(c.SoLuong * c.DonGia), 0)
                FROM dbo.HoaDon AS h WITH (UPDLOCK, HOLDLOCK)
                LEFT JOIN dbo.ChiTietHoaDon AS c ON c.MaHoaDon = h.MaHoaDon
                WHERE h.MaBan = @MaBan AND h.TrangThai = 0
                GROUP BY h.MaHoaDon;
                """, connection, transaction))
            {
                getInvoice.Parameters.AddWithValue("@MaBan", tableId);
                using var reader = getInvoice.ExecuteReader();
                if (!reader.Read())
                {
                    throw new InvalidOperationException("This table has no unpaid invoice.");
                }
                invoiceId = reader.GetInt32(0);
                total = reader.GetDecimal(1);
            }

            if (total <= 0)
            {
                throw new InvalidOperationException("The invoice has no items to pay.");
            }

            using (var updateInvoice = new SqlCommand("""
                UPDATE dbo.HoaDon
                SET GioRa = GETDATE(), TongTien = @TongTien, TrangThai = 1
                WHERE MaHoaDon = @MaHoaDon AND TrangThai = 0;
                """, connection, transaction))
            {
                updateInvoice.Parameters.AddWithValue("@TongTien", total);
                updateInvoice.Parameters.AddWithValue("@MaHoaDon", invoiceId);
                if (updateInvoice.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("The invoice has already been paid.");
                }
            }

            using (var updateTable = new SqlCommand("""
                UPDATE dbo.BanAn SET TrangThai = N'Trống'
                WHERE MaBan = @MaBan
                  AND NOT EXISTS (SELECT 1 FROM dbo.HoaDon WHERE MaBan = @MaBan AND TrangThai = 0);
                """, connection, transaction))
            {
                updateTable.Parameters.AddWithValue("@MaBan", tableId);
                updateTable.ExecuteNonQuery();
            }

            transaction.Commit();
            return total;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void MoveActiveInvoice(int sourceTableId, int destinationTableId)
    {
        if (sourceTableId == destinationTableId)
        {
            throw new InvalidOperationException("Choose a different destination table.");
        }

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var checkDestination = new SqlCommand("""
                SELECT COUNT(*) FROM dbo.HoaDon WITH (UPDLOCK, HOLDLOCK)
                WHERE MaBan = @MaBan AND TrangThai = 0;
                """, connection, transaction))
            {
                checkDestination.Parameters.AddWithValue("@MaBan", destinationTableId);
                if (Convert.ToInt32(checkDestination.ExecuteScalar()) > 0)
                {
                    throw new InvalidOperationException("The destination table is occupied.");
                }
            }

            using (var moveInvoice = new SqlCommand("""
                UPDATE dbo.HoaDon SET MaBan = @Destination
                WHERE MaBan = @Source AND TrangThai = 0;
                """, connection, transaction))
            {
                moveInvoice.Parameters.AddWithValue("@Destination", destinationTableId);
                moveInvoice.Parameters.AddWithValue("@Source", sourceTableId);
                if (moveInvoice.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("The selected table has no active invoice.");
                }
            }

            using (var updateTables = new SqlCommand("""
                UPDATE dbo.BanAn SET TrangThai = CASE WHEN MaBan = @Destination THEN N'Có khách' ELSE N'Trống' END
                WHERE MaBan IN (@Source, @Destination);
                """, connection, transaction))
            {
                updateTables.Parameters.AddWithValue("@Source", sourceTableId);
                updateTables.Parameters.AddWithValue("@Destination", destinationTableId);
                updateTables.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void SaveCategory(int? categoryId, string name)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(categoryId.HasValue
            ? "UPDATE dbo.DanhMuc SET TenDanhMuc = @TenDanhMuc WHERE MaDanhMuc = @MaDanhMuc;"
            : "INSERT INTO dbo.DanhMuc (TenDanhMuc) VALUES (@TenDanhMuc);", connection);
        command.Parameters.AddWithValue("@TenDanhMuc", name);
        if (categoryId.HasValue)
        {
            command.Parameters.AddWithValue("@MaDanhMuc", categoryId.Value);
        }
        connection.Open();
        command.ExecuteNonQuery();
    }

    public void DeleteCategory(int categoryId)
    {
        Execute("DELETE FROM dbo.DanhMuc WHERE MaDanhMuc = @MaDanhMuc;", new SqlParameter("@MaDanhMuc", categoryId));
    }

    public void SaveMenuItem(int? productId, string name, int categoryId, decimal price)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(productId.HasValue
            ? "UPDATE dbo.MonAn SET TenMon = @TenMon, MaDanhMuc = @MaDanhMuc, DonGia = @DonGia WHERE MaMon = @MaMon;"
            : "INSERT INTO dbo.MonAn (TenMon, MaDanhMuc, DonGia) VALUES (@TenMon, @MaDanhMuc, @DonGia);", connection);
        command.Parameters.AddWithValue("@TenMon", name);
        command.Parameters.AddWithValue("@MaDanhMuc", categoryId);
        command.Parameters.Add("@DonGia", SqlDbType.Decimal).Value = price;
        command.Parameters["@DonGia"].Precision = 18;
        command.Parameters["@DonGia"].Scale = 2;
        if (productId.HasValue)
        {
            command.Parameters.AddWithValue("@MaMon", productId.Value);
        }
        connection.Open();
        command.ExecuteNonQuery();
    }

    public void DeleteMenuItem(int productId)
    {
        Execute("DELETE FROM dbo.MonAn WHERE MaMon = @MaMon;", new SqlParameter("@MaMon", productId));
    }

    private DataTable LoadTable(string query, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        using var adapter = new SqlDataAdapter(query, connection);
        if (parameters.Length > 0)
        {
            adapter.SelectCommand!.Parameters.AddRange(parameters);
        }
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    private void Execute(string query, params SqlParameter[] parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddRange(parameters);
        connection.Open();
        command.ExecuteNonQuery();
    }
}
