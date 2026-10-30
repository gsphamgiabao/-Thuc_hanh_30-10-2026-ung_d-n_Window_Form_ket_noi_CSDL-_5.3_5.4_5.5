using InventoryManagement.DTO;

namespace InventoryManagement;

internal sealed class SupplierDialog : Form
{
    private readonly TextBox name = new();
    private readonly TextBox phone = new();
    private readonly TextBox address = new();
    private readonly int id;
    public Supplier Value => new() { MaNCC = id, TenNCC = name.Text.Trim(), DienThoai = phone.Text.Trim(), DiaChi = address.Text.Trim() };

    public SupplierDialog(int id = 0, string? name = null, string? phone = null, string? address = null)
    {
        this.id = id; Text = "Thông tin nhà cung cấp"; Width = 400; Height = 230; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent;
        this.name.Text = name ?? string.Empty; this.phone.Text = phone ?? string.Empty; this.address.Text = address ?? string.Empty;
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 2, RowCount = 4 };
        AddRow(table, "Tên nhà cung cấp", this.name, 0); AddRow(table, "Điện thoại", this.phone, 1); AddRow(table, "Địa chỉ", this.address, 2);
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, AutoSize = true }; table.Controls.Add(save, 1, 3); AcceptButton = save; Controls.Add(table);
    }
    private static void AddRow(TableLayoutPanel table, string label, Control control, int row) { table.Controls.Add(new Label { Text = label, AutoSize = true }, 0, row); table.Controls.Add(control, 1, row); control.Dock = DockStyle.Fill; }
}
