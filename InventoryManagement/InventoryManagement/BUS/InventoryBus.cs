using InventoryManagement.DAL;
using InventoryManagement.DTO;

namespace InventoryManagement.BUS;

public sealed class InventoryBus
{
    private readonly MaterialDal materialDal = new();
    private readonly SupplierDal supplierDal = new();
    private readonly StockDal stockDal = new();

    public List<Material> SearchMaterials(string? search) => materialDal.GetAll(search);
    public void AddMaterial(Material material) => materialDal.Add(ValidateMaterial(material));
    public void UpdateMaterial(Material material) => materialDal.Update(ValidateMaterial(material));
    public void DeleteMaterial(string code) => materialDal.Delete(Required(code, "Mã vật tư"));

    public List<Supplier> SearchSuppliers(string? search) => supplierDal.GetAll(search);
    public int AddSupplier(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.TenNCC)) throw new ArgumentException("Tên nhà cung cấp không được trống.");
        return supplierDal.Add(supplier);
    }
    public void UpdateSupplier(Supplier supplier)
    {
        if (supplier.MaNCC <= 0 || string.IsNullOrWhiteSpace(supplier.TenNCC)) throw new ArgumentException("Thông tin nhà cung cấp không hợp lệ.");
        supplierDal.Update(supplier);
    }
    public void DeleteSupplier(int id) => supplierDal.Delete(id);

    public void SaveReceipt(StockReceipt receipt)
    {
        if (receipt.LoaiPhieu is not ("NHAP" or "XUAT")) throw new ArgumentException("Loại phiếu không hợp lệ.");
        if (receipt.LoaiPhieu == "NHAP" && receipt.MaNCC is null) throw new ArgumentException("Phiếu nhập phải có nhà cung cấp.");
        if (receipt.ChiTiet.Count == 0) throw new ArgumentException("Phiếu kho phải có ít nhất một dòng vật tư.");
        if (receipt.ChiTiet.Any(x => string.IsNullOrWhiteSpace(x.MaVatTu) || x.SoLuong <= 0 || x.DonGia < 0)) throw new ArgumentException("Số lượng phải lớn hơn 0 và đơn giá không được âm.");
        if (receipt.ChiTiet.GroupBy(x => x.MaVatTu, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1)) throw new ArgumentException("Mỗi vật tư chỉ được xuất hiện một lần trong phiếu.");
        stockDal.SaveReceipt(receipt);
    }

    public List<StockAlert> GetLowStock() => stockDal.GetLowStock(DatabaseSettings.SafetyStockThreshold);
    public decimal GetTotalStockValue() => stockDal.GetTotalStockValue();

    private static Material ValidateMaterial(Material material)
    {
        if (string.IsNullOrWhiteSpace(material.MaVatTu) || material.MaVatTu.Length > 20 || string.IsNullOrWhiteSpace(material.TenVatTu))
            throw new ArgumentException("Mã và tên vật tư là bắt buộc; mã tối đa 20 ký tự.");
        return material;
    }

    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} là bắt buộc.") : value.Trim();
}
