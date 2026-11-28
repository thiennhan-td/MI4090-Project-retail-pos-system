using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class HoaDon : ChungTuNghiepVu
{
    private readonly List<DongBan> _dongBans = [];
    private readonly List<ThanhToanBan> _thanhToanBans = [];

    public HoaDon(
        string maHoaDon,
        string maCa,
        string maNguoiLap,
        DateTime lucLap)
    {
        MaHoaDon = QuyTac.Chuoi(maHoaDon, 32);
        MaCa = QuyTac.Chuoi(maCa, 32);
        MaNguoiLap = QuyTac.Chuoi(maNguoiLap, 32);
        LucLap = QuyTac.Utc(lucLap);
        TrangThai = TrangThaiChungTu.Nhap;
    }

    public string MaHoaDon { get; }
    public string MaCa { get; }
    public string MaNguoiLap { get; }
    public string? MaNguoiHuy { get; private set; }
    public string? MaCaDao { get; private set; }
    public DateTime LucLap { get; }
    public DateTime? LucXacNhan { get; private set; }
    public string TrangThai { get; private set; }
    public DateTime? LucHuy { get; private set; }
    public string? LyDoHuy { get; private set; }

    public override decimal TinhGiaTriAnhHuong() => _dongBans.Sum(dong => dong.ThanhTien);
    public override bool CoTheHuy() => TrangThai is TrangThaiChungTu.Nhap or TrangThaiChungTu.DaXacNhan;
    public override string TaoTomTatHienThi() => $"Hóa đơn {MaHoaDon}";

    public void XacNhan(DateTime lucXacNhan)
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        QuyTac.Dat(_dongBans.Count > 0, "Chứng từ phải có dòng hàng.");
        QuyTac.Dat(_thanhToanBans.Sum(khoan => khoan.SoTienThanhToan) == _dongBans.Sum(dong => dong.ThanhTien),
            "Tổng thanh toán không khớp giá trị bán.");
        LucXacNhan = QuyTac.Utc(lucXacNhan, LucLap);
        TrangThai = TrangThaiChungTu.DaXacNhan;
    }

    public void Huy(string maNguoiHuy, DateTime lucHuy, string lyDoHuy, string? maCaDao, bool coPhieuTraHieuLuc)
    {
        QuyTac.TrangThai(CoTheHuy());
        QuyTac.Dat(!coPhieuTraHieuLuc, "Hóa đơn đã có phiếu trả hiệu lực.");
        var canDaoKet = LucXacNhan is not null &&
            _thanhToanBans.Any(khoan => khoan.PhuongThuc == PhuongThucThanhToan.TienMat);
        QuyTac.Dat(canDaoKet == (maCaDao is not null), "Thông tin ca đảo tiền mặt không hợp lệ.");
        var caDao = maCaDao is null ? null : QuyTac.Chuoi(maCaDao, 32);
        var nguoiHuy = QuyTac.Chuoi(maNguoiHuy, 32);
        var luc = QuyTac.Utc(lucHuy, LucXacNhan ?? LucLap);
        var lyDo = QuyTac.Chuoi(lyDoHuy, 500);
        MaNguoiHuy = nguoiHuy;
        LucHuy = luc;
        LyDoHuy = lyDo;
        MaCaDao = caDao;
        TrangThai = TrangThaiChungTu.DaHuy;
    }

    public IReadOnlyList<DongBan> DongBans => _dongBans.AsReadOnly();

    public void ThemDongBan(DongBan dong)
    {
        ArgumentNullException.ThrowIfNull(dong);
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        QuyTac.Dat(dong.MaHoaDon == MaHoaDon, "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(_dongBans.All(hienCo => hienCo.MaDongBan != dong.MaDongBan), "Dòng đã tồn tại.");
        _dongBans.Add(dong);
    }

    public IReadOnlyList<ThanhToanBan> ThanhToanBans => _thanhToanBans.AsReadOnly();

    public void ThemThanhToanBan(ThanhToanBan dong)
    {
        ArgumentNullException.ThrowIfNull(dong);
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        QuyTac.Dat(dong.MaHoaDon == MaHoaDon, "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(_thanhToanBans.All(hienCo => hienCo.MaThanhToan != dong.MaThanhToan), "Dòng đã tồn tại.");
        _thanhToanBans.Add(dong);
    }
}
