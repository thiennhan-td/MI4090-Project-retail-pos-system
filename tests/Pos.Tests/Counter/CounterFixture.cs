using Moq;
using Pos.Contracts;
using Pos.Counter.Services;
using Pos.Counter.Strategies;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Interfaces;

namespace Pos.Tests.Counter;

internal sealed class CounterFixture
{
    public static readonly DateTime Luc = new(2026, 11, 28, 8, 0, 0, DateTimeKind.Utc);
    public const string Actor = "TK-1";
    public const string MaCa = "CA-1";
    public const string MaHoaDon = "HD-1";
    public const string MaDong = "DB-1";
    public const string MaPhieu = "PT-1";
    public Mock<IUnitOfWork> Work { get; } = new();
    public Mock<IAuthorizationService> Auth { get; } = new();
    public Mock<IInventoryService> Kho { get; } = new();
    public Mock<IInventoryReportQueryService> BaoCaoKho { get; } = new();
    public Mock<IHoaDonRepository> HoaDon { get; } = new();
    public Mock<IPhieuTraRepository> Tra { get; } = new();
    public Mock<ICaBanRepository> Ca { get; } = new();
    public Mock<ICounterReportRepository> BaoCao { get; } = new();
    public Mock<IPhieuThuChiRepository> ThuChi { get; } = new();
    public Mock<ICatalogQueryService> Catalog { get; } = new();

    public CounterFixture()
    {
        Ca.Setup(repo => repo.KhoaTheoMaAsync(MaCa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaBan(MaCa, Actor, Luc, 300_000m));
    }

    public static PaymentProcessorContext Payments() => new([new TienMatStrategy(), new ChuyenKhoanStrategy()]);
    public SaleService Sale() => new(HoaDon.Object, Ca.Object, Tra.Object, Kho.Object, Catalog.Object,
        Auth.Object, Work.Object, Payments());
    public ReturnService Return() => new(Tra.Object, HoaDon.Object, Ca.Object, Kho.Object, Auth.Object, Work.Object);
    public ShiftCashService Shift() => new(Ca.Object, ThuChi.Object, BaoCaoKho.Object, Auth.Object, Work.Object);

    public HoaDon TaoHoaDon(bool daXacNhan = false)
    {
        var h = new HoaDon(MaHoaDon, MaCa, Actor, Luc);
        h.ThemDongBan(new(MaDong, MaHoaDon, "MH-1", 2, 25_000m));
        h.ThemThanhToanBan(new("TT-1", MaHoaDon, PhuongThucThanhToan.TienMat, 50_000m, 60_000m));
        if (daXacNhan)
        {
            h.XacNhan(Luc);
        }
        HoaDon.Setup(repo => repo.KhoaTheoMaAsync(MaHoaDon, It.IsAny<CancellationToken>())).ReturnsAsync(h);
        return h;
    }

    public PhieuTra TaoPhieuTra(int nhapLai = 1)
    {
        var phieu = new PhieuTra(MaPhieu, MaHoaDon, MaCa, Actor, Luc);
        phieu.ThemDongTra(new("DT-1", MaPhieu, MaDong, 1, nhapLai));
        phieu.ThemKhoanHoan(new("KH-1", MaPhieu, PhuongThucThanhToan.TienMat, 25_000m));
        Tra.Setup(repo => repo.KhoaTheoMaAsync(MaPhieu, It.IsAny<CancellationToken>())).ReturnsAsync(phieu);
        return phieu;
    }
}
