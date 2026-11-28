using Moq;
using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;
using static Pos.Tests.Counter.CounterFixture;

namespace Pos.Tests.Counter;

public sealed class ReturnServiceTests
{
    [Fact]
    public async Task XacNhanTra_NhapLaiMotDongVaCommit()
    {
        var f = new CounterFixture();
        f.TaoHoaDon(true);
        f.TaoPhieuTra();

        var phieu = await f.Return().XacNhanTraAsync(MaPhieu, MaCa, Actor, Luc);

        Assert.Equal(TrangThaiChungTu.DaXacNhan, phieu.TrangThai);
        f.Tra.Verify(r => r.KhoaTheoMaAsync(MaPhieu, default), Times.Once);
        f.Tra.Verify(r => r.LayTheoMaAsync(MaPhieu, default), Times.Never);
        f.Kho.Verify(k => k.GhiNhanNhapTraAsync(It.Is<YeuCauNhapTraDto>(req =>
            req.MaPhieuTra == MaPhieu && req.DongHang[0].SoLuongNhapLai == 1), default), Times.Once);
        f.Work.Verify(w => w.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task XoaNhap_KhongXoaPhieuDaXacNhanSauKhiLayKhoa()
    {
        var f = new CounterFixture();
        var phieu = f.TaoPhieuTra();
        phieu.XacNhan(Luc, 25_000m);

        await Assert.ThrowsAsync<InvalidStateTransitionException>(() => f.Return().XoaNhapAsync(MaPhieu, MaCa, Actor));

        f.Tra.Verify(r => r.KhoaTheoMaAsync(MaPhieu, default), Times.Once);
        f.Tra.Verify(r => r.XoaNhapAsync(MaPhieu, default), Times.Never);
        f.Work.Verify(w => w.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task XacNhanTra_VuotSoLuongConLaiBiChan()
    {
        var f = new CounterFixture();
        f.TaoHoaDon(true);
        f.TaoPhieuTra();
        f.Tra.Setup(r => r.TinhLuongDaTraAsync(MaDong, default)).ReturnsAsync(2);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => f.Return().XacNhanTraAsync(MaPhieu, MaCa, Actor, Luc));

        f.Kho.Verify(k => k.GhiNhanNhapTraAsync(It.IsAny<YeuCauNhapTraDto>(), default), Times.Never);
        f.Work.Verify(w => w.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task XacNhanTra_KhoLoiThiRollbackVaGiuNgoaiLeGoc()
    {
        var f = new CounterFixture();
        f.TaoHoaDon(true);
        f.TaoPhieuTra();
        var failure = new ConcurrencyConflictException("Thẻ kho thay đổi.");
        f.Kho.Setup(s => s.GhiNhanNhapTraAsync(It.IsAny<YeuCauNhapTraDto>(), default)).ThrowsAsync(failure);

        var actual = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => f.Return().XacNhanTraAsync(MaPhieu, MaCa, Actor, Luc));

        Assert.Same(failure, actual);
        f.Work.Verify(w => w.RollbackAsync(default), Times.Once);
        f.Work.Verify(w => w.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task XacNhanTra_KhongCoHangNhapLaiThiKhongGhiKho()
    {
        var f = new CounterFixture();
        f.TaoHoaDon(true);
        f.TaoPhieuTra(0);

        var phieu = await f.Return().XacNhanTraAsync(MaPhieu, MaCa, Actor, Luc);

        Assert.Equal(TrangThaiChungTu.DaXacNhan, phieu.TrangThai);
        f.Kho.Verify(k => k.GhiNhanNhapTraAsync(It.IsAny<YeuCauNhapTraDto>(), default), Times.Never);
        f.Work.Verify(w => w.CommitAsync(default), Times.Once);
    }
}
