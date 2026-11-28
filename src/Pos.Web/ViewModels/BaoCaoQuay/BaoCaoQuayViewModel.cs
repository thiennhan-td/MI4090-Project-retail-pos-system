namespace Pos.Web.ViewModels.BaoCaoQuay;

public sealed record LocBaoCaoQuayViewModel(
    DateOnly? Ngay, DateOnly? Tu, DateOnly? Den, string? MaHang, string? MaCa,
    string? TrangThaiCa, string VaiTro = "QuanLy")
{
    public Dictionary<string, string> ThamSo()
    {
        var values = new Dictionary<string, string> { ["vaiTro"] = VaiTro };
        if (Ngay is { } ngay) values["ngay"] = ngay.ToString("yyyy-MM-dd");
        if (Tu is { } tu) values["tu"] = tu.ToString("yyyy-MM-dd");
        if (Den is { } den) values["den"] = den.ToString("yyyy-MM-dd");
        if (MaHang is not null) values["maHang"] = MaHang;
        if (MaCa is not null) values["maCa"] = MaCa;
        if (TrangThaiCa is not null) values["trangThaiCa"] = TrangThaiCa;
        return values;
    }
}

public sealed record ChiSoBaoCaoQuayViewModel(string Nhan, decimal GiaTri, bool LaTien = true);

public sealed record BangBaoCaoQuayViewModel(
    string TieuDe, string[] Cot, object?[][] Dong, int CotTienTu, int? CotChenhLech = null,
    object?[]? Tong = null);

public sealed record BaoCaoQuayViewModel(
    LocBaoCaoQuayViewModel Loc, string HanhDong, string TieuDe,
    ChiSoBaoCaoQuayViewModel[] ChiSo, BangBaoCaoQuayViewModel[] Bang,
    string[]? MaCa = null, string? GhiChu = null);
