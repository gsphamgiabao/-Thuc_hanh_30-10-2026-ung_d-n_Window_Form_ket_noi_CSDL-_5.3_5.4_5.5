using HotelManagement.DTO;
using Microsoft.Data.SqlClient;

namespace HotelManagement.DAL;

public sealed class HotelRepository
{
    private readonly DbConnectionFactory connectionFactory;

    public HotelRepository(DbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<RoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT SoPhong, LoaiPhong, GiaTheoGio, TrangThai FROM dbo.Phong ORDER BY SoPhong";
        var rooms = new List<RoomDto>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(new RoomDto
            {
                RoomNumber = reader.GetString(0),
                RoomType = reader.GetString(1),
                HourlyRate = reader.GetDecimal(2),
                Status = reader.GetString(3)
            });
        }

        return rooms;
    }

    public async Task<CustomerDto?> FindCustomerByCitizenIdAsync(string citizenId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT MaKH, HoTen, CCCD, SoDienThoai FROM dbo.KhachHang WHERE CCCD = @cccd";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@cccd", citizenId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new CustomerDto
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                CitizenId = reader.GetString(2),
                PhoneNumber = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            }
            : null;
    }

    public async Task<int> AddCustomerAsync(CustomerDto customer, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO dbo.KhachHang (HoTen, CCCD, SoDienThoai) OUTPUT INSERTED.MaKH VALUES (@name, @cccd, @phone)";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", customer.FullName);
        command.Parameters.AddWithValue("@cccd", customer.CitizenId);
        command.Parameters.AddWithValue("@phone", (object?)customer.PhoneNumber ?? DBNull.Value);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("Không thể thêm khách hàng."));
    }

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT MaDV, TenDV, DonGia FROM dbo.DichVu ORDER BY TenDV";
        var services = new List<ServiceDto>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            services.Add(new ServiceDto { Id = reader.GetInt32(0), Name = reader.GetString(1), UnitPrice = reader.GetDecimal(2) });
        }

        return services;
    }

    public async Task<int> CheckInAsync(string roomNumber, int customerId, DateTime checkInAt, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string bookingSql = "INSERT INTO dbo.PhieuDatPhong (SoPhong, MaKH, NgayNhanPhong) OUTPUT INSERTED.MaPhieu VALUES (@room, @customer, @checkIn)";
            await using var bookingCommand = new SqlCommand(bookingSql, connection, transaction);
            bookingCommand.Parameters.AddWithValue("@room", roomNumber);
            bookingCommand.Parameters.AddWithValue("@customer", customerId);
            bookingCommand.Parameters.AddWithValue("@checkIn", checkInAt);
            var bookingId = (int)(await bookingCommand.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("Không thể lập phiếu."));

            const string roomSql = "UPDATE dbo.Phong SET TrangThai = N'Đang ở' WHERE SoPhong = @room AND TrangThai = N'Trống'";
            await using var roomCommand = new SqlCommand(roomSql, connection, transaction);
            roomCommand.Parameters.AddWithValue("@room", roomNumber);
            if (await roomCommand.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                throw new InvalidOperationException("Phòng không còn trống.");
            }

            await transaction.CommitAsync(cancellationToken);
            return bookingId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AddServiceUsageAsync(ServiceUsageDto usage, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO dbo.SuDungDichVu (MaPhieu, MaDV, SoLuong, ThoiGian) VALUES (@booking, @service, @quantity, @usedAt)";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@booking", usage.BookingId);
        command.Parameters.AddWithValue("@service", usage.ServiceId);
        command.Parameters.AddWithValue("@quantity", usage.Quantity);
        command.Parameters.AddWithValue("@usedAt", usage.UsedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingDto>> GetActiveBookingsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT p.MaPhieu, p.SoPhong, k.MaKH, k.HoTen, k.CCCD, p.NgayNhanPhong FROM dbo.PhieuDatPhong p INNER JOIN dbo.KhachHang k ON k.MaKH = p.MaKH WHERE p.NgayTraPhong IS NULL ORDER BY p.SoPhong";
        var bookings = new List<BookingDto>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            bookings.Add(new BookingDto { Id = reader.GetInt32(0), RoomNumber = reader.GetString(1), CustomerId = reader.GetInt32(2), CustomerName = reader.GetString(3), CitizenId = reader.GetString(4), CheckInAt = reader.GetDateTime(5) });
        }

        return bookings;
    }

    public async Task<CheckoutResultDto> CheckoutAsync(int bookingId, DateTime checkoutAt, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string readSql = "SELECT p.SoPhong, p.NgayNhanPhong, ph.GiaTheoGio, COALESCE(SUM(sd.SoLuong * dv.DonGia), 0) FROM dbo.PhieuDatPhong p INNER JOIN dbo.Phong ph ON ph.SoPhong = p.SoPhong LEFT JOIN dbo.SuDungDichVu sd ON sd.MaPhieu = p.MaPhieu LEFT JOIN dbo.DichVu dv ON dv.MaDV = sd.MaDV WHERE p.MaPhieu = @booking AND p.NgayTraPhong IS NULL GROUP BY p.SoPhong, p.NgayNhanPhong, ph.GiaTheoGio";
            await using var readCommand = new SqlCommand(readSql, connection, transaction);
            readCommand.Parameters.AddWithValue("@booking", bookingId);
            await using var reader = await readCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Phiếu không tồn tại hoặc đã thanh toán.");
            }

            var roomNumber = reader.GetString(0);
            var checkInAt = reader.GetDateTime(1);
            var hourlyRate = reader.GetDecimal(2);
            var serviceTotal = reader.GetDecimal(3);
            var duration = checkoutAt - checkInAt;
            if (duration <= TimeSpan.Zero) throw new InvalidOperationException("Thời điểm trả phòng phải sau thời điểm nhận phòng.");
            var billedHours = Math.Max(1, (int)Math.Ceiling(duration.TotalHours));
            var roomTotal = billedHours * hourlyRate;
            await reader.CloseAsync();

            const string updateBookingSql = "UPDATE dbo.PhieuDatPhong SET NgayTraPhong = @checkout, TongTienThanhToan = @total WHERE MaPhieu = @booking AND NgayTraPhong IS NULL";
            await using var updateBooking = new SqlCommand(updateBookingSql, connection, transaction);
            updateBooking.Parameters.AddWithValue("@checkout", checkoutAt);
            updateBooking.Parameters.AddWithValue("@total", roomTotal + serviceTotal);
            updateBooking.Parameters.AddWithValue("@booking", bookingId);
            if (await updateBooking.ExecuteNonQueryAsync(cancellationToken) != 1) throw new InvalidOperationException("Không thể cập nhật phiếu.");

            const string updateRoomSql = "UPDATE dbo.Phong SET TrangThai = N'Đang dọn dẹp' WHERE SoPhong = @room";
            await using var updateRoom = new SqlCommand(updateRoomSql, connection, transaction);
            updateRoom.Parameters.AddWithValue("@room", roomNumber);
            await updateRoom.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new CheckoutResultDto { BookingId = bookingId, RoomNumber = roomNumber, StayDuration = duration, RoomTotal = roomTotal, ServiceTotal = serviceTotal };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
