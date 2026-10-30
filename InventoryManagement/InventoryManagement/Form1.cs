using InventoryManagement.BUS;
using InventoryManagement.DTO;

namespace InventoryManagement;

public partial class Form1 : Form
{
    private readonly InventoryBus bus = new();
    private readonly DataGridView materialGrid = new();
    private readonly DataGridView supplierGrid = new();
    private readonly DataGridView lineGrid = new();
    private readonly DataGridView alertGrid = new();
    private readonly TextBox materialSearch = new();
    private readonly TextBox supplierSearch = new();
    private readonly ComboBox receiptType = new();
    private readonly ComboBox receiptSupplier = new();
    private readonly ComboBox receiptMaterial = new();
    private readonly NumericUpDown receiptQuantity = new() { Minimum = 0.01M, Maximum = 100000000, DecimalPlaces = 2, Value = 1 };
    private readonly NumericUpDown receiptPrice = new() { Maximum = 100000000, DecimalPlaces = 2 };
    private readonly Label totalValue = new();

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        LoadData();
    }

    private void BuildUi()
    {
        Text = "Quản lý nhập - xuất kho nguyên vật liệu";
        Width = 1200;
        Height = 750;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildMaterialsTab());
        tabs.TabPages.Add(BuildSuppliersTab());
        tabs.TabPages.Add(BuildReceiptTab());
        tabs.TabPages.Add(BuildReportTab());
        Controls.Add(tabs);
    }

    private TabPage BuildMaterialsTab()
    {
        var page = new TabPage("Vật tư");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        materialSearch.Width = 220;
        panel.Controls.Add(new Label { Text = "Tìm mã/tên:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        panel.Controls.Add(materialSearch);
        panel.Controls.Add(Button("Tìm", (_, _) => LoadMaterials()));
        panel.Controls.Add(Button("Thêm", (_, _) => AddMaterial()));
        panel.Controls.Add(Button("Sửa dòng chọn", (_, _) => UpdateMaterial()));
        panel.Controls.Add(Button("Xóa dòng chọn", (_, _) => DeleteMaterial()));
        ConfigureGrid(materialGrid, ["Mã vật tư", "Tên vật tư", "ĐVT", "Tồn kho"]);
        page.Controls.Add(materialGrid); page.Controls.Add(panel); return page;
    }

    private TabPage BuildSuppliersTab()
    {
        var page = new TabPage("Nhà cung cấp");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        supplierSearch.Width = 220;
        panel.Controls.Add(new Label { Text = "Tìm kiếm:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        panel.Controls.Add(supplierSearch); panel.Controls.Add(Button("Tìm", (_, _) => LoadSuppliers()));
        panel.Controls.Add(Button("Thêm", (_, _) => AddSupplier())); panel.Controls.Add(Button("Sửa dòng chọn", (_, _) => UpdateSupplier())); panel.Controls.Add(Button("Xóa dòng chọn", (_, _) => DeleteSupplier()));
        ConfigureGrid(supplierGrid, ["Mã NCC", "Tên nhà cung cấp", "Điện thoại", "Địa chỉ"]);
        page.Controls.Add(supplierGrid); page.Controls.Add(panel); return page;
    }

    private TabPage BuildReceiptTab()
    {
        var page = new TabPage("Lập phiếu kho");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 85, Padding = new Padding(6), WrapContents = true };
        receiptType.Items.AddRange(["NHAP", "XUAT"]); receiptType.SelectedIndex = 0; receiptType.Width = 85; receiptSupplier.Width = 220; receiptMaterial.Width = 180;
        top.Controls.Add(new Label { Text = "Loại phiếu", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); top.Controls.Add(receiptType);
        top.Controls.Add(new Label { Text = "Nhà cung cấp", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); top.Controls.Add(receiptSupplier);
        top.Controls.Add(new Label { Text = "Vật tư", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); top.Controls.Add(receiptMaterial);
        top.Controls.Add(new Label { Text = "Số lượng", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); top.Controls.Add(receiptQuantity);
        top.Controls.Add(new Label { Text = "Đơn giá", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); top.Controls.Add(receiptPrice); top.Controls.Add(Button("Thêm dòng", (_, _) => AddReceiptLine()));
        ConfigureGrid(lineGrid, ["Mã vật tư", "Tên vật tư", "Số lượng", "Đơn giá", "Thành tiền"]);
        var save = Button("Lưu phiếu kho", (_, _) => SaveReceipt()); save.Dock = DockStyle.Bottom; save.Height = 40;
        page.Controls.Add(lineGrid); page.Controls.Add(save); page.Controls.Add(top); receiptType.SelectedIndexChanged += (_, _) => receiptSupplier.Enabled = receiptType.Text == "NHAP"; return page;
    }

    private TabPage BuildReportTab()
    {
        var page = new TabPage("Cảnh báo & báo cáo"); var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        panel.Controls.Add(Button("Làm mới", (_, _) => LoadReports())); panel.Controls.Add(totalValue);
        ConfigureGrid(alertGrid, ["Mã vật tư", "Tên vật tư", "Số lượng tồn"]); page.Controls.Add(alertGrid); page.Controls.Add(panel); return page;
    }

    private void LoadData() { try { LoadMaterials(); LoadSuppliers(); LoadReports(); } catch (Exception ex) { ShowError(ex); } }
    private void LoadMaterials() { materialGrid.Rows.Clear(); foreach (var item in bus.SearchMaterials(materialSearch.Text)) materialGrid.Rows.Add(item.MaVatTu, item.TenVatTu, item.DVT, item.SoLuongTon); LoadReceiptSelectors(); }
    private void LoadSuppliers() { supplierGrid.Rows.Clear(); foreach (var item in bus.SearchSuppliers(supplierSearch.Text)) supplierGrid.Rows.Add(item.MaNCC, item.TenNCC, item.DienThoai, item.DiaChi); LoadReceiptSelectors(); }
    private void LoadReceiptSelectors()
    {
        receiptMaterial.Items.Clear(); foreach (var item in bus.SearchMaterials(null)) receiptMaterial.Items.Add(new Selector(item.MaVatTu, $"{item.MaVatTu} - {item.TenVatTu}"));
        receiptSupplier.Items.Clear(); foreach (var item in bus.SearchSuppliers(null)) receiptSupplier.Items.Add(new Selector(item.MaNCC.ToString(), item.TenNCC));
        if (receiptMaterial.Items.Count > 0) receiptMaterial.SelectedIndex = 0; if (receiptSupplier.Items.Count > 0) receiptSupplier.SelectedIndex = 0;
    }
    private void LoadReports() { alertGrid.Rows.Clear(); foreach (var item in bus.GetLowStock()) alertGrid.Rows.Add(item.MaVatTu, item.TenVatTu, item.SoLuongTon); totalValue.Text = $"Tổng giá trị tồn kho: {bus.GetTotalStockValue():N2}"; }
    private void AddMaterial() { using var dialog = new MaterialDialog(); if (dialog.ShowDialog() == DialogResult.OK) Execute(() => bus.AddMaterial(dialog.Value), LoadMaterials); }
    private void UpdateMaterial() { if (materialGrid.CurrentRow is null) return; using var dialog = new MaterialDialog(materialGrid.CurrentRow.Cells[0].Value?.ToString(), materialGrid.CurrentRow.Cells[1].Value?.ToString(), materialGrid.CurrentRow.Cells[2].Value?.ToString()); if (dialog.ShowDialog() == DialogResult.OK) Execute(() => bus.UpdateMaterial(dialog.Value), LoadMaterials); }
    private void DeleteMaterial() { if (materialGrid.CurrentRow is not null) Execute(() => bus.DeleteMaterial(materialGrid.CurrentRow.Cells[0].Value?.ToString()), LoadMaterials); }
    private void AddSupplier() { using var dialog = new SupplierDialog(); if (dialog.ShowDialog() == DialogResult.OK) Execute(() => bus.AddSupplier(dialog.Value), LoadSuppliers); }
    private void UpdateSupplier() { if (supplierGrid.CurrentRow is not null) using (var dialog = new SupplierDialog(Convert.ToInt32(supplierGrid.CurrentRow.Cells[0].Value), supplierGrid.CurrentRow.Cells[1].Value?.ToString(), supplierGrid.CurrentRow.Cells[2].Value?.ToString(), supplierGrid.CurrentRow.Cells[3].Value?.ToString())) if (dialog.ShowDialog() == DialogResult.OK) Execute(() => bus.UpdateSupplier(dialog.Value), LoadSuppliers); }
    private void DeleteSupplier() { if (supplierGrid.CurrentRow is not null) Execute(() => bus.DeleteSupplier(Convert.ToInt32(supplierGrid.CurrentRow.Cells[0].Value)), LoadSuppliers); }
    private void AddReceiptLine() { if (receiptMaterial.SelectedItem is Selector selector) lineGrid.Rows.Add(selector.Key, selector.Text, receiptQuantity.Value, receiptPrice.Value, receiptQuantity.Value * receiptPrice.Value); }
    private void SaveReceipt()
    {
        var receipt = new StockReceipt { LoaiPhieu = receiptType.Text, MaNCC = receiptSupplier.SelectedItem is Selector supplier ? int.Parse(supplier.Key) : null };
        foreach (DataGridViewRow row in lineGrid.Rows) if (!row.IsNewRow) receipt.ChiTiet.Add(new StockLine { MaVatTu = row.Cells[0].Value.ToString()!, SoLuong = Convert.ToDecimal(row.Cells[2].Value), DonGia = Convert.ToDecimal(row.Cells[3].Value) });
        Execute(() => bus.SaveReceipt(receipt), () => { lineGrid.Rows.Clear(); LoadData(); });
    }
    private void Execute(Action action, Action refresh) { try { action(); refresh(); MessageBox.Show("Đã thực hiện thành công.", "Thông báo"); } catch (Exception ex) { ShowError(ex); } }
    private static void ShowError(Exception ex) => MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    private static Button Button(string text, EventHandler click) { var button = new Button { Text = text, AutoSize = true }; button.Click += click; return button; }
    private static void ConfigureGrid(DataGridView grid, string[] headers) { grid.Dock = DockStyle.Fill; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; grid.AllowUserToAddRows = false; grid.ReadOnly = true; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; foreach (var header in headers) grid.Columns.Add(header, header); }
    private sealed record Selector(string Key, string Text) { public override string ToString() => Text; }
}
