using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class PhieuTra : ChungTuNghiepVu
{
    private readonly List<DongTra> _dongTras = [];
    private readonly List<KhoanHoan> _khoanHoans = [];

    public PhieuTra(
        string maPhieuTra,
        string maHoaDon,
        string maCaNhan,
        string maNguoiNhan,
        DateTime lucLap)
    {
        MaPhieuTra = QuyTac.Chuoi(maPhieuTra, 32);
        MaHoaDon = QuyTac.Chuoi(maHoaDon, 32);
        MaCaNhan = QuyTac.Chuoi(maCaNhan, 32);
        MaNguoiNhan = QuyTac.Chuoi(maNguoiNhan, 32);
        LucLap = QuyTac.Utc(lucLap);
        TrangThai = TrangThaiChungTu.Nhap;
    }

    public string MaPhieuTra { get; }
    public string MaHoaDon { get; }
    public string MaCaNhan { get; }
    public string MaNguoiNhan { get; }
    public DateTime LucLap { get; }
    public DateTime? LucXacNhan { get; private set; }
    public string TrangThai { get; private set; }

    public override decimal TinhGiaTriAnhHuong() => -_khoanHoans.Sum(khoan => khoan.SoTienHoan);
    public override bool CoTheHuy() => TrangThai == TrangThaiChungTu.Nhap;
    public override string TaoTomTatHienThi() => $"Phiếu trả {MaPhieuTra}";

    public void XacNhan(DateTime lucXacNhan, decimal tongTienTra)
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        QuyTac.Dat(_dongTras.Count > 0, "Chứng từ phải có dòng hàng.");
        QuyTac.Dat(_khoanHoans.Sum(khoan => khoan.SoTienHoan) == QuyTac.Tien(tongTienTra), "Tổng hoàn tiền không khớp giá trị trả.");
        LucXacNhan = QuyTac.Utc(lucXacNhan, LucLap);
        TrangThai = TrangThaiChungTu.DaXacNhan;
    }

    public IReadOnlyList<DongTra> DongTras => _dongTras.AsReadOnly();

    public void ThemDongTra(DongTra dong)
    {
        ArgumentNullException.ThrowIfNull(dong);
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        QuyTac.Dat(dong.MaPhieuTra == MaPhieuTra, "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(_dongTras.All(hienCo => hienCo.MaDongTra != dong.MaDongTra), "Dòng đã tồn tại.");
        _dongTras.Add(dong);
    }

    public IReadOnlyList<KhoanHoan> KhoanHoans => _khoanHoans.AsReadOnly();

    public void ThemKhoanHoan(KhoanHoan dong)
    {
        ArgumentNullException.ThrowIfNull(dong);
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        QuyTac.Dat(dong.MaPhieuTra == MaPhieuTra, "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(_khoanHoans.All(hienCo => hienCo.MaKhoanHoan != dong.MaKhoanHoan), "Dòng đã tồn tại.");
        _khoanHoans.Add(dong);
    }
}
