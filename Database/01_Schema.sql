-- SQL Server 2017+ | Execute inside a dedicated, empty POS database.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

IF DB_ID() <= 4 OR EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
    THROW 50001, N'Run 01_Schema.sql in a dedicated empty database.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    CREATE SEQUENCE dbo.Seq_TaiKhoan AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_NhomHang AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_MatHang AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_NhaCungCap AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_CaBan AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_PhieuThuChi AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_HoaDon AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_DongBan AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_ThanhToanBan AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_PhieuTra AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_DongTra AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_KhoanHoan AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_PhieuNhap AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_DongNhap AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_TraNhaCungCap AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_PhieuKiem AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;
    CREATE SEQUENCE dbo.Seq_BienDongKho AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO CYCLE CACHE 32;

    CREATE TABLE dbo.VaiTro (
        maVaiTro varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        tenVaiTro nvarchar(150) NOT NULL,
        CONSTRAINT PK_VaiTro PRIMARY KEY (maVaiTro),
        CONSTRAINT UQ_VaiTro_tenVaiTro UNIQUE (tenVaiTro),
        CONSTRAINT CK_VaiTro_tenVaiTro CHECK (LEN(TRIM(tenVaiTro)) > 0)
    );

    CREATE TABLE dbo.Quyen (
        maQuyen varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        tenQuyen nvarchar(150) NOT NULL,
        CONSTRAINT PK_Quyen PRIMARY KEY (maQuyen)
    );

    CREATE TABLE dbo.QuyenVaiTro (
        maVaiTro varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maQuyen varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        CONSTRAINT PK_QuyenVaiTro PRIMARY KEY (maVaiTro, maQuyen),
        CONSTRAINT FK_QuyenVaiTro_maVaiTro FOREIGN KEY (maVaiTro)
            REFERENCES dbo.VaiTro (maVaiTro) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_QuyenVaiTro_maQuyen FOREIGN KEY (maQuyen)
            REFERENCES dbo.Quyen (maQuyen) ON DELETE NO ACTION ON UPDATE NO ACTION
    );

    CREATE TABLE dbo.TaiKhoan (
        maTaiKhoan varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_TaiKhoan_maTaiKhoan DEFAULT (CONCAT('TK-', NEXT VALUE FOR dbo.Seq_TaiKhoan)),
        maVaiTro varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        hoTen nvarchar(150) NOT NULL,
        tenDangNhap nvarchar(64) COLLATE Latin1_General_100_CI_AS NOT NULL,
        matKhauBam varchar(256) COLLATE Latin1_General_100_BIN2 NOT NULL,
        trangThai varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_TaiKhoan_trangThai DEFAULT ('HoatDong'),
        CONSTRAINT PK_TaiKhoan PRIMARY KEY (maTaiKhoan),
        CONSTRAINT UQ_TaiKhoan_tenDangNhap UNIQUE (tenDangNhap),
        CONSTRAINT FK_TaiKhoan_maVaiTro FOREIGN KEY (maVaiTro)
            REFERENCES dbo.VaiTro (maVaiTro) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_TaiKhoan_tenDangNhap CHECK (LEN(TRIM(tenDangNhap)) > 0),
        CONSTRAINT CK_TaiKhoan_TrangThai CHECK (
            trangThai IN ('HoatDong', 'BiKhoa')
        )
    );

    CREATE TABLE dbo.NhomHang (
        maNhom varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_NhomHang_maNhom DEFAULT (CONCAT('NH-', NEXT VALUE FOR dbo.Seq_NhomHang)),
        tenNhom nvarchar(150) NOT NULL,
        trangThai varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_NhomHang_trangThai DEFAULT ('HoatDong'),
        CONSTRAINT PK_NhomHang PRIMARY KEY (maNhom),
        CONSTRAINT UQ_NhomHang_tenNhom UNIQUE (tenNhom),
        CONSTRAINT CK_NhomHang_tenNhom CHECK (LEN(TRIM(tenNhom)) > 0),
        CONSTRAINT CK_NhomHang_TrangThai CHECK (
            trangThai IN ('HoatDong', 'NgungSuDung')
        )
    );

    CREATE TABLE dbo.MatHang (
        maHang varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_MatHang_maHang DEFAULT (CONCAT('MH-', NEXT VALUE FOR dbo.Seq_MatHang)),
        maNhom varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        tenHang nvarchar(150) NOT NULL,
        donViTinh nvarchar(32) NOT NULL,
        giaBanHienHanh decimal(19,0) NOT NULL,
        trangThai varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_MatHang_trangThai DEFAULT ('HoatDong'),
        CONSTRAINT PK_MatHang PRIMARY KEY (maHang),
        CONSTRAINT UQ_MatHang_tenHang UNIQUE (tenHang),
        CONSTRAINT FK_MatHang_maNhom FOREIGN KEY (maNhom)
            REFERENCES dbo.NhomHang (maNhom) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_MatHang_tenHang CHECK (LEN(TRIM(tenHang)) > 0),
        CONSTRAINT CK_MatHang_TrangThai CHECK (
            trangThai IN ('HoatDong', 'NgungSuDung')
        ),
        CONSTRAINT CK_MatHang_Gia CHECK (
            giaBanHienHanh >= 0
        )
    );

    CREATE TABLE dbo.NhaCungCap (
        maNhaCungCap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_NhaCungCap_maNhaCungCap DEFAULT (CONCAT('NCC-', NEXT VALUE FOR dbo.Seq_NhaCungCap)),
        tenNhaCungCap nvarchar(150) NOT NULL,
        thongTinLienHe nvarchar(500) NOT NULL,
        CONSTRAINT PK_NhaCungCap PRIMARY KEY (maNhaCungCap),
        CONSTRAINT UQ_NhaCungCap_tenNhaCungCap UNIQUE (tenNhaCungCap),
        CONSTRAINT CK_NhaCungCap_tenNhaCungCap CHECK (LEN(TRIM(tenNhaCungCap)) > 0)
    );

    CREATE TABLE dbo.CaBan (
        maCa varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_CaBan_maCa DEFAULT (CONCAT('CA-', NEXT VALUE FOR dbo.Seq_CaBan)),
        maNguoiMo varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiChot varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        lucMo datetime2(3) NOT NULL,
        tienMatDauCa decimal(19,0) NOT NULL,
        trangThai varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_CaBan_trangThai DEFAULT ('DangMo'),
        lucDong datetime2(3) NULL,
        tienDuKienKhiChot decimal(19,0) NULL,
        tienThucDem decimal(19,0) NULL,
        CONSTRAINT PK_CaBan PRIMARY KEY (maCa),
        CONSTRAINT FK_CaBan_maNguoiMo FOREIGN KEY (maNguoiMo)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_CaBan_maNguoiChot FOREIGN KEY (maNguoiChot)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_CaBan_Tien CHECK (
            tienMatDauCa >= 0 AND (tienThucDem IS NULL OR tienThucDem >= 0)
        ),
        CONSTRAINT CK_CaBan_VongDoi CHECK (
            (trangThai = 'DangMo' AND maNguoiChot IS NULL AND lucDong IS NULL
             AND tienDuKienKhiChot IS NULL AND tienThucDem IS NULL)
        OR (trangThai = 'DaDong' AND maNguoiChot IS NOT NULL AND maNguoiChot = maNguoiMo
             AND lucDong IS NOT NULL AND lucDong >= lucMo
             AND tienDuKienKhiChot IS NOT NULL AND tienThucDem IS NOT NULL)
        )
    );

    CREATE TABLE dbo.PhieuThuChi (
        maPhieuThuChi varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_PhieuThuChi_maPhieuThuChi DEFAULT (CONCAT('TC-', NEXT VALUE FOR dbo.Seq_PhieuThuChi)),
        maCa varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiLap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        lucGhi datetime2(3) NOT NULL,
        loaiThuChi varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL,
        nguonQuy varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL,
        phuongThuc varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL,
        soTien decimal(19,0) NOT NULL,
        lyDo nvarchar(500) NOT NULL,
        soChungTuKemTheo nvarchar(64) NULL,
        CONSTRAINT PK_PhieuThuChi PRIMARY KEY (maPhieuThuChi),
        CONSTRAINT FK_PhieuThuChi_maCa FOREIGN KEY (maCa)
            REFERENCES dbo.CaBan (maCa) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_PhieuThuChi_maNguoiLap FOREIGN KEY (maNguoiLap)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_PhieuThuChi_lyDo CHECK (LEN(TRIM(lyDo)) > 0),
        CONSTRAINT CK_PhieuThuChi_Loai CHECK (
            loaiThuChi IN ('Thu', 'Chi')
        ),
        CONSTRAINT CK_PhieuThuChi_Nguon CHECK (
            nguonQuy IN ('KetCa', 'QuyCuaHang')
        ),
        CONSTRAINT CK_PhieuThuChi_Tien CHECK (
            soTien > 0
        ),
        CONSTRAINT CK_PhieuThuChi_PhuongThuc CHECK (
            phuongThuc IN ('TienMat', 'ChuyenKhoan')
        )
    );

    CREATE TABLE dbo.HoaDon (
        maHoaDon varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_HoaDon_maHoaDon DEFAULT (CONCAT('HD-', NEXT VALUE FOR dbo.Seq_HoaDon)),
        maCa varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiLap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiHuy varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        maCaDao varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        lucLap datetime2(3) NOT NULL,
        lucXacNhan datetime2(3) NULL,
        trangThai varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_HoaDon_trangThai DEFAULT ('Nhap'),
        lucHuy datetime2(3) NULL,
        lyDoHuy nvarchar(500) NULL,
        CONSTRAINT PK_HoaDon PRIMARY KEY (maHoaDon),
        CONSTRAINT FK_HoaDon_maCa FOREIGN KEY (maCa)
            REFERENCES dbo.CaBan (maCa) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_HoaDon_maNguoiLap FOREIGN KEY (maNguoiLap)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_HoaDon_maNguoiHuy FOREIGN KEY (maNguoiHuy)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_HoaDon_maCaDao FOREIGN KEY (maCaDao)
            REFERENCES dbo.CaBan (maCa) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_HoaDon_VongDoi CHECK (
            (trangThai = 'Nhap' AND lucXacNhan IS NULL AND lucHuy IS NULL
             AND maNguoiHuy IS NULL AND lyDoHuy IS NULL AND maCaDao IS NULL)
        OR (trangThai = 'DaXacNhan' AND lucXacNhan IS NOT NULL AND lucHuy IS NULL
             AND maNguoiHuy IS NULL AND lyDoHuy IS NULL AND maCaDao IS NULL)
        OR (trangThai = 'DaHuy' AND lucHuy IS NOT NULL AND lucHuy >= lucLap
             AND maNguoiHuy IS NOT NULL AND lyDoHuy IS NOT NULL
             AND LEN(TRIM(lyDoHuy)) > 0
             AND (lucXacNhan IS NULL OR lucHuy >= lucXacNhan) AND (lucXacNhan IS NOT NULL OR maCaDao IS NULL))
        ),
        CONSTRAINT CK_HoaDon_ThoiDiem CHECK (
            lucXacNhan IS NULL OR lucXacNhan >= lucLap
        )
    );

    CREATE TABLE dbo.DongBan (
        maDongBan varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_DongBan_maDongBan DEFAULT (CONCAT('DB-', NEXT VALUE FOR dbo.Seq_DongBan)),
        maHoaDon varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maHang varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        soLuongBan int NOT NULL,
        donGiaThucBan decimal(19,0) NOT NULL,
        CONSTRAINT PK_DongBan PRIMARY KEY (maDongBan),
        CONSTRAINT FK_DongBan_maHoaDon FOREIGN KEY (maHoaDon)
            REFERENCES dbo.HoaDon (maHoaDon) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_DongBan_maHang FOREIGN KEY (maHang)
            REFERENCES dbo.MatHang (maHang) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_DongBan_LuongGia CHECK (
            soLuongBan > 0 AND donGiaThucBan >= 0
        )
    );

    CREATE TABLE dbo.ThanhToanBan (
        maThanhToan varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_ThanhToanBan_maThanhToan DEFAULT (CONCAT('TT-', NEXT VALUE FOR dbo.Seq_ThanhToanBan)),
        maHoaDon varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        phuongThuc varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL,
        soTienThanhToan decimal(19,0) NOT NULL,
        tienKhachDua decimal(19,0) NULL,
        CONSTRAINT PK_ThanhToanBan PRIMARY KEY (maThanhToan),
        CONSTRAINT FK_ThanhToanBan_maHoaDon FOREIGN KEY (maHoaDon)
            REFERENCES dbo.HoaDon (maHoaDon) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_ThanhToanBan_Tien CHECK (
            soTienThanhToan > 0 AND (
            (phuongThuc = 'TienMat' AND tienKhachDua IS NOT NULL AND tienKhachDua >= soTienThanhToan)
         OR (phuongThuc = 'ChuyenKhoan' AND tienKhachDua IS NULL))
        )
    );

    CREATE TABLE dbo.PhieuTra (
        maPhieuTra varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_PhieuTra_maPhieuTra DEFAULT (CONCAT('PT-', NEXT VALUE FOR dbo.Seq_PhieuTra)),
        maHoaDon varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maCaNhan varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiNhan varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        lucLap datetime2(3) NOT NULL,
        lucXacNhan datetime2(3) NULL,
        trangThai varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_PhieuTra_trangThai DEFAULT ('Nhap'),
        CONSTRAINT PK_PhieuTra PRIMARY KEY (maPhieuTra),
        CONSTRAINT FK_PhieuTra_maHoaDon FOREIGN KEY (maHoaDon)
            REFERENCES dbo.HoaDon (maHoaDon) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_PhieuTra_maCaNhan FOREIGN KEY (maCaNhan)
            REFERENCES dbo.CaBan (maCa) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_PhieuTra_maNguoiNhan FOREIGN KEY (maNguoiNhan)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_PhieuTra_VongDoi CHECK (
            (trangThai = 'Nhap' AND lucXacNhan IS NULL)
         OR (trangThai = 'DaXacNhan' AND lucXacNhan IS NOT NULL AND lucXacNhan >= lucLap)
        )
    );

    CREATE TABLE dbo.DongTra (
        maDongTra varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_DongTra_maDongTra DEFAULT (CONCAT('DT-', NEXT VALUE FOR dbo.Seq_DongTra)),
        maPhieuTra varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maDongBan varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        soLuongTra int NOT NULL,
        soLuongNhapLai int NOT NULL,
        CONSTRAINT PK_DongTra PRIMARY KEY (maDongTra),
        CONSTRAINT FK_DongTra_maPhieuTra FOREIGN KEY (maPhieuTra)
            REFERENCES dbo.PhieuTra (maPhieuTra) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_DongTra_maDongBan FOREIGN KEY (maDongBan)
            REFERENCES dbo.DongBan (maDongBan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_DongTra_Luong CHECK (
            soLuongTra > 0 AND soLuongNhapLai BETWEEN 0 AND soLuongTra
        )
    );

    CREATE TABLE dbo.KhoanHoan (
        maKhoanHoan varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_KhoanHoan_maKhoanHoan DEFAULT (CONCAT('KH-', NEXT VALUE FOR dbo.Seq_KhoanHoan)),
        maPhieuTra varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        phuongThuc varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL,
        soTienHoan decimal(19,0) NOT NULL,
        CONSTRAINT PK_KhoanHoan PRIMARY KEY (maKhoanHoan),
        CONSTRAINT FK_KhoanHoan_maPhieuTra FOREIGN KEY (maPhieuTra)
            REFERENCES dbo.PhieuTra (maPhieuTra) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_KhoanHoan_Tien CHECK (
            soTienHoan > 0
        ),
        CONSTRAINT CK_KhoanHoan_PhuongThuc CHECK (
            phuongThuc IN ('TienMat', 'ChuyenKhoan')
        )
    );

    CREATE TABLE dbo.PhieuNhap (
        maPhieuNhap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_PhieuNhap_maPhieuNhap DEFAULT (CONCAT('PN-', NEXT VALUE FOR dbo.Seq_PhieuNhap)),
        maNhaCungCap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiLap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiHuy varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        lucLap datetime2(3) NOT NULL,
        lucXacNhan datetime2(3) NULL,
        trangThai varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_PhieuNhap_trangThai DEFAULT ('Nhap'),
        lucHuy datetime2(3) NULL,
        lyDoHuy nvarchar(500) NULL,
        hanThanhToan date NULL,
        CONSTRAINT PK_PhieuNhap PRIMARY KEY (maPhieuNhap),
        CONSTRAINT FK_PhieuNhap_maNhaCungCap FOREIGN KEY (maNhaCungCap)
            REFERENCES dbo.NhaCungCap (maNhaCungCap) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_PhieuNhap_maNguoiLap FOREIGN KEY (maNguoiLap)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_PhieuNhap_maNguoiHuy FOREIGN KEY (maNguoiHuy)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_PhieuNhap_VongDoi CHECK (
            (trangThai = 'Nhap' AND lucXacNhan IS NULL AND lucHuy IS NULL
             AND maNguoiHuy IS NULL AND lyDoHuy IS NULL)
        OR (trangThai = 'DaXacNhan' AND lucXacNhan IS NOT NULL AND lucHuy IS NULL
             AND maNguoiHuy IS NULL AND lyDoHuy IS NULL)
        OR (trangThai = 'DaHuy' AND lucHuy IS NOT NULL AND lucHuy >= lucLap
             AND maNguoiHuy IS NOT NULL AND lyDoHuy IS NOT NULL
             AND LEN(TRIM(lyDoHuy)) > 0
             AND (lucXacNhan IS NULL OR lucHuy >= lucXacNhan))
        ),
        CONSTRAINT CK_PhieuNhap_ThoiDiem CHECK (
            lucXacNhan IS NULL OR lucXacNhan >= lucLap
        )
    );

    CREATE TABLE dbo.DongNhap (
        maDongNhap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_DongNhap_maDongNhap DEFAULT (CONCAT('DN-', NEXT VALUE FOR dbo.Seq_DongNhap)),
        maPhieuNhap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maHang varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        soLuongNhap int NOT NULL,
        donGiaNhap decimal(19,0) NOT NULL,
        CONSTRAINT PK_DongNhap PRIMARY KEY (maDongNhap),
        CONSTRAINT FK_DongNhap_maPhieuNhap FOREIGN KEY (maPhieuNhap)
            REFERENCES dbo.PhieuNhap (maPhieuNhap) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_DongNhap_maHang FOREIGN KEY (maHang)
            REFERENCES dbo.MatHang (maHang) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_DongNhap_LuongGia CHECK (
            soLuongNhap > 0 AND donGiaNhap >= 0
        )
    );

    CREATE TABLE dbo.TraNhaCungCap (
        maKhoanTra varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_TraNhaCungCap_maKhoanTra DEFAULT (CONCAT('TNCC-', NEXT VALUE FOR dbo.Seq_TraNhaCungCap)),
        maPhieuNhap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiGhi varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maCaChi varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        lucTra datetime2(3) NOT NULL,
        soTienTra decimal(19,0) NOT NULL,
        phuongThuc varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL,
        nguonChi varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL,
        CONSTRAINT PK_TraNhaCungCap PRIMARY KEY (maKhoanTra),
        CONSTRAINT FK_TraNhaCungCap_maPhieuNhap FOREIGN KEY (maPhieuNhap)
            REFERENCES dbo.PhieuNhap (maPhieuNhap) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_TraNhaCungCap_maNguoiGhi FOREIGN KEY (maNguoiGhi)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_TraNhaCungCap_maCaChi FOREIGN KEY (maCaChi)
            REFERENCES dbo.CaBan (maCa) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_TraNhaCungCap_Tien CHECK (
            soTienTra > 0
        ),
        CONSTRAINT CK_TraNhaCungCap_PhuongThuc CHECK (
            phuongThuc IN ('TienMat', 'ChuyenKhoan')
        ),
        CONSTRAINT CK_TraNhaCungCap_Nguon CHECK (
            (nguonChi = 'KetCa' AND phuongThuc = 'TienMat' AND maCaChi IS NOT NULL)
        OR (nguonChi = 'QuyCuaHang' AND maCaChi IS NULL)
        )
    );

    CREATE TABLE dbo.PhieuKiem (
        maPhieuKiem varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_PhieuKiem_maPhieuKiem DEFAULT (CONCAT('PK-', NEXT VALUE FOR dbo.Seq_PhieuKiem)),
        maNguoiLap varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maNguoiDuyet varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        mocKiem datetime2(3) NOT NULL,
        lucHoanTat datetime2(3) NULL,
        trangThai varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_PhieuKiem_trangThai DEFAULT ('DangKiem'),
        CONSTRAINT PK_PhieuKiem PRIMARY KEY (maPhieuKiem),
        CONSTRAINT FK_PhieuKiem_maNguoiLap FOREIGN KEY (maNguoiLap)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_PhieuKiem_maNguoiDuyet FOREIGN KEY (maNguoiDuyet)
            REFERENCES dbo.TaiKhoan (maTaiKhoan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_PhieuKiem_VongDoi CHECK (
            (trangThai IN ('DangKiem', 'ChoDuyet', 'DaHuy') AND maNguoiDuyet IS NULL AND lucHoanTat IS NULL)
        OR (trangThai = 'HoanTat' AND maNguoiDuyet IS NOT NULL
             AND lucHoanTat IS NOT NULL AND lucHoanTat >= mocKiem)
        )
    );

    CREATE TABLE dbo.DongKiem (
        maPhieuKiem varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maHang varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        tonSoTaiMoc int NOT NULL,
        soLuongDem int NULL,
        giaiTrinh nvarchar(500) NULL,
        lucDem datetime2(3) NULL,
        CONSTRAINT PK_DongKiem PRIMARY KEY (maPhieuKiem, maHang),
        CONSTRAINT FK_DongKiem_maPhieuKiem FOREIGN KEY (maPhieuKiem)
            REFERENCES dbo.PhieuKiem (maPhieuKiem) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_DongKiem_maHang FOREIGN KEY (maHang)
            REFERENCES dbo.MatHang (maHang) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_DongKiem_LucDem CHECK (lucDem IS NULL OR soLuongDem IS NOT NULL),
        CONSTRAINT CK_DongKiem_Luong CHECK (
            tonSoTaiMoc >= 0 AND (soLuongDem IS NULL OR soLuongDem >= 0)
        )
    );

    CREATE TABLE dbo.BienDongKho (
        maBienDong varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT DF_BienDongKho_maBienDong DEFAULT (CONCAT('BD-', NEXT VALUE FOR dbo.Seq_BienDongKho)),
        maHang varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
        maDongBan varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        maDongTra varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        maDongNhap varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        maPhieuKiem varchar(32) COLLATE Latin1_General_100_BIN2 NULL,
        loaiSuKien varchar(24) COLLATE Latin1_General_100_BIN2 NOT NULL,
        soLuongThayDoi int NOT NULL,
        thoiDiemGhi datetime2(3) NOT NULL,
        khoaSuKien varchar(256) COLLATE Latin1_General_100_BIN2 NOT NULL,
        CONSTRAINT PK_BienDongKho PRIMARY KEY (maBienDong),
        CONSTRAINT UQ_BienDongKho_khoaSuKien UNIQUE (khoaSuKien),
        CONSTRAINT FK_BienDongKho_maHang FOREIGN KEY (maHang)
            REFERENCES dbo.MatHang (maHang) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_BienDongKho_maDongBan FOREIGN KEY (maDongBan)
            REFERENCES dbo.DongBan (maDongBan) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_BienDongKho_maDongTra FOREIGN KEY (maDongTra)
            REFERENCES dbo.DongTra (maDongTra) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_BienDongKho_maDongNhap FOREIGN KEY (maDongNhap)
            REFERENCES dbo.DongNhap (maDongNhap) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT FK_BienDongKho_maPhieuKiem_maHang FOREIGN KEY (maPhieuKiem, maHang)
            REFERENCES dbo.DongKiem (maPhieuKiem, maHang) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT CK_BienDongKho_MotNguon CHECK (
            (CASE WHEN maDongBan IS NULL THEN 0 ELSE 1 END
          + CASE WHEN maDongTra IS NULL THEN 0 ELSE 1 END
          + CASE WHEN maDongNhap IS NULL THEN 0 ELSE 1 END
          + CASE WHEN maPhieuKiem IS NULL THEN 0 ELSE 1 END) = 1
        ),
        CONSTRAINT CK_BienDongKho_LoaiVaDau CHECK (
            (loaiSuKien = 'XuatBan' AND maDongBan IS NOT NULL AND soLuongThayDoi < 0)
        OR (loaiSuKien = 'DaoXuatBan' AND maDongBan IS NOT NULL AND soLuongThayDoi > 0)
        OR (loaiSuKien = 'NhapTraHang' AND maDongTra IS NOT NULL AND soLuongThayDoi > 0)
        OR (loaiSuKien = 'NhapKho' AND maDongNhap IS NOT NULL AND soLuongThayDoi > 0)
        OR (loaiSuKien = 'DaoNhapKho' AND maDongNhap IS NOT NULL AND soLuongThayDoi < 0)
        OR (loaiSuKien = 'DieuChinhKiemKho' AND maPhieuKiem IS NOT NULL AND soLuongThayDoi <> 0)
        ),
        CONSTRAINT CK_BienDongKho_KhoaSuKien CHECK (
            khoaSuKien = CASE WHEN maPhieuKiem IS NOT NULL
            THEN CONCAT(loaiSuKien, ':', maPhieuKiem, ':', maHang)
            ELSE CONCAT(loaiSuKien, ':', COALESCE(maDongBan, maDongTra, maDongNhap))
        END
        )
    );

    CREATE UNIQUE INDEX UX_CaBan_NguoiMo_DangMo ON dbo.CaBan (maNguoiMo)
        WHERE trangThai = 'DangMo';
    CREATE INDEX IX_HoaDon_Ca_XacNhan ON dbo.HoaDon (maCa, lucXacNhan);
    CREATE INDEX IX_HoaDon_CaDao_Huy ON dbo.HoaDon (maCaDao, lucHuy);
    CREATE INDEX IX_DongBan_HoaDon ON dbo.DongBan (maHoaDon);
    CREATE INDEX IX_ThanhToanBan_HoaDon ON dbo.ThanhToanBan (maHoaDon);
    CREATE INDEX IX_PhieuTra_HoaDon ON dbo.PhieuTra (maHoaDon, trangThai);
    CREATE INDEX IX_PhieuTra_CaNhan ON dbo.PhieuTra (maCaNhan, lucXacNhan);
    CREATE INDEX IX_DongTra_PhieuTra ON dbo.DongTra (maPhieuTra);
    CREATE INDEX IX_DongTra_DongBan ON dbo.DongTra (maDongBan);
    CREATE INDEX IX_KhoanHoan_PhieuTra ON dbo.KhoanHoan (maPhieuTra);
    CREATE INDEX IX_PhieuThuChi_Ca ON dbo.PhieuThuChi (maCa, lucGhi);
    CREATE INDEX IX_PhieuNhap_NCC_XacNhan ON dbo.PhieuNhap (maNhaCungCap, lucXacNhan);
    CREATE INDEX IX_DongNhap_PhieuNhap ON dbo.DongNhap (maPhieuNhap);
    CREATE INDEX IX_TraNhaCungCap_PhieuNhap ON dbo.TraNhaCungCap (maPhieuNhap, lucTra);
    CREATE INDEX IX_TraNhaCungCap_CaChi ON dbo.TraNhaCungCap (maCaChi, lucTra);
    CREATE INDEX IX_DongKiem_Hang ON dbo.DongKiem (maHang, maPhieuKiem) INCLUDE (lucDem);
    CREATE INDEX IX_BienDongKho_Hang_ThoiDiem ON dbo.BienDongKho (maHang, thoiDiemGhi, maBienDong)
        INCLUDE (soLuongThayDoi, loaiSuKien);

    -- Assign the application's database user to this role during deployment.
    CREATE ROLE pos_app AUTHORIZATION dbo;
    DENY UPDATE, DELETE ON dbo.BienDongKho TO pos_app;

    -- The simple source FKs establish existence. This insert trigger also checks the SKU.
    EXEC sys.sp_executesql N'
        CREATE TRIGGER dbo.TR_BienDongKho_KiemHangNguon
        ON dbo.BienDongKho
        AFTER INSERT
        AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS (
                SELECT 1
                FROM inserted AS i
                LEFT JOIN dbo.DongBan AS ban ON ban.maDongBan = i.maDongBan
                LEFT JOIN dbo.DongTra AS tra ON tra.maDongTra = i.maDongTra
                LEFT JOIN dbo.DongBan AS goc ON goc.maDongBan = tra.maDongBan
                LEFT JOIN dbo.DongNhap AS nhap ON nhap.maDongNhap = i.maDongNhap
                WHERE (i.maDongBan IS NOT NULL AND ban.maHang <> i.maHang)
                   OR (i.maDongTra IS NOT NULL AND goc.maHang <> i.maHang)
                   OR (i.maDongNhap IS NOT NULL AND nhap.maHang <> i.maHang)
            )
                THROW 50002, N''Stock event item differs from its source item.'', 1;
        END;';
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
