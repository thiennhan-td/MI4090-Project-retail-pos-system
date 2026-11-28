using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class PhieuNhap : ChungTuNghiepVu
{
    private readonly List<DongNhap> _dongNhaps = [];
    private readonly List<TraNhaCungCap> _khoanTras = [];

    public PhieuNhap(
        string maPhieuNhap,
        string maNhaCungCap,
        string maNguoiLap,
        DateTime lucLap,
        DateOnly? hanThanhToan)
    {
        MaPhieuNhap = QuyTac.Chuoi(maPhieuNhap, 32);
        MaNhaCungCap = QuyTac.Chuoi(maNhaCungCap, 32);
        MaNguoiLap = QuyTac.Chuoi(maNguoiLap, 32);
        LucLap = QuyTac.Utc(lucLap);
        HanThanhToan = hanThanhToan;
        TrangThai = TrangThaiChungTu.Nhap;
    }

    public string MaPhieuNhap { get; }
    public string MaNhaCungCap { get; }
    public string MaNguoiLap { get; }
    public string? MaNguoiHuy { get; private set; }
    public DateTime LucLap { get; }
    public DateTime? LucXacNhan { get; private set; }
    public string TrangThai { get; private set; }
    public DateTime? LucHuy { get; private set; }
    public string? LyDoHuy { get; private set; }
    public DateOnly? HanThanhToan { get; }

    public override decimal TinhGiaTriAnhHuong() => _dongNhaps.Sum(dong => dong.ThanhTien);
    public override bool CoTheHuy() => TrangThai is TrangThaiChungTu.Nhap or TrangThaiChungTu.DaXacNhan;
    public override string TaoTomTatHienThi() => $"Phiếu nhập {MaPhieuNhap}";

    public void XacNhan(DateTime lucXacNhan)
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        QuyTac.Dat(_dongNhaps.Count > 0, "Chứng từ phải có dòng hàng.");
        LucXacNhan = QuyTac.Utc(lucXacNhan, LucLap);
        TrangThai = TrangThaiChungTu.DaXacNhan;
    }

    public void Huy(string maNguoiHuy, DateTime lucHuy, string lyDoHuy)
    {
        QuyTac.TrangThai(CoTheHuy());
        QuyTac.Dat(_khoanTras.Count == 0, "Phiếu nhập đã có khoản trả nhà cung cấp.");
        var nguoiHuy = QuyTac.Chuoi(maNguoiHuy, 32);
        var luc = QuyTac.Utc(lucHuy, LucXacNhan ?? LucLap);
        var lyDo = QuyTac.Chuoi(lyDoHuy, 500);
        MaNguoiHuy = nguoiHuy;
        LucHuy = luc;
        LyDoHuy = lyDo;
        TrangThai = TrangThaiChungTu.DaHuy;
    }

    public IReadOnlyList<DongNhap> DongNhaps => _dongNhaps.AsReadOnly();

    public void ThemDongNhap(DongNhap dong)
    {
        ArgumentNullException.ThrowIfNull(dong);
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        QuyTac.Dat(dong.MaPhieuNhap == MaPhieuNhap, "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(_dongNhaps.All(hienCo => hienCo.MaDongNhap != dong.MaDongNhap), "Dòng đã tồn tại.");
        _dongNhaps.Add(dong);
    }

    public IReadOnlyList<TraNhaCungCap> KhoanTras => _khoanTras.AsReadOnly();

    public void ThemTraNhaCungCap(TraNhaCungCap dong)
    {
        ArgumentNullException.ThrowIfNull(dong);
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.DaXacNhan);
        QuyTac.Dat(dong.MaPhieuNhap == MaPhieuNhap, "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(_khoanTras.All(hienCo => hienCo.MaKhoanTra != dong.MaKhoanTra), "Dòng đã tồn tại.");
        QuyTac.Dat(_khoanTras.Sum(khoan => khoan.SoTienTra) + dong.SoTienTra <= TinhGiaTriAnhHuong(),
            "Tổng đã trả vượt giá trị phiếu nhập.");
        QuyTac.Dat(dong.LucTra >= LucXacNhan, "Khoản trả phải ghi sau khi xác nhận phiếu nhập.");
        _khoanTras.Add(dong);
    }
}
