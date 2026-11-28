using System.ComponentModel.DataAnnotations;
using Pos.Web.Demo;

namespace Pos.Web.ViewModels.BanHang;

public sealed class BanHangPosViewModel
{
    public required string MaCaHienTai { get; init; }
    public required string TenThuNgan { get; init; }
    public required string MaYeuCauMacDinh { get; init; }
    public HoaDonDemoRecord? HoaDonNhap { get; init; }
    public string? ThongBaoLoi { get; init; }
    public string? TraCuuUrl { get; init; }
    public IReadOnlyList<string> DanhSachNhomHang { get; init; } = [];
    public IReadOnlyList<MatHangDemoItem> DanhSachMatHang { get; init; } = [];
}

public sealed class DongBanYeuCauViewModel
{
    [Required(ErrorMessage = "Thiếu mã mặt hàng.")]
    public string MaHang { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Số lượng bán phải là số nguyên dương.")]
    public int SoLuong { get; set; }
}

public sealed class GhiHoaDonRequestViewModel
{
    [Required(ErrorMessage = "Thiếu mã yêu cầu giao dịch.")]
    public string MaYeuCau { get; set; } = string.Empty;

    public string? MaNhap { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Phiên bản hóa đơn nháp không hợp lệ.")]
    public int? PhienBan { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    public string CheDoThanhToan { get; set; } = "TienMat";

    public decimal? TienMatPhanBo { get; set; }
    public decimal? TienKhachDua { get; set; }
    public decimal? TienChuyenKhoan { get; set; }
    public bool CaDangMo { get; set; } = true;
    public List<DongBanYeuCauViewModel> DongBans { get; set; } = [];
}
