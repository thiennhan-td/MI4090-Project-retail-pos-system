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
        var repository = new Mock<IQuanTriTaiKhoanRepository>();
        var hasher = new Mock<IMatKhauHasher>();
        repository.Setup(x => x.LayTheoTenDangNhapAsync("admin", default)).ReturnsAsync(TaoTaiKhoan());
        hasher.Setup(x => x.KiemTra("mat-khau", "da-bam")).Returns(true);
        repository.Setup(x => x.LayHoSoDangNhapAsync("TK-1", default))
            .ReturnsAsync(new HoSoDangNhap(TaoTaiKhoan(), new HashSet<string> { MaQuyenPos.HT_PHAN_QUYEN }));
        var service = new AuthService(repository.Object, hasher.Object);

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
        var repository = new Mock<IQuanTriTaiKhoanRepository>(MockBehavior.Strict);
        repository.Setup(x => x.LayTheoTenDangNhapAsync("admin", default)).ReturnsAsync(taiKhoan);
        var hasher = new Mock<IMatKhauHasher>(MockBehavior.Strict);
        var service = new AuthService(repository.Object, hasher.Object);

        Assert.Null(await service.DangNhapAsync("admin", "mat-khau", default));
    }

    [Fact]
    public async Task DangNhap_TuChoiMatKhauDaDoiTrongKhiXacThuc()
    {
        var repository = new Mock<IQuanTriTaiKhoanRepository>();
        var hasher = new Mock<IMatKhauHasher>();
        repository.Setup(x => x.LayTheoTenDangNhapAsync("admin", default)).ReturnsAsync(TaoTaiKhoan());
        hasher.Setup(x => x.KiemTra("mat-khau-cu", "da-bam")).Returns(true);
        var hienTai = TaoTaiKhoan();
        hienTai.DoiMatKhauBam("da-bam-moi");
        repository.Setup(x => x.LayHoSoDangNhapAsync("TK-1", default))
            .ReturnsAsync(new HoSoDangNhap(hienTai, new HashSet<string> { MaQuyenPos.HT_PHAN_QUYEN }));
        var service = new AuthService(repository.Object, hasher.Object);

        Assert.Null(await service.DangNhapAsync("admin", "mat-khau-cu", default));
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
        repository.Setup(x => x.TimTaiKhoanAsync(null, default)).ReturnsAsync([TaoTaiKhoan()]);
        repository.Setup(x => x.DemQuanLyKhacDangHoatDongAsync("TK-1", default)).ReturnsAsync(0);
        var service = new TaiKhoanService(repository.Object, work.Object, Mock.Of<IAuthorizationService>(),
            Mock.Of<IMatKhauHasher>());
        var daDoc = Assert.Single(await service.TimAsync("TK-1", null, default));

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.LuuAsync("TK-1", new("TK-1", "Quản lý", "admin", "QuanLy", null, false, daDoc.PhienBan), default));

        repository.Verify(x => x.CapNhatAsync(It.IsAny<TaiKhoan>(), default), Times.Never);
        work.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        work.Verify(x => x.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task DoiMatKhauQuanLy_YeuCauPhanQuyenNeuNguoiSuaKhongCoQuyenCuaTaiKhoanDich()
    {
        var repository = new Mock<IQuanTriTaiKhoanRepository>();
        var work = new Mock<IUnitOfWork>();
        var authorization = new Mock<IAuthorizationService>();
        var hasher = new Mock<IMatKhauHasher>();
        repository.Setup(x => x.KhoaVaiTroAsync("QuanLy", default)).ReturnsAsync(new VaiTro("QuanLy", "Quản lý"));
        repository.Setup(x => x.LayTheoMaAsync("TK-1", default)).ReturnsAsync(TaoTaiKhoan());
        repository.Setup(x => x.TimTaiKhoanAsync(null, default)).ReturnsAsync([TaoTaiKhoan()]);
        repository.Setup(x => x.LayQuyenVaiTroAsync("QuanLy", default))
            .ReturnsAsync(new HashSet<string> { MaQuyenPos.HT_QL_TAI_KHOAN, MaQuyenPos.HT_PHAN_QUYEN });
        authorization.Setup(x => x.LayTapQuyenAsync("TK-2", default))
            .ReturnsAsync(new HashSet<string> { MaQuyenPos.HT_QL_TAI_KHOAN });
        authorization.Setup(x => x.KiemTraQuyenAsync("TK-2", MaQuyenPos.HT_PHAN_QUYEN, default))
            .ThrowsAsync(new UnauthorizedActionException(MaQuyenPos.HT_PHAN_QUYEN));
        hasher.Setup(x => x.Bam("mat-khau-moi")).Returns("da-bam-moi");
        var service = new TaiKhoanService(repository.Object, work.Object, authorization.Object, hasher.Object);
        var daDoc = Assert.Single(await service.TimAsync("TK-2", null, default));

        await Assert.ThrowsAsync<UnauthorizedActionException>(() => service.LuuAsync("TK-2",
            new("TK-1", "Quản lý", "admin", "QuanLy", "mat-khau-moi", true, daDoc.PhienBan), default));

        repository.Verify(x => x.CapNhatAsync(It.IsAny<TaiKhoan>(), default), Times.Never);
        work.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task LuuTaiKhoan_PhienBanCuKhongTheMoLaiTaiKhoanBiKhoa()
    {
        var taiKhoan = TaoTaiKhoan();
        var repository = new Mock<IQuanTriTaiKhoanRepository>();
        var work = new Mock<IUnitOfWork>();
        repository.Setup(x => x.KhoaVaiTroAsync("QuanLy", default)).ReturnsAsync(new VaiTro("QuanLy", "Quản lý"));
        repository.Setup(x => x.TimTaiKhoanAsync(null, default)).ReturnsAsync([taiKhoan]);
        repository.Setup(x => x.LayTheoMaAsync("TK-1", default)).ReturnsAsync(taiKhoan);
        var service = new TaiKhoanService(repository.Object, work.Object, Mock.Of<IAuthorizationService>(),
            Mock.Of<IMatKhauHasher>());
        var daDoc = Assert.Single(await service.TimAsync("TK-2", null, default));
        taiKhoan.Khoa();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.LuuAsync("TK-2",
            new(daDoc.MaTaiKhoan, "Họ tên mới", daDoc.TenDangNhap, daDoc.MaVaiTro, null, daDoc.HoatDong,
                daDoc.PhienBan), default));

        var hienTai = Assert.Single(await service.TimAsync("TK-2", null, default));
        Assert.False(hienTai.HoatDong);
        Assert.NotEqual(daDoc.PhienBan, hienTai.PhienBan);
        repository.Verify(x => x.CapNhatAsync(It.IsAny<TaiKhoan>(), default), Times.Never);
        work.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        work.Verify(x => x.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task PhanQuyen_TuChoiMaVaiTroCoKhoangTrangDu()
    {
        var repository = new Mock<IQuanTriTaiKhoanRepository>();
        repository.Setup(x => x.KhoaVaiTroAsync("QuanLy", default)).ReturnsAsync(new VaiTro("QuanLy", "Quản lý"));
        repository.Setup(x => x.KhoaVaiTroAsync("QuanLy ", default)).ReturnsAsync(new VaiTro("QuanLy", "Quản lý"));
        var service = new TaiKhoanService(repository.Object, Mock.Of<IUnitOfWork>(),
            Mock.Of<IAuthorizationService>(), Mock.Of<IMatKhauHasher>());

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.CapNhatQuyenAsync("TK-1", "QuanLy ", [], default));

        repository.Verify(x => x.ThayQuyenVaiTroAsync(It.IsAny<string>(), It.IsAny<IReadOnlySet<string>>(), default),
            Times.Never);
    }

    [Fact]
    public async Task KiemTraQuyen_TuChoiMaTaiKhoanKhongChinhTac()
    {
        var repository = new Mock<ITaiKhoanRepository>(MockBehavior.Strict);
        var service = new AuthorizationService(repository.Object);

        await Assert.ThrowsAsync<UnauthorizedActionException>(
            () => service.KiemTraQuyenAsync("TK-1 ", MaQuyenPos.HT_PHAN_QUYEN, default));
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
