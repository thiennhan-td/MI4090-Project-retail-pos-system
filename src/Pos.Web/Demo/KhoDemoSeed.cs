using System.Globalization;

namespace Pos.Web.Demo;

public sealed partial class KhoDemoStore
{
    public const string MatKhauMau = "Demo@123";

    public static IReadOnlyDictionary<string, string> VaiTros { get; } =
        new Dictionary<string, string>
    {
        ["ThuNgan"] = "Thu ngân",
        ["NhanVienKho"] = "Nhân viên kho",
        ["QuanLy"] = "Quản lý",
    };

    public static IReadOnlyList<QuyenDemo> DanhSachQuyen { get; } =
    [
        new("HT_QL_TAI_KHOAN", "Quản lý tài khoản nhân viên (tạo, sửa, khóa)"),
        new("HT_PHAN_QUYEN", "Gán vai trò cho tài khoản & cấu hình quyền vai trò"),
        new("DM_NHOM_QL", "Thêm, sửa, ngừng dùng nhóm hàng"),
        new("DM_NHOM_XEM", "Tra cứu nhóm hàng"),
        new("DM_HANG_QL", "Thêm, sửa mặt hàng và cập nhật giá bán hiện hành"),
        new("DM_HANG_XEM", "Tra cứu mặt hàng và giá bán hiện hành"),
        new("DM_NCC_QL", "Thêm, sửa nhà cung cấp"),
        new("DM_NCC_XEM", "Tra cứu nhà cung cấp"),
        new("BAN_LAP_HD", "Lập, sửa hóa đơn nháp và tính tiền"),
        new("BAN_THANH_TOAN", "Ghi nhận khoản thanh toán (tiền mặt, chuyển khoản)"),
        new("BAN_XAC_NHAN", "Xác nhận bán hàng"),
        new("BAN_TRA_IN_HD", "Tra cứu và in lại hóa đơn"),
        new("BAN_HUY_HD_NHAP", "Hủy hóa đơn nháp (chưa xác nhận)"),
        new("BAN_HUY_HD_XAC_NHAN", "Hủy hóa đơn đã xác nhận (đảo kho & tiền)"),
        new("TRA_LAP_PHIEU", "Tra hóa đơn gốc và lập phiếu trả hàng"),
        new("TRA_XAC_NHAN", "Xác nhận hoàn tiền và nhập lại kho"),
        new("TRA_TRA_CUU", "Tra cứu phiếu trả hàng"),
        new("NHAP_LAP_NHAP", "Lập và sửa phiếu nhập nháp"),
        new("NHAP_XAC_NHAN", "Xác nhận nhập kho"),
        new("NCC_GHI_TRA_TIEN", "Ghi khoản trả nhà cung cấp"),
        new("NCC_TRA_NO", "Tra cứu lịch sử trả và dư nợ nhà cung cấp"),
        new("NHAP_TRA_CUU", "Tra cứu phiếu nhập hàng"),
        new("NHAP_HUY_NHAP", "Hủy phiếu nhập nháp"),
        new("NHAP_HUY_XAC_NHAN", "Hủy phiếu nhập đã xác nhận (đảo kho & nợ)"),
        new("KHO_TRA_SO_HANG", "Tra cứu sổ hàng hóa (thẻ kho)"),
        new("KHO_LAP_KIEM", "Lập phiếu kiểm và ghi số lượng thực đếm"),
        new("KHO_GIAI_TRINH_KIEM", "Ghi giải trình chênh lệch và gửi duyệt"),
        new("KHO_XEM_XET_KIEM", "Xem xét căn cứ chênh lệch kiểm kho"),
        new("KHO_DUYET_DIEU_CHINH", "Duyệt hoàn tất kiểm kho và điều chỉnh tồn"),
        new("KHO_HUY_KIEM", "Hủy phiếu kiểm chưa hoàn tất"),
        new("CA_MO", "Mở ca bán hàng và ghi tiền đầu ca"),
        new("QUY_LAP_THU_CHI", "Lập phiếu thu, chi riêng"),
        new("QUY_TRA_SO", "Tra cứu sổ quỹ và giao dịch trong ca"),
        new("CA_DONG", "Kiểm đếm, đối chiếu và đóng ca"),
        new("CA_TRA_BAN_GIAO", "Tra cứu lịch sử bàn giao ca"),
        new("BC_CUOI_NGAY", "Xem và xuất báo cáo tổng hợp cuối ngày"),
        new("BC_BAN_TRA", "Xem và xuất thống kê bán hàng và trả hàng"),
        new("BC_XUAT_NHAP_TON", "Xem và xuất thống kê xuất nhập tồn"),
        new("BC_THANH_TOAN_CA", "Xem và xuất tổng hợp thanh toán, lệch ca"),
        new("BC_NHAP_CONG_NO", "Xem và xuất thống kê nhập hàng, công nợ NCC"),
    ];

    private void NapDuLieuMau()
    {
        foreach (var vaiTro in VaiTros.Keys)
            _quyenTheoVaiTro.Add(vaiTro, new HashSet<string>(StringComparer.Ordinal));

        (string VaiTro, string Quyen)[] phanQuyen =
        [
        ("QuanLy", "HT_QL_TAI_KHOAN"),
        ("QuanLy", "HT_PHAN_QUYEN"),
        ("QuanLy", "DM_NHOM_QL"),
        ("ThuNgan", "DM_NHOM_XEM"),
        ("NhanVienKho", "DM_NHOM_XEM"),
        ("QuanLy", "DM_NHOM_XEM"),
        ("QuanLy", "DM_HANG_QL"),
        ("ThuNgan", "DM_HANG_XEM"),
        ("NhanVienKho", "DM_HANG_XEM"),
        ("QuanLy", "DM_HANG_XEM"),
        ("QuanLy", "DM_NCC_QL"),
        ("NhanVienKho", "DM_NCC_XEM"),
        ("QuanLy", "DM_NCC_XEM"),
        ("ThuNgan", "BAN_LAP_HD"),
        ("ThuNgan", "BAN_THANH_TOAN"),
        ("ThuNgan", "BAN_XAC_NHAN"),
        ("ThuNgan", "BAN_TRA_IN_HD"),
        ("QuanLy", "BAN_TRA_IN_HD"),
        ("ThuNgan", "BAN_HUY_HD_NHAP"),
        ("QuanLy", "BAN_HUY_HD_NHAP"),
        ("QuanLy", "BAN_HUY_HD_XAC_NHAN"),
        ("ThuNgan", "TRA_LAP_PHIEU"),
        ("ThuNgan", "TRA_XAC_NHAN"),
        ("ThuNgan", "TRA_TRA_CUU"),
        ("QuanLy", "TRA_TRA_CUU"),
        ("NhanVienKho", "NHAP_LAP_NHAP"),
        ("NhanVienKho", "NHAP_XAC_NHAN"),
        ("QuanLy", "NCC_GHI_TRA_TIEN"),
        ("NhanVienKho", "NCC_TRA_NO"),
        ("QuanLy", "NCC_TRA_NO"),
        ("NhanVienKho", "NHAP_TRA_CUU"),
        ("QuanLy", "NHAP_TRA_CUU"),
        ("NhanVienKho", "NHAP_HUY_NHAP"),
        ("QuanLy", "NHAP_HUY_NHAP"),
        ("QuanLy", "NHAP_HUY_XAC_NHAN"),
        ("NhanVienKho", "KHO_TRA_SO_HANG"),
        ("QuanLy", "KHO_TRA_SO_HANG"),
        ("NhanVienKho", "KHO_LAP_KIEM"),
        ("NhanVienKho", "KHO_GIAI_TRINH_KIEM"),
        ("NhanVienKho", "KHO_XEM_XET_KIEM"),
        ("QuanLy", "KHO_XEM_XET_KIEM"),
        ("QuanLy", "KHO_DUYET_DIEU_CHINH"),
        ("NhanVienKho", "KHO_HUY_KIEM"),
        ("QuanLy", "KHO_HUY_KIEM"),
        ("ThuNgan", "CA_MO"),
        ("ThuNgan", "QUY_LAP_THU_CHI"),
        ("QuanLy", "QUY_LAP_THU_CHI"),
        ("ThuNgan", "QUY_TRA_SO"),
        ("QuanLy", "QUY_TRA_SO"),
        ("ThuNgan", "CA_DONG"),
        ("ThuNgan", "CA_TRA_BAN_GIAO"),
        ("QuanLy", "CA_TRA_BAN_GIAO"),
        ("QuanLy", "BC_CUOI_NGAY"),
        ("QuanLy", "BC_BAN_TRA"),
        ("NhanVienKho", "BC_XUAT_NHAP_TON"),
        ("QuanLy", "BC_XUAT_NHAP_TON"),
        ("QuanLy", "BC_THANH_TOAN_CA"),
        ("QuanLy", "BC_NHAP_CONG_NO"),
        ];
        foreach (var (vaiTro, quyen) in phanQuyen)
            _quyenTheoVaiTro[vaiTro].Add(quyen);

        TaiKhoanDemo[] taiKhoans =
        [
        new("S-TK-QL", "Nguyễn An", "quanly", "QuanLy", _bamMatKhauMau, true),
        new("S-TK-KHO", "Phạm Hải", "kho", "NhanVienKho", _bamMatKhauMau, true),
        new("S-TK-TN1", "Lê Linh", "thungan1", "ThuNgan", _bamMatKhauMau, true),
        new("S-TK-TN2", "Trần Minh", "thungan2", "ThuNgan", _bamMatKhauMau, true),
        new("S-TK-CU", "Vũ Giang", "nghiviec", "ThuNgan", _bamMatKhauMau, false),
        ];
        foreach (var taiKhoan in taiKhoans)
            _taiKhoans.Add(taiKhoan.Ma, taiKhoan);

        NhomHangDemo[] nhomHangs =
        [
        new("S-N-SUA", "Sữa và đồ uống", true),
        new("S-N-BANH", "Bánh và thực phẩm", true),
        new("S-N-CU", "Danh mục ngừng sử dụng", false),
        ];
        foreach (var nhomHang in nhomHangs)
            _nhomHangs.Add(nhomHang.Ma, nhomHang);

        MatHangKhoDemo[] matHangs =
        [
        new("S-SUA", "S-N-SUA", "Sữa tươi 180 ml", "Hộp", 26000m, true),
        new("S-BANH", "S-N-BANH", "Bánh mì đóng gói", "Gói", 12000m, true),
        new("S-CU", "S-N-CU", "Mặt hàng đã ngừng bán", "Gói", 5000m, false),
        ];
        foreach (var matHang in matHangs)
            _matHangs.Add(matHang.Ma, matHang);

        NhaCungCapDemo[] nhaCungCaps =
        [
        new("S-NCC-SUA", "Nhà cung cấp Sữa An Phú", "sales@example.invalid"),
        new("S-NCC-BANH", "Nhà cung cấp Bánh Bình Minh", "orders@example.invalid"),
        ];
        foreach (var nhaCungCap in nhaCungCaps)
            _nhaCungCaps.Add(nhaCungCap.Ma, nhaCungCap);

        PhieuNhapDemo[] phieuNhaps =
        [
        new("S-PN-SEP-SUA", "S-NCC-SUA", "S-TK-KHO", Utc("2026-09-15T01:30:00"), DateOnly.ParseExact("2026-09-30", "yyyy-MM-dd", CultureInfo.InvariantCulture), "DaXacNhan", Utc("2026-09-15T02:00:00"), null, null, [new("S-DN-SEP-SUA", "S-SUA", 100, 15000m)]),
        new("S-PN-SEP-BANH", "S-NCC-BANH", "S-TK-KHO", Utc("2026-09-15T01:30:00"), DateOnly.ParseExact("2026-09-30", "yyyy-MM-dd", CultureInfo.InvariantCulture), "DaXacNhan", Utc("2026-09-15T02:00:00"), null, null, [new("S-DN-SEP-BANH", "S-BANH", 50, 8000m)]),
        new("S-PN-OCT-SUA", "S-NCC-SUA", "S-TK-KHO", Utc("2026-10-10T01:30:00"), DateOnly.ParseExact("2026-10-31", "yyyy-MM-dd", CultureInfo.InvariantCulture), "DaXacNhan", Utc("2026-10-10T02:00:00"), null, null, [new("S-DN-OCT-SUA", "S-SUA", 40, 15000m)]),
        new("S-PN-NOV-BANH", "S-NCC-BANH", "S-TK-KHO", Utc("2026-11-12T01:30:00"), null, "DaHuy", Utc("2026-11-12T02:00:00"), Utc("2026-11-12T02:15:00"), "Hủy lô nhập sai chứng từ", [new("S-DN-NOV-BANH", "S-BANH", 4, 8000m)]),
        new("S-PN-NOV-NHAP", "S-NCC-SUA", "S-TK-KHO", Utc("2026-11-26T01:30:00"), DateOnly.ParseExact("2026-12-05", "yyyy-MM-dd", CultureInfo.InvariantCulture), "Nhap", null, null, null, [new("S-DN-NOV-NHAP", "S-SUA", 8, 15000m)]),
        ];
        foreach (var phieuNhap in phieuNhaps)
            _phieuNhaps.Add(phieuNhap.Ma, phieuNhap);

        TraNhaCungCapDemo[] khoanTras =
        [
        new("S-TNCC-01", "S-PN-SEP-SUA", "S-TK-QL", null, Utc("2026-09-15T04:00:00"), 300000m, "ChuyenKhoan", "QuyCuaHang", null),
        new("S-TNCC-02", "S-PN-OCT-SUA", "S-TK-QL", "S-CA-1010", Utc("2026-10-10T06:00:00"), 15000m, "TienMat", "KetCa", null),
        new("S-TNCC-03", "S-PN-OCT-SUA", "S-TK-QL", null, Utc("2026-10-11T05:00:00"), 100000m, "ChuyenKhoan", "QuyCuaHang", null),
        ];
        _khoanTras.AddRange(khoanTras);

        PhieuKiemDemo[] phieuKiems =
        [
        new("S-PK-01", "S-TK-KHO", "S-TK-QL", Utc("2026-10-11T04:00:00"), Utc("2026-10-11T04:30:00"), "HoanTat", [new("S-SUA", 136, 134, "Hai hộp hỏng khi kiểm đếm"), new("S-BANH", 50, 50, null)]),
        new("S-PK-02", "S-TK-KHO", null, Utc("2026-11-26T04:00:00"), null, "DangKiem", [new("S-BANH", 32, null, null)]),
        new("S-PK-03", "S-TK-KHO", null, Utc("2026-11-26T05:00:00"), null, "ChoDuyet", [new("S-SUA", 110, 110, null)]),
        ];
        foreach (var phieuKiem in phieuKiems)
            _phieuKiems.Add(phieuKiem.Ma, phieuKiem);

        BienDongKhoDemo[] bienDongs =
        [
        new("S-BD-001", "S-SUA", "NhapKho", 100, Utc("2026-09-15T02:00:00"), "S-DN-SEP-SUA", "NhapKho:S-DN-SEP-SUA"),
        new("S-BD-002", "S-BANH", "NhapKho", 50, Utc("2026-09-15T02:00:00"), "S-DN-SEP-BANH", "NhapKho:S-DN-SEP-BANH"),
        new("S-BD-003", "S-SUA", "NhapKho", 40, Utc("2026-10-10T02:00:00"), "S-DN-OCT-SUA", "NhapKho:S-DN-OCT-SUA"),
        new("S-BD-004", "S-BANH", "NhapKho", 4, Utc("2026-11-12T02:00:00"), "S-DN-NOV-BANH", "NhapKho:S-DN-NOV-BANH"),
        new("S-BD-005", "S-BANH", "DaoNhapKho", -4, Utc("2026-11-12T02:15:00"), "S-DN-NOV-BANH", "DaoNhapKho:S-DN-NOV-BANH"),
        new("S-BD-006", "S-SUA", "XuatBan", -5, Utc("2026-10-10T03:00:00"), "S-DB-01-1", "XuatBan:S-DB-01-1"),
        new("S-BD-007", "S-BANH", "XuatBan", -3, Utc("2026-10-10T03:30:00"), "S-DB-02-1", "XuatBan:S-DB-02-1"),
        new("S-BD-008", "S-BANH", "DaoXuatBan", 3, Utc("2026-10-11T02:00:00"), "S-DB-02-1", "DaoXuatBan:S-DB-02-1"),
        new("S-BD-009", "S-SUA", "NhapTraHang", 1, Utc("2026-10-11T03:00:00"), "S-DT-01", "NhapTraHang:S-DT-01"),
        new("S-BD-010", "S-SUA", "DieuChinhKiemKho", -2, Utc("2026-10-11T04:30:00"), "S-PK-01", "DieuChinhKiemKho:S-PK-01:S-SUA"),
        new("S-BD-011", "S-SUA", "XuatBan", -1, Utc("2026-11-12T03:00:00"), "S-DB-NOV-01-1", "XuatBan:S-DB-NOV-01-1"),
        new("S-BD-012", "S-BANH", "XuatBan", -1, Utc("2026-11-12T03:00:00"), "S-DB-NOV-01-2", "XuatBan:S-DB-NOV-01-2"),
        new("S-BD-013", "S-SUA", "XuatBan", -2, Utc("2026-11-12T03:03:00"), "S-DB-NOV-02-1", "XuatBan:S-DB-NOV-02-1"),
        new("S-BD-014", "S-BANH", "XuatBan", -2, Utc("2026-11-12T03:03:00"), "S-DB-NOV-02-2", "XuatBan:S-DB-NOV-02-2"),
        new("S-BD-015", "S-SUA", "XuatBan", -3, Utc("2026-11-12T03:06:00"), "S-DB-NOV-03-1", "XuatBan:S-DB-NOV-03-1"),
        new("S-BD-016", "S-BANH", "XuatBan", -1, Utc("2026-11-12T03:06:00"), "S-DB-NOV-03-2", "XuatBan:S-DB-NOV-03-2"),
        new("S-BD-017", "S-SUA", "XuatBan", -1, Utc("2026-11-12T03:09:00"), "S-DB-NOV-04-1", "XuatBan:S-DB-NOV-04-1"),
        new("S-BD-018", "S-BANH", "XuatBan", -2, Utc("2026-11-12T03:09:00"), "S-DB-NOV-04-2", "XuatBan:S-DB-NOV-04-2"),
        new("S-BD-019", "S-SUA", "XuatBan", -2, Utc("2026-11-12T03:12:00"), "S-DB-NOV-05-1", "XuatBan:S-DB-NOV-05-1"),
        new("S-BD-020", "S-BANH", "XuatBan", -1, Utc("2026-11-12T03:12:00"), "S-DB-NOV-05-2", "XuatBan:S-DB-NOV-05-2"),
        new("S-BD-021", "S-SUA", "XuatBan", -3, Utc("2026-11-12T03:15:00"), "S-DB-NOV-06-1", "XuatBan:S-DB-NOV-06-1"),
        new("S-BD-022", "S-BANH", "XuatBan", -2, Utc("2026-11-12T03:15:00"), "S-DB-NOV-06-2", "XuatBan:S-DB-NOV-06-2"),
        new("S-BD-023", "S-SUA", "XuatBan", -1, Utc("2026-11-12T03:18:00"), "S-DB-NOV-07-1", "XuatBan:S-DB-NOV-07-1"),
        new("S-BD-024", "S-BANH", "XuatBan", -1, Utc("2026-11-12T03:18:00"), "S-DB-NOV-07-2", "XuatBan:S-DB-NOV-07-2"),
        new("S-BD-025", "S-SUA", "XuatBan", -2, Utc("2026-11-12T03:21:00"), "S-DB-NOV-08-1", "XuatBan:S-DB-NOV-08-1"),
        new("S-BD-026", "S-BANH", "XuatBan", -2, Utc("2026-11-12T03:21:00"), "S-DB-NOV-08-2", "XuatBan:S-DB-NOV-08-2"),
        new("S-BD-027", "S-SUA", "XuatBan", -3, Utc("2026-11-12T03:24:00"), "S-DB-NOV-09-1", "XuatBan:S-DB-NOV-09-1"),
        new("S-BD-028", "S-BANH", "XuatBan", -1, Utc("2026-11-12T03:24:00"), "S-DB-NOV-09-2", "XuatBan:S-DB-NOV-09-2"),
        new("S-BD-029", "S-SUA", "XuatBan", -1, Utc("2026-11-12T03:27:00"), "S-DB-NOV-10-1", "XuatBan:S-DB-NOV-10-1"),
        new("S-BD-030", "S-BANH", "XuatBan", -2, Utc("2026-11-12T03:27:00"), "S-DB-NOV-10-2", "XuatBan:S-DB-NOV-10-2"),
        new("S-BD-031", "S-SUA", "XuatBan", -2, Utc("2026-11-12T03:30:00"), "S-DB-NOV-11-1", "XuatBan:S-DB-NOV-11-1"),
        new("S-BD-032", "S-BANH", "XuatBan", -1, Utc("2026-11-12T03:30:00"), "S-DB-NOV-11-2", "XuatBan:S-DB-NOV-11-2"),
        new("S-BD-033", "S-SUA", "XuatBan", -3, Utc("2026-11-12T03:33:00"), "S-DB-NOV-12-1", "XuatBan:S-DB-NOV-12-1"),
        new("S-BD-034", "S-BANH", "XuatBan", -2, Utc("2026-11-12T03:33:00"), "S-DB-NOV-12-2", "XuatBan:S-DB-NOV-12-2"),
        ];
        _bienDongs.AddRange(bienDongs);
    }

    private static DateTimeOffset Utc(string value) =>
        DateTimeOffset.ParseExact(value, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);
}
