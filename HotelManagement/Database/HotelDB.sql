IF DB_ID(N'HotelDB') IS NULL
BEGIN
	CREATE DATABASE HotelDB;
END
GO

USE HotelDB;
GO

IF OBJECT_ID(N'dbo.SuDungDichVu', N'U') IS NOT NULL DROP TABLE dbo.SuDungDichVu;
IF OBJECT_ID(N'dbo.PhieuDatPhong', N'U') IS NOT NULL DROP TABLE dbo.PhieuDatPhong;
IF OBJECT_ID(N'dbo.DichVu', N'U') IS NOT NULL DROP TABLE dbo.DichVu;
IF OBJECT_ID(N'dbo.KhachHang', N'U') IS NOT NULL DROP TABLE dbo.KhachHang;
IF OBJECT_ID(N'dbo.Phong', N'U') IS NOT NULL DROP TABLE dbo.Phong;
GO

CREATE TABLE dbo.Phong
(
	SoPhong varchar(10) NOT NULL CONSTRAINT PK_Phong PRIMARY KEY,
	LoaiPhong nvarchar(50) NOT NULL,
	GiaTheoGio decimal(18, 2) NOT NULL CONSTRAINT CK_Phong_Gia CHECK (GiaTheoGio >= 0),
	TrangThai nvarchar(30) NOT NULL CONSTRAINT DF_Phong_TrangThai DEFAULT N'Trống'
);

CREATE TABLE dbo.KhachHang
(
	MaKH int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_KhachHang PRIMARY KEY,
	HoTen nvarchar(100) NOT NULL,
	CCCD varchar(20) NOT NULL CONSTRAINT UQ_KhachHang_CCCD UNIQUE,
	SoDienThoai varchar(15) NULL
);

CREATE TABLE dbo.DichVu
(
	MaDV int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_DichVu PRIMARY KEY,
	TenDV nvarchar(100) NOT NULL,
	DonGia decimal(18, 2) NOT NULL CONSTRAINT CK_DichVu_DonGia CHECK (DonGia >= 0)
);

CREATE TABLE dbo.PhieuDatPhong
(
	MaPhieu int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_PhieuDatPhong PRIMARY KEY,
	SoPhong varchar(10) NOT NULL,
	MaKH int NOT NULL,
	NgayNhanPhong datetime2 NOT NULL,
	NgayTraPhong datetime2 NULL,
	TongTienThanhToan decimal(18, 2) NULL,
	CONSTRAINT FK_PhieuDatPhong_Phong FOREIGN KEY (SoPhong) REFERENCES dbo.Phong(SoPhong),
	CONSTRAINT FK_PhieuDatPhong_KhachHang FOREIGN KEY (MaKH) REFERENCES dbo.KhachHang(MaKH)
);

CREATE TABLE dbo.SuDungDichVu
(
	MaPhieu int NOT NULL,
	MaDV int NOT NULL,
	SoLuong int NOT NULL CONSTRAINT CK_SuDungDichVu_SoLuong CHECK (SoLuong > 0),
	ThoiGian datetime2 NOT NULL CONSTRAINT DF_SuDungDichVu_ThoiGian DEFAULT SYSUTCDATETIME(),
	CONSTRAINT PK_SuDungDichVu PRIMARY KEY (MaPhieu, MaDV, ThoiGian),
	CONSTRAINT FK_SuDungDichVu_Phieu FOREIGN KEY (MaPhieu) REFERENCES dbo.PhieuDatPhong(MaPhieu),
	CONSTRAINT FK_SuDungDichVu_DichVu FOREIGN KEY (MaDV) REFERENCES dbo.DichVu(MaDV)
);
GO

CREATE INDEX IX_Phong_Loc ON dbo.Phong(LoaiPhong, TrangThai);
CREATE INDEX IX_PhieuDatPhong_DangO ON dbo.PhieuDatPhong(SoPhong, NgayTraPhong);
GO

INSERT INTO dbo.Phong (SoPhong, LoaiPhong, GiaTheoGio, TrangThai) VALUES
('P101', N'Đơn', 100000, N'Trống'),
('P102', N'Đơn', 100000, N'Trống'),
('P201', N'Đôi', 180000, N'Trống'),
('P202', N'Đôi', 180000, N'Trống'),
('P301', N'VIP', 350000, N'Trống');

INSERT INTO dbo.DichVu (TenDV, DonGia) VALUES
(N'Nước uống', 20000),
(N'Giặt là', 50000),
(N'Đồ ăn', 100000);
GO