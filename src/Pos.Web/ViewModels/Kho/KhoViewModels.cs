using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;

namespace Pos.Web.ViewModels.Kho;

public sealed class DangNhapKhoViewModel
{
    [Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(64)]
    public string TenDangNhap { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(100), DataType(DataType.Password)]
    public string MatKhau { get; set; } = "";
    public string? TroVe { get; set; }
}

public sealed class TaiKhoanFormViewModel
{
    [Required(ErrorMessage = "Nhập họ tên."), StringLength(150)]
    public string HoTen { get; set; } = "";
    [Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(64), RegularExpression(@"[A-Za-z0-9_.-]+", ErrorMessage = "Dùng chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    public string TenDangNhap { get; set; } = "";
    [Required(ErrorMessage = "Chọn vai trò.")]
    public string VaiTro { get; set; } = "";
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu cần từ 6 đến 100 ký tự."), DataType(DataType.Password)]
    public string? MatKhau { get; set; }
    public bool HoatDong { get; set; } = true;
}

public sealed class PhanQuyenViewModel
{
    [Required(ErrorMessage = "Chọn vai trò.")]
    public string VaiTro { get; set; } = "";
    public string[] Quyen { get; set; } = [];
}

public class NhomHangFormViewModel
{
    [Required(ErrorMessage = "Nhập tên."), StringLength(150)]
    public string Ten { get; set; } = "";
}

public sealed class MatHangFormViewModel : NhomHangFormViewModel
{
    [Required(ErrorMessage = "Chọn nhóm hàng.")]
    public string MaNhom { get; set; } = "";
    [Required(ErrorMessage = "Nhập đơn vị."), StringLength(32)]
    public string DonVi { get; set; } = "";
    [ModelBinder(BinderType = typeof(TienDongModelBinder))]
    [Required(ErrorMessage = "Nhập giá bán."), Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá bán phải từ 0 đến 999.999.999 ₫.")]
    public decimal? GiaBan { get; set; }
}

public sealed class NhaCungCapFormViewModel : NhomHangFormViewModel
{
    [StringLength(500)]
    public string? LienHe { get; set; }
}

public sealed record DanhMucDongViewModel(string Ma, string Ten, string? Nhom = null, string? DonVi = null,
    decimal? GiaBan = null, bool? HoatDong = null, string? LienHe = null);

public sealed record DanhMucViewModel(string TieuDe, string? TuKhoa, string? TrangThai, string? MaNhom,
    bool CoNhom, bool CoLienHe, bool CoTrangThai, bool DuocSua, IReadOnlyList<DanhMucDongViewModel> Dongs,
    IReadOnlyList<NhomHangDemo> NhomHangs);

public sealed class PhieuNhapFormViewModel
{
    [Range(0, int.MaxValue)]
    public int? PhienBan { get; set; }
    [Required(ErrorMessage = "Chọn nhà cung cấp.")]
    public string MaNcc { get; set; } = "";
    [ModelBinder(BinderType = typeof(NgayKhoModelBinder))]
    public DateOnly? HanThanhToan { get; set; }
    public List<DongNhapFormViewModel> Dongs { get; set; } = [];
}

public sealed class DongNhapFormViewModel
{
    [Required(ErrorMessage = "Chọn mặt hàng trên mỗi dòng.")]
    public string MaHang { get; set; } = "";
    [Required(ErrorMessage = "Nhập số lượng."), Range(1, 1_000_000, ErrorMessage = "Số lượng mỗi dòng phải từ 1 đến 1.000.000.")]
    public int? SoLuong { get; set; }
    [ModelBinder(BinderType = typeof(TienDongModelBinder))]
    [Required(ErrorMessage = "Nhập đơn giá."), Range(typeof(decimal), "0", "999999999", ErrorMessage = "Đơn giá nhập phải từ 0 đến 999.999.999 ₫.")]
    public decimal? DonGia { get; set; }
}

public sealed class TraTienNccViewModel
{
    [ModelBinder(BinderType = typeof(TienDongModelBinder))]
    [Required(ErrorMessage = "Nhập số tiền."), Range(typeof(decimal), "1", "100000000000000000", ErrorMessage = "Số tiền trả phải lớn hơn 0.")]
    public decimal? SoTien { get; set; }
    [Required, RegularExpression("TienMat|ChuyenKhoan", ErrorMessage = "Chọn phương thức hợp lệ.")]
    public string PhuongThuc { get; set; } = "";
    [Required, RegularExpression("KetCa|QuyCuaHang", ErrorMessage = "Chọn nguồn chi hợp lệ.")]
    public string Nguon { get; set; } = "";
    public string? MaCa { get; set; }
    [Required(ErrorMessage = "Tải lại trang trước khi ghi khoản trả.")]
    public Guid? KhoaYeuCau { get; set; }
}

public sealed class HuyNhapFormViewModel
{
    [Required(ErrorMessage = "Nhập lý do hủy."), StringLength(500)]
    public string LyDo { get; set; } = "";
}

public sealed record PhieuNhapDongViewModel(PhieuNhapDemo Phieu, string TenNcc, decimal? DaTra, decimal? DuNo);

public sealed record SoNhapViewModel(string? TuKhoa, string? MaNcc, string? TrangThai, DateOnly? Tu, DateOnly? Den, IReadOnlyList<NhaCungCapDemo> NhaCungCaps,
    IReadOnlyList<PhieuNhapDongViewModel> Dongs, decimal? TongNo);

public sealed record ChiTietNhapViewModel(PhieuNhapDongViewModel Dong, IReadOnlyDictionary<string, string> TenHangs,
    IReadOnlyList<TraNhaCungCapDemo> LichSuTra, IReadOnlyList<CaChiDemo> CaDangMo, bool CoTheHuy,
    decimal? DuNoCoTheTra, Guid KhoaYeuCau, TraTienNccViewModel? TraTienDaNhap = null,
    HuyNhapFormViewModel? HuyDaNhap = null);

public sealed record KhoRejectedViewModel(string TieuDe, string? ThongBao, string? Url, string NhanLienKet);

public sealed class LapKiemFormViewModel
{
    public string[] MaHangs { get; set; } = [];
}

public sealed class GhiDemFormViewModel
{
    [Required(ErrorMessage = "Mở lại phiếu kiểm để lấy phiên bản hiện tại."), Range(0, int.MaxValue)]
    public int? PhienBan { get; set; }
    public List<DongKiemFormViewModel> Dongs { get; set; } = [];
    public bool GuiDuyet { get; set; }
}

public sealed class DongKiemFormViewModel
{
    [Required]
    public string MaHang { get; set; } = "";
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng thực đếm phải là số nguyên không âm.")]
    public int? SoLuong { get; set; }
    [StringLength(500)]
    public string? GiaiTrinh { get; set; }
}

public sealed class KyBaoCaoViewModel
{
    [ModelBinder(BinderType = typeof(NgayKhoModelBinder))]
    public DateOnly Tu { get; set; } = new(2026, 10, 1);
    [ModelBinder(BinderType = typeof(NgayKhoModelBinder))]
    public DateOnly Den { get; set; } = new(2026, 10, 31);
    public string? MaHang { get; set; }
    public string? MaNhom { get; set; }
    public string? MaNcc { get; set; }
}

public sealed record TheKhoViewModel(KyBaoCaoViewModel Ky, IReadOnlyList<MatHangKhoDemo> MatHangs, TheKhoDemo? The);
public sealed record ChiTietKiemViewModel(PhieuKiemDemo Phieu, IReadOnlyDictionary<string, string> TenHangs);
public sealed record XuatNhapTonViewModel(KyBaoCaoViewModel Ky, IReadOnlyList<NhomHangDemo> NhomHangs, IReadOnlyList<XuatNhapTonDemo> Dongs);
public sealed record CongNoNccViewModel(KyBaoCaoViewModel Ky, IReadOnlyList<NhaCungCapDemo> NhaCungCaps, IReadOnlyList<CongNoNccDemo> Dongs);
