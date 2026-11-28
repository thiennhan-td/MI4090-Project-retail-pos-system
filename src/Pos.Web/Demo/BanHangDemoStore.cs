using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Pos.Web.Demo;

public sealed class BanHangDemoSessionProvider
{
    private const string SessionInitKey = "PosDemoStoreInitialized";
    private readonly IMemoryCache _cache;

    public BanHangDemoSessionProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    public BanHangDemoStore LayKhoTheoPhien(HttpContext httpContext)
    {
        httpContext.Session.SetString(SessionInitKey, "1");
        var sessionKey = $"pos-demo-store:{httpContext.Session.Id}";

        return _cache.GetOrCreate(sessionKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(4);
            return new BanHangDemoStore();
        })!;
    }
}

public sealed partial class BanHangDemoStore
{
    private readonly object _sync = new();
    private readonly List<MatHangDemoItem> _matHangs;
    private readonly List<CaBanDemoItem> _caBans;
    private readonly List<HoaDonDemoRecord> _hoaDons;
    private readonly ConcurrentDictionary<string, string> _idempotencyKeys = new(StringComparer.Ordinal);
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
                DongBans =
                [
                    new DongBanDemoRecord("S-SUA", "Sữa tươi 180 ml", "Hộp", 1, 26000m)
                ],
                ThanhToans = []
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
                    new ThanhToanDemoRecord("TienMat", 52000m, 60000m)
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

            return (hoaDon, false);
        }
    }

    public KetQuaTaoHoaDonDemo TaoHoacLayHoaDonDaXacNhan(
        string maYeuCau,
        IReadOnlyList<DongBanDemoRecord> dongBans,
        IReadOnlyList<ThanhToanDemoRecord> thanhToans)
    {
        lock (_sync)
        {
            if (_idempotencyKeys.TryGetValue(maYeuCau, out var existingCode))
            {
                var existing = _hoaDons.First(h => h.MaHoaDon == existingCode);
                if (LaCungNoiDungNghiepVu(existing, dongBans, thanhToans))
                {
                    return new KetQuaTaoHoaDonDemo(KetQuaIdempotency.GuiLaiTrungKhop, existing);
                }

                return new KetQuaTaoHoaDonDemo(KetQuaIdempotency.XungDotNoiDung, existing);
            }

            var now = DateTime.Now;
            var maHoaDon = $"HD-{now:ddMM}-{_nextInvoiceNumber:D3}";
            _nextInvoiceNumber++;

            var record = new HoaDonDemoRecord
            {
                MaHoaDon = maHoaDon,
                MaCa = "S-CA-1126",
                MaNguoiLap = "S-TK-TN1",
                TenNguoiLap = "Lê Linh",
                LucLap = now,
                LucXacNhan = now,
                TrangThai = "DaXacNhan",
                DongBans = dongBans.ToList(),
                ThanhToans = thanhToans.ToList()
            };

            _hoaDons.Insert(0, record);
            _idempotencyKeys[maYeuCau] = maHoaDon;
            return new KetQuaTaoHoaDonDemo(KetQuaIdempotency.TaoMoi, record);
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

            if (hoaDon.TrangThai == "Nhap")
            {
                if (!laQuanLy)
                {
                    var ca = _caBans.FirstOrDefault(c => c.MaCa == hoaDon.MaCa);
                    if (hoaDon.MaNguoiLap != "S-TK-TN1" || ca is null || ca.TrangThai != "DangMo")
                    {
                        return (false, "Thu ngân chỉ được hủy hóa đơn nháp thuộc ca đang mở của chính mình.");
                    }
                }

                hoaDon.TrangThai = "DaHuy";
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
                if (coTienMat && !coCaDaoMo)
                {
                    return (false, $"Hóa đơn {hoaDon.MaHoaDon} có thanh toán tiền mặt, cần có một ca đang mở để ghi nhận đảo tiền mặt.");
                }

                hoaDon.TrangThai = "DaHuy";
                hoaDon.LucHuy = DateTime.Now;
                hoaDon.TenNguoiHuy = "Nguyễn An";
                hoaDon.MaCaDao = coTienMat ? "S-CA-1126" : null;
                hoaDon.LyDoHuy = lyDoHuy.Trim();
                return (true, coTienMat
                    ? $"Đã hủy hóa đơn {hoaDon.MaHoaDon} và ghi nhận ca đảo tiền mặt S-CA-1126."
                    : $"Đã hủy hóa đơn {hoaDon.MaHoaDon} (thanh toán chuyển khoản, không tác động két ca).");
            }

            return (false, "Trạng thái hóa đơn không hợp lệ để hủy.");
        }
    }
}

public enum KetQuaIdempotency
{
    TaoMoi,
    GuiLaiTrungKhop,
    XungDotNoiDung
}

public sealed record KetQuaTaoHoaDonDemo(
    KetQuaIdempotency TrangThai,
    HoaDonDemoRecord HoaDon);

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

public sealed class HoaDonDemoRecord
{
    public required string MaHoaDon { get; init; }
    public required string MaCa { get; init; }
    public required string MaNguoiLap { get; init; }
    public required string TenNguoiLap { get; init; }
    public DateTime LucLap { get; init; }
    public DateTime? LucXacNhan { get; set; }
    public required string TrangThai { get; set; }
    public DateTime? LucHuy { get; set; }
    public string? TenNguoiHuy { get; set; }
    public string? MaCaDao { get; set; }
    public string? LyDoHuy { get; set; }
    public bool CoPhieuTraHieuLuc { get; set; }
    public string? MaPhieuTraLienQuan { get; set; }
    public List<DongBanDemoRecord> DongBans { get; init; } = [];
    public List<ThanhToanDemoRecord> ThanhToans { get; init; } = [];

    public decimal TongTienHang => DongBans.Sum(d => d.ThanhTien);
    public decimal TongTienMatPhanBo => ThanhToans.Where(t => t.PhuongThuc == "TienMat").Sum(t => t.SoTienThanhToan);
    public decimal TongChuyenKhoan => ThanhToans.Where(t => t.PhuongThuc == "ChuyenKhoan").Sum(t => t.SoTienThanhToan);
    public decimal TongTienKhachDua => ThanhToans.Where(t => t.PhuongThuc == "TienMat" && t.TienKhachDua.HasValue).Sum(t => t.TienKhachDua!.Value);
    public decimal TienThua => Math.Max(0m, TongTienKhachDua - TongTienMatPhanBo);
}
