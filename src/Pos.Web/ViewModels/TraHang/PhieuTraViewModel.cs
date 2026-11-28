namespace Pos.Web.ViewModels.TraHang;

public record DongChonViewModel(int Dong, int SoLuong, int NhapLai);

public record YeuCauTraViewModel(
    string MaHoaDon,
    string MaYeuCau,
    string? MaNhap,
    bool CaMo,
    DongChonViewModel[] DongTra,
    decimal TienMat,
    decimal ChuyenKhoan);

public record DongGocViewModel(
    int Dong,
    string MaHang,
    string TenHang,
    string Dvt,
    decimal Gia,
    int DaMua,
    int DaTra);

public record LapTraViewModel(
    string? MaHoaDon,
    string? Loi,
    DongGocViewModel[] Dong,
    PhieuTraViewModel? NhapDangMo = null);

public record DongTraViewModel(
    int Dong,
    string TenHang,
    decimal Gia,
    int SoLuong,
    int NhapLai);

public record PhieuTraViewModel(
    string Ma,
    string HoaDon,
    string Ca,
    string Nguoi,
    DateTime Ngay,
    bool XacNhan,
    DongTraViewModel[] Dong,
    decimal TienMat,
    decimal ChuyenKhoan);

public record LocTraViewModel(
    string? Ma,
    string? HoaDon,
    DateOnly? TuNgay,
    DateOnly? DenNgay,
    string? Ca,
    string? TrangThai,
    string? VaiTro,
    string? Chon,
    bool CaMo = true);

public record TraCuuTraViewModel(
    LocTraViewModel Loc,
    PhieuTraViewModel[] Phieu,
    PhieuTraViewModel? Chon,
    string[] CacCa);
