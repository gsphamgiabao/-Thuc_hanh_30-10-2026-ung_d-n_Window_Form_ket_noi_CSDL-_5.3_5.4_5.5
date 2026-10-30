IF DB_ID(N'InventoryDB') IS NULL
	CREATE DATABASE InventoryDB;
GO

USE InventoryDB;
GO

IF OBJECT_ID(N'dbo.ChiTietPhieuKho', N'U') IS NOT NULL DROP TABLE dbo.ChiTietPhieuKho;
IF OBJECT_ID(N'dbo.PhieuKho', N'U') IS NOT NULL DROP TABLE dbo.PhieuKho;
IF OBJECT_ID(N'dbo.VatTu', N'U') IS NOT NULL DROP TABLE dbo.VatTu;
IF OBJECT_ID(N'dbo.NhaCungCap', N'U') IS NOT NULL DROP TABLE dbo.NhaCungCap;
GO

CREATE TABLE dbo.NhaCungCap
(
	MaNCC int IDENTITY(1,1) NOT NULL CONSTRAINT PK_NhaCungCap PRIMARY KEY,
	TenNCC nvarchar(150) NOT NULL,
	DienThoai varchar(15) NULL CONSTRAINT UQ_NhaCungCap_DienThoai UNIQUE,
	DiaChi nvarchar(255) NULL
);

CREATE TABLE dbo.VatTu
(
	MaVatTu varchar(20) NOT NULL CONSTRAINT PK_VatTu PRIMARY KEY,
	TenVatTu nvarchar(150) NOT NULL,
	DVT nvarchar(20) NULL,
	SoLuongTon decimal(18,2) NOT NULL CONSTRAINT DF_VatTu_SoLuongTon DEFAULT 0,
	DonGiaTon decimal(18,2) NOT NULL CONSTRAINT DF_VatTu_DonGiaTon DEFAULT 0,
	CONSTRAINT CK_VatTu_SoLuongTon CHECK (SoLuongTon >= 0)
);

CREATE TABLE dbo.PhieuKho
(
	MaPhieu int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhieuKho PRIMARY KEY,
	LoaiPhieu nvarchar(10) NOT NULL,
	NgayLap datetime NOT NULL CONSTRAINT DF_PhieuKho_NgayLap DEFAULT GETDATE(),
	MaNCC int NULL,
	TongTien decimal(18,2) NOT NULL CONSTRAINT DF_PhieuKho_TongTien DEFAULT 0,
	CONSTRAINT CK_PhieuKho_LoaiPhieu CHECK (LoaiPhieu IN (N'NHAP', N'XUAT')),
	CONSTRAINT FK_PhieuKho_NhaCungCap FOREIGN KEY (MaNCC) REFERENCES dbo.NhaCungCap(MaNCC)
);

CREATE TABLE dbo.ChiTietPhieuKho
(
	MaPhieu int NOT NULL,
	MaVatTu varchar(20) NOT NULL,
	SoLuong decimal(18,2) NOT NULL,
	DonGia decimal(18,2) NOT NULL CONSTRAINT DF_ChiTietPhieuKho_DonGia DEFAULT 0,
	CONSTRAINT PK_ChiTietPhieuKho PRIMARY KEY (MaPhieu, MaVatTu),
	CONSTRAINT CK_ChiTietPhieuKho_SoLuong CHECK (SoLuong > 0),
	CONSTRAINT CK_ChiTietPhieuKho_DonGia CHECK (DonGia >= 0),
	CONSTRAINT FK_ChiTietPhieuKho_PhieuKho FOREIGN KEY (MaPhieu) REFERENCES dbo.PhieuKho(MaPhieu),
	CONSTRAINT FK_ChiTietPhieuKho_VatTu FOREIGN KEY (MaVatTu) REFERENCES dbo.VatTu(MaVatTu)
);
GO

INSERT INTO dbo.NhaCungCap (TenNCC, DienThoai, DiaChi)
VALUES
(N'Công ty Vật tư Minh Phát', '0901000001', N'Hà Nội'),
(N'Công ty Thiết bị An Khang', '0901000002', N'Hải Phòng'),
(N'Nhà cung cấp Đại Thành', '0901000003', N'Quảng Ninh'),
(N'Công ty Nguyên liệu Việt', '0901000004', N'Đà Nẵng'),
(N'Công ty Cơ khí Hoàng Long', '0901000005', N'Hồ Chí Minh'),
(N'Công ty Vật liệu Đông Á', '0901000006', N'Bình Dương'),
(N'Nhà cung cấp Thành Công', '0901000007', N'Đồng Nai'),
(N'Công ty Kỹ thuật Nam Việt', '0901000008', N'Cần Thơ'),
(N'Công ty Phú Quý', '0901000009', N'Bắc Ninh'),
(N'Công ty Tân Tiến', '0901000010', N'Hưng Yên');

INSERT INTO dbo.VatTu (MaVatTu, TenVatTu, DVT, SoLuongTon, DonGiaTon)
VALUES
('VT001', N'Thép tấm ly 5mm', N'Tấm', 120.00, 185000.00),
('VT002', N'Bu lông M10', N'Cái', 850.00, 3500.00),
('VT003', N'Đai ốc M10', N'Cái', 900.00, 1800.00),
('VT004', N'Sơn chống gỉ đỏ', N'Thùng', 35.00, 1250000.00),
('VT005', N'Dầu máy công nghiệp', N'Can', 28.00, 780000.00),
('VT006', N'Găng tay bảo hộ', N'Đôi', 450.00, 12000.00),
('VT007', N'Kính bảo hộ lao động', N'Cái', 75.00, 45000.00),
('VT008', N'Dây điện Cadivi 2.5mm', N'Mét', 600.00, 16500.00),
('VT009', N'Ống thép mạ kẽm', N'Mét', 90.00, 95000.00),
('VT010', N'Vòng bi 6204', N'Cái', 18.00, 135000.00);
GO
