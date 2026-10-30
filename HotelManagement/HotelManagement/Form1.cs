using System.Data;
using HotelManagement.BUS;
using HotelManagement.DAL;
using HotelManagement.DTO;

namespace HotelManagement;

public partial class Form1 : Form
{
    private readonly HotelService hotelService;
    private readonly DataTable roomTable = new();
    private readonly ComboBox roomTypeFilter = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox statusFilter = new() { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView roomsGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly DataGridView bookingsGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox fullNameText = new();
    private readonly TextBox citizenIdText = new();
    private readonly TextBox phoneText = new();
    private readonly TextBox roomText = new();
    private readonly TextBox quantityText = new() { Text = "1" };
    private readonly ComboBox serviceCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly Label connectionStatus = new() { AutoSize = true, ForeColor = Color.DarkGreen };

    public Form1()
    {
        InitializeComponent();
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        roomsGrid.Font = Font;
        bookingsGrid.Font = Font;
        roomsGrid.DefaultCellStyle.Font = Font;
        bookingsGrid.DefaultCellStyle.Font = Font;
        roomsGrid.ColumnHeadersDefaultCellStyle.Font = Font;
        bookingsGrid.ColumnHeadersDefaultCellStyle.Font = Font;
        hotelService = new HotelService(new HotelRepository(new DbConnectionFactory()));
        BuildLayout();
        Load += async (_, _) => await LoadDataAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(10), Font = Font };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        Controls.Add(root);

        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, WrapContents = false, Padding = new Padding(3, 5, 3, 3), Font = Font };
        filters.Controls.Add(new Label { Text = "Loại phòng:", AutoSize = true, Margin = new Padding(3, 8, 3, 3), Font = Font });
        roomTypeFilter.Items.AddRange(new object[] { "Tất cả", "Đơn", "Đôi", "VIP" });
        roomTypeFilter.SelectedIndex = 0;
        roomTypeFilter.Font = Font;
        filters.Controls.Add(roomTypeFilter);

        filters.Controls.Add(new Label { Text = "Trạng thái:", AutoSize = true, Margin = new Padding(15, 8, 3, 3), Font = Font });
        statusFilter.Items.AddRange(new object[] { "Tất cả", "Trống", "Đang ở", "Đang dọn dẹp" });
        statusFilter.SelectedIndex = 0;
        statusFilter.Font = Font;
        filters.Controls.Add(statusFilter);

        var refresh = new Button { Text = "Tải lại", AutoSize = true, Font = Font };
        refresh.Click += async (_, _) => await LoadDataAsync();
        filters.Controls.Add(refresh);

        root.Controls.Add(filters, 0, 0);
        roomTypeFilter.SelectedIndexChanged += (_, _) => ApplyRoomFilter();
        statusFilter.SelectedIndexChanged += (_, _) => ApplyRoomFilter();
        root.Controls.Add(roomsGrid, 0, 1);

        var operations = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Font = Font };
        operations.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        operations.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        operations.Controls.Add(BuildCheckInPanel(), 0, 0);
        operations.Controls.Add(BuildServicePanel(), 1, 0);
        root.Controls.Add(operations, 0, 2);

        connectionStatus.Font = Font;
        root.Controls.Add(connectionStatus, 0, 3);
    }

    private Control BuildCheckInPanel()
    {
        var group = new GroupBox { Text = "Check-in / Nhận phòng", Dock = DockStyle.Fill, Padding = new Padding(8), Font = Font };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Font = Font };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        fullNameText.Font = Font;
        citizenIdText.Font = Font;
        phoneText.Font = Font;
        roomText.Font = Font;

        AddField(panel, "Họ tên:", fullNameText, 0);
        AddField(panel, "CCCD:", citizenIdText, 1);
        AddField(panel, "Điện thoại:", phoneText, 2);
        AddField(panel, "Phòng:", roomText, 3);

        var checkIn = new Button { Text = "Nhận phòng", AutoSize = true, Font = Font };
        checkIn.Click += async (_, _) => await CheckInAsync();
        panel.Controls.Add(checkIn, 1, 4);

        group.Controls.Add(panel);
        return group;
    }

    private Control BuildServicePanel()
    {
        var group = new GroupBox { Text = "Dịch vụ / Check-out", Dock = DockStyle.Fill, Padding = new Padding(8), Font = Font };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(3), Font = Font };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        panel.Controls.Add(new Label { Text = "Phiếu đang ở:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = Font }, 0, 0);
        panel.Controls.Add(bookingsGrid, 1, 0);

        panel.Controls.Add(new Label { Text = "Dịch vụ:", AutoSize = true, Anchor = AnchorStyles.Left, Font = Font }, 0, 1);
        serviceCombo.Dock = DockStyle.Fill;
        serviceCombo.Font = Font;
        panel.Controls.Add(serviceCombo, 1, 1);

        panel.Controls.Add(new Label { Text = "Số lượng:", AutoSize = true, Anchor = AnchorStyles.Left, Font = Font }, 0, 2);
        quantityText.Dock = DockStyle.Fill;
        quantityText.Font = Font;
        panel.Controls.Add(quantityText, 1, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Font = Font };
        var add = new Button { Text = "Gọi dịch vụ", AutoSize = true, Font = Font };
        add.Click += async (_, _) => await AddServiceAsync();
        var checkout = new Button { Text = "Check-out", AutoSize = true, Font = Font };
        checkout.Click += async (_, _) => await CheckoutAsync();
        buttons.Controls.Add(add);
        buttons.Controls.Add(checkout);
        panel.Controls.Add(buttons, 1, 3);

        group.Controls.Add(panel);
        return group;
    }

    private void AddField(TableLayoutPanel panel, string label, Control control, int row)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 7, 3, 3), Font = Font }, 0, row);
        control.Dock = DockStyle.Fill;
        panel.Controls.Add(control, 1, row);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var rooms = await hotelService.GetRoomsAsync();
            roomTable.Clear();
            roomTable.Merge(RoomFilterService.ToDataTable(rooms));
            roomsGrid.DataSource = roomTable;
            ApplyRoomFilter();

            bookingsGrid.DataSource = (await hotelService.GetActiveBookingsAsync()).ToList();
            serviceCombo.DataSource = (await hotelService.GetServicesAsync()).ToList();
            serviceCombo.DisplayMember = nameof(ServiceDto.Name);
            serviceCombo.ValueMember = nameof(ServiceDto.Id);
            connectionStatus.Text = "Đã kết nối CSDL";

            FormatGrids();
        }
        catch (Exception ex)
        {
            connectionStatus.Text = "Chưa kết nối được CSDL";
            MessageBox.Show(ex.Message, "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FormatGrids()
    {
        if (roomsGrid.Columns["SoPhong"] is { } roomCol) roomCol.HeaderText = "Số phòng";
        if (roomsGrid.Columns["LoaiPhong"] is { } typeCol) typeCol.HeaderText = "Loại phòng";
        if (roomsGrid.Columns["GiaTheoGio"] is { } priceCol)
        {
            priceCol.HeaderText = "Giá / giờ (VNĐ)";
            priceCol.DefaultCellStyle.Format = "N0";
        }
        if (roomsGrid.Columns["TrangThai"] is { } statusCol) statusCol.HeaderText = "Trạng thái";

        if (bookingsGrid.Columns["Id"] is { } idCol) idCol.HeaderText = "Mã phiếu";
        if (bookingsGrid.Columns["RoomNumber"] is { } bRoomCol) bRoomCol.HeaderText = "Số phòng";
        if (bookingsGrid.Columns["CustomerId"] is { } custIdCol) custIdCol.Visible = false;
        if (bookingsGrid.Columns["CustomerName"] is { } custNameCol) custNameCol.HeaderText = "Tên khách";
        if (bookingsGrid.Columns["CitizenId"] is { } citizenCol) citizenCol.HeaderText = "CCCD";
        if (bookingsGrid.Columns["CheckInAt"] is { } checkInCol)
        {
            checkInCol.HeaderText = "Thời gian nhận";
            checkInCol.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }
    }

    private void ApplyRoomFilter()
    {
        if (roomTable.Columns.Count == 0) return;
        var type = roomTypeFilter.SelectedIndex <= 0 ? null : roomTypeFilter.Text;
        var status = statusFilter.SelectedIndex <= 0 ? null : statusFilter.Text;
        roomsGrid.DataSource = RoomFilterService.Filter(roomTable, type, status);
        FormatGrids();
    }

    private async Task CheckInAsync()
    {
        try
        {
            var customer = await hotelService.GetOrCreateCustomerAsync(fullNameText.Text, citizenIdText.Text, phoneText.Text);
            var id = await hotelService.CheckInAsync(roomText.Text, customer);
            MessageBox.Show($"Đã nhận phòng thành công. Mã phiếu: {id}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Check-in thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task AddServiceAsync()
    {
        if (bookingsGrid.CurrentRow?.DataBoundItem is not BookingDto booking || serviceCombo.SelectedItem is not ServiceDto service || !int.TryParse(quantityText.Text, out var quantity))
        {
            MessageBox.Show("Vui lòng chọn phiếu, dịch vụ và số lượng hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await hotelService.AddServiceUsageAsync(booking.Id, service.Id, quantity);
            MessageBox.Show("Đã ghi nhận dịch vụ thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gọi dịch vụ thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task CheckoutAsync()
    {
        if (bookingsGrid.CurrentRow?.DataBoundItem is not BookingDto booking)
        {
            MessageBox.Show("Vui lòng chọn phiếu cần thanh toán.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var result = await hotelService.CheckoutAsync(booking.Id);
            MessageBox.Show($"Tổng thanh toán: {result.GrandTotal:N0} VNĐ\n- Tiền phòng: {result.RoomTotal:N0} VNĐ\n- Tiền dịch vụ: {result.ServiceTotal:N0} VNĐ", "Hóa đơn thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Check-out thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
