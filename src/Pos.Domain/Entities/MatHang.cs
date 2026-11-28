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
    public string MaNhom { get; private set; }
    public string TenHang { get; private set; }
    public string DonViTinh { get; private set; }
    public decimal GiaBanHienHanh { get; private set; }
    public string TrangThai { get; private set; }

    public void DoiGiaBan(decimal gia) => GiaBanHienHanh = QuyTac.Tien(gia);
    public void CapNhatDanhMuc(string maNhom, string tenHang, string donViTinh, decimal gia)
    {
        var nhom = QuyTac.Chuoi(maNhom, 32);
        var ten = QuyTac.Chuoi(tenHang, 150);
        var donVi = QuyTac.Chuoi(donViTinh, 32);
        var giaHopLe = QuyTac.Tien(gia);
        MaNhom = nhom;
        TenHang = ten;
        DonViTinh = donVi;
        GiaBanHienHanh = giaHopLe;
    }
    public void NgungSuDung() => TrangThai = TrangThaiChungTu.NgungSuDung;
    public void KichHoat() => TrangThai = TrangThaiChungTu.HoatDong;
}
