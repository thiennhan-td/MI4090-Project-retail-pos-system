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
    public string MaNhaCungCap { get; private set; }
    public string MaNguoiLap { get; }
    public string? MaNguoiHuy { get; private set; }
    public DateTime LucLap { get; }
    public DateTime? LucXacNhan { get; private set; }
    public string TrangThai { get; private set; }
    public DateTime? LucHuy { get; private set; }
    public string? LyDoHuy { get; private set; }
    public DateOnly? HanThanhToan { get; private set; }

    public decimal DaTra => _khoanTras.Sum(khoan => khoan.SoTienTra);
    public decimal DuNo => TrangThai == TrangThaiChungTu.DaXacNhan ? TinhGiaTriAnhHuong() - DaTra : 0;

    public override decimal TinhGiaTriAnhHuong() => _dongNhaps.Sum(dong => dong.ThanhTien);
    public override bool CoTheHuy() => TrangThai is TrangThaiChungTu.Nhap or TrangThaiChungTu.DaXacNhan;
    public override string TaoTomTatHienThi() => $"Phiếu nhập {MaPhieuNhap}";

    public static PhieuNhap KhoiPhuc(string maPhieuNhap, string maNhaCungCap, string maNguoiLap,
        DateTime lucLap, DateOnly? hanThanhToan, string trangThai, DateTime? lucXacNhan,
        string? maNguoiHuy, DateTime? lucHuy, string? lyDoHuy, IEnumerable<DongNhap> dongNhaps,
        IEnumerable<TraNhaCungCap> khoanTras)
    {
        var phieu = new PhieuNhap(maPhieuNhap, maNhaCungCap, maNguoiLap, lucLap, hanThanhToan)
        {
            TrangThai = trangThai,
            LucXacNhan = lucXacNhan,
            MaNguoiHuy = maNguoiHuy,
            LucHuy = lucHuy,
            LyDoHuy = lyDoHuy
        };
        phieu._dongNhaps.AddRange(dongNhaps);
        phieu._khoanTras.AddRange(khoanTras);
        return phieu;
    }

    public void LuuNhap(string maNhaCungCap, DateOnly? hanThanhToan, IReadOnlyList<DongNhap> dongNhaps)
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.Nhap);
        var nhaCungCap = QuyTac.Chuoi(maNhaCungCap, 32);
        var dongs = dongNhaps.ToArray();
        QuyTac.Dat(dongs.Length > 0, "Phiếu nhập phải có dòng hàng.");
        QuyTac.Dat(dongs.All(dong => dong.MaPhieuNhap == MaPhieuNhap), "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(dongs.Select(dong => dong.MaDongNhap).Distinct(StringComparer.Ordinal).Count() == dongs.Length,
            "Mã dòng nhập đã tồn tại.");
        QuyTac.Dat(dongs.Select(dong => dong.MaHang).Distinct(StringComparer.Ordinal).Count() == dongs.Length,
            "Mỗi mặt hàng chỉ xuất hiện một lần trên phiếu nhập.");

        decimal tongTien = 0;
        foreach (var dong in dongs)
        {
            QuyTac.Dat(decimal.MaxValue - tongTien >= dong.ThanhTien, "Tổng giá trị phiếu nhập quá lớn.");
            tongTien += dong.ThanhTien;
        }

        MaNhaCungCap = nhaCungCap;
        HanThanhToan = hanThanhToan;
        _dongNhaps.Clear();
        _dongNhaps.AddRange(dongs);
    }

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
        var lyDo = QuyTac.Chuoi(lyDoHuy?.Trim() ?? string.Empty, 500);
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
        QuyTac.Dat(dong.SoTienTra <= DuNo, "Số tiền trả vượt dư nợ phiếu nhập.");
        QuyTac.Dat(dong.LucTra >= LucXacNhan, "Khoản trả phải ghi sau khi xác nhận phiếu nhập.");
        _khoanTras.Add(dong);
    }
}
