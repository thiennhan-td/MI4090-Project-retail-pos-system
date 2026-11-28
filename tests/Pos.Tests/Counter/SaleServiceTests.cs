using Moq;
using Pos.Contracts;
using Pos.Counter.Strategies;
using Pos.Counter.Services;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;
using static Pos.Tests.Counter.CounterFixture;

namespace Pos.Tests.Counter;

public sealed class SaleServiceTests
{
    [Fact]
    public async Task XacNhanBan_GhiXuatSauLuuVaCommitSauKho()
    {
        var f = new CounterFixture();
        var h = f.TaoHoaDon();
        var steps = new List<string>();
        f.Auth.Setup(s => s.KiemTraQuyenAsync(Actor, MaQuyenPos.BAN_XAC_NHAN, default))
            .Callback(() => steps.Add("quyen")).Returns(Task.CompletedTask);
        f.Work.Setup(w => w.BeginTransactionAsync(default)).Callback(() => steps.Add("begin")).Returns(Task.CompletedTask);
        f.HoaDon.Setup(r => r.CapNhatAsync(h, default)).Callback(() => steps.Add("luu")).Returns(Task.CompletedTask);
        f.Kho.Setup(s => s.GhiNhanXuatBanAsync(It.IsAny<YeuCauXuatBanDto>(), default))
            .Callback(() => steps.Add("xuat")).ReturnsAsync(new KetQuaGhiTheKhoDto(1, 0));
        f.Work.Setup(w => w.CommitAsync(default)).Callback(() => steps.Add("commit")).Returns(Task.CompletedTask);

        var result = await f.Sale().XacNhanBanAsync(MaHoaDon, MaCa, Actor, Luc, PhienBanNhap.CuaHoaDon(h), "req-1");

        Assert.Equal(TrangThaiChungTu.DaXacNhan, result.TrangThai);
        Assert.Equal(new[] { "quyen", "begin", "luu", "xuat", "commit" }, steps);
        f.Kho.Verify(s => s.GhiNhanXuatBanAsync(It.Is<YeuCauXuatBanDto>(r =>
            r.MaHoaDon == MaHoaDon && r.DongHang[0].SoLuongBan == 2), default), Times.Once);
    }

    [Fact]
    public async Task XacNhanBan_ThieuTonRollbackVaGiuNgoaiLeGoc()
    {
        var f = new CounterFixture();
        var h = f.TaoHoaDon();
        var failure = new InsufficientStockException("MH-1", 1, 2);
        f.Kho.Setup(s => s.GhiNhanXuatBanAsync(It.IsAny<YeuCauXuatBanDto>(), default)).ThrowsAsync(failure);

        var actual = await Assert.ThrowsAsync<InsufficientStockException>(
            () => f.Sale().XacNhanBanAsync(MaHoaDon, MaCa, Actor, Luc, PhienBanNhap.CuaHoaDon(h), "req-1"));

        Assert.Same(failure, actual);
        f.Requests.Verify(r => r.ThemAsync(It.IsAny<string>(), It.IsAny<Pos.Domain.Interfaces.KetQuaYeuCauQuay>(),
            default), Times.Never);
        f.Work.Verify(w => w.RollbackAsync(CancellationToken.None), Times.Once);
        f.Work.Verify(w => w.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task XacNhanBan_RequestHuyVaRollbackLoiVanGiuNgoaiLeGoc()
    {
        var f = new CounterFixture();
        var h = f.TaoHoaDon();
        using var cancellation = new CancellationTokenSource();
        var original = new InsufficientStockException("MH-1", 1, 2);
        var rollback = new InvalidOperationException("Lỗi dọn giao dịch.");
        f.Kho.Setup(s => s.GhiNhanXuatBanAsync(It.IsAny<YeuCauXuatBanDto>(), cancellation.Token))
            .Callback(() => cancellation.Cancel()).ThrowsAsync(original);
        f.Work.Setup(w => w.RollbackAsync(CancellationToken.None)).ThrowsAsync(rollback);

        var actual = await Assert.ThrowsAsync<InsufficientStockException>(
            () => f.Sale().XacNhanBanAsync(MaHoaDon, MaCa, Actor, Luc, PhienBanNhap.CuaHoaDon(h), "req-1", cancellation.Token));

        Assert.Same(original, actual);
        Assert.Same(rollback, actual.Data["Counter.RollbackException"]);
        f.Work.Verify(w => w.RollbackAsync(CancellationToken.None), Times.Once);
        f.Work.Verify(w => w.RollbackAsync(cancellation.Token), Times.Never);
    }

    [Fact]
    public async Task XacNhanBan_ThieuQuyenKhongMoGiaoDich()
    {
        var f = new CounterFixture();
        var h = f.TaoHoaDon();
        f.Auth.Setup(s => s.KiemTraQuyenAsync(Actor, MaQuyenPos.BAN_XAC_NHAN, default))
            .ThrowsAsync(new UnauthorizedActionException(MaQuyenPos.BAN_XAC_NHAN));

        var error = await Assert.ThrowsAsync<UnauthorizedActionException>(
            () => f.Sale().XacNhanBanAsync(MaHoaDon, MaCa, Actor, Luc, PhienBanNhap.CuaHoaDon(h), "req-1"));

        Assert.Equal(MaQuyenPos.BAN_XAC_NHAN, error.MaQuyen);
        f.Work.Verify(w => w.BeginTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task HuyHoaDon_DaCoTraHieuLucBiChan()
    {
        var f = new CounterFixture();
        var h = f.TaoHoaDon(true);
        f.CounterAuth.Setup(r => r.LayVaiTroCoQuyenAsync(Actor, MaQuyenPos.BAN_HUY_HD_XAC_NHAN, default))
            .ReturnsAsync("QuanLy");
        f.Tra.Setup(r => r.CoPhieuTraHieuLucAsync(MaHoaDon, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => f.Sale().HuyHoaDonAsync(MaHoaDon, Actor, "Hủy đơn", MaCa, Luc));

        Assert.Equal(TrangThaiChungTu.DaXacNhan, h.TrangThai);
        f.Kho.Verify(s => s.GhiNhanDaoHuyBanAsync(It.IsAny<YeuCauDaoHuyBanDto>(), default), Times.Never);
        f.Work.Verify(w => w.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public void TienMat_TinhTienThuaVaAnhHuongKet()
    {
        IPhuongThucThanhToanStrategy strategy = new TienMatStrategy();

        var tienThua = strategy.TinhTienThua(50_000m, 60_000m);

        Assert.Equal(10_000m, tienThua);
        Assert.Equal(-50_000m, strategy.TinhBienDongKetCa(50_000m, false));
        Assert.Throws<BusinessRuleViolationException>(() => strategy.KiemTraHopLe(50_000m, 49_999m));
    }

    [Fact]
    public void ChuyenKhoan_KhongCoTienThuaVaKhongAnhHuongKet()
    {
        IPhuongThucThanhToanStrategy strategy = new ChuyenKhoanStrategy();

        var tienThua = strategy.TinhTienThua(50_000m, null);

        Assert.Equal(0m, tienThua);
        Assert.Equal(0m, strategy.TinhBienDongKetCa(50_000m, true));
        Assert.Throws<BusinessRuleViolationException>(() => strategy.KiemTraHopLe(50_000m, 50_000m));
    }
}
