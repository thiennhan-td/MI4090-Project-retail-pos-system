using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class PhieuThuChi
{
    public PhieuThuChi(
        string maPhieuThuChi,
        string maCa,
        string maNguoiLap,
        DateTime lucGhi,
        string loaiThuChi,
        string nguonQuy,
        string phuongThuc,
        decimal soTien,
        string lyDo,
        string? soChungTuKemTheo)
    {
        MaPhieuThuChi = QuyTac.Chuoi(maPhieuThuChi, 32);
        MaCa = QuyTac.Chuoi(maCa, 32);
        MaNguoiLap = QuyTac.Chuoi(maNguoiLap, 32);
        LucGhi = QuyTac.Utc(lucGhi);
        LoaiThuChi = QuyTac.Chuoi(loaiThuChi, 24);
        NguonQuy = QuyTac.Chuoi(nguonQuy, 24);
        PhuongThuc = QuyTac.PhuongThuc(phuongThuc);
        SoTien = QuyTac.TienDuong(soTien);
        LyDo = QuyTac.Chuoi(lyDo, 500);
        SoChungTuKemTheo = soChungTuKemTheo is null ? null : QuyTac.Chuoi(soChungTuKemTheo, 64);
        QuyTac.Dat(LoaiThuChi is "Thu" or "Chi", "Loại thu chi không hợp lệ.");
        QuyTac.Dat(NguonQuy is Constants.NguonQuy.KetCa or Constants.NguonQuy.QuyCuaHang,
            "Nguồn quỹ không hợp lệ.");
        QuyTac.Dat(NguonQuy != Constants.NguonQuy.KetCa || PhuongThuc == PhuongThucThanhToan.TienMat,
            "Két ca chỉ giao dịch tiền mặt.");
    }

    public string MaPhieuThuChi { get; }
    public string MaCa { get; }
    public string MaNguoiLap { get; }
    public DateTime LucGhi { get; }
    public string LoaiThuChi { get; }
    public string NguonQuy { get; }
    public string PhuongThuc { get; }
    public decimal SoTien { get; }
    public string LyDo { get; }
    public string? SoChungTuKemTheo { get; }
}
