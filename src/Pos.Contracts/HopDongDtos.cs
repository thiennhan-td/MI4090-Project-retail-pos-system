namespace Pos.Contracts;

public sealed record DongXuatBanDto(string MaDongBan, string MaHang, int SoLuongBan);
public sealed record DongNhapTraDto(string MaDongTra, string MaHang, int SoLuongNhapLai);

public sealed record YeuCauXuatBanDto(string MaHoaDon, DateTime LucXacNhanUtc,
    IReadOnlyList<DongXuatBanDto> DongHang);
public sealed record YeuCauDaoXuatBanDto(string MaHoaDon, DateTime LucHuyUtc,
    IReadOnlyList<DongXuatBanDto> DongHang);
public sealed record YeuCauDaoHuyBanDto(string MaHoaDon, DateTime LucHuyUtc,
    IReadOnlyList<DongXuatBanDto> DongHang);
public sealed record YeuCauNhapTraDto(string MaPhieuTra, DateTime LucXacNhanUtc,
    IReadOnlyList<DongNhapTraDto> DongHang);

public sealed record KetQuaGhiTheKhoDto(int SoDongDaGhi, int SoDongDaTonTai);
public sealed record MatHangBanDto(string MaHang, string MaNhom, string TenHang, string DonViTinh,
    decimal GiaBanHienHanh);
public sealed record NhomHangDto(string MaNhom, string TenNhom);
public sealed record ThongTinCaMoDto(string MaCa, string MaNguoiMo, DateTime LucMoUtc);

public sealed record TongHopBanTraCaDto(int SoHoaDon, decimal TongGiaTriBan,
    decimal ThuBanTienMat, decimal ThuBanChuyenKhoan, decimal HoanTienMat, decimal HoanChuyenKhoan,
    int SoHoaDonHuy, decimal DaoHuyTienMat, decimal DaoHuyChuyenKhoan);
public sealed record TongHopChiNccTrongKyDto(decimal ChiTuKetCa, decimal ChiQuyCuaHangTienMat,
    decimal ChiQuyCuaHangChuyenKhoan);
