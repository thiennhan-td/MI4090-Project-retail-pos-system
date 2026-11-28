namespace Pos.Domain.Reports;

public sealed record DongXuatNhapTonDto(
    string MaHang, string TenHang, string TenNhom, string DonViTinh,
    long TonDau, long NhapMuaRong, long XuatBanRong, long TraNhapLai,
    long DieuChinhTon, long TongNhap, long TongXuat)
{
    public long TonCuoi => checked(TonDau + (TongNhap - TongXuat));
}

public sealed record DongNhapCongNoDto(
    string MaNhaCungCap, string TenNhaCungCap,
    decimal DuDau, decimal GiaTriNhap, decimal GiaTriDaoNhap, decimal DaTra)
{
    public decimal DuCuoi => DuDau + GiaTriNhap - GiaTriDaoNhap - DaTra;
}

public sealed record ChiNccTheoKyDto(
    decimal ChiTuKetCa, decimal ChiQuyCuaHangTienMat, decimal ChiQuyCuaHangChuyenKhoan);
