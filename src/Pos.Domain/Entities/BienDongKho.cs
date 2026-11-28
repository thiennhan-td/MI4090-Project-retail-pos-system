using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class BienDongKho
{
    private BienDongKho(string maBienDong, string maHang, string loaiSuKien, int soLuongThayDoi,
        DateTime thoiDiemGhi, string? maDongBan, string? maDongTra, string? maDongNhap, string? maPhieuKiem)
    {
        MaBienDong = QuyTac.Chuoi(maBienDong, 32);
        MaHang = QuyTac.Chuoi(maHang, 32);
        var (nguon, dungDau) = loaiSuKien switch
        {
            LoaiBienDongKho.XuatBan => (maDongBan, soLuongThayDoi < 0),
            LoaiBienDongKho.DaoXuatBan => (maDongBan, soLuongThayDoi > 0),
            LoaiBienDongKho.NhapTraHang => (maDongTra, soLuongThayDoi > 0),
            LoaiBienDongKho.NhapKho => (maDongNhap, soLuongThayDoi > 0),
            LoaiBienDongKho.DaoNhapKho => (maDongNhap, soLuongThayDoi < 0),
            LoaiBienDongKho.DieuChinhKiemKho => (maPhieuKiem, soLuongThayDoi != 0),
            _ => (null, false)
        };
        QuyTac.Dat(nguon is not null && dungDau && new[] { maDongBan, maDongTra, maDongNhap, maPhieuKiem }
            .Count(ma => ma is not null) == 1, "Nguồn hoặc dấu của biến động kho không hợp lệ.");
        var maNguon = QuyTac.Chuoi(nguon!, 32);
        MaDongBan = maDongBan;
        MaDongTra = maDongTra;
        MaDongNhap = maDongNhap;
        MaPhieuKiem = maPhieuKiem;
        LoaiSuKien = loaiSuKien;
        SoLuongThayDoi = soLuongThayDoi;
        ThoiDiemGhi = QuyTac.Utc(thoiDiemGhi);
        KhoaSuKien = maPhieuKiem is null ? $"{loaiSuKien}:{maNguon}" : $"{loaiSuKien}:{maNguon}:{MaHang}";
    }

    public static BienDongKho Tao(string maBienDong, string maHang, string loaiSuKien, int soLuongThayDoi,
        DateTime thoiDiemGhi, string? maDongBan = null, string? maDongTra = null,
        string? maDongNhap = null, string? maPhieuKiem = null)
        => new(maBienDong, maHang, loaiSuKien, soLuongThayDoi, thoiDiemGhi,
            maDongBan, maDongTra, maDongNhap, maPhieuKiem);

    public string MaBienDong { get; }
    public string MaHang { get; }
    public string? MaDongBan { get; }
    public string? MaDongTra { get; }
    public string? MaDongNhap { get; }
    public string? MaPhieuKiem { get; }
    public string LoaiSuKien { get; }
    public int SoLuongThayDoi { get; }
    public DateTime ThoiDiemGhi { get; }
    public string KhoaSuKien { get; }
}
