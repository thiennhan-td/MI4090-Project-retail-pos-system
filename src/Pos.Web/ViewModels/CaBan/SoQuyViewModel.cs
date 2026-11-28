using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Pos.Web.Demo;

namespace Pos.Web.ViewModels.CaBan;

public record LocCaViewModel(string? Ma = null, string? Chon = null,
    DateOnly? Tu = null, DateOnly? Den = null, string? TrangThai = null, string? PhuongThuc = null);
public record PhieuQuyViewModel(string MaCa, string Loai, string Nguon, string PhuongThuc,
    long SoTien, [StringLength(500)] string LyDo, [StringLength(64)] string? ChungTu, Guid MaYeuCau);
public record DongQuyViewModel(string MaCa, DateTime Luc, string Ma, string Loai, string Nguon,
    string PhuongThuc, decimal GiaTri, string LyDo = "", string? ChungTu = null, string? MaNguoiLapHoaDon = null)
{
    public decimal TienKet => Nguon == "KetCa" ? GiaTri : 0;
}
public record BanGiaoViewModel(decimal Dau, decimal? Chot = null, decimal? Dem = null, DateTime? Dong = null);
public record CaBanViewModel(LocCaViewModel Loc, CaBanDemoItem[] CacCa, CaBanDemoItem[] LichSu,
    CaBanDemoItem? Chon, BanGiaoViewModel? BanGiao, DongQuyViewModel[] So, DongQuyViewModel[] DongCa,
    bool CoTheMo, bool CoTheDong, bool CoTheLapThuChi)
{
    public decimal Tong(string loai) => DongCa.Where(d => d.Loai == loai).Sum(d => d.TienKet);
    public decimal DuKien => BanGiao?.Chot ?? ((BanGiao?.Dau ?? 0) + DongCa.Sum(d => d.TienKet));
    public static string Tien(decimal n) => n.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
}
