using InventoryManagement.DTO;

namespace InventoryManagement;

internal sealed class MaterialDialog : Form
{
    private readonly TextBox code = new();
    private readonly TextBox name = new();
    private readonly TextBox unit = new();
    public Material Value => new() { MaVatTu = code.Text.Trim(), TenVatTu = name.Text.Trim(), DVT = unit.Text.Trim() };

    public MaterialDialog(string? code = null, string? name = null, string? unit = null)
    {
        Text = "Thông tin vật tư"; Width = 360; Height = 210; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent;
        this.code.Text = code ?? string.Empty; this.name.Text = name ?? string.Empty; this.unit.Text = unit ?? string.Empty; this.code.ReadOnly = code is not null;
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 2, RowCount = 4 };
        AddRow(table, "Mã vật tư", this.code, 0); AddRow(table, "Tên vật tư", this.name, 1); AddRow(table, "ĐVT", this.unit, 2);
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, AutoSize = true }; table.Controls.Add(save, 1, 3); AcceptButton = save; Controls.Add(table);
    }
    private static void AddRow(TableLayoutPanel table, string label, Control control, int row) { table.Controls.Add(new Label { Text = label, AutoSize = true }, 0, row); table.Controls.Add(control, 1, row); control.Dock = DockStyle.Fill; }
}
