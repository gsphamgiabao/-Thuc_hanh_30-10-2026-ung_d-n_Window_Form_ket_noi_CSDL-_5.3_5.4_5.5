using System.Data;
using CafePOSManager.Services;

namespace CafePOSManager;

public partial class Form1 : Form
{
    private readonly CafeService _service = new();
    private readonly FlowLayoutPanel _tablePanel = new();
    private readonly DataGridView _orderGrid = new();
    private readonly ComboBox _orderCategory = new();
    private readonly ComboBox _orderProduct = new();
    private readonly NumericUpDown _orderQuantity = new();
    private readonly Label _selectedTableLabel = new();
    private readonly Label _totalLabel = new();
    private readonly DataGridView _menuGrid = new();
    private readonly ComboBox _categoryPicker = new();
    private readonly ComboBox _productCategory = new();
    private readonly TextBox _categoryName = new();
    private readonly TextBox _productName = new();
    private readonly NumericUpDown _productPrice = new();
    private readonly Button _saveCategoryButton = new();
    private readonly Button _saveProductButton = new();
    private DataTable _menuItems = new();
    private int? _selectedTableId;
    private int? _selectedProductId;
    private bool _loading;

    public Form1()
    {
        InitializeComponent();
        Text = "Cafe POS Manager";
        ClientSize = new Size(1180, 760);
        MinimumSize = new Size(900, 600);
        BuildInterface();
        Load += (_, _) => InitializeData();
    }

    private void BuildInterface()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var orderTab = new TabPage("Sơ đồ bàn & Gọi món");
        var menuTab = new TabPage("Quản lý thực đơn");
        tabs.TabPages.Add(orderTab);
        tabs.TabPages.Add(menuTab);
        Controls.Add(tabs);

        BuildOrderTab(orderTab);
        BuildMenuTab(menuTab);
    }

    private void BuildOrderTab(TabPage page)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical
        };
        page.Controls.Add(split);

        void UpdateSplitter()
        {
            if (split.Width >= 750)
            {
                try
                {
                    split.Panel1MinSize = 250;
                    split.Panel2MinSize = 500;
                    var maxDistance = split.Width - split.Panel2MinSize;
                    if (maxDistance >= split.Panel1MinSize)
                    {
                        split.SplitterDistance = Math.Clamp(360, split.Panel1MinSize, maxDistance);
                    }
                }
                catch
                {
                    // Ignore sizing constraints during initialization or minimization
                }
            }
        }

        split.SizeChanged += (_, _) => UpdateSplitter();

        var tableLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(12) };
        tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tableLayout.Controls.Add(new Label { Text = "SƠ ĐỒ BÀN", AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), Margin = new Padding(3, 3, 3, 12) }, 0, 0);
        _tablePanel.Dock = DockStyle.Fill;
        _tablePanel.AutoScroll = true;
        _tablePanel.WrapContents = true;
        _tablePanel.Padding = new Padding(2);
        tableLayout.Controls.Add(_tablePanel, 0, 1);
        split.Panel1.Controls.Add(tableLayout);

        var orderLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(12) };
        orderLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var header = new Panel { Dock = DockStyle.Fill, Height = 48 };
        _selectedTableLabel.Text = "Chưa chọn bàn";
        _selectedTableLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        _selectedTableLabel.AutoSize = true;
        _selectedTableLabel.Location = new Point(3, 5);
        _totalLabel.Text = "Tổng: 0 ₫";
        _totalLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _totalLabel.AutoSize = true;
        _totalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _totalLabel.Location = new Point(610, 9);
        header.Controls.Add(_selectedTableLabel);
        header.Controls.Add(_totalLabel);
        header.Resize += (_, _) => _totalLabel.Left = header.ClientSize.Width - _totalLabel.Width - 8;
        orderLayout.Controls.Add(header, 0, 0);

        ConfigureGrid(_orderGrid);
        _orderGrid.ReadOnly = true;
        orderLayout.Controls.Add(_orderGrid, 0, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 10, 0, 0) };
        _orderCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        _orderCategory.Width = 150;
        _orderCategory.DisplayMember = "TenDanhMuc";
        _orderCategory.ValueMember = "MaDanhMuc";
        _orderCategory.SelectedIndexChanged += (_, _) => FilterOrderProducts();
        _orderProduct.DropDownStyle = ComboBoxStyle.DropDownList;
        _orderProduct.Width = 200;
        _orderProduct.DisplayMember = "TenMon";
        _orderProduct.ValueMember = "MaMon";
        _orderQuantity.Minimum = 1;
        _orderQuantity.Maximum = 999;
        _orderQuantity.Width = 70;
        actions.Controls.AddRange([
            _orderCategory,
            _orderProduct,
            _orderQuantity,
            MakeButton("Thêm món", AddOrderItem),
            MakeButton("Đổi bàn", MoveOrder),
            MakeButton("Thanh toán", PayOrder),
            MakeButton("Làm mới", RefreshAll)
        ]);
        orderLayout.Controls.Add(actions, 0, 2);
        split.Panel2.Controls.Add(orderLayout);
    }

    private void BuildMenuTab(TabPage page)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(layout);

        var categoryBar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, 10) };
        categoryBar.Controls.Add(new Label { Text = "Danh mục:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        _categoryPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoryPicker.Width = 200;
        _categoryPicker.DisplayMember = "TenDanhMuc";
        _categoryPicker.ValueMember = "MaDanhMuc";
        _categoryPicker.SelectedIndexChanged += (_, _) =>
        {
            if (!_loading && _categoryPicker.SelectedItem is DataRowView row)
            {
                _categoryName.Text = row["TenDanhMuc"].ToString();
            }
        };
        _categoryName.Width = 200;
        _categoryName.PlaceholderText = "Tên danh mục";
        _saveCategoryButton.Text = "Lưu danh mục";
        _saveCategoryButton.AutoSize = true;
        _saveCategoryButton.Click += (_, _) => SaveCategory();
        categoryBar.Controls.AddRange([
            _categoryPicker,
            _categoryName,
            MakeButton("Danh mục mới", NewCategory),
            _saveCategoryButton,
            MakeButton("Xóa danh mục", DeleteCategory)
        ]);
        layout.Controls.Add(categoryBar, 0, 0);

        ConfigureGrid(_menuGrid);
        _menuGrid.ReadOnly = true;
        _menuGrid.SelectionChanged += (_, _) => SelectMenuItem();
        layout.Controls.Add(_menuGrid, 0, 1);

        var editPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 12, 0, 0) };
        _productName.Width = 220;
        _productName.MaxLength = 100;
        _productName.PlaceholderText = "Tên món";
        _productCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        _productCategory.Width = 190;
        _productCategory.DisplayMember = "TenDanhMuc";
        _productCategory.ValueMember = "MaDanhMuc";
        _productPrice.Minimum = 0;
        _productPrice.Maximum = 9999999999999999;
        _productPrice.DecimalPlaces = 2;
        _productPrice.ThousandsSeparator = true;
        _productPrice.Width = 130;
        _saveProductButton.Text = "Thêm món";
        _saveProductButton.AutoSize = true;
        _saveProductButton.Click += (_, _) => SaveMenuItem();
        editPanel.Controls.AddRange([
            _productName,
            _productCategory,
            _productPrice,
            _saveProductButton,
            MakeButton("Món mới", NewMenuItem),
            MakeButton("Xóa món", DeleteMenuItem)
        ]);
        layout.Controls.Add(editPanel, 0, 2);
    }

    private void InitializeData()
    {
        try
        {
            RefreshAll();
            RefreshAdminData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể kết nối CafePOSDB. Hãy chạy Database/CafePOSDB.sql và kiểm tra SQL Server Express.\n\n{ex.Message}",
                "Lỗi kết nối cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshAll()
    {
        var categories = _service.GetCategories();
        _menuItems = _service.GetMenu();
        _loading = true;
        _orderCategory.DataSource = categories;
        _loading = false;
        FilterOrderProducts();
        RefreshTables();
        RefreshOrder();
    }

    private void RefreshTables()
    {
        var tables = _service.GetTables();
        _tablePanel.Controls.Clear();
        foreach (DataRow row in tables.Rows)
        {
            var tableId = Convert.ToInt32(row["MaBan"]);
            var isOccupied = string.Equals(row["TrangThai"].ToString(), "Có khách", StringComparison.OrdinalIgnoreCase);
            var button = new Button
            {
                Text = $"{row["TenBan"]}\n{(isOccupied ? "Có khách" : "Trống")}",
                Tag = tableId,
                Size = new Size(145, 86),
                Margin = new Padding(6),
                BackColor = isOccupied ? Color.LightCoral : Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            button.Click += (_, _) => SelectTable((int)button.Tag!);
            _tablePanel.Controls.Add(button);
        }
    }

    private void SelectTable(int tableId)
    {
        _selectedTableId = tableId;
        var tableName = _service.GetTables().Select($"MaBan = {tableId}").FirstOrDefault();
        _selectedTableLabel.Text = tableName is null ? "Bàn" : $"{tableName["TenBan"]}";
        RefreshOrder();
    }

    private void RefreshOrder()
    {
        if (!_selectedTableId.HasValue)
        {
            _orderGrid.DataSource = null;
            _totalLabel.Text = "Tổng: 0 ₫";
            return;
        }

        var details = _service.GetOrderDetails(_selectedTableId.Value);
        _orderGrid.DataSource = details;
        ConfigureOrderColumns();
        var total = details.Rows.Cast<DataRow>().Sum(row => Convert.ToDecimal(row["ThanhTien"]));
        _totalLabel.Text = $"Tổng: {total:N0} ₫";
    }

    private void FilterOrderProducts()
    {
        if (_loading || _orderCategory.SelectedValue is null || _menuItems.Rows.Count == 0)
        {
            _orderProduct.DataSource = null;
            return;
        }
        var categoryId = Convert.ToInt32(_orderCategory.SelectedValue);
        var view = new DataView(_menuItems) { RowFilter = $"MaDanhMuc = {categoryId}" };
        _orderProduct.DataSource = view;
    }

    private void AddOrderItem()
    {
        if (!RequireTable() || _orderProduct.SelectedValue is null)
        {
            return;
        }
        try
        {
            _service.AddItem(_selectedTableId!.Value, Convert.ToInt32(_orderProduct.SelectedValue), (int)_orderQuantity.Value);
            RefreshAll();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void PayOrder()
    {
        if (!RequireTable())
        {
            return;
        }
        try
        {
            var total = _service.Pay(_selectedTableId!.Value);
            MessageBox.Show($"Thanh toán thành công: {total:N0} ₫", "Thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _selectedTableId = null;
            _selectedTableLabel.Text = "Chưa chọn bàn";
            RefreshAll();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void MoveOrder()
    {
        if (!RequireTable())
        {
            return;
        }
        try
        {
            var tables = _service.GetTables();
            var destinations = tables.AsEnumerable()
                .Where(row => Convert.ToInt32(row["MaBan"]) != _selectedTableId && row["TrangThai"].ToString() == "Trống")
                .Select(row => new TableOption(Convert.ToInt32(row["MaBan"]), row["TenBan"].ToString()!))
                .ToList();
            if (destinations.Count == 0)
            {
                MessageBox.Show("Không có bàn trống để chuyển.", "Đổi bàn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dialog = new Form { Text = "Chọn bàn nhận", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, ClientSize = new Size(300, 120) };
            var picker = new ComboBox { Left = 15, Top = 15, Width = 270, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = destinations, DisplayMember = nameof(TableOption.Name), ValueMember = nameof(TableOption.Id) };
            var ok = new Button { Text = "Chuyển", Left = 125, Top = 65, Width = 75, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Hủy", Left = 210, Top = 65, Width = 75, DialogResult = DialogResult.Cancel };
            dialog.Controls.AddRange([picker, ok, cancel]);
            dialog.AcceptButton = ok;
            dialog.CancelButton = cancel;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _service.MoveActiveInvoice(_selectedTableId!.Value, Convert.ToInt32(picker.SelectedValue));
                _selectedTableId = Convert.ToInt32(picker.SelectedValue);
                RefreshAll();
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RefreshAdminData()
    {
        _loading = true;
        var categories = _service.GetCategories();
        _categoryPicker.DataSource = categories.Copy();
        _productCategory.DataSource = categories;
        _loading = false;
        _menuItems = _service.GetMenu();
        _menuGrid.DataSource = _menuItems;
        ConfigureMenuColumns();
        NewMenuItem();
    }

    private void SaveCategory()
    {
        var name = _categoryName.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show("Tên danh mục không được để trống.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            int? categoryId = _categoryPicker.SelectedValue is null ? null : Convert.ToInt32(_categoryPicker.SelectedValue);
            _service.SaveCategory(categoryId, name);
            RefreshAll();
            RefreshAdminData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void NewCategory()
    {
        _categoryPicker.SelectedIndex = -1;
        _categoryName.Clear();
        _categoryName.Focus();
    }

    private void DeleteCategory()
    {
        if (_categoryPicker.SelectedValue is null)
        {
            return;
        }
        if (MessageBox.Show("Xóa danh mục đã chọn? Danh mục đang có món sẽ không thể xóa.", "Xóa danh mục", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }
        try
        {
            _service.DeleteCategory(Convert.ToInt32(_categoryPicker.SelectedValue));
            RefreshAll();
            RefreshAdminData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void SelectMenuItem()
    {
        if (_menuGrid.CurrentRow?.DataBoundItem is not DataRowView row)
        {
            return;
        }
        _selectedProductId = Convert.ToInt32(row["MaMon"]);
        _productName.Text = row["TenMon"].ToString();
        _productCategory.SelectedValue = Convert.ToInt32(row["MaDanhMuc"]);
        _productPrice.Value = Math.Clamp(Convert.ToDecimal(row["DonGia"]), _productPrice.Minimum, _productPrice.Maximum);
        _saveProductButton.Text = "Lưu món";
    }

    private void SaveMenuItem()
    {
        var name = _productName.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show("Tên món không được để trống.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_productCategory.SelectedValue is null)
        {
            MessageBox.Show("Hãy chọn danh mục cho món.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_productPrice.Value < 0)
        {
            MessageBox.Show("Đơn giá không được âm.", "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _service.SaveMenuItem(_selectedProductId, name, Convert.ToInt32(_productCategory.SelectedValue), _productPrice.Value);
            RefreshAll();
            RefreshAdminData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void NewMenuItem()
    {
        _selectedProductId = null;
        _productName.Clear();
        _productPrice.Value = 0;
        _saveProductButton.Text = "Thêm món";
        if (_menuGrid.Rows.Count > 0)
        {
            _menuGrid.ClearSelection();
        }
    }

    private void DeleteMenuItem()
    {
        if (!_selectedProductId.HasValue)
        {
            return;
        }
        if (MessageBox.Show("Xóa món đã chọn? Món đã xuất hiện trong hóa đơn không thể xóa.", "Xóa món", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }
        try
        {
            _service.DeleteMenuItem(_selectedProductId.Value);
            RefreshAll();
            RefreshAdminData();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private bool RequireTable()
    {
        if (_selectedTableId.HasValue)
        {
            return true;
        }
        MessageBox.Show("Hãy chọn một bàn trước.", "Cafe POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private static Button MakeButton(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Padding = new Padding(7, 4, 7, 4), Margin = new Padding(4) };
        button.Click += (_, _) => action();
        return button;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
    }

    private void ConfigureOrderColumns()
    {
        if (_orderGrid.Columns["MaMon"] is { } maMon) maMon.Visible = false;
        if (_orderGrid.Columns["TenMon"] is { } tenMon) tenMon.HeaderText = "Tên món";
        if (_orderGrid.Columns["SoLuong"] is { } soLuong) soLuong.HeaderText = "Số lượng";
        if (_orderGrid.Columns["DonGia"] is { } donGia)
        {
            donGia.HeaderText = "Đơn giá";
            donGia.DefaultCellStyle.Format = "N0";
        }
        if (_orderGrid.Columns["ThanhTien"] is { } thanhTien)
        {
            thanhTien.HeaderText = "Thành tiền";
            thanhTien.DefaultCellStyle.Format = "N0";
        }
    }

    private void ConfigureMenuColumns()
    {
        if (_menuGrid.Columns["MaMon"] is { } maMon) maMon.Visible = false;
        if (_menuGrid.Columns["MaDanhMuc"] is { } maDanhMuc) maDanhMuc.Visible = false;
        if (_menuGrid.Columns["TenMon"] is { } tenMon) tenMon.HeaderText = "Tên món";
        if (_menuGrid.Columns["TenDanhMuc"] is { } tenDanhMuc) tenDanhMuc.HeaderText = "Danh mục";
        if (_menuGrid.Columns["DonGia"] is { } donGia)
        {
            donGia.HeaderText = "Đơn giá";
            donGia.DefaultCellStyle.Format = "N0";
        }
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "Không thể hoàn tất thao tác", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private sealed record TableOption(int Id, string Name);
}
