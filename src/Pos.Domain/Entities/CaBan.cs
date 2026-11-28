using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class CaBan
{
    public CaBan(
        string maCa,
        string maNguoiMo,
        DateTime lucMo,
        decimal tienMatDauCa)
    {
        MaCa = QuyTac.Chuoi(maCa, 32);
        MaNguoiMo = QuyTac.Chuoi(maNguoiMo, 32);
        LucMo = QuyTac.Utc(lucMo);
        TienMatDauCa = QuyTac.Tien(tienMatDauCa);
        TrangThai = TrangThaiChungTu.DangMo;
    }

    public string MaCa { get; }
    public string MaNguoiMo { get; }
    public string? MaNguoiChot { get; private set; }
    public DateTime LucMo { get; }
    public decimal TienMatDauCa { get; }
    public string TrangThai { get; private set; }
    public DateTime? LucDong { get; private set; }
    public decimal? TienDuKienKhiChot { get; private set; }
    public decimal? TienThucDem { get; private set; }

    public decimal? ChenhLech => TienThucDem - TienDuKienKhiChot;

    public void Dong(string maNguoiChot, DateTime lucDong, decimal tienDuKien, decimal tienThucDem)
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.DangMo);
        QuyTac.Dat(maNguoiChot == MaNguoiMo, "Chỉ người mở ca được đóng ca.");
        var luc = QuyTac.Utc(lucDong, LucMo);
        var duKien = QuyTac.TienCoDau(tienDuKien);
        var thucDem = QuyTac.Tien(tienThucDem);
        MaNguoiChot = maNguoiChot;
        LucDong = luc;
        TienDuKienKhiChot = duKien;
        TienThucDem = thucDem;
        TrangThai = TrangThaiChungTu.DaDong;
    }
}
