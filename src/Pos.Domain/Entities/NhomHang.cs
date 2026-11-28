using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class NhomHang
{
    public NhomHang(
        string maNhom,
        string tenNhom)
    {
        MaNhom = QuyTac.Chuoi(maNhom, 32);
        TenNhom = QuyTac.Chuoi(tenNhom, 150);
        TrangThai = TrangThaiChungTu.HoatDong;
    }

    public string MaNhom { get; }
    public string TenNhom { get; }
    public string TrangThai { get; private set; }

    public void NgungSuDung() => TrangThai = TrangThaiChungTu.NgungSuDung;
    public void KichHoat() => TrangThai = TrangThaiChungTu.HoatDong;
}
