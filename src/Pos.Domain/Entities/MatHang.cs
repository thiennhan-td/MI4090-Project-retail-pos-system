using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class MatHang
{
    public MatHang(
        string maHang,
        string maNhom,
        string tenHang,
        string donViTinh,
        decimal giaBanHienHanh)
    {
        MaHang = QuyTac.Chuoi(maHang, 32);
        MaNhom = QuyTac.Chuoi(maNhom, 32);
        TenHang = QuyTac.Chuoi(tenHang, 150);
        DonViTinh = QuyTac.Chuoi(donViTinh, 32);
        GiaBanHienHanh = QuyTac.Tien(giaBanHienHanh);
        TrangThai = TrangThaiChungTu.HoatDong;
    }

    public string MaHang { get; }
    public string MaNhom { get; }
    public string TenHang { get; }
    public string DonViTinh { get; }
    public decimal GiaBanHienHanh { get; private set; }
    public string TrangThai { get; private set; }

    public void DoiGiaBan(decimal gia) => GiaBanHienHanh = QuyTac.Tien(gia);
    public void NgungSuDung() => TrangThai = TrangThaiChungTu.NgungSuDung;
    public void KichHoat() => TrangThai = TrangThaiChungTu.HoatDong;
}
