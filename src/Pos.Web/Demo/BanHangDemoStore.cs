using Microsoft.Extensions.Caching.Memory;

namespace Pos.Web.Demo;

public sealed class BanHangDemoSessionProvider
{
    private const string SessionInitKey = "PosDemoStoreInitialized";
    private readonly IMemoryCache _cache;
    private readonly object _sync = new();

    public BanHangDemoSessionProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    public BanHangDemoStore LayKhoTheoPhien(HttpContext httpContext)
    {
        httpContext.Session.SetString(SessionInitKey, "1");
        var sessionKey = $"pos-demo-store:{httpContext.Session.Id}";

        lock (_sync)
        {
            return _cache.GetOrCreate(sessionKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(4);
                return new BanHangDemoStore();
            })!;
        }
    }
}

public sealed partial class BanHangDemoStore
{
    public const string MaCaHienTai = "S-CA-1126";
    private const string MaThuNgan = "S-TK-TN1";
    private readonly object _sync = new();
    private readonly List<MatHangDemoItem> _matHangs;
    private readonly List<CaBanDemoItem> _caBans;
    private readonly List<HoaDonDemoRecord> _hoaDons;
    private readonly Dictionary<string, LanGhiHoaDon> _lanGhiHoaDon = new(StringComparer.Ordinal);
    private int _nextInvoiceNumber = 1;

    public BanHangDemoStore()
    {
        _matHangs =
        [
            new MatHangDemoItem("S-SUA-1L", "Sữa tươi tiệt trùng 1L", "Sữa và đồ uống", "Hộp", 25000m, 40),
            new MatHangDemoItem("S-SUA", "Sữa tươi 180 ml", "Sữa và đồ uống", "Hộp", 26000m, 110),
            new MatHangDemoItem("S-BANH", "Bánh mì đóng gói", "Bánh và thực phẩm", "Gói", 12000m, 32),
            new MatHangDemoItem("S-NUOC", "Nước ngọt 320 ml", "Sữa và đồ uống", "Lon", 10000m, 60),
            new MatHangDemoItem("S-MI", "Mì gói ăn liền", "Bánh và thực phẩm", "Gói", 5000m, 80)
        ];

        _caBans =
        [
            new CaBanDemoItem("S-CA-1126", "S-TK-TN1", "Lê Linh", "DangMo", new DateTime(2026, 11, 26, 8, 0, 0)),
            new CaBanDemoItem("S-CA-1112", "S-TK-TN1", "Lê Linh", "DaDong", new DateTime(2026, 11, 12, 8, 0, 0)),
            new CaBanDemoItem("S-CA-1011", "S-TK-TN2", "Trần Minh", "DaDong", new DateTime(2026, 10, 11, 8, 0, 0)),
            new CaBanDemoItem("S-CA-1010", "S-TK-TN1", "Lê Linh", "DaDong", new DateTime(2026, 10, 10, 8, 0, 0))
        ];

        _hoaDons =
        [
            new HoaDonDemoRecord
            {
                MaHoaDon = "S-HD-NOV-NHAP",
                MaCa = "S-CA-1126",
                MaNguoiLap = "S-TK-TN1",
                TenNguoiLap = "Lê Linh",
                LucLap = new DateTime(2026, 11, 26, 9, 30, 0),
                TrangThai = "Nhap",
                PhienBan = 1,
                DongBans =
                [
                    new DongBanDemoRecord("S-SUA", "Sữa tươi 180 ml", "Hộp", 1, 26000m)
                ],
                ThanhToans = [],
                ThanhToanNhap = new("TienMat", 26000m, null, null)
            },
            new HoaDonDemoRecord
            {
                MaHoaDon = "S-HD-NOV-01",
                MaCa = "S-CA-1112",
                MaNguoiLap = "S-TK-TN1",
                TenNguoiLap = "Lê Linh",
                LucLap = new DateTime(2026, 11, 12, 9, 30, 0),
                LucXacNhan = new DateTime(2026, 11, 12, 10, 0, 0),
                TrangThai = "DaXacNhan",
                DongBans =
                [
                    new DongBanDemoRecord("S-SUA", "Sữa tươi 180 ml", "Hộp", 1, 26000m),
                    new DongBanDemoRecord("S-BANH", "Bánh mì đóng gói", "Gói", 1, 12000m)
                ],
                ThanhToans =
                [
                    new ThanhToanDemoRecord("ChuyenKhoan", 38000m, null)
                ]
            },
            new HoaDonDemoRecord
            {
                MaHoaDon = "S-HD-1011-01",
                MaCa = "S-CA-1011",
                MaNguoiLap = "S-TK-TN2",
                TenNguoiLap = "Trần Minh",
                LucLap = new DateTime(2026, 10, 11, 10, 15, 0),
                LucXacNhan = new DateTime(2026, 10, 11, 10, 20, 0),
                TrangThai = "DaXacNhan",
                DongBans =
                [
                    new DongBanDemoRecord("S-SUA", "Sữa tươi 180 ml", "Hộp", 2, 26000m)
                ],
                ThanhToans =
                [
                    new ThanhToanDemoRecord("ChuyenKhoan", 52000m, null)
                ]
            },
            new HoaDonDemoRecord
            {
                MaHoaDon = "S-HD-01",
                MaCa = "S-CA-1010",
                MaNguoiLap = "S-TK-TN1",
                TenNguoiLap = "Lê Linh",
                LucLap = new DateTime(2026, 10, 10, 9, 30, 0),
                LucXacNhan = new DateTime(2026, 10, 10, 10, 0, 0),
                TrangThai = "DaXacNhan",
                CoPhieuTraHieuLuc = true,
                MaPhieuTraLienQuan = "S-PT-01",
                DongBans =
                [
                    new DongBanDemoRecord("S-SUA", "Sữa tươi 180 ml", "Hộp", 5, 25000m)
                ],
                ThanhToans =
                [
                    new ThanhToanDemoRecord("TienMat", 75000m, 100000m),
                    new ThanhToanDemoRecord("ChuyenKhoan", 50000m, null)
                ]
            },
            new HoaDonDemoRecord
            {
                MaHoaDon = "S-HD-02",
                MaCa = "S-CA-1010",
                MaNguoiLap = "S-TK-TN1",
                TenNguoiLap = "Lê Linh",
                LucLap = new DateTime(2026, 10, 10, 9, 30, 0),
                LucXacNhan = new DateTime(2026, 10, 10, 10, 30, 0),
                TrangThai = "DaHuy",
                TenNguoiHuy = "Nguyễn An",
                LucHuy = new DateTime(2026, 10, 11, 9, 0, 0),
                MaCaDao = "S-CA-1011",
                LyDoHuy = "Hủy giao dịch bán nhầm",
                DongBans =
                [
                    new DongBanDemoRecord("S-BANH", "Bánh mì đóng gói", "Gói", 3, 12000m)
                ],
                ThanhToans =
                [
                    new ThanhToanDemoRecord("TienMat", 36000m, 40000m)
                ]
            }
        ];
        // S-HD-NOV-02 đến S-HD-NOV-12 trong 02_SeedData.sql cùng mẫu dòng với S-HD-NOV-01.
        var mau = _hoaDons.Single(h => h.MaHoaDon == "S-HD-NOV-01");
        for (int i = 2; i <= 12; i++)
        {
            var dong = new List<DongBanDemoRecord>
            {
                mau.DongBans[0] with { SoLuongBan = 1 + (i - 1) % 3 },
                mau.DongBans[1] with { SoLuongBan = i % 2 == 0 ? 2 : 1 }
            };
            _hoaDons.Add(new HoaDonDemoRecord
            {
                MaHoaDon = $"S-HD-NOV-{i:D2}", MaCa = mau.MaCa, MaNguoiLap = mau.MaNguoiLap,
                TenNguoiLap = mau.TenNguoiLap, LucLap = mau.LucLap, LucXacNhan = mau.LucXacNhan?.AddMinutes(3 * (i-1)),
                TrangThai = "DaXacNhan", DongBans = dong,
                ThanhToans = [new("ChuyenKhoan", dong.Sum(d => d.ThanhTien), null)]
            });
        }
    }

    public IReadOnlyList<MatHangDemoItem> LayDanhSachMatHang()
    {
        lock (_sync)
        {
            return _matHangs.ToList();
        }
    }

    public IReadOnlyList<CaBanDemoItem> LayDanhSachCaBanTheoVaiTro(string vaiTroDemo)
    {
        lock (_sync)
        {
            if (string.Equals(vaiTroDemo, "QuanLy", StringComparison.OrdinalIgnoreCase))
            {
                return _caBans.ToList();
            }

            return _caBans.Where(c => c.MaNguoiMo == "S-TK-TN1").ToList();
        }
    }

    public bool CoQuyenXemHoaDon(HoaDonDemoRecord hoaDon, string vaiTroDemo)
    {
        if (string.Equals(vaiTroDemo, "QuanLy", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        lock (_sync)
        {
            return _caBans.Any(c =>
                string.Equals(c.MaCa, hoaDon.MaCa, StringComparison.OrdinalIgnoreCase)
                && c.MaNguoiMo == "S-TK-TN1");
        }
    }

    public IReadOnlyList<HoaDonDemoRecord> LayDanhSachHoaDonTheoPhamVi(string vaiTroDemo)
    {
        lock (_sync)
        {
            var isManager = string.Equals(vaiTroDemo, "QuanLy", StringComparison.OrdinalIgnoreCase);
            HashSet<string>? caCuaToi = null;
            if (!isManager)
            {
                caCuaToi = _caBans
                    .Where(c => c.MaNguoiMo == "S-TK-TN1")
                    .Select(c => c.MaCa)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            return _hoaDons
                .Where(h => isManager || caCuaToi!.Contains(h.MaCa))
                .OrderByDescending(h => h.LucLap)
                .ThenByDescending(h => h.MaHoaDon, StringComparer.Ordinal)
                .Select(h => h with { })
                .ToList();
        }
    }

    public (HoaDonDemoRecord? HoaDon, bool BiChanPhamVi) TimHoaDonTheoPhamVi(string maHoaDon, string vaiTroDemo)
    {
        lock (_sync)
        {
            var hoaDon = _hoaDons.FirstOrDefault(h => string.Equals(h.MaHoaDon, maHoaDon, StringComparison.OrdinalIgnoreCase));
            if (hoaDon is null)
            {
                return (null, false);
            }

            if (!CoQuyenXemHoaDon(hoaDon, vaiTroDemo))
            {
                return (null, true);
            }

            return (hoaDon with { }, false);
        }
    }

    public CaBanDemoItem? LayCaHienTai()
    {
        lock (_sync)
        {
            return _caBans.FirstOrDefault(ca => ca.MaNguoiMo == MaThuNgan && ca.TrangThai == "DangMo");
        }
    }

    public bool CoQuyenSuaNhap(HoaDonDemoRecord hoaDon)
    {
        var ca = LayCaHienTai();
        return ca is not null
            && ca.TrangThai == "DangMo"
            && ca.MaNguoiMo == MaThuNgan
            && hoaDon.MaCa == ca.MaCa
            && hoaDon.MaNguoiLap == MaThuNgan;
    }

    public KetQuaGhiHoaDonDemo GhiHoaDon(
        string maYeuCau,
        string? maNhap,
        int? phienBan,
        bool caDangMo,
        IReadOnlyList<DongBanDemoRecord> dongBans,
        ThanhToanNhapDemoRecord? thanhToanNhap,
        IReadOnlyList<ThanhToanDemoRecord> thanhToans)
    {
        lock (_sync)
        {
            var ca = LayCaHienTai();
            if (!caDangMo || ca is null || ca.TrangThai != "DangMo" || ca.MaNguoiMo != MaThuNgan)
            {
                return new(TrangThaiGhiHoaDonDemo.CaDaDong, null, "Ca bán hiện tại đã đóng. Vui lòng mở ca trước khi ghi hóa đơn.");
            }

            if (_lanGhiHoaDon.TryGetValue(maYeuCau, out var lanTruoc))
            {
                var daGhi = _hoaDons.First(h => h.MaHoaDon == lanTruoc.HoaDon.MaHoaDon);
                var trungKhop = string.Equals(maNhap, lanTruoc.MaNhap, StringComparison.OrdinalIgnoreCase)
                    && phienBan == lanTruoc.PhienBan
                    && thanhToanNhap == lanTruoc.HoaDon.ThanhToanNhap
                    && LaCungNoiDungNghiepVu(lanTruoc.HoaDon, dongBans, thanhToans);
                if (!trungKhop)
                {
                    return new(TrangThaiGhiHoaDonDemo.XungDot, daGhi with { },
                        $"Mã yêu cầu đã được dùng cho hóa đơn {daGhi.MaHoaDon} với nội dung khác.");
                }

                return daGhi.PhienBan == lanTruoc.HoaDon.PhienBan
                    ? new(TrangThaiGhiHoaDonDemo.GuiLaiTrungKhop, daGhi with { })
                    : new(TrangThaiGhiHoaDonDemo.XungDot, daGhi with { }, $"Hóa đơn {daGhi.MaHoaDon} đã thay đổi sau yêu cầu này.");
            }

            var chiSo = maNhap is null ? -1 : _hoaDons.FindIndex(h =>
                string.Equals(h.MaHoaDon, maNhap, StringComparison.OrdinalIgnoreCase));
            HoaDonDemoRecord? hoaDon = chiSo < 0 ? null : _hoaDons[chiSo];
            if (maNhap is not null)
            {
                if (hoaDon is null)
                {
                    return new(TrangThaiGhiHoaDonDemo.KhongTimThay, null, "Không tìm thấy hóa đơn nháp.");
                }

                if (!CoQuyenSuaNhap(hoaDon))
                {
                    return new(TrangThaiGhiHoaDonDemo.KhongCoQuyen, null, "Thu ngân chỉ được sửa hóa đơn nháp thuộc ca đang mở của chính mình.");
                }

                if (hoaDon.TrangThai != "Nhap" || hoaDon.PhienBan != phienBan)
                {
                    return new(TrangThaiGhiHoaDonDemo.XungDot, hoaDon with { },
                        $"Hóa đơn {hoaDon.MaHoaDon} đã được cập nhật, xác nhận hoặc hủy ở nơi khác.");
                }
            }

            if (hoaDon is not null && thanhToanNhap is not null && hoaDon.ThanhToanNhap == thanhToanNhap
                && LaCungNoiDungNghiepVu(hoaDon, dongBans, thanhToans))
            {
                var banHienTai = hoaDon with { };
                _lanGhiHoaDon.Add(maYeuCau, new(maNhap, phienBan, banHienTai));
                return new(TrangThaiGhiHoaDonDemo.DaGhi, banHienTai);
            }

            var now = DateTime.Now;
            var laNhap = thanhToanNhap is not null;
            var record = new HoaDonDemoRecord
            {
                MaHoaDon = hoaDon?.MaHoaDon ?? $"HD-{now:ddMM}-{_nextInvoiceNumber++:D3}",
                MaCa = hoaDon?.MaCa ?? ca.MaCa,
                MaNguoiLap = hoaDon?.MaNguoiLap ?? ca.MaNguoiMo,
                TenNguoiLap = hoaDon?.TenNguoiLap ?? ca.TenNguoiMo,
                LucLap = hoaDon?.LucLap ?? now,
                LucXacNhan = laNhap ? null : now,
                TrangThai = laNhap ? "Nhap" : "DaXacNhan",
                PhienBan = (hoaDon?.PhienBan ?? 0) + 1,
                DongBans = dongBans.ToArray(),
                ThanhToans = thanhToans.ToArray(),
                ThanhToanNhap = thanhToanNhap
            };

            if (chiSo >= 0)
            {
                _hoaDons[chiSo] = record;
            }
            else
            {
                _hoaDons.Insert(0, record);
            }

            _lanGhiHoaDon.Add(maYeuCau, new(maNhap, phienBan, record with { }));
            return new(TrangThaiGhiHoaDonDemo.DaGhi, record with { });
        }
    }

    private static bool LaCungNoiDungNghiepVu(
        HoaDonDemoRecord existing,
        IReadOnlyList<DongBanDemoRecord> dongBans,
        IReadOnlyList<ThanhToanDemoRecord> thanhToans)
    {
        var existingLines = existing.DongBans
            .GroupBy(d => (MaHang: d.MaHang.ToUpperInvariant(), d.DonGiaThucBan))
            .Select(g => (g.Key.MaHang, SoLuong: g.Sum(x => x.SoLuongBan), g.Key.DonGiaThucBan))
            .OrderBy(x => x.MaHang, StringComparer.Ordinal)
            .ThenBy(x => x.DonGiaThucBan)
            .ToList();

        var incomingLines = dongBans
            .GroupBy(d => (MaHang: d.MaHang.ToUpperInvariant(), d.DonGiaThucBan))
            .Select(g => (g.Key.MaHang, SoLuong: g.Sum(x => x.SoLuongBan), g.Key.DonGiaThucBan))
            .OrderBy(x => x.MaHang, StringComparer.Ordinal)
            .ThenBy(x => x.DonGiaThucBan)
            .ToList();

        if (!existingLines.SequenceEqual(incomingLines))
        {
            return false;
        }

        var existingPayments = existing.ThanhToans
            .Select(t => (PhuongThuc: t.PhuongThuc.ToUpperInvariant(), t.SoTienThanhToan, t.TienKhachDua))
            .OrderBy(x => x.PhuongThuc, StringComparer.Ordinal)
            .ThenBy(x => x.SoTienThanhToan)
            .ThenBy(x => x.TienKhachDua ?? -1m)
            .ToList();

        var incomingPayments = thanhToans
            .Select(t => (PhuongThuc: t.PhuongThuc.ToUpperInvariant(), t.SoTienThanhToan, t.TienKhachDua))
            .OrderBy(x => x.PhuongThuc, StringComparer.Ordinal)
            .ThenBy(x => x.SoTienThanhToan)
            .ThenBy(x => x.TienKhachDua ?? -1m)
            .ToList();

        return existingPayments.SequenceEqual(incomingPayments);
    }

    public (bool ThanhCong, string ThongBao) HuyHoaDon(
        string maHoaDon,
        int? phienBan,
        string lyDoHuy,
        string vaiTroDemo,
        bool coCaDaoMo)
    {
        lock (_sync)
        {
            var hoaDon = _hoaDons.FirstOrDefault(h => string.Equals(h.MaHoaDon, maHoaDon, StringComparison.OrdinalIgnoreCase));
            if (hoaDon is null)
            {
                return (false, $"Không tìm thấy hóa đơn {maHoaDon}.");
            }

            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                return (false, "Vui lòng nhập lý do hủy hóa đơn.");
            }

            if (hoaDon.TrangThai == "DaHuy")
            {
                return (false, $"Hóa đơn {hoaDon.MaHoaDon} đã ở trạng thái Đã hủy, không thể hủy lại.");
            }

            var laQuanLy = string.Equals(vaiTroDemo, "QuanLy", StringComparison.OrdinalIgnoreCase);
            if (phienBan.HasValue && hoaDon.PhienBan != phienBan)
            {
                return (false, $"Hóa đơn {hoaDon.MaHoaDon} vừa thay đổi. Vui lòng xem lại thông tin trước khi hủy.");
            }

            if (hoaDon.TrangThai == "Nhap")
            {
                if (phienBan is null)
                {
                    return (false, "Vui lòng mở lại hóa đơn nháp để lấy phiên bản hiện tại trước khi hủy.");
                }

                if (!laQuanLy)
                {
                    var ca = _caBans.FirstOrDefault(c => c.MaCa == hoaDon.MaCa);
                    if (hoaDon.MaNguoiLap != "S-TK-TN1" || ca is null || ca.TrangThai != "DangMo")
                    {
                        return (false, "Thu ngân chỉ được hủy hóa đơn nháp thuộc ca đang mở của chính mình.");
                    }
                }

                hoaDon.TrangThai = "DaHuy";
                hoaDon.PhienBan++;
                hoaDon.LucHuy = DateTime.Now;
                hoaDon.TenNguoiHuy = laQuanLy ? "Nguyễn An" : "Lê Linh";
                hoaDon.LyDoHuy = lyDoHuy.Trim();
                return (true, $"Đã hủy hóa đơn nháp {hoaDon.MaHoaDon}.");
            }

            if (hoaDon.TrangThai == "DaXacNhan")
            {
                if (!laQuanLy)
                {
                    return (false, "Chỉ Quản lý mới có quyền hủy hóa đơn đã xác nhận.");
                }

                if (hoaDon.CoPhieuTraHieuLuc)
                {
                    return (false, $"Không thể hủy {hoaDon.MaHoaDon} vì đã có phiếu trả hiệu lực ({hoaDon.MaPhieuTraLienQuan}).");
                }

                var coTienMat = hoaDon.ThanhToans.Any(t => t.PhuongThuc == "TienMat" && t.SoTienThanhToan > 0);
                if (coTienMat && (!coCaDaoMo || MaCaMo == null))
                {
                    return (false, $"Hóa đơn {hoaDon.MaHoaDon} có thanh toán tiền mặt, cần có một ca đang mở để ghi nhận đảo tiền mặt.");
                }

                hoaDon.TrangThai = "DaHuy";
                hoaDon.PhienBan++;
                hoaDon.LucHuy = DateTime.Now;
                hoaDon.TenNguoiHuy = "Nguyễn An";
                hoaDon.MaCaDao = coTienMat ? MaCaMo : null;
                hoaDon.LyDoHuy = lyDoHuy.Trim();
                return (true, coTienMat
                    ? $"Đã hủy hóa đơn {hoaDon.MaHoaDon} và ghi nhận ca đảo tiền mặt {hoaDon.MaCaDao}."
                    : $"Đã hủy hóa đơn {hoaDon.MaHoaDon} (thanh toán chuyển khoản, không tác động két ca).");
            }

            return (false, "Trạng thái hóa đơn không hợp lệ để hủy.");
        }
    }

    private sealed record LanGhiHoaDon(string? MaNhap, int? PhienBan, HoaDonDemoRecord HoaDon);
}

public enum TrangThaiGhiHoaDonDemo
{
    DaGhi,
    GuiLaiTrungKhop,
    XungDot,
    KhongTimThay,
    KhongCoQuyen,
    CaDaDong
}

public sealed record KetQuaGhiHoaDonDemo(
    TrangThaiGhiHoaDonDemo TrangThai,
    HoaDonDemoRecord? HoaDon,
    string? ThongBao = null);

public sealed record MatHangDemoItem(
    string MaHang,
    string TenHang,
    string TenNhom,
    string DonViTinh,
    decimal GiaBanHienHanh,
    int TonMau);

public sealed record CaBanDemoItem(
    string MaCa,
    string MaNguoiMo,
    string TenNguoiMo,
    string TrangThai,
    DateTime LucMo);

public sealed record DongBanDemoRecord(
    string MaHang,
    string TenHang,
    string DonViTinh,
    int SoLuongBan,
    decimal DonGiaThucBan)
{
    public decimal ThanhTien => SoLuongBan * DonGiaThucBan;
}

public sealed record ThanhToanDemoRecord(
    string PhuongThuc,
    decimal SoTienThanhToan,
    decimal? TienKhachDua);

public sealed record ThanhToanNhapDemoRecord(
    string CheDoThanhToan,
    decimal? TienMatPhanBo,
    decimal? TienKhachDua,
    decimal? TienChuyenKhoan);

public sealed record HoaDonDemoRecord
{
    public required string MaHoaDon { get; init; }
    public required string MaCa { get; init; }
    public required string MaNguoiLap { get; init; }
    public required string TenNguoiLap { get; init; }
    public DateTime LucLap { get; init; }
    public DateTime? LucXacNhan { get; set; }
    public required string TrangThai { get; set; }
    public int PhienBan { get; set; }
    public DateTime? LucHuy { get; set; }
    public string? TenNguoiHuy { get; set; }
    public string? MaCaDao { get; set; }
    public string? LyDoHuy { get; set; }
    public bool CoPhieuTraHieuLuc { get; set; }
    public string? MaPhieuTraLienQuan { get; set; }
    public IReadOnlyList<DongBanDemoRecord> DongBans { get; init; } = [];
    public IReadOnlyList<ThanhToanDemoRecord> ThanhToans { get; init; } = [];
    public ThanhToanNhapDemoRecord? ThanhToanNhap { get; init; }

    public decimal TongTienHang => DongBans.Sum(d => d.ThanhTien);
    public decimal TongTienMatPhanBo => ThanhToans.Where(t => t.PhuongThuc == "TienMat").Sum(t => t.SoTienThanhToan);
    public decimal TongChuyenKhoan => ThanhToans.Where(t => t.PhuongThuc == "ChuyenKhoan").Sum(t => t.SoTienThanhToan);
    public decimal TongTienKhachDua => ThanhToans.Where(t => t.PhuongThuc == "TienMat" && t.TienKhachDua.HasValue).Sum(t => t.TienKhachDua!.Value);
    public decimal TienThua => Math.Max(0m, TongTienKhachDua - TongTienMatPhanBo);
}
