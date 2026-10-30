using System.Data;
using HotelManagement.DTO;

namespace HotelManagement.BUS;

public static class RoomFilterService
{
    public static DataTable ToDataTable(IEnumerable<RoomDto> rooms)
    {
        var table = new DataTable();
        table.Columns.Add("SoPhong", typeof(string));
        table.Columns.Add("LoaiPhong", typeof(string));
        table.Columns.Add("GiaTheoGio", typeof(decimal));
        table.Columns.Add("TrangThai", typeof(string));
        foreach (var room in rooms)
        {
            table.Rows.Add(room.RoomNumber, room.RoomType, room.HourlyRate, room.Status);
        }
        return table;
    }

    public static DataView Filter(DataTable table, string? roomType, string? status)
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(roomType)) filters.Add($"LoaiPhong = '{Escape(roomType)}'");
        if (!string.IsNullOrWhiteSpace(status)) filters.Add($"TrangThai = '{Escape(status)}'");
        var view = new DataView(table) { RowFilter = string.Join(" AND ", filters) };
        return view;
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
