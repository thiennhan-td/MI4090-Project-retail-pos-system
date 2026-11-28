using Pos.Web.ViewModels.BaoCaoQuay;
using Pos.Web.ViewModels.CaBan;

namespace Pos.Web.Demo;

public sealed partial class BanHangDemoStore
{
    private static bool TrongKy(DateTime? luc, DateOnly tu, DateOnly den)
        => luc is { } time && DateOnly.FromDateTime(time) >= tu && DateOnly.FromDateTime(time) <= den;

    public BaoCaoQuayViewModel BaoCaoCuoiNgay(LocBaoCaoQuayViewModel loc, DateOnly ngay)
    {
        lock (_sync)
        {
            var ban = _hoaDons.Where(h => TrongKy(h.LucXacNhan, ngay, ngay)).ToArray();
            var dao = _hoaDons.Where(h => h.LucXacNhan.HasValue && TrongKy(h.LucHuy, ngay, ngay)).ToArray();
            var so = DocSo().ToArray();
            var trongNgay = so.Where(d => TrongKy(d.Luc, ngay, ngay)).ToArray();
            var chiSo = new ChiSoBaoCaoQuayViewModel[]
            {
                new("Hóa đơn xác nhận", ban.Length, false),
                new("Tổng giá trị bán", ban.Sum(h => h.TongTienHang)),
                new("Thu bán tiền mặt", TongQuy(trongNgay, "Thu bán", "TienMat")),
                new("Thu bán chuyển khoản", TongQuy(trongNgay, "Thu bán", "ChuyenKhoan")),
                new("Hoàn trả tiền mặt", -TongQuy(trongNgay, "Hoàn trả", "TienMat")),
                new("Hoàn trả chuyển khoản", -TongQuy(trongNgay, "Hoàn trả", "ChuyenKhoan")),
                new("Hóa đơn bị hủy", dao.Length, false),
                new("Giá trị đảo bán", dao.Sum(h => h.TongTienHang))
            };
            var chungTu = trongNgay.OrderBy(d => d.Luc).Select(d => new object?[]
            {
                d.Luc, d, d.MaCa, d.Loai, TenQuy(d.Nguon), TenPhuongThuc(d.PhuongThuc), d.LyDo, d.GiaTri
            }).ToArray();
            var caCoButToan = trongNgay.Select(d => d.MaCa).ToHashSet();
            var ca = _caBans.Where(c => TrongKy(c.LucMo, ngay, ngay)
                || TrongKy(_banGiao[c.MaCa].Dong, ngay, ngay) || caCoButToan.Contains(c.MaCa));
            return new(loc, "CuoiNgay", "Báo cáo cuối ngày", chiSo,
            [
                BangThanhToan(trongNgay, "Thu chi két ca và quỹ cửa hàng", chiThuChi: true),
                new("Chi tiết chứng từ trong ngày",
                    ["Thời điểm", "Chứng từ", "Ca", "Nghiệp vụ", "Nguồn quỹ", "Phương thức", "Diễn giải", "Giá trị"],
                    chungTu, 7),
                BangLechCa(ca, so, ngay, "Ca mở, đóng hoặc phát sinh giao dịch trong ngày")
            ]);
        }
    }

    public BaoCaoQuayViewModel BaoCaoBanTra(LocBaoCaoQuayViewModel loc, DateOnly tu, DateOnly den)
    {
        lock (_sync)
        {
            // Mỗi dòng hàng góp một lần cho từng sự kiện, độc lập với số khoản thanh toán.
            var hoaDon = _hoaDons.Where(h => h.LucXacNhan.HasValue)
                .ToDictionary(h => h.MaHoaDon, StringComparer.OrdinalIgnoreCase);
            var ban = hoaDon.Values.Where(h => TrongKy(h.LucXacNhan, tu, den))
                .SelectMany(h => h.DongBans.Select(d => (Hang: d, Ban: d.SoLuongBan, Dao: 0, Tra: 0, TienBan: d.ThanhTien, TienDao: 0m, TienTra: 0m)));
            var dao = hoaDon.Values.Where(h => TrongKy(h.LucHuy, tu, den))
                .SelectMany(h => h.DongBans.Select(d => (Hang: d, Ban: 0, Dao: d.SoLuongBan, Tra: 0, TienBan: 0m, TienDao: d.ThanhTien, TienTra: 0m)));
            var tra = _phieuTra.Values.Where(p => p.XacNhan && TrongKy(p.LucXacNhan ?? p.Ngay, tu, den))
                .SelectMany(p => p.Dong.Select(d =>
                    (Hang: hoaDon[p.HoaDon].DongBans[d.Dong], Ban: 0, Dao: 0, Tra: d.SoLuong,
                        TienBan: 0m, TienDao: 0m, TienTra: d.SoLuong * d.Gia)));
            var nhom = ban.Concat(dao).Concat(tra)
                .Where(d => loc.MaHang is null || d.Hang.MaHang.Equals(loc.MaHang, StringComparison.OrdinalIgnoreCase))
                .GroupBy(d => d.Hang.MaHang).OrderBy(g => g.Key).Select(g => new
                {
                    Hang = g.First().Hang,
                    Ban = g.Sum(d => d.Ban), Dao = g.Sum(d => d.Dao), Tra = g.Sum(d => d.Tra),
                    TienBan = g.Sum(d => d.TienBan), TienDao = g.Sum(d => d.TienDao), TienTra = g.Sum(d => d.TienTra)
                }).ToArray();
            var tienBan = nhom.Sum(d => d.TienBan);
            var tienDao = nhom.Sum(d => d.TienDao);
            var tienTra = nhom.Sum(d => d.TienTra);
            return new(loc, "BanTra", "Thống kê bán hàng và trả hàng",
            [
                new("Bán trước đảo", tienBan), new("Đảo bán", tienDao), new("Bán sau đảo", tienBan - tienDao),
                new("Giá trị trả hàng", tienTra), new("Bán thuần", tienBan - tienDao - tienTra)
            ],
            [
                new("Chi tiết theo mặt hàng",
                    ["Mã hàng", "Tên hàng", "ĐVT", "SL bán trước đảo", "SL đảo bán", "SL trả", "SL bán thuần",
                        "Giá trị bán sau đảo", "Giá trị trả", "Bán thuần"],
                    nhom.Select(d => new object?[] { d.Hang.MaHang, d.Hang.TenHang, d.Hang.DonViTinh,
                        d.Ban, d.Dao, d.Tra, d.Ban - d.Dao - d.Tra, d.TienBan - d.TienDao, d.TienTra,
                        d.TienBan - d.TienDao - d.TienTra }).ToArray(), 7)
            ], GhiChu: "Dữ liệu demo có thêm hóa đơn S-HD-1011-01 trị giá 52.000 đ ngày 11/10/2026 ngoài seed SQL.");
        }
    }

    public BaoCaoQuayViewModel BaoCaoThanhToanCa(LocBaoCaoQuayViewModel loc, DateOnly tu, DateOnly den)
    {
        lock (_sync)
        {
            var so = DocSo().ToArray();
            var caLoc = _caBans.Where(c =>
                (loc.MaCa is null || c.MaCa.Equals(loc.MaCa, StringComparison.OrdinalIgnoreCase))
                && (loc.TrangThaiCa is null || (DaChotDen(c, den) ? "DaDong" : "DangMo") == loc.TrangThaiCa)).ToArray();
            var maCaLoc = caLoc.Select(c => c.MaCa).ToHashSet();
            var trongKy = so.Where(d => maCaLoc.Contains(d.MaCa) && TrongKy(d.Luc, tu, den)).ToArray();
            var caCoButToan = trongKy.Select(d => d.MaCa).ToHashSet();
            var caTrongKy = caLoc.Where(c => TrongKy(c.LucMo, tu, den)
                || TrongKy(_banGiao[c.MaCa].Dong, tu, den) || caCoButToan.Contains(c.MaCa));
            return new(loc, "ThanhToanLechCa", "Thanh toán và chênh lệch ca", [],
                [BangThanhToan(trongKy, "Phương thức thanh toán trong kỳ"), BangLechCa(caTrongKy, so, den, "Đối soát từng ca")],
                _caBans.Select(c => c.MaCa).ToArray());
        }
    }

    private bool DaChotDen(CaBanDemoItem ca, DateOnly den) =>
        _banGiao[ca.MaCa].Dong is { } lucDong && DateOnly.FromDateTime(lucDong) <= den;

    private BangBaoCaoQuayViewModel BangLechCa(IEnumerable<CaBanDemoItem> ca, DongQuyViewModel[] so, DateOnly den, string tieuDe)
    {
        var rong = so.Where(d => DateOnly.FromDateTime(d.Luc) <= den)
            .GroupBy(d => d.MaCa).ToDictionary(g => g.Key, g => g.Sum(d => d.TienKet));
        decimal? tongLech = null;
        var dong = new List<object?[]>();
        foreach (var c in ca.OrderBy(c => c.LucMo))
        {
            var b = _banGiao[c.MaCa];
            var daChot = DaChotDen(c, den);
            var duKien = (daChot ? b.Chot : null) ?? b.Dau + rong.GetValueOrDefault(c.MaCa);
            var thucDem = daChot ? b.Dem : null;
            var lech = thucDem - duKien;
            if (lech is decimal chenhLech)
                tongLech = (tongLech ?? 0m) + chenhLech;
            dong.Add([c.MaCa, c.TenNguoiMo, daChot ? "Đã đóng" : "Đang mở",
                c.LucMo, daChot ? b.Dong : null, b.Dau, duKien - b.Dau, duKien, thucDem, lech]);
        }

        return new(tieuDe, ["Mã ca", "Thu ngân", "Trạng thái", "Mở lúc", "Đóng lúc", "Tiền đầu ca",
            "Thu chi ròng két", "Dự kiến két", "Thực đếm", "Chênh lệch"], dong.ToArray(), 5, 9,
            ["Tổng chênh lệch", null, null, null, null, null, null, null, null, tongLech]);
    }

    private static BangBaoCaoQuayViewModel BangThanhToan(DongQuyViewModel[] so, string tieuDe, bool chiThuChi = false)
    {
        var dong = new List<object?[]>();
        if (!chiThuChi)
        {
            foreach (var loai in new[] { "Thu bán", "Đảo hủy", "Hoàn trả" })
                dong.Add([loai, TongQuy(so, loai, "TienMat"), TongQuy(so, loai, "ChuyenKhoan")]);
        }
        foreach (var (nguon, ten) in new[] { ("KetCa", "két ca"), ("QuyCuaHang", "quỹ cửa hàng") })
        {
            foreach (var loai in new[] { "Thu riêng", "Chi riêng" })
                dong.Add([$"{loai} · {ten}", TongQuy(so, loai, "TienMat", nguon),
                    nguon == "KetCa" ? null : TongQuy(so, loai, "ChuyenKhoan", nguon)]);
        }
        dong.Add(["Chi nhà cung cấp từ két", TongQuy(so, "Trả nhà cung cấp", "TienMat", "KetCa"), null]);
        return new(tieuDe, ["Nghiệp vụ", "Tiền mặt", "Chuyển khoản"], dong.ToArray(), 1);
    }

    private static decimal TongQuy(IEnumerable<DongQuyViewModel> so, string loai, string phuongThuc, string? nguon = null)
        => so.Where(d => d.Loai == loai && d.PhuongThuc == phuongThuc && (nguon is null || d.Nguon == nguon))
            .Sum(d => d.GiaTri);

    private static string TenPhuongThuc(string cach) => cach == "TienMat" ? "Tiền mặt" : "Chuyển khoản";
    private static string TenQuy(string nguon) => nguon == "KetCa" ? "Két ca" : "Quỹ cửa hàng";
}
