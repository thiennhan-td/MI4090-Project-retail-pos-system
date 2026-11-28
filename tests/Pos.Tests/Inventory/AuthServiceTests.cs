using Moq;
using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Security;
using Pos.Inventory.Services;

namespace Pos.Tests.Inventory;

public sealed class AuthServiceTests
{
    private static TaiKhoan TaoTaiKhoan() => new("TK-1", "QuanLy", "Quản lý", "admin", "da-bam");

    [Fact]
    public async Task DangNhap_ChuanHoaTenVaChiTraDanhTinhSauKhiKiemTraMatKhau()
    {
        var repository = new Mock<ITaiKhoanRepository>();
        var authorization = new Mock<IAuthorizationService>();
        var hasher = new Mock<IMatKhauHasher>();
        repository.Setup(x => x.LayTheoTenDangNhapAsync("admin", default)).ReturnsAsync(TaoTaiKhoan());
        hasher.Setup(x => x.KiemTra("mat-khau", "da-bam")).Returns(true);
        authorization.Setup(x => x.LayTapQuyenAsync("TK-1", default))
            .ReturnsAsync(new HashSet<string>(StringComparer.Ordinal) { MaQuyenPos.HT_PHAN_QUYEN });
        var service = new AuthService(repository.Object, hasher.Object, authorization.Object);

        var ketQua = await service.DangNhapAsync(" admin ", "mat-khau", default);

        Assert.NotNull(ketQua);
        Assert.Equal("TK-1", ketQua.MaTaiKhoan);
        Assert.Contains(MaQuyenPos.HT_PHAN_QUYEN, ketQua.Quyen);
    }

    [Fact]
    public async Task DangNhap_TaiKhoanBiKhoaKhongCapDanhTinhHoacNapQuyen()
    {
        var taiKhoan = TaoTaiKhoan();
        taiKhoan.Khoa();
        var repository = new Mock<ITaiKhoanRepository>();
        repository.Setup(x => x.LayTheoTenDangNhapAsync("admin", default)).ReturnsAsync(taiKhoan);
        var hasher = new Mock<IMatKhauHasher>(MockBehavior.Strict);
        var authorization = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var service = new AuthService(repository.Object, hasher.Object, authorization.Object);

        Assert.Null(await service.DangNhapAsync("admin", "mat-khau", default));
    }

    [Fact]
    public async Task KiemTraQuyen_TuChoiQuyenKhongDuocCap()
    {
        var repository = new Mock<ITaiKhoanRepository>();
        repository.Setup(x => x.LayTapQuyenAsync("TK-1", default))
            .ReturnsAsync(new HashSet<string>(StringComparer.Ordinal) { MaQuyenPos.DM_NHOM_XEM });
        var service = new AuthorizationService(repository.Object);

        var error = await Assert.ThrowsAsync<UnauthorizedActionException>(
            () => service.KiemTraQuyenAsync("TK-1", MaQuyenPos.DM_NHOM_QL, default));

        Assert.Equal(MaQuyenPos.DM_NHOM_QL, error.MaQuyen);
    }

    [Fact]
    public async Task LuuTaiKhoan_RollbackKhiKhoaQuanLyCuoiCung()
    {
        var repository = new Mock<IQuanTriTaiKhoanRepository>();
        var work = new Mock<IUnitOfWork>();
        repository.Setup(x => x.KhoaVaiTroAsync("QuanLy", default)).ReturnsAsync(new VaiTro("QuanLy", "Quản lý"));
        repository.Setup(x => x.LayTheoMaAsync("TK-1", default)).ReturnsAsync(TaoTaiKhoan());
        repository.Setup(x => x.DemQuanLyKhacDangHoatDongAsync("TK-1", default)).ReturnsAsync(0);
        var service = new TaiKhoanService(repository.Object, work.Object, Mock.Of<IAuthorizationService>(),
            Mock.Of<IMatKhauHasher>());

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.LuuAsync("TK-1", new("TK-1", "Quản lý", "admin", "QuanLy", null, false), default));

        repository.Verify(x => x.CapNhatAsync(It.IsAny<TaiKhoan>(), default), Times.Never);
        work.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        work.Verify(x => x.CommitAsync(default), Times.Never);
    }

    [Fact]
    public void BamMatKhau_DungSaltRiengVaTuChoiHashGiuCho()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var bam = hasher.Bam("mat-khau");

        Assert.NotEqual(bam, hasher.Bam("mat-khau"));
        Assert.True(hasher.KiemTra("mat-khau", bam));
        Assert.False(hasher.KiemTra("mat-khau-sai", bam));
        Assert.False(hasher.KiemTra("mat-khau", "DEMO_DISABLED"));
    }
}
