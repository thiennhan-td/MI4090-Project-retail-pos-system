using Moq;
using Pos.Contracts.Reports;
using Pos.Counter.Reports;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using static Pos.Tests.Counter.CounterFixture;

namespace Pos.Tests.Counter;

public sealed class CounterReportTests
{
    private static readonly TimeZoneInfo MuiGio = TimeZoneInfo.CreateCustomTimeZone("UTC+7",
        TimeSpan.FromHours(7), "UTC+7", "UTC+7");
    private static readonly DateOnly Ngay = new(2026, 11, 28);

    [Theory]
    [InlineData(MaQuyenPos.BC_CUOI_NGAY)]
    [InlineData(MaQuyenPos.BC_BAN_TRA)]
    [InlineData(MaQuyenPos.BC_THANH_TOAN_CA)]
    public async Task BaoCao_ThieuQuyenChanTruocTruyVan(string quyen)
    {
        var f = new CounterFixture();
        IBaoCaoNghiepVu baoCao = quyen switch
        {
            MaQuyenPos.BC_CUOI_NGAY => new BaoCaoTongHopCuoiNgay(f.Auth.Object, MuiGio, f.BaoCao.Object,
                f.BaoCaoKho.Object, f.Work.Object),
            MaQuyenPos.BC_BAN_TRA => new BaoCaoBanTra(f.Auth.Object, MuiGio, f.BaoCao.Object, f.Work.Object),
            _ => new BaoCaoThanhToanLechCa(f.Auth.Object, MuiGio, f.BaoCao.Object, f.Ca.Object,
                f.BaoCaoKho.Object, f.Work.Object)
        };
        f.Auth.Setup(a => a.KiemTraQuyenAsync(Actor, quyen, default)).ThrowsAsync(new UnauthorizedActionException(quyen));

        var ex = await Assert.ThrowsAsync<UnauthorizedActionException>(
            () => baoCao.ThucThiAsync(new(Ngay, Ngay), Actor, default));

        Assert.Equal(quyen, ex.MaQuyen);
        f.BaoCao.VerifyNoOtherCalls();
        f.Work.Verify(w => w.BeginTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task BaoCaoBanTra_DungQuyenKhoangUtcVaTongHopTruocKhiXuat()
    {
        var f = new CounterFixture();
        IBaoCaoNghiepVu baoCao = new BaoCaoBanTra(f.Auth.Object, MuiGio, f.BaoCao.Object, f.Work.Object);
        var tu = new DateTime(2026, 11, 27, 17, 0, 0, DateTimeKind.Utc);
        var den = tu.AddDays(1);
        var steps = new List<string>();
        f.Auth.Setup(a => a.KiemTraQuyenAsync(Actor, MaQuyenPos.BC_BAN_TRA, default))
            .Callback(() => steps.Add("quyen")).Returns(Task.CompletedTask);
        f.BaoCao.Setup(r => r.LaySuKienAsync(tu, den, default))
            .Callback(() => steps.Add("truy van"))
            .ReturnsAsync(new SuKienBanTra[]
            {
                new(Luc, "HD-1", MaCa, "Ban", "MH-1", 3, 75_000m, null),
                new(Luc, "HD-2", MaCa, "DaoBan", "MH-1", 1, 25_000m, null),
                new(Luc, "PT-1", MaCa, "Tra", "MH-1", 1, 25_000m, null),
                new(Luc, "HD-1", MaCa, "ThuBan", null, 0, 75_000m, PhuongThucThanhToan.TienMat)
            });

        var result = await baoCao.ThucThiAsync(new(Ngay, Ngay), Actor, default);

        Assert.Equal(new[] { "quyen", "truy van" }, steps);
        Assert.Equal(baoCao.TenBaoCao, result.TenBaoCao);
        Assert.Equal(new object?[] { "MH-1", 3L, 1L, 1L, 1L, 75_000m, 25_000m, 50_000m, 25_000m, 25_000m },
            result.Dong[0]);
        f.BaoCao.Verify(r => r.LaySuKienAsync(tu, den, default), Times.Once);
    }
}
