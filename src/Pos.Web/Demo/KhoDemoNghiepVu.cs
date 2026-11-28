using System.Globalization;

namespace Pos.Web.Demo;

public sealed partial class KhoDemoStore
{
    public PhieuNhapDemo[] PhieuNhaps() => _phieuNhaps.Values.OrderByDescending(x => x.LucLap).ToArray();
    public PhieuNhapDemo? PhieuNhap(string? ma) => ma is null ? null : _phieuNhaps.GetValueOrDefault(ma);
    public TraNhaCungCapDemo[] LichSuTra(string ma) => _khoanTras.Where(x => x.MaPhieu == ma).OrderBy(x => x.LucTra).ToArray();

    public string LuuNhap(string? ma, int? phienBan, string maNcc, DateOnly? han,
        IEnumerable<(string MaHang, int SoLuong, decimal DonGia)> lines)
    {
        KiemTraQuyen("NHAP_LAP_NHAP");
        var cu = ma is null ? null : Tim(_phieuNhaps, ma, "Không tìm thấy phiếu nhập.");
        if (cu is not null && (cu.TrangThai != Nhap || cu.PhienBan != phienBan))
            throw new XungDotPhienBanKhoDemoException("Phiếu nhập đã thay đổi. Mở phiếu hiện tại trước khi sửa lại.");

        var ncc = Tim(_nhaCungCaps, maNcc, "Chọn nhà cung cấp hợp lệ.");
        var dongNhaps = lines.Select(x =>
        {
            if (!DangDung(x.MaHang))
                throw new ThaoTacKhoDemoException($"Mặt hàng {x.MaHang} phải đang hoạt động.");
            if (x.SoLuong < 1 || x.DonGia < 0)
                throw new ThaoTacKhoDemoException("Số lượng và đơn giá nhập không hợp lệ.");

            KiemTraTien(x.DonGia);
            var maHang = _matHangs[x.MaHang].Ma;
            var maDong = cu?.Dongs.FirstOrDefault(d => d.MaHang == maHang)?.Ma ?? TaoMa("DN");
            return new DongNhapDemo(maDong, maHang, x.SoLuong, x.DonGia);
        }).ToArray();
        if (dongNhaps.Length == 0 || dongNhaps.Select(x => x.MaHang).Distinct().Count() != dongNhaps.Length)
            throw new ThaoTacKhoDemoException("Phiếu nhập cần ít nhất một mặt hàng và mỗi mặt hàng chỉ xuất hiện một lần.");

        var phieu = new PhieuNhapDemo(cu?.Ma ?? TaoMa("PN"), ncc.Ma, cu?.NguoiLap ?? _maDangNhap!,
            cu?.LucLap ?? DateTimeOffset.UtcNow, han, Nhap, null, null, null, dongNhaps, (cu?.PhienBan ?? 0) + 1);
        _phieuNhaps[phieu.Ma] = phieu;
        return phieu.Ma;
    }

    public void XacNhanNhap(string ma, int phienBan)
    {
        KiemTraQuyen("NHAP_XAC_NHAN", NhanVienKho);
        var phieu = Tim(_phieuNhaps, ma, "Không tìm thấy phiếu nhập.");
        if (phieu.PhienBan != phienBan)
            throw new ThaoTacKhoDemoException("Phiếu nhập đã thay đổi. Xem lại chi tiết trước khi xác nhận.");
        if (phieu.TrangThai == DaXacNhan) return;
        if (phieu.TrangThai != Nhap || phieu.Dongs.Count == 0)
            throw new ThaoTacKhoDemoException("Chỉ xác nhận được một lần trên phiếu nháp có hàng.");
        if (phieu.Dongs.Any(x => !DangDung(x.MaHang)))
            throw new ThaoTacKhoDemoException("Phiếu có mặt hàng ngừng sử dụng. Hãy sửa phiếu trước khi xác nhận.");

        var now = DateTimeOffset.UtcNow;
        GhiBienDong(phieu.Dongs.Select(x => BienDong(x.MaHang, "NhapKho", x.SoLuong, now, x.Ma)));
        _phieuNhaps[phieu.Ma] = phieu with { TrangThai = DaXacNhan, LucXacNhan = now };
    }

    public bool CoTheDaoNhap(PhieuNhapDemo phieu) => phieu.TrangThai == DaXacNhan
        && _khoanTras.All(x => x.MaPhieu != phieu.Ma)
        && phieu.Dongs.All(x => TonHienTai(x.MaHang) >= x.SoLuong);

    public void HuyNhap(string ma, string lyDo)
    {
        var phieu = Tim(_phieuNhaps, ma, "Không tìm thấy phiếu nhập.");
        if (phieu.LucXacNhan is not null) KiemTraQuyen("NHAP_HUY_XAC_NHAN", QuanLy);
        else KiemTraQuyen("NHAP_HUY_NHAP");
        if (string.IsNullOrWhiteSpace(lyDo))
            throw new ThaoTacKhoDemoException("Nhập lý do hủy.");
        if (phieu.TrangThai == DaHuy)
            throw new ThaoTacKhoDemoException("Phiếu nhập đã được hủy.");

        var now = DateTimeOffset.UtcNow;
        if (phieu.TrangThai == DaXacNhan)
        {
            if (!CoTheDaoNhap(phieu))
                throw new ThaoTacKhoDemoException("Phiếu đã có khoản trả hoặc việc hủy sẽ làm âm tồn.");
            GhiBienDong(phieu.Dongs.Select(x => BienDong(x.MaHang, "DaoNhapKho", -x.SoLuong, now, x.Ma)));
        }

        _phieuNhaps[phieu.Ma] = phieu with { TrangThai = DaHuy, LucHuy = now, LyDoHuy = lyDo.Trim() };
    }

    internal IEnumerable<TraNhaCungCapDemo> CacKhoanTraTuKet() =>
        _khoanTras.Where(x => x.Nguon == "KetCa" && x.MaCa is not null);

    public decimal DaTra(string maPhieu) => _khoanTras.Where(x => x.MaPhieu == maPhieu).Sum(x => x.SoTien);
    public decimal DuNo(PhieuNhapDemo phieu) => phieu.TrangThai == DaXacNhan ? phieu.TongTien - DaTra(phieu.Ma) : 0;

    public void TraNcc(string ma, decimal soTien, string phuongThuc, string nguon, string? maCa, Guid khoaYeuCau)
    {
        KiemTraQuyen("NCC_GHI_TRA_TIEN", QuanLy);
        var phieu = Tim(_phieuNhaps, ma, "Không tìm thấy phiếu nhập.");
        KiemTraTien(soTien);
        if (phuongThuc is not ("TienMat" or "ChuyenKhoan") || nguon is not ("KetCa" or "QuyCuaHang") || khoaYeuCau == Guid.Empty)
            throw new ThaoTacKhoDemoException("Phương thức, nguồn chi hoặc yêu cầu thanh toán không hợp lệ.");
        var daGhi = _khoanTras.FirstOrDefault(x => x.KhoaYeuCau == khoaYeuCau);
        if (daGhi is not null)
        {
            if (daGhi.MaPhieu == phieu.Ma && daGhi.SoTien == soTien && daGhi.PhuongThuc == phuongThuc
                && daGhi.Nguon == nguon && daGhi.MaCa == maCa && daGhi.NguoiGhi == _maDangNhap)
                return;
            throw new ThaoTacKhoDemoException("Yêu cầu thanh toán đã được sử dụng.");
        }
        if (phieu.TrangThai != DaXacNhan || soTien <= 0 || soTien > DuNo(phieu))
            throw new ThaoTacKhoDemoException("Chỉ trả số tiền dương và tối đa bằng dư nợ của phiếu đã xác nhận.");
        if (nguon == "KetCa" && phuongThuc != "TienMat")
            throw new ThaoTacKhoDemoException("Chi từ két ca yêu cầu thanh toán tiền mặt.");
        if (nguon == "KetCa" && !CaDangMo().Any(x => x.Ma == maCa))
            throw new ThaoTacKhoDemoException("Ca đã đóng hoặc không còn hợp lệ. Tải lại trang để chọn ca đang mở.");
        if (nguon == "QuyCuaHang" && maCa is not null)
            throw new ThaoTacKhoDemoException("Chi từ quỹ cửa hàng không gắn với mã ca.");

        _khoanTras.Add(new(TaoMa("TNCC"), phieu.Ma, _maDangNhap!, maCa, DateTimeOffset.UtcNow,
            soTien, phuongThuc, nguon, khoaYeuCau));
    }

    public PhieuKiemDemo[] PhieuKiems() => _phieuKiems.Values.OrderByDescending(x => x.MocKiem).ToArray();
    public PhieuKiemDemo? PhieuKiem(string? ma) => ma is null ? null : _phieuKiems.GetValueOrDefault(ma);

    public string LapPhieuKiem(IEnumerable<string> maHangs)
    {
        KiemTraQuyen("KHO_LAP_KIEM", NhanVienKho);
        var items = maHangs.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (items.Length == 0 || items.Any(x => !DangDung(x)))
            throw new ThaoTacKhoDemoException("Chọn ít nhất một mặt hàng đang hoạt động.");

        var phieu = new PhieuKiemDemo(TaoMa("PK"), _maDangNhap!, null, DateTimeOffset.UtcNow, null, DangKiem,
            items.Select(x => _matHangs[x].Ma).Select(x => new DongKiemDemo(x, TonHienTai(x), null, null)).ToArray());
        _phieuKiems.Add(phieu.Ma, phieu);
        return phieu.Ma;
    }

    public void GhiDem(string ma, int phienBan, IEnumerable<(string MaHang, int? SoLuong, string? GiaiTrinh)> counts, bool guiDuyet)
    {
        KiemTraQuyen("KHO_LAP_KIEM", NhanVienKho);
        var phieu = Tim(_phieuKiems, ma, "Không tìm thấy phiếu kiểm.");
        if (phieu.TrangThai != DangKiem || phieu.PhienBan != phienBan)
            throw new XungDotPhienBanKhoDemoException("Phiếu kiểm đã thay đổi. Mở phiếu hiện tại trước khi cập nhật số đếm.");

        var nhom = counts.GroupBy(x => x.MaHang, StringComparer.OrdinalIgnoreCase).ToArray();
        if (nhom.Length != phieu.Dongs.Count || nhom.Any(x => x.Count() != 1)
            || phieu.Dongs.Any(x => nhom.All(y => !Khop(y.Key, x.MaHang))))
            throw new ThaoTacKhoDemoException("Danh sách hàng không khớp với phiếu kiểm.");

        var duocGiaiTrinh = CoQuyen("KHO_GIAI_TRINH_KIEM", NhanVienKho);
        if (guiDuyet || nhom.Any(x => !string.IsNullOrWhiteSpace(x.Single().GiaiTrinh)))
            KiemTraQuyen("KHO_GIAI_TRINH_KIEM", NhanVienKho);
        if (nhom.Any(x => x.Single().SoLuong < 0))
            throw new ThaoTacKhoDemoException("Số lượng thực đếm không được âm.");

        var values = nhom.ToDictionary(x => x.Key, x => x.Single(), StringComparer.OrdinalIgnoreCase);
        var dongs = phieu.Dongs.Select(x => x with
        {
            SoLuongDem = values[x.MaHang].SoLuong,
            GiaiTrinh = duocGiaiTrinh ? values[x.MaHang].GiaiTrinh?.Trim() : x.GiaiTrinh
        }).ToArray();

        if (guiDuyet && dongs.Any(x => x.SoLuongDem is null
            || (x.ChenhLech != 0 && string.IsNullOrWhiteSpace(x.GiaiTrinh))))
            throw new ThaoTacKhoDemoException("Cần điền đủ số đếm và giải trình từng dòng chênh lệch.");

        _phieuKiems[phieu.Ma] = phieu with
        {
            Dongs = dongs,
            TrangThai = guiDuyet ? ChoDuyet : DangKiem,
            PhienBan = phieu.PhienBan + 1
        };
    }

    public void DuyetKiem(string ma)
    {
        KiemTraQuyen("KHO_DUYET_DIEU_CHINH", QuanLy);
        var phieu = Tim(_phieuKiems, ma, "Không tìm thấy phiếu kiểm.");
        if (phieu.TrangThai == HoanTat) return;
        if (phieu.TrangThai != ChoDuyet || phieu.Dongs.Any(x => x.SoLuongDem is null
            || (x.ChenhLech != 0 && string.IsNullOrWhiteSpace(x.GiaiTrinh))))
            throw new ThaoTacKhoDemoException("Phiếu phải ở trạng thái chờ duyệt và có đầy đủ số đếm, giải trình.");

        if (_bienDongs.Any(x => x.Loai == "DieuChinhKiemKho" && x.LucGhi > phieu.MocKiem
            && phieu.Dongs.Any(d => d.MaHang == x.MaHang)))
            throw new ThaoTacKhoDemoException("Mặt hàng đã được điều chỉnh từ một phiếu kiểm khác sau mốc này. Hãy lập phiếu mới.");

        var now = DateTimeOffset.UtcNow;
        GhiBienDong(phieu.Dongs.Where(x => x.ChenhLech != 0).Select(x =>
            BienDong(x.MaHang, "DieuChinhKiemKho", x.ChenhLech!.Value, now, phieu.Ma, $":{x.MaHang}")));
        _phieuKiems[phieu.Ma] = phieu with { TrangThai = HoanTat, NguoiDuyet = _maDangNhap, LucHoanTat = now };
    }

    public void HuyKiem(string ma)
    {
        KiemTraQuyen("KHO_HUY_KIEM");
        var phieu = Tim(_phieuKiems, ma, "Không tìm thấy phiếu kiểm.");
        if (phieu.TrangThai == ChoDuyet) KiemTraQuyen("KHO_HUY_KIEM", QuanLy);
        if (phieu.TrangThai is not (DangKiem or ChoDuyet))
            throw new ThaoTacKhoDemoException("Chỉ hủy được phiếu kiểm chưa hoàn tất.");

        _phieuKiems[phieu.Ma] = phieu with { TrangThai = DaHuy };
    }

    public TheKhoDemo TheKho(string ma, DateOnly tu, DateOnly den)
    {
        var hang = Tim(_matHangs, ma, "Không tìm thấy mặt hàng.");
        var kho = _bienDongs.Where(x => x.MaHang == hang.Ma).ToArray();
        var tonDau = kho.Where(x => NgayDiaPhuong(x.LucGhi) < tu).Sum(x => (long)x.SoLuong);
        var ton = tonDau;
        var dongs = kho.Where(x => TrongKy(x.LucGhi, tu, den)).OrderBy(x => x.LucGhi).ThenBy(x => x.Ma)
            .Select(x => new DongTheKhoDemo(x, ton += x.SoLuong, MaChungTu(x))).ToArray();
        return new(hang, tonDau, ton, dongs);
    }

    public XuatNhapTonDemo[] XuatNhapTon(DateOnly tu, DateOnly den, string? maNhom) =>
        MatHangs().Where(x => maNhom is null || Khop(x.MaNhom, maNhom)).Select(hang =>
        {
            var the = TheKho(hang.Ma, tu, den);
            var bienDongs = the.Dongs.Select(x => x.BienDong).ToArray();
            return new XuatNhapTonDemo(hang, _nhomHangs[hang.MaNhom].Ten, the.TonDau,
                bienDongs.Where(x => x.SoLuong > 0).Sum(x => (long)x.SoLuong),
                bienDongs.Where(x => x.SoLuong < 0).Sum(x => -(long)x.SoLuong), the.TonCuoi,
                bienDongs.Where(x => x.Loai is "NhapKho" or "DaoNhapKho").Sum(x => (long)x.SoLuong),
                bienDongs.Where(x => x.Loai is "XuatBan" or "DaoXuatBan").Sum(x => -(long)x.SoLuong),
                bienDongs.Where(x => x.Loai == "NhapTraHang").Sum(x => (long)x.SoLuong),
                bienDongs.Where(x => x.Loai == "DieuChinhKiemKho").Sum(x => (long)x.SoLuong));
        }).ToArray();

    public CongNoNccDemo[] CongNoNcc(DateOnly tu, DateOnly den, string? maNcc) =>
        NhaCungCaps().Where(x => maNcc is null || Khop(x.Ma, maNcc)).Select(ncc =>
        {
            var phieus = _phieuNhaps.Values.Where(x => x.MaNcc == ncc.Ma && x.LucXacNhan is not null).ToArray();
            var maPhieus = phieus.Select(x => x.Ma).ToHashSet(StringComparer.Ordinal);
            var khoanTras = _khoanTras.Where(x => maPhieus.Contains(x.MaPhieu)).ToArray();
            var dau = phieus.Where(x => NgayDiaPhuong(x.LucXacNhan!.Value) < tu).Sum(x => x.TongTien)
                - phieus.Where(x => x.LucHuy.HasValue && NgayDiaPhuong(x.LucHuy.Value) < tu).Sum(x => x.TongTien)
                - khoanTras.Where(x => NgayDiaPhuong(x.LucTra) < tu).Sum(x => x.SoTien);
            var nhap = phieus.Where(x => TrongKy(x.LucXacNhan!.Value, tu, den)).Sum(x => x.TongTien);
            var dao = phieus.Where(x => x.LucHuy.HasValue && TrongKy(x.LucHuy.Value, tu, den)).Sum(x => x.TongTien);
            var tra = khoanTras.Where(x => TrongKy(x.LucTra, tu, den)).Sum(x => x.SoTien);
            return new CongNoNccDemo(ncc, dau, nhap, dao, tra, dau + nhap - dao - tra);
        }).ToArray();

    private string? MaChungTu(BienDongKhoDemo bienDong) =>
        bienDong.Loai is "NhapKho" or "DaoNhapKho"
            ? _phieuNhaps.Values.FirstOrDefault(x => x.Dongs.Any(d => d.Ma == bienDong.Nguon))?.Ma
            : null;

    private int TonHienTai(string ma) => (int)_bienDongs.Where(x => x.MaHang == ma).Sum(x => (long)x.SoLuong);

    private void GhiBienDong(IEnumerable<BienDongKhoDemo> entries)
    {
        var dongs = entries.ToArray();
        if (dongs.Any(x => _bienDongs.Any(y => y.KhoaSuKien == x.KhoaSuKien)))
            throw new ThaoTacKhoDemoException("Biến động đã được ghi.");
        foreach (var group in dongs.GroupBy(x => x.MaHang))
        {
            var ton = TonHienTai(group.Key) + group.Sum(x => (long)x.SoLuong);
            if (ton is < 0 or > int.MaxValue)
                throw new ThaoTacKhoDemoException($"Tồn mặt hàng {group.Key} sẽ vượt phạm vi cho phép.");
        }

        _bienDongs.AddRange(dongs);
    }

    private static BienDongKhoDemo BienDong(string maHang, string loai, int soLuong, DateTimeOffset luc, string nguon,
        string hauTo = "") => new(TaoMa("BD"), maHang, loai, soLuong, luc, nguon, $"{loai}:{nguon}{hauTo}");

    private static readonly TimeSpan GioVietNam = TimeSpan.FromHours(7);
    public static DateOnly NgayDiaPhuong(DateTimeOffset luc) => DateOnly.FromDateTime(luc.ToOffset(GioVietNam).DateTime);
    private static bool TrongKy(DateTimeOffset luc, DateOnly tu, DateOnly den) =>
        NgayDiaPhuong(luc) >= tu && NgayDiaPhuong(luc) <= den;

    public static string HienThiNgayGio(DateTimeOffset luc) =>
        luc.ToOffset(GioVietNam).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
}

public sealed record DongNhapDemo(string Ma, string MaHang, int SoLuong, decimal DonGia)
{
    public decimal ThanhTien => SoLuong * DonGia;
}

public sealed record PhieuNhapDemo(string Ma, string MaNcc, string NguoiLap, DateTimeOffset LucLap,
    DateOnly? HanThanhToan, string TrangThai, DateTimeOffset? LucXacNhan, DateTimeOffset? LucHuy,
    string? LyDoHuy, IReadOnlyList<DongNhapDemo> Dongs, int PhienBan = 0)
{
    public decimal TongTien => Dongs.Sum(x => x.ThanhTien);
}

public sealed record TraNhaCungCapDemo(string Ma, string MaPhieu, string NguoiGhi, string? MaCa, DateTimeOffset LucTra,
    decimal SoTien, string PhuongThuc, string Nguon, Guid? KhoaYeuCau);

public sealed record DongKiemDemo(string MaHang, int TonSo, int? SoLuongDem, string? GiaiTrinh)
{
    public int? ChenhLech => SoLuongDem - TonSo;
}

public sealed record PhieuKiemDemo(string Ma, string NguoiLap, string? NguoiDuyet, DateTimeOffset MocKiem,
    DateTimeOffset? LucHoanTat, string TrangThai, IReadOnlyList<DongKiemDemo> Dongs, int PhienBan = 0);

internal sealed class XungDotPhienBanKhoDemoException(string message) : Exception(message);

public sealed record BienDongKhoDemo(string Ma, string MaHang, string Loai, int SoLuong, DateTimeOffset LucGhi,
    string Nguon, string KhoaSuKien);
public sealed record DongTheKhoDemo(BienDongKhoDemo BienDong, long TonSau, string? MaChungTu);
public sealed record TheKhoDemo(MatHangKhoDemo Hang, long TonDau, long TonCuoi, IReadOnlyList<DongTheKhoDemo> Dongs);
public sealed record XuatNhapTonDemo(MatHangKhoDemo Hang, string TenNhom, long TonDau, long Nhap, long Xuat,
    long TonCuoi, long NhapMuaRong, long XuatBanRong, long TraNhapLai, long DieuChinhKiemKe);
public sealed record CongNoNccDemo(NhaCungCapDemo Ncc, decimal Dau, decimal Nhap, decimal Dao, decimal Tra, decimal Cuoi);
