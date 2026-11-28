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

public enum LoaiSuKienNhapCongNo
{
    XacNhanNhap = 1,
    DaoNhap = 2,
    TraNhaCungCap = 3
}

/// <summary>Giá trị gốc của một nghiệp vụ. Đảo nhập và khoản trả được trừ khi tính dư nợ.</summary>
public sealed record DongChiTietNhapCongNoDto(
    LoaiSuKienNhapCongNo LoaiSuKien, DateTime ThoiDiemUtc, string MaPhieuNhap,
    string MaNhaCungCap, string? MaKhoanTra, decimal SoTien, bool TruocKy);

public sealed record ChiNccTheoKyDto(
    decimal ChiTuKetCa, decimal ChiQuyCuaHangTienMat, decimal ChiQuyCuaHangChuyenKhoan);
