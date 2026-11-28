namespace Pos.Contracts;

public interface IPhuongThucThanhToanStrategy
{
    string LoaiPhuongThuc { get; }
    void KiemTraHopLe(decimal soTien, decimal? tienKhachDua);
    decimal TinhTienThua(decimal soTien, decimal? tienKhachDua);
    decimal TinhBienDongKetCa(decimal soTien, bool laKhoanThu);
}
