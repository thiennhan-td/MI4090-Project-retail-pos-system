-- Seed dataset: 235 linked rows across 21 tables, from September to November 2026.
-- All datetime2 literals are UTC. S- identifiers reserve a namespace apart from sequence defaults.
-- DEMO_DISABLED deliberately disables login until the application provisions a password.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;


IF OBJECT_ID(N'dbo.BienDongKho', N'U') IS NULL
    THROW 50010, N'Run 01_Schema.sql in this database first.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'RetailCorePos.Seed',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 5000;
    IF @LockResult < 0
        THROW 50011, N'Unable to acquire the POS seed lock.', 1;

    -- Use an empty schema. A repeated run leaves the existing dataset untouched.
    IF EXISTS (SELECT 1 FROM dbo.VaiTro)
        OR EXISTS (SELECT 1 FROM dbo.Quyen)
        OR EXISTS (SELECT 1 FROM dbo.QuyenVaiTro)
        OR EXISTS (SELECT 1 FROM dbo.TaiKhoan)
        OR EXISTS (SELECT 1 FROM dbo.NhomHang)
        OR EXISTS (SELECT 1 FROM dbo.MatHang)
        OR EXISTS (SELECT 1 FROM dbo.NhaCungCap)
        OR EXISTS (SELECT 1 FROM dbo.CaBan)
        OR EXISTS (SELECT 1 FROM dbo.PhieuThuChi)
        OR EXISTS (SELECT 1 FROM dbo.HoaDon)
        OR EXISTS (SELECT 1 FROM dbo.DongBan)
        OR EXISTS (SELECT 1 FROM dbo.ThanhToanBan)
        OR EXISTS (SELECT 1 FROM dbo.PhieuTra)
        OR EXISTS (SELECT 1 FROM dbo.DongTra)
        OR EXISTS (SELECT 1 FROM dbo.KhoanHoan)
        OR EXISTS (SELECT 1 FROM dbo.PhieuNhap)
        OR EXISTS (SELECT 1 FROM dbo.DongNhap)
        OR EXISTS (SELECT 1 FROM dbo.TraNhaCungCap)
        OR EXISTS (SELECT 1 FROM dbo.PhieuKiem)
        OR EXISTS (SELECT 1 FROM dbo.DongKiem)
        OR EXISTS (SELECT 1 FROM dbo.BienDongKho)
        THROW 50012, N'Seed requires empty POS tables. No existing data was changed.', 1;

    INSERT INTO dbo.VaiTro (maVaiTro, tenVaiTro)
    VALUES
        ('ThuNgan', N'Thu ngân'),
        ('NhanVienKho', N'Nhân viên kho'),
        ('QuanLy', N'Quản lý');

    INSERT INTO dbo.Quyen (maQuyen, tenQuyen)
    VALUES
        ('HT_QL_TAI_KHOAN', N'Quản lý tài khoản nhân viên (tạo, sửa, khóa)'),
        ('HT_PHAN_QUYEN', N'Gán vai trò cho tài khoản & cấu hình quyền vai trò'),
        ('DM_NHOM_QL', N'Thêm, sửa, ngừng dùng nhóm hàng'),
        ('DM_NHOM_XEM', N'Tra cứu nhóm hàng'),
        ('DM_HANG_QL', N'Thêm, sửa mặt hàng và cập nhật giá bán hiện hành'),
        ('DM_HANG_XEM', N'Tra cứu mặt hàng và giá bán hiện hành'),
        ('DM_NCC_QL', N'Thêm, sửa nhà cung cấp'),
        ('DM_NCC_XEM', N'Tra cứu nhà cung cấp'),
        ('BAN_LAP_HD', N'Lập, sửa hóa đơn nháp và tính tiền'),
        ('BAN_THANH_TOAN', N'Ghi nhận khoản thanh toán (tiền mặt, chuyển khoản)'),
        ('BAN_XAC_NHAN', N'Xác nhận bán hàng'),
        ('BAN_TRA_IN_HD', N'Tra cứu và in lại hóa đơn'),
        ('BAN_HUY_HD_NHAP', N'Hủy hóa đơn nháp (chưa xác nhận)'),
        ('BAN_HUY_HD_XAC_NHAN', N'Hủy hóa đơn đã xác nhận (đảo kho & tiền)'),
        ('TRA_LAP_PHIEU', N'Tra hóa đơn gốc và lập phiếu trả hàng'),
        ('TRA_XAC_NHAN', N'Xác nhận hoàn tiền và nhập lại kho'),
        ('TRA_TRA_CUU', N'Tra cứu phiếu trả hàng'),
        ('NHAP_LAP_NHAP', N'Lập và sửa phiếu nhập nháp'),
        ('NHAP_XAC_NHAN', N'Xác nhận nhập kho'),
        ('NCC_GHI_TRA_TIEN', N'Ghi khoản trả nhà cung cấp'),
        ('NCC_TRA_NO', N'Tra cứu lịch sử trả và dư nợ nhà cung cấp'),
        ('NHAP_TRA_CUU', N'Tra cứu phiếu nhập hàng'),
        ('NHAP_HUY_NHAP', N'Hủy phiếu nhập nháp'),
        ('NHAP_HUY_XAC_NHAN', N'Hủy phiếu nhập đã xác nhận (đảo kho & nợ)'),
        ('KHO_TRA_SO_HANG', N'Tra cứu sổ hàng hóa (thẻ kho)'),
        ('KHO_LAP_KIEM', N'Lập phiếu kiểm và ghi số lượng thực đếm'),
        ('KHO_GIAI_TRINH_KIEM', N'Ghi giải trình chênh lệch và gửi duyệt'),
        ('KHO_XEM_XET_KIEM', N'Xem xét căn cứ chênh lệch kiểm kho'),
        ('KHO_DUYET_DIEU_CHINH', N'Duyệt hoàn tất kiểm kho và điều chỉnh tồn'),
        ('KHO_HUY_KIEM', N'Hủy phiếu kiểm chưa hoàn tất'),
        ('CA_MO', N'Mở ca bán hàng và ghi tiền đầu ca'),
        ('QUY_LAP_THU_CHI', N'Lập phiếu thu, chi riêng'),
        ('QUY_TRA_SO', N'Tra cứu sổ quỹ và giao dịch trong ca'),
        ('CA_DONG', N'Kiểm đếm, đối chiếu và đóng ca'),
        ('CA_TRA_BAN_GIAO', N'Tra cứu lịch sử bàn giao ca'),
        ('BC_CUOI_NGAY', N'Xem và xuất báo cáo tổng hợp cuối ngày'),
        ('BC_BAN_TRA', N'Xem và xuất thống kê bán hàng và trả hàng'),
        ('BC_XUAT_NHAP_TON', N'Xem và xuất thống kê xuất nhập tồn'),
        ('BC_THANH_TOAN_CA', N'Xem và xuất tổng hợp thanh toán, lệch ca'),
        ('BC_NHAP_CONG_NO', N'Xem và xuất thống kê nhập hàng, công nợ NCC');

    INSERT INTO dbo.QuyenVaiTro (maVaiTro, maQuyen)
    VALUES
        ('QuanLy', 'HT_QL_TAI_KHOAN'),
        ('QuanLy', 'HT_PHAN_QUYEN'),
        ('QuanLy', 'DM_NHOM_QL'),
        ('ThuNgan', 'DM_NHOM_XEM'),
        ('NhanVienKho', 'DM_NHOM_XEM'),
        ('QuanLy', 'DM_NHOM_XEM'),
        ('QuanLy', 'DM_HANG_QL'),
        ('ThuNgan', 'DM_HANG_XEM'),
        ('NhanVienKho', 'DM_HANG_XEM'),
        ('QuanLy', 'DM_HANG_XEM'),
        ('QuanLy', 'DM_NCC_QL'),
        ('NhanVienKho', 'DM_NCC_XEM'),
        ('QuanLy', 'DM_NCC_XEM'),
        ('ThuNgan', 'BAN_LAP_HD'),
        ('ThuNgan', 'BAN_THANH_TOAN'),
        ('ThuNgan', 'BAN_XAC_NHAN'),
        ('ThuNgan', 'BAN_TRA_IN_HD'),
        ('QuanLy', 'BAN_TRA_IN_HD'),
        ('ThuNgan', 'BAN_HUY_HD_NHAP'),
        ('QuanLy', 'BAN_HUY_HD_NHAP'),
        ('QuanLy', 'BAN_HUY_HD_XAC_NHAN'),
        ('ThuNgan', 'TRA_LAP_PHIEU'),
        ('ThuNgan', 'TRA_XAC_NHAN'),
        ('ThuNgan', 'TRA_TRA_CUU'),
        ('QuanLy', 'TRA_TRA_CUU'),
        ('NhanVienKho', 'NHAP_LAP_NHAP'),
        ('NhanVienKho', 'NHAP_XAC_NHAN'),
        ('QuanLy', 'NCC_GHI_TRA_TIEN'),
        ('NhanVienKho', 'NCC_TRA_NO'),
        ('QuanLy', 'NCC_TRA_NO'),
        ('NhanVienKho', 'NHAP_TRA_CUU'),
        ('QuanLy', 'NHAP_TRA_CUU'),
        ('NhanVienKho', 'NHAP_HUY_NHAP'),
        ('QuanLy', 'NHAP_HUY_NHAP'),
        ('QuanLy', 'NHAP_HUY_XAC_NHAN'),
        ('NhanVienKho', 'KHO_TRA_SO_HANG'),
        ('QuanLy', 'KHO_TRA_SO_HANG'),
        ('NhanVienKho', 'KHO_LAP_KIEM'),
        ('NhanVienKho', 'KHO_GIAI_TRINH_KIEM'),
        ('NhanVienKho', 'KHO_XEM_XET_KIEM'),
        ('QuanLy', 'KHO_XEM_XET_KIEM'),
        ('QuanLy', 'KHO_DUYET_DIEU_CHINH'),
        ('NhanVienKho', 'KHO_HUY_KIEM'),
        ('QuanLy', 'KHO_HUY_KIEM'),
        ('ThuNgan', 'CA_MO'),
        ('ThuNgan', 'QUY_LAP_THU_CHI'),
        ('QuanLy', 'QUY_LAP_THU_CHI'),
        ('ThuNgan', 'QUY_TRA_SO'),
        ('QuanLy', 'QUY_TRA_SO'),
        ('ThuNgan', 'CA_DONG'),
        ('ThuNgan', 'CA_TRA_BAN_GIAO'),
        ('QuanLy', 'CA_TRA_BAN_GIAO'),
        ('QuanLy', 'BC_CUOI_NGAY'),
        ('QuanLy', 'BC_BAN_TRA'),
        ('NhanVienKho', 'BC_XUAT_NHAP_TON'),
        ('QuanLy', 'BC_XUAT_NHAP_TON'),
        ('QuanLy', 'BC_THANH_TOAN_CA'),
        ('QuanLy', 'BC_NHAP_CONG_NO');

    INSERT INTO dbo.TaiKhoan (maTaiKhoan, maVaiTro, hoTen, tenDangNhap, matKhauBam, trangThai)
    VALUES
        ('S-TK-QL', 'QuanLy', N'Nguyễn An', N'quanly', 'DEMO_DISABLED', 'HoatDong'),
        ('S-TK-KHO', 'NhanVienKho', N'Phạm Hải', N'kho', 'DEMO_DISABLED', 'HoatDong'),
        ('S-TK-TN1', 'ThuNgan', N'Lê Linh', N'thungan1', 'DEMO_DISABLED', 'HoatDong'),
        ('S-TK-TN2', 'ThuNgan', N'Trần Minh', N'thungan2', 'DEMO_DISABLED', 'HoatDong'),
        ('S-TK-CU', 'ThuNgan', N'Vũ Giang', N'nghiviec', 'DEMO_DISABLED', 'BiKhoa');

    INSERT INTO dbo.NhomHang (maNhom, tenNhom, trangThai)
    VALUES
        ('S-N-SUA', N'Sữa và đồ uống', 'HoatDong'),
        ('S-N-BANH', N'Bánh và thực phẩm', 'HoatDong'),
        ('S-N-CU', N'Danh mục ngừng sử dụng', 'NgungSuDung');

    INSERT INTO dbo.MatHang (maHang, maNhom, tenHang, donViTinh, giaBanHienHanh, trangThai)
    VALUES
        ('S-SUA', 'S-N-SUA', N'Sữa tươi 180 ml', N'Hộp', 26000, 'HoatDong'),
        ('S-BANH', 'S-N-BANH', N'Bánh mì đóng gói', N'Gói', 12000, 'HoatDong'),
        ('S-CU', 'S-N-CU', N'Mặt hàng đã ngừng bán', N'Gói', 5000, 'NgungSuDung');

    INSERT INTO dbo.NhaCungCap (maNhaCungCap, tenNhaCungCap, thongTinLienHe)
    VALUES
        ('S-NCC-SUA', N'Nhà cung cấp Sữa An Phú', N'sales@example.invalid'),
        ('S-NCC-BANH', N'Nhà cung cấp Bánh Bình Minh', N'orders@example.invalid');

    INSERT INTO dbo.CaBan (maCa, maNguoiMo, maNguoiChot, lucMo, tienMatDauCa, trangThai, lucDong, tienDuKienKhiChot, tienThucDem)
    VALUES
        ('S-CA-1010', 'S-TK-TN1', 'S-TK-TN1', '2026-10-10T01:00:00', 500000, 'DaDong', '2026-10-10T10:00:00', 600000, 598000),
        ('S-CA-1011', 'S-TK-TN2', 'S-TK-TN2', '2026-10-11T01:00:00', 500000, 'DaDong', '2026-10-11T10:00:00', 414000, 410000),
        ('S-CA-1112', 'S-TK-TN1', 'S-TK-TN1', '2026-11-12T01:00:00', 500000, 'DaDong', '2026-11-12T10:00:00', 500000, 501000),
        ('S-CA-1126', 'S-TK-TN1', NULL, '2026-11-26T01:00:00', 300000, 'DangMo', NULL, NULL, NULL);

    INSERT INTO dbo.PhieuThuChi (maPhieuThuChi, maCa, maNguoiLap, lucGhi, loaiThuChi, nguonQuy, phuongThuc, soTien, lyDo, soChungTuKemTheo)
    VALUES
        ('S-TC-01', 'S-CA-1010', 'S-TK-TN1', '2026-10-10T04:00:00', 'Chi', 'KetCa', 'TienMat', 6000, N'Mua bút viết', N'BB-01'),
        ('S-TC-02', 'S-CA-1010', 'S-TK-QL', '2026-10-10T05:00:00', 'Thu', 'KetCa', 'TienMat', 10000, N'Thu lại tiền ứng mua vật tư', NULL),
        ('S-TC-03', 'S-CA-1010', 'S-TK-QL', '2026-10-10T05:30:00', 'Thu', 'QuyCuaHang', 'ChuyenKhoan', 20000, N'Thu ngoài két ca', NULL);

    INSERT INTO dbo.HoaDon (maHoaDon, maCa, maNguoiLap, maNguoiHuy, maCaDao, lucLap, lucXacNhan, trangThai, lucHuy, lyDoHuy)
    VALUES
        ('S-HD-01', 'S-CA-1010', 'S-TK-TN1', NULL, NULL, '2026-10-10T02:30:00', '2026-10-10T03:00:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-02', 'S-CA-1010', 'S-TK-TN1', 'S-TK-QL', 'S-CA-1011', '2026-10-10T02:30:00', '2026-10-10T03:30:00', 'DaHuy', '2026-10-11T02:00:00', N'Hủy giao dịch bán nhầm'),
        ('S-HD-NOV-01', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:00:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-02', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:03:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-03', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:06:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-04', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:09:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-05', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:12:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-06', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:15:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-07', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:18:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-08', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:21:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-09', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:24:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-10', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:27:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-11', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:30:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-12', 'S-CA-1112', 'S-TK-TN1', NULL, NULL, '2026-11-12T02:30:00', '2026-11-12T03:33:00', 'DaXacNhan', NULL, NULL),
        ('S-HD-NOV-NHAP', 'S-CA-1126', 'S-TK-TN1', NULL, NULL, '2026-11-26T02:30:00', NULL, 'Nhap', NULL, NULL);

    INSERT INTO dbo.DongBan (maDongBan, maHoaDon, maHang, soLuongBan, donGiaThucBan)
    VALUES
        ('S-DB-01-1', 'S-HD-01', 'S-SUA', 5, 25000),
        ('S-DB-02-1', 'S-HD-02', 'S-BANH', 3, 12000),
        ('S-DB-NOV-01-1', 'S-HD-NOV-01', 'S-SUA', 1, 26000),
        ('S-DB-NOV-01-2', 'S-HD-NOV-01', 'S-BANH', 1, 12000),
        ('S-DB-NOV-02-1', 'S-HD-NOV-02', 'S-SUA', 2, 26000),
        ('S-DB-NOV-02-2', 'S-HD-NOV-02', 'S-BANH', 2, 12000),
        ('S-DB-NOV-03-1', 'S-HD-NOV-03', 'S-SUA', 3, 26000),
        ('S-DB-NOV-03-2', 'S-HD-NOV-03', 'S-BANH', 1, 12000),
        ('S-DB-NOV-04-1', 'S-HD-NOV-04', 'S-SUA', 1, 26000),
        ('S-DB-NOV-04-2', 'S-HD-NOV-04', 'S-BANH', 2, 12000),
        ('S-DB-NOV-05-1', 'S-HD-NOV-05', 'S-SUA', 2, 26000),
        ('S-DB-NOV-05-2', 'S-HD-NOV-05', 'S-BANH', 1, 12000),
        ('S-DB-NOV-06-1', 'S-HD-NOV-06', 'S-SUA', 3, 26000),
        ('S-DB-NOV-06-2', 'S-HD-NOV-06', 'S-BANH', 2, 12000),
        ('S-DB-NOV-07-1', 'S-HD-NOV-07', 'S-SUA', 1, 26000),
        ('S-DB-NOV-07-2', 'S-HD-NOV-07', 'S-BANH', 1, 12000),
        ('S-DB-NOV-08-1', 'S-HD-NOV-08', 'S-SUA', 2, 26000),
        ('S-DB-NOV-08-2', 'S-HD-NOV-08', 'S-BANH', 2, 12000),
        ('S-DB-NOV-09-1', 'S-HD-NOV-09', 'S-SUA', 3, 26000),
        ('S-DB-NOV-09-2', 'S-HD-NOV-09', 'S-BANH', 1, 12000),
        ('S-DB-NOV-10-1', 'S-HD-NOV-10', 'S-SUA', 1, 26000),
        ('S-DB-NOV-10-2', 'S-HD-NOV-10', 'S-BANH', 2, 12000),
        ('S-DB-NOV-11-1', 'S-HD-NOV-11', 'S-SUA', 2, 26000),
        ('S-DB-NOV-11-2', 'S-HD-NOV-11', 'S-BANH', 1, 12000),
        ('S-DB-NOV-12-1', 'S-HD-NOV-12', 'S-SUA', 3, 26000),
        ('S-DB-NOV-12-2', 'S-HD-NOV-12', 'S-BANH', 2, 12000),
        ('S-DB-NOV-NHAP-1', 'S-HD-NOV-NHAP', 'S-SUA', 1, 26000);

    INSERT INTO dbo.ThanhToanBan (maThanhToan, maHoaDon, phuongThuc, soTienThanhToan, tienKhachDua)
    VALUES
        ('S-TT-01-1', 'S-HD-01', 'TienMat', 75000, 100000),
        ('S-TT-01-2', 'S-HD-01', 'ChuyenKhoan', 50000, NULL),
        ('S-TT-02-1', 'S-HD-02', 'TienMat', 36000, 40000),
        ('S-TT-NOV-01-1', 'S-HD-NOV-01', 'ChuyenKhoan', 38000, NULL),
        ('S-TT-NOV-02-1', 'S-HD-NOV-02', 'ChuyenKhoan', 76000, NULL),
        ('S-TT-NOV-03-1', 'S-HD-NOV-03', 'ChuyenKhoan', 90000, NULL),
        ('S-TT-NOV-04-1', 'S-HD-NOV-04', 'ChuyenKhoan', 50000, NULL),
        ('S-TT-NOV-05-1', 'S-HD-NOV-05', 'ChuyenKhoan', 64000, NULL),
        ('S-TT-NOV-06-1', 'S-HD-NOV-06', 'ChuyenKhoan', 102000, NULL),
        ('S-TT-NOV-07-1', 'S-HD-NOV-07', 'ChuyenKhoan', 38000, NULL),
        ('S-TT-NOV-08-1', 'S-HD-NOV-08', 'ChuyenKhoan', 76000, NULL),
        ('S-TT-NOV-09-1', 'S-HD-NOV-09', 'ChuyenKhoan', 90000, NULL),
        ('S-TT-NOV-10-1', 'S-HD-NOV-10', 'ChuyenKhoan', 50000, NULL),
        ('S-TT-NOV-11-1', 'S-HD-NOV-11', 'ChuyenKhoan', 64000, NULL),
        ('S-TT-NOV-12-1', 'S-HD-NOV-12', 'ChuyenKhoan', 102000, NULL);

    INSERT INTO dbo.PhieuTra (maPhieuTra, maHoaDon, maCaNhan, maNguoiNhan, lucLap, lucXacNhan, trangThai)
    VALUES
        ('S-PT-01', 'S-HD-01', 'S-CA-1011', 'S-TK-TN2', '2026-10-11T02:30:00', '2026-10-11T03:00:00', 'DaXacNhan');

    INSERT INTO dbo.DongTra (maDongTra, maPhieuTra, maDongBan, soLuongTra, soLuongNhapLai)
    VALUES
        ('S-DT-01', 'S-PT-01', 'S-DB-01-1', 2, 1);

    INSERT INTO dbo.KhoanHoan (maKhoanHoan, maPhieuTra, phuongThuc, soTienHoan)
    VALUES
        ('S-KH-01', 'S-PT-01', 'TienMat', 50000);

    INSERT INTO dbo.PhieuNhap (maPhieuNhap, maNhaCungCap, maNguoiLap, maNguoiHuy, lucLap, lucXacNhan, trangThai, lucHuy, lyDoHuy, hanThanhToan)
    VALUES
        ('S-PN-SEP-SUA', 'S-NCC-SUA', 'S-TK-KHO', NULL, '2026-09-15T01:30:00', '2026-09-15T02:00:00', 'DaXacNhan', NULL, NULL, '2026-09-30'),
        ('S-PN-SEP-BANH', 'S-NCC-BANH', 'S-TK-KHO', NULL, '2026-09-15T01:30:00', '2026-09-15T02:00:00', 'DaXacNhan', NULL, NULL, '2026-09-30'),
        ('S-PN-OCT-SUA', 'S-NCC-SUA', 'S-TK-KHO', NULL, '2026-10-10T01:30:00', '2026-10-10T02:00:00', 'DaXacNhan', NULL, NULL, '2026-10-31'),
        ('S-PN-NOV-BANH', 'S-NCC-BANH', 'S-TK-KHO', 'S-TK-QL', '2026-11-12T01:30:00', '2026-11-12T02:00:00', 'DaHuy', '2026-11-12T02:15:00', N'Hủy lô nhập sai chứng từ', NULL),
        ('S-PN-NOV-NHAP', 'S-NCC-SUA', 'S-TK-KHO', NULL, '2026-11-26T01:30:00', NULL, 'Nhap', NULL, NULL, '2026-12-05');

    INSERT INTO dbo.DongNhap (maDongNhap, maPhieuNhap, maHang, soLuongNhap, donGiaNhap)
    VALUES
        ('S-DN-SEP-SUA', 'S-PN-SEP-SUA', 'S-SUA', 100, 15000),
        ('S-DN-SEP-BANH', 'S-PN-SEP-BANH', 'S-BANH', 50, 8000),
        ('S-DN-OCT-SUA', 'S-PN-OCT-SUA', 'S-SUA', 40, 15000),
        ('S-DN-NOV-BANH', 'S-PN-NOV-BANH', 'S-BANH', 4, 8000),
        ('S-DN-NOV-NHAP', 'S-PN-NOV-NHAP', 'S-SUA', 8, 15000);

    INSERT INTO dbo.TraNhaCungCap (maKhoanTra, maPhieuNhap, maNguoiGhi, maCaChi, lucTra, soTienTra, phuongThuc, nguonChi)
    VALUES
        ('S-TNCC-01', 'S-PN-SEP-SUA', 'S-TK-QL', NULL, '2026-09-15T04:00:00', 300000, 'ChuyenKhoan', 'QuyCuaHang'),
        ('S-TNCC-02', 'S-PN-OCT-SUA', 'S-TK-QL', 'S-CA-1010', '2026-10-10T06:00:00', 15000, 'TienMat', 'KetCa'),
        ('S-TNCC-03', 'S-PN-OCT-SUA', 'S-TK-QL', NULL, '2026-10-11T05:00:00', 100000, 'ChuyenKhoan', 'QuyCuaHang');

    INSERT INTO dbo.PhieuKiem (maPhieuKiem, maNguoiLap, maNguoiDuyet, mocKiem, lucHoanTat, trangThai)
    VALUES
        ('S-PK-01', 'S-TK-KHO', 'S-TK-QL', '2026-10-11T04:00:00', '2026-10-11T04:30:00', 'HoanTat'),
        ('S-PK-02', 'S-TK-KHO', NULL, '2026-11-26T04:00:00', NULL, 'DangKiem'),
        ('S-PK-03', 'S-TK-KHO', NULL, '2026-11-26T05:00:00', NULL, 'ChoDuyet');

    INSERT INTO dbo.DongKiem (maPhieuKiem, maHang, tonSoTaiMoc, soLuongDem, giaiTrinh)
    VALUES
        ('S-PK-01', 'S-SUA', 136, 134, N'Hai hộp hỏng khi kiểm đếm'),
        ('S-PK-01', 'S-BANH', 50, 50, NULL),
        ('S-PK-02', 'S-BANH', 32, NULL, NULL),
        ('S-PK-03', 'S-SUA', 110, 110, NULL);

    INSERT INTO dbo.BienDongKho (maBienDong, maHang, maDongBan, maDongTra, maDongNhap, maPhieuKiem, loaiSuKien, soLuongThayDoi, thoiDiemGhi, khoaSuKien)
    VALUES
        ('S-BD-001', 'S-SUA', NULL, NULL, 'S-DN-SEP-SUA', NULL, 'NhapKho', 100, '2026-09-15T02:00:00', 'NhapKho:S-DN-SEP-SUA'),
        ('S-BD-002', 'S-BANH', NULL, NULL, 'S-DN-SEP-BANH', NULL, 'NhapKho', 50, '2026-09-15T02:00:00', 'NhapKho:S-DN-SEP-BANH'),
        ('S-BD-003', 'S-SUA', NULL, NULL, 'S-DN-OCT-SUA', NULL, 'NhapKho', 40, '2026-10-10T02:00:00', 'NhapKho:S-DN-OCT-SUA'),
        ('S-BD-004', 'S-BANH', NULL, NULL, 'S-DN-NOV-BANH', NULL, 'NhapKho', 4, '2026-11-12T02:00:00', 'NhapKho:S-DN-NOV-BANH'),
        ('S-BD-005', 'S-BANH', NULL, NULL, 'S-DN-NOV-BANH', NULL, 'DaoNhapKho', -4, '2026-11-12T02:15:00', 'DaoNhapKho:S-DN-NOV-BANH'),
        ('S-BD-006', 'S-SUA', 'S-DB-01-1', NULL, NULL, NULL, 'XuatBan', -5, '2026-10-10T03:00:00', 'XuatBan:S-DB-01-1'),
        ('S-BD-007', 'S-BANH', 'S-DB-02-1', NULL, NULL, NULL, 'XuatBan', -3, '2026-10-10T03:30:00', 'XuatBan:S-DB-02-1'),
        ('S-BD-008', 'S-BANH', 'S-DB-02-1', NULL, NULL, NULL, 'DaoXuatBan', 3, '2026-10-11T02:00:00', 'DaoXuatBan:S-DB-02-1'),
        ('S-BD-009', 'S-SUA', NULL, 'S-DT-01', NULL, NULL, 'NhapTraHang', 1, '2026-10-11T03:00:00', 'NhapTraHang:S-DT-01'),
        ('S-BD-010', 'S-SUA', NULL, NULL, NULL, 'S-PK-01', 'DieuChinhKiemKho', -2, '2026-10-11T04:30:00', 'DieuChinhKiemKho:S-PK-01:S-SUA'),
        ('S-BD-011', 'S-SUA', 'S-DB-NOV-01-1', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:00:00', 'XuatBan:S-DB-NOV-01-1'),
        ('S-BD-012', 'S-BANH', 'S-DB-NOV-01-2', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:00:00', 'XuatBan:S-DB-NOV-01-2'),
        ('S-BD-013', 'S-SUA', 'S-DB-NOV-02-1', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:03:00', 'XuatBan:S-DB-NOV-02-1'),
        ('S-BD-014', 'S-BANH', 'S-DB-NOV-02-2', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:03:00', 'XuatBan:S-DB-NOV-02-2'),
        ('S-BD-015', 'S-SUA', 'S-DB-NOV-03-1', NULL, NULL, NULL, 'XuatBan', -3, '2026-11-12T03:06:00', 'XuatBan:S-DB-NOV-03-1'),
        ('S-BD-016', 'S-BANH', 'S-DB-NOV-03-2', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:06:00', 'XuatBan:S-DB-NOV-03-2'),
        ('S-BD-017', 'S-SUA', 'S-DB-NOV-04-1', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:09:00', 'XuatBan:S-DB-NOV-04-1'),
        ('S-BD-018', 'S-BANH', 'S-DB-NOV-04-2', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:09:00', 'XuatBan:S-DB-NOV-04-2'),
        ('S-BD-019', 'S-SUA', 'S-DB-NOV-05-1', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:12:00', 'XuatBan:S-DB-NOV-05-1'),
        ('S-BD-020', 'S-BANH', 'S-DB-NOV-05-2', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:12:00', 'XuatBan:S-DB-NOV-05-2'),
        ('S-BD-021', 'S-SUA', 'S-DB-NOV-06-1', NULL, NULL, NULL, 'XuatBan', -3, '2026-11-12T03:15:00', 'XuatBan:S-DB-NOV-06-1'),
        ('S-BD-022', 'S-BANH', 'S-DB-NOV-06-2', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:15:00', 'XuatBan:S-DB-NOV-06-2'),
        ('S-BD-023', 'S-SUA', 'S-DB-NOV-07-1', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:18:00', 'XuatBan:S-DB-NOV-07-1'),
        ('S-BD-024', 'S-BANH', 'S-DB-NOV-07-2', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:18:00', 'XuatBan:S-DB-NOV-07-2'),
        ('S-BD-025', 'S-SUA', 'S-DB-NOV-08-1', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:21:00', 'XuatBan:S-DB-NOV-08-1'),
        ('S-BD-026', 'S-BANH', 'S-DB-NOV-08-2', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:21:00', 'XuatBan:S-DB-NOV-08-2'),
        ('S-BD-027', 'S-SUA', 'S-DB-NOV-09-1', NULL, NULL, NULL, 'XuatBan', -3, '2026-11-12T03:24:00', 'XuatBan:S-DB-NOV-09-1'),
        ('S-BD-028', 'S-BANH', 'S-DB-NOV-09-2', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:24:00', 'XuatBan:S-DB-NOV-09-2'),
        ('S-BD-029', 'S-SUA', 'S-DB-NOV-10-1', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:27:00', 'XuatBan:S-DB-NOV-10-1'),
        ('S-BD-030', 'S-BANH', 'S-DB-NOV-10-2', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:27:00', 'XuatBan:S-DB-NOV-10-2'),
        ('S-BD-031', 'S-SUA', 'S-DB-NOV-11-1', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:30:00', 'XuatBan:S-DB-NOV-11-1'),
        ('S-BD-032', 'S-BANH', 'S-DB-NOV-11-2', NULL, NULL, NULL, 'XuatBan', -1, '2026-11-12T03:30:00', 'XuatBan:S-DB-NOV-11-2'),
        ('S-BD-033', 'S-SUA', 'S-DB-NOV-12-1', NULL, NULL, NULL, 'XuatBan', -3, '2026-11-12T03:33:00', 'XuatBan:S-DB-NOV-12-1'),
        ('S-BD-034', 'S-BANH', 'S-DB-NOV-12-2', NULL, NULL, NULL, 'XuatBan', -2, '2026-11-12T03:33:00', 'XuatBan:S-DB-NOV-12-2');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
