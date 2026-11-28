using Moq;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using static Pos.Tests.Counter.CounterFixture;

namespace Pos.Tests.Counter;

public sealed class ShiftCashServiceTests
{
    [Fact]
    public async Task MoCa_DaCoCaMoThiChan()
    {
        var f = new CounterFixture();
        f.Ca.Setup(r => r.LayCaMoAsync(Actor, default)).ReturnsAsync(new CaBan(MaCa, Actor, Luc, 300_000m));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => f.Shift().MoCaAsync(Actor, 100_000m, Luc));

        f.Ca.Verify(r => r.ThemAsync(It.IsAny<CaBan>(), default), Times.Never);
        f.Work.Verify(w => w.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task DongCa_TinhDuSoGiaoDichVaChiNcc()
    {
        var f = new CounterFixture();
        f.Ca.Setup(r => r.TongHopKetAsync(MaCa, default)).ReturnsAsync(new SoQuyCa(80_000, 15_000, 20_000, 10_000, 5_000));
        f.BaoCaoKho.Setup(r => r.TinhTongChiNccTuKetTheoCaAsync(MaCa, default)).ReturnsAsync(35_000);

        var ca = await f.Shift().DongCaAsync(MaCa, Actor, 330_000m, Luc);

        Assert.Equal(TrangThaiChungTu.DaDong, ca.TrangThai);
        Assert.Equal(335_000m, ca.TienDuKienKhiChot);
        Assert.Equal(-5_000m, ca.ChenhLech);
        f.Ca.Verify(r => r.CapNhatAsync(ca, default), Times.Once);
        f.Work.Verify(w => w.CommitAsync(default), Times.Once);
    }
}
