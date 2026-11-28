using Pos.Web.ViewModels.TraHang;

namespace Pos.Web.Demo;

public sealed partial class BanHangDemoStore
{
    private string CaTra => MaCaMo ?? string.Empty;

    private readonly Dictionary<string, PhieuTraViewModel> _phieuTra = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S-PT-01"] = new(
            "S-PT-01",
            "S-HD-01",
            "S-CA-1011",
            "Trần Minh",
            new DateTime(2026, 10, 11, 10, 0, 0),
            true,
            [new(0, "Sữa tươi 180 ml", 25000, 2, 1)],
            50000,
            0)
    };

    private readonly Dictionary<string, (YeuCauTraViewModel Gui, string Ma)> _lanHoan = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (YeuCauTraViewModel Gui, string Ma)> _lanLuuNhap = new(StringComparer.Ordinal);
    private int _soPhieu;

    public LapTraViewModel TraHoaDon(string? ma, string? maNhap = null)
    {
        lock (_sync)
        {
            PhieuTraViewModel? nhap = null;
            if (!string.IsNullOrWhiteSpace(maNhap))
            {
                if (!_phieuTra.TryGetValue(maNhap.Trim(), out var timThay)
                    || timThay.XacNhan
                    || timThay.Ca != CaTra
                    || timThay.Nguoi != "Lê Linh")
                {
                    return new(ma, "Phiếu trả nháp không tồn tại hoặc không thuộc quyền sửa của bạn.", []);
                }

                nhap = timThay;
                ma = string.IsNullOrWhiteSpace(ma) ? timThay.HoaDon : ma;
            }

            var hd = _hoaDons.Find(h => string.Equals(h.MaHoaDon, ma?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (hd?.TrangThai != "DaXacNhan")
            {
                var loi = string.IsNullOrWhiteSpace(ma)
                    ? null
                    : hd == null
                        ? "Không tìm thấy hóa đơn."
                        : "Chỉ nhận trả hóa đơn đã xác nhận.";
                return new(ma, loi, []);
            }

            if (nhap != null && !string.Equals(nhap.HoaDon, hd.MaHoaDon, StringComparison.OrdinalIgnoreCase))
            {
                return new(hd.MaHoaDon, "Phiếu trả nháp không khớp với hóa đơn gốc đang chọn.", []);
            }

            var daTra = _phieuTra.Values
                .Where(p => p.XacNhan && string.Equals(p.HoaDon, hd.MaHoaDon, StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => p.Dong)
                .ToLookup(d => d.Dong);

            var dong = hd.DongBans
                .Select((d, i) => new DongGocViewModel(
                    i,
                    d.MaHang,
                    d.TenHang,
                    d.DonViTinh,
                    d.DonGiaThucBan,
                    d.SoLuongBan,
                    daTra[i].Sum(t => t.SoLuong)))
                .ToArray();

            return new(hd.MaHoaDon, null, dong, nhap);
        }
    }

    public (int Status, string Message, string? Ma, int PhienBan) GhiTra(YeuCauTraViewModel q, bool xacNhan)
    {
        lock (_sync)
        {
            if (q.DongTra == null || q.DongTra.Any(d => d == null) || string.IsNullOrWhiteSpace(q.MaYeuCau))
            {
                return (400, "Thiếu thông tin phiếu trả.", null, 0);
            }

            var dong = q.DongTra.OrderBy(d => d.Dong).ToArray();
            var lichSu = xacNhan ? _lanHoan : _lanLuuNhap;
            if (lichSu.TryGetValue(q.MaYeuCau, out var cu))
            {
                var a = cu.Gui;
                var trung = string.Equals(a.MaNhap, q.MaNhap, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(a.MaHoaDon?.Trim(), q.MaHoaDon?.Trim(), StringComparison.OrdinalIgnoreCase)
                    && a.PhienBan == q.PhienBan
                    && a.TienMat == q.TienMat
                    && a.ChuyenKhoan == q.ChuyenKhoan
                    && a.DongTra.SequenceEqual(dong);

                if (!_phieuTra.TryGetValue(cu.Ma, out var current))
                {
                    return (400, "Phiếu nháp đã được xóa.", null, 0);
                }

                if (!xacNhan && (current.XacNhan || current.PhienBan != a.PhienBan + 1))
                    return (409, "Phiếu nháp đã thay đổi. Hãy mở phiên bản hiện tại.", cu.Ma, current.PhienBan);

                return trung
                    ? (200, xacNhan ? "Đã xác nhận phiếu trả." : "Đã lưu nháp.", cu.Ma, current.PhienBan)
                    : (409, "Mã yêu cầu đã dùng cho phiếu trả có nội dung khác.", cu.Ma, current.PhienBan);
            }

            if (!q.CaMo || MaCaMo == null)
            {
                return (400, "Cần mở ca để nhận trả hàng.", null, 0);
            }

            var goc = TraHoaDon(q.MaHoaDon);
            if (goc.Dong.Length == 0)
            {
                return (400, goc.Loi ?? "Thiếu hóa đơn gốc.", null, 0);
            }

            PhieuTraViewModel? nhap = null;
            if (!string.IsNullOrWhiteSpace(q.MaNhap)
                && (!_phieuTra.TryGetValue(q.MaNhap, out nhap)
                    || nhap.Ca != CaTra
                    || nhap.Nguoi != "Lê Linh"
                    || !string.Equals(nhap.HoaDon, goc.MaHoaDon, StringComparison.OrdinalIgnoreCase)))
            {
                return (400, "Phiếu nháp không còn hợp lệ.", null, 0);
            }

            if (nhap != null && (nhap.XacNhan || nhap.PhienBan != q.PhienBan))
                return (409, "Phiếu đã được cập nhật ở tab khác. Hãy xem phiếu hiện tại.", nhap.Ma, nhap.PhienBan);

            var lines = new List<DongTraViewModel>();
            foreach (var d in dong)
            {
                if (d.Dong < 0 || d.Dong >= goc.Dong.Length || lines.Any(l => l.Dong == d.Dong))
                {
                    return (400, "Dòng hàng không hợp lệ.", null, 0);
                }

                var g = goc.Dong[d.Dong];
                if (d.SoLuong <= 0 || d.SoLuong > g.DaMua - g.DaTra || d.NhapLai < 0 || d.NhapLai > d.SoLuong)
                {
                    return (400, "Kiểm tra số lượng trả và số lượng nhập lại.", null, 0);
                }

                lines.Add(new(d.Dong, g.TenHang, g.Gia, d.SoLuong, d.NhapLai));
            }

            var tong = lines.Sum(d => d.SoLuong * d.Gia);
            if (lines.Count == 0
                || q.TienMat < 0
                || q.ChuyenKhoan < 0
                || q.TienMat > tong
                || q.TienMat != decimal.Truncate(q.TienMat)
                || q.ChuyenKhoan != tong - q.TienMat)
            {
                return (400, "Chọn hàng và phân bổ đủ số tiền hoàn bằng đồng nguyên.", null, 0);
            }

            var ma = string.IsNullOrWhiteSpace(q.MaNhap) ? $"PT-{++_soPhieu:D5}" : q.MaNhap;
            _phieuTra[ma] = new(
                ma,
                goc.MaHoaDon!,
                CaTra,
                "Lê Linh",
                nhap?.Ngay ?? DateTime.Now,
                xacNhan,
                lines.ToArray(),
                q.TienMat,
                q.ChuyenKhoan, (nhap?.PhienBan ?? 0) + 1, xacNhan ? DateTime.Now : null);

            lichSu.Add(q.MaYeuCau, (q with { MaHoaDon = goc.MaHoaDon!, DongTra = dong }, ma));

            if (xacNhan)
            {
                var hd = _hoaDons.First(h => string.Equals(h.MaHoaDon, goc.MaHoaDon, StringComparison.OrdinalIgnoreCase));
                hd.CoPhieuTraHieuLuc = true;
                hd.MaPhieuTraLienQuan = ma;
            }

            return (200, xacNhan ? "Đã xác nhận phiếu trả." : "Đã lưu nháp.", ma, _phieuTra[ma].PhienBan);
        }
    }

    public (int Status, TraCuuTraViewModel? Model) TimPhieuTra(LocTraViewModel loc)
    {
        lock (_sync)
        {
            var vaiTro = string.Equals(loc.VaiTro, "QuanLy", StringComparison.OrdinalIgnoreCase) ? "QuanLy" : "ThuNgan";
            var ca = LayDanhSachCaBanTheoVaiTro(vaiTro).Select(c => c.MaCa).ToArray();

            PhieuTraViewModel? chon = null;
            if (!string.IsNullOrWhiteSpace(loc.Chon))
            {
                if (!_phieuTra.TryGetValue(loc.Chon.Trim(), out chon))
                {
                    return (404, null);
                }

                if (!ca.Contains(chon.Ca, StringComparer.OrdinalIgnoreCase))
                {
                    return (403, null);
                }
            }

            var list = _phieuTra.Values
                .Where(p => ca.Contains(p.Ca, StringComparer.OrdinalIgnoreCase)
                    && p.Ma.Contains(loc.Ma?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                    && p.HoaDon.Contains(loc.HoaDon?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(loc.Ca) || string.Equals(p.Ca, loc.Ca, StringComparison.OrdinalIgnoreCase))
                    && (loc.TuNgay == null || DateOnly.FromDateTime(p.Ngay) >= loc.TuNgay)
                    && (loc.DenNgay == null || DateOnly.FromDateTime(p.Ngay) <= loc.DenNgay)
                    && (string.IsNullOrWhiteSpace(loc.TrangThai) || loc.TrangThai == (p.XacNhan ? "DaXacNhan" : "Nhap")))
                .OrderByDescending(p => p.Ngay)
                .ThenByDescending(p => p.Ma, StringComparer.Ordinal)
                .ToArray();

            return (200, new(loc with { VaiTro = vaiTro }, list, chon, ca));
        }
    }

    public (int Status, string Message) XoaNhap(string? ma, bool caMo, string? vaiTro)
    {
        lock (_sync)
        {
            if (string.Equals(vaiTro, "QuanLy", StringComparison.OrdinalIgnoreCase))
            {
                return (403, "Chỉ thu ngân lập phiếu mới được xóa phiếu trả nháp của mình.");
            }

            if (string.IsNullOrWhiteSpace(ma))
            {
                return (400, "Thiếu mã phiếu trả nháp cần xóa.");
            }

            if (!caMo)
            {
                return (400, "Chỉ xóa được phiếu trả nháp khi ca bán đang mở.");
            }

            if (!_phieuTra.TryGetValue(ma.Trim(), out var p))
            {
                return (404, "Không tìm thấy phiếu trả nháp.");
            }

            if (p.XacNhan)
            {
                return (400, "Phiếu trả đã xác nhận là bất biến, không thể xóa.");
            }

            if (p.Ca != CaTra || p.Nguoi != "Lê Linh")
            {
                return (403, "Chỉ xóa được phiếu trả nháp thuộc ca đang mở của chính bạn.");
            }

            _phieuTra.Remove(p.Ma);
            return (200, $"Đã xóa phiếu trả nháp {p.Ma}.");
        }
    }
}
