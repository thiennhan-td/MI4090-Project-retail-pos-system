using System.ComponentModel.DataAnnotations;
using Pos.Web.Demo;

namespace Pos.Web.ViewModels.BanHang;

public sealed class TraCuuHoaDonViewModel
{
    public string? MaHoaDon { get; set; }
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public string? MaCa { get; set; }
    public string TrangThai { get; set; } = "TatCa";
    public string VaiTroDemo { get; set; } = "ThuNgan";
    public bool CoCaDaoMo { get; set; } = true;
    public string? MaHoaDonDangChon { get; set; }
    public string? ThongBaoThanhCong { get; set; }
    public string? ThongBaoLoi { get; set; }
    public string? ThongBaoLoiHuy { get; init; }
    public string? LyDoHuyNhap { get; init; }
    public bool CoTheSuaNhap { get; init; }

    public IReadOnlyList<CaBanDemoItem> DanhSachCaBan { get; init; } = [];
    public IReadOnlyList<HoaDonDemoRecord> DanhSachHoaDon { get; init; } = [];
    public HoaDonDemoRecord? HoaDonDangChon { get; init; }
}

public sealed class HuyHoaDonRequestViewModel
{
    [Required(ErrorMessage = "Thiếu mã hóa đơn cần hủy.")]
    public string MaHoaDon { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Phiên bản hóa đơn không hợp lệ.")]
    public int? PhienBan { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do hủy hóa đơn.")]
    [StringLength(500, ErrorMessage = "Lý do hủy tối đa 500 ký tự.")]
    public string LyDoHuy { get; set; } = string.Empty;

    public string VaiTroDemo { get; set; } = "ThuNgan";
    public bool CoCaDaoMo { get; set; } = true;
}

public sealed class InHoaDonViewModel
{
    public required HoaDonDemoRecord HoaDon { get; init; }
    public string TenCuaHang { get; init; } = "Cửa hàng Retail Core POS";
    public string DiaChiCuaHang { get; init; } = "12 Phố Huế, Hai Bà Trưng, Hà Nội";
    public string SoDienThoaiCuaHang { get; init; } = "024 3825 0000";
    public string? QuayLaiUrl { get; init; }
}
