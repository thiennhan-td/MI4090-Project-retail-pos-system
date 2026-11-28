using System.Security.Cryptography;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;

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
    public string PhienBan => TaoPhienBan();

    public override decimal TinhGiaTriAnhHuong() => 0m;
    public override bool CoTheHuy() => TrangThai is TrangThaiChungTu.DangKiem or TrangThaiChungTu.ChoDuyet;
    public override string TaoTomTatHienThi() => $"Phiếu kiểm {MaPhieuKiem}";

    public static PhieuKiem KhoiPhuc(string maPhieuKiem, string maNguoiLap, DateTime mocKiem, string trangThai,
        string? maNguoiDuyet, DateTime? lucHoanTat, IReadOnlyList<DongKiem> dongKiems)
    {
        var phieu = new PhieuKiem(maPhieuKiem, maNguoiLap, mocKiem);
        foreach (var dong in dongKiems)
        {
            phieu.KiemTraDongKiem(dong);
            phieu._dongKiems.Add(dong);
        }

        if (trangThai is TrangThaiChungTu.ChoDuyet or TrangThaiChungTu.HoanTat)
        {
            phieu.KiemTraKetQuaDem();
        }
        if (trangThai == TrangThaiChungTu.HoanTat)
        {
            QuyTac.Dat(maNguoiDuyet is not null && lucHoanTat.HasValue,
                "Phiếu hoàn tất phải có người duyệt và thời điểm hoàn tất.");
            phieu.LuuThongTinDuyet(maNguoiDuyet!, lucHoanTat!.Value);
        }
        else
        {
            QuyTac.TrangThai(trangThai is TrangThaiChungTu.DangKiem or TrangThaiChungTu.ChoDuyet or TrangThaiChungTu.DaHuy);
            QuyTac.Dat(maNguoiDuyet is null && lucHoanTat is null, "Thông tin duyệt chỉ được lưu khi hoàn tất.");
        }

        phieu.TrangThai = trangThai;
        return phieu;
    }

    public void GuiDuyet()
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.DangKiem);
        KiemTraMocDem("Hãy đếm lại hoặc hủy và lập phiếu mới.");
        KiemTraKetQuaDem();
        TrangThai = TrangThaiChungTu.ChoDuyet;
    }

    private void KiemTraMocDem(string huongDan)
        => QuyTac.Dat(_dongKiems.All(dong => !dong.SoLuongDem.HasValue || dong.LucDem.HasValue),
            $"Phiếu kiểm có số đếm cũ thiếu thời điểm đếm. {huongDan}");

    private void KiemTraKetQuaDem()
    {
        QuyTac.Dat(_dongKiems.Count > 0 && _dongKiems.All(dong => dong.SoLuongDem.HasValue),
            "Phải đếm tất cả các dòng trước khi gửi duyệt.");
        QuyTac.Dat(_dongKiems.All(dong => dong.ChenhLech == 0 || !string.IsNullOrWhiteSpace(dong.GiaiTrinh)),
            "Phải giải trình từng dòng kiểm kho có chênh lệch trước khi gửi duyệt.");
    }

    public void KiemTraCoTheDuyet()
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.ChoDuyet);
        KiemTraMocDem("Hãy hủy và lập phiếu mới để đếm lại.");
    }

    public void Duyet(string maNguoiDuyet, DateTime lucHoanTat)
    {
        KiemTraCoTheDuyet();
        QuyTac.Dat(_dongKiems.All(dong => dong.LucDem <= lucHoanTat),
            "Thời điểm hoàn tất không được trước thời điểm đếm.");
        LuuThongTinDuyet(maNguoiDuyet, lucHoanTat);
        TrangThai = TrangThaiChungTu.HoanTat;
    }

    private void LuuThongTinDuyet(string maNguoiDuyet, DateTime lucHoanTat)
    {
        QuyTac.Dat(maNguoiDuyet != MaNguoiLap, "Người lập không được tự duyệt.");
        var nguoiDuyet = QuyTac.Chuoi(maNguoiDuyet, 32);
        var luc = QuyTac.Utc(lucHoanTat, MocKiem);
        MaNguoiDuyet = nguoiDuyet;
        LucHoanTat = luc;
    }

    public void Huy()
    {
        QuyTac.TrangThai(CoTheHuy());
        TrangThai = TrangThaiChungTu.DaHuy;
    }

    public IReadOnlyList<DongKiem> DongKiems => _dongKiems.AsReadOnly();

    public void GhiSoLuongDem(string maHang, int? soLuongDem, DateTime? lucDem, string? giaiTrinh = null)
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.DangKiem);
        var index = _dongKiems.FindIndex(dong => dong.MaHang == maHang);
        if (index < 0)
        {
            throw new EntityNotFoundException($"Mặt hàng {maHang} không có trong phiếu kiểm.");
        }

        var dong = _dongKiems[index];
        QuyTac.Dat(soLuongDem.HasValue == lucDem.HasValue, "Số đếm mới phải đi cùng thời điểm đếm.");
        if (lucDem.HasValue)
        {
            QuyTac.Utc(lucDem.Value, MocKiem);
        }
        _dongKiems[index] = new DongKiem(MaPhieuKiem, dong.MaHang, dong.TonSoTaiMoc, soLuongDem, giaiTrinh, lucDem);
    }

    public void ThemDongKiem(DongKiem dong)
    {
        QuyTac.TrangThai(TrangThai == TrangThaiChungTu.DangKiem);
        KiemTraDongKiem(dong);
        _dongKiems.Add(dong);
    }

    private void KiemTraDongKiem(DongKiem dong)
    {
        ArgumentNullException.ThrowIfNull(dong);
        QuyTac.Dat(dong.MaPhieuKiem == MaPhieuKiem, "Dòng không thuộc chứng từ này.");
        QuyTac.Dat(_dongKiems.All(hienCo => hienCo.MaHang != dong.MaHang), "Dòng đã tồn tại.");
        QuyTac.Dat(!dong.LucDem.HasValue || dong.LucDem.Value >= MocKiem, "Thời điểm đếm không được trước mốc kiểm.");
    }

    private string TaoPhienBan()
    {
        using var duLieu = new MemoryStream();
        using var ghi = new BinaryWriter(duLieu);
        ghi.Write(MaPhieuKiem);
        ghi.Write(MaNguoiLap);
        ghi.Write(MaNguoiDuyet ?? string.Empty);
        ghi.Write(MocKiem.Ticks);
        ghi.Write(LucHoanTat?.Ticks ?? -1);
        ghi.Write(TrangThai);
        ghi.Write(_dongKiems.Count);
        foreach (var dong in _dongKiems.OrderBy(dong => dong.MaHang, StringComparer.Ordinal))
        {
            ghi.Write(dong.MaHang);
            ghi.Write(dong.TonSoTaiMoc);
            ghi.Write(dong.SoLuongDem ?? -1);
            ghi.Write(dong.GiaiTrinh ?? string.Empty);
            ghi.Write(dong.LucDem?.Ticks ?? -1);
        }
        ghi.Flush();
        return Convert.ToHexString(SHA256.HashData(duLieu.GetBuffer().AsSpan(0, checked((int)duLieu.Length))));
    }
}
