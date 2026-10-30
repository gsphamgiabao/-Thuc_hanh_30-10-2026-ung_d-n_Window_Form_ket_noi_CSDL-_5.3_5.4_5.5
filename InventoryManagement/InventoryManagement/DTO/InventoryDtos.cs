namespace InventoryManagement.DTO;

public sealed class Material
{
    public string MaVatTu { get; set; } = string.Empty;
    public string TenVatTu { get; set; } = string.Empty;
    public string DVT { get; set; } = string.Empty;
    public decimal SoLuongTon { get; set; }
}

public sealed class Supplier
{
    public int MaNCC { get; set; }
    public string TenNCC { get; set; } = string.Empty;
    public string? DienThoai { get; set; }
    public string? DiaChi { get; set; }
}

public sealed class StockLine
{
    public string MaVatTu { get; set; } = string.Empty;
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
}

public sealed class StockReceipt
{
    public string LoaiPhieu { get; set; } = "NHAP";
    public int? MaNCC { get; set; }
    public List<StockLine> ChiTiet { get; set; } = [];
}

public sealed class StockAlert
{
    public string MaVatTu { get; set; } = string.Empty;
    public string TenVatTu { get; set; } = string.Empty;
    public decimal SoLuongTon { get; set; }
}
