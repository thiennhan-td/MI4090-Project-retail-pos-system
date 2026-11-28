using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class PhieuKiem : ChungTuNghiepVu
{
    private readonly List<DongKiem> _dongKiems = [];

    public PhieuKiem(
        string maPhieuKiem,
        string maNguoiLap,
        DateTime mocKiem)
    {
        MaPhieuKiem = QuyTac.Chuoi(maPhieuKiem, 32);
        MaNguoiLap = QuyTac.Chuoi(maNguoiLap, 32);
        MocKiem = QuyTac.Utc(mocKiem);
        TrangThai = TrangThaiChungTu.DangKiem;
    }

    public string MaPhieuKiem { get; }
    public string MaNguoiLap { get; }
    public string? MaNguoiDuyet { get; private set; }
    public DateTime MocKiem { get; }
    public DateTime? LucHoanTat { get; private set; }
    public string TrangThai { get; private set; }

    public override decimal TinhGiaTriAnhHuong() => 0m;
    public override bool CoTheHuy() => TrangThai is TrangThaiChungTu.DangKiem or TrangThaiChungTu.ChoDuyet;
    public override string TaoTomTatHienThi() => $"Phiếu kiểm {MaPhieuKiem}";

    public void GuiDuyet()
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.DangKiem);
        QuyTac.Dat(_dongKiems.Count > 0 && _dongKiems.All(dong => dong.SoLuongDem.HasValue),
            "Phải đếm tất cả các dòng trước khi gửi duyệt.");
        TrangThai = TrangThaiChungTu.ChoDuyet;
    }

    public void Duyet(string maNguoiDuyet, DateTime lucHoanTat)
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.ChoDuyet);
        QuyTac.Dat(maNguoiDuyet != MaNguoiLap, "Người lập không được tự duyệt.");
        var nguoiDuyet = QuyTac.Chuoi(maNguoiDuyet, 32);
        var luc = QuyTac.Utc(lucHoanTat, MocKiem);
        MaNguoiDuyet = nguoiDuyet;
        LucHoanTat = luc;
        TrangThai = TrangThaiChungTu.HoanTat;
    }

    public void Huy()
    {
        QuyTac.TrangThai(CoTheHuy());
        TrangThai = TrangThaiChungTu.DaHuy;
    }

    public IReadOnlyList<DongKiem> DongKiems => _dongKiems.AsReadOnly();

    public void ThemDongKiem(DongKiem dong)
    {
        ArgumentNullException.ThrowIfNull(dong);
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.DangKiem);
        QuyTac.Dat(dong.MaPhieuKiem == MaPhieuKiem, "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(_dongKiems.All(hienCo => hienCo.MaHang != dong.MaHang), "Dòng đã tồn tại.");
        _dongKiems.Add(dong);
    }
}
