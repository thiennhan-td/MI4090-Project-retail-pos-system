using Moq;
using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using Pos.Infrastructure.Repositories.Inventory;
using Pos.Inventory.Services;

namespace Pos.Tests.Inventory;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task LayMatHang_TuChoiMaQuaDaiTruocKhiGuiThamSoSql()
    {
        var factory = new SqlConnectionFactory("Server=localhost;Database=RetailCorePosDemo;Integrated Security=True");
        await using var work = new SqlUnitOfWork(factory);
        var repository = new SqlDanhMucRepository(work, factory);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => repository.LayMatHangAsync(new string('A', 33), default));
    }

    [Fact]
    public async Task TraDanhMucBan_ChiHienHangVaNhomDangHoatDong()
    {
        var sua = new MatHang("MH-1", "NH-1", "Sữa", "Hộp", 20_000);
        var banh = new MatHang("MH-2", "NH-2", "Bánh", "Hộp", 30_000);
        var hetBan = new MatHang("MH-3", "NH-1", "Đường", "Gói", 40_000);
        hetBan.NgungSuDung();
        var thucPham = new NhomHang("NH-1", "Thực phẩm");
        var nhomNgungDung = new NhomHang("NH-2", "Bánh");
        nhomNgungDung.NgungSuDung();
        var repository = new Mock<IDanhMucRepository>();
        repository.Setup(x => x.LayNhomHangAsync(default)).ReturnsAsync([thucPham, nhomNgungDung]);
        repository.Setup(x => x.TimMatHangAsync(null, null, default)).ReturnsAsync([sua, banh, hetBan]);
        repository.Setup(x => x.LayMatHangAsync("MH-2", default)).ReturnsAsync(banh);
        var service = new CatalogQueryService(repository.Object);

        var ketQua = await service.TimKiemMatHangBanAsync(null, null, default);

        Assert.Equal("MH-1", Assert.Single(ketQua).MaHang);
        Assert.Null(await service.LayMatHangDangHoatDongAsync("MH-2", default));
    }

    [Fact]
    public async Task NgungNhom_ChuaDuocPhepKhiConHangDangHoatDong()
    {
        var repository = new Mock<IQuanLyDanhMucRepository>();
        repository.Setup(x => x.KhoaNhomHangAsync("NH-1", default)).ReturnsAsync(new NhomHang("NH-1", "Thực phẩm"));
        repository.Setup(x => x.CoMatHangDangHoatDongAsync("NH-1", default)).ReturnsAsync(true);
        var work = new Mock<IUnitOfWork>();
        var authorization = new Mock<IAuthorizationService>();
        var service = new CatalogService(repository.Object, work.Object, authorization.Object);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => service.NgungNhomAsync("TK-1", "NH-1", default));

        authorization.Verify(x => x.KiemTraQuyenAsync("TK-1", MaQuyenPos.DM_NHOM_QL, default), Times.Once);
        repository.Verify(x => x.CapNhatNhomAsync(It.IsAny<NhomHang>(), default), Times.Never);
        work.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData("  Hộp  ", true, "Hộp", 0)]
    [InlineData("Thùng", false, "Thùng", 1)]
    public async Task LuuHang_LuuTenVaGiaKhiDonViTinhHopLe(string donViTinh, bool donViDaDuocSuDung,
        string donViSauLuu, int soLanKiemTra)
    {
        var repository = TaoRepositoryMatHang(donViDaDuocSuDung);
        var work = new Mock<IUnitOfWork>();
        var service = new CatalogService(repository.Object, work.Object, Mock.Of<IAuthorizationService>());

        var ketQua = await service.LuuHangAsync("TK-1", new("MH-1", "NH-1", "Sữa mới", donViTinh, 25_000), default);

        Assert.Equal(donViSauLuu, ketQua.DonViTinh);
        Assert.Equal("Sữa mới", ketQua.TenHang);
        Assert.Equal(25_000, ketQua.GiaBanHienHanh);
        repository.Verify(x => x.DonViDaDuocSuDungAsync("MH-1", default), Times.Exactly(soLanKiemTra));
        repository.Verify(x => x.CapNhatHangAsync(ketQua, default), Times.Once);
        work.Verify(x => x.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task LuuHang_TuChoiDoiDonViTinhDaDuocSuDung()
    {
        var repository = TaoRepositoryMatHang(donViDaDuocSuDung: true);
        var work = new Mock<IUnitOfWork>();
        var service = new CatalogService(repository.Object, work.Object, Mock.Of<IAuthorizationService>());

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.LuuHangAsync("TK-1", new("MH-1", "NH-1", "Sữa mới", "Thùng", 25_000), default));

        repository.Verify(x => x.KhoaMatHangAsync("MH-1", default), Times.Once);
        repository.Verify(x => x.DonViDaDuocSuDungAsync("MH-1", default), Times.Once);
        repository.Verify(x => x.CapNhatHangAsync(It.IsAny<MatHang>(), default), Times.Never);
        work.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
    }

    private static Mock<IQuanLyDanhMucRepository> TaoRepositoryMatHang(bool donViDaDuocSuDung)
    {
        var repository = new Mock<IQuanLyDanhMucRepository>();
        repository.Setup(x => x.KhoaNhomHangAsync("NH-1", default)).ReturnsAsync(new NhomHang("NH-1", "Thực phẩm"));
        repository.Setup(x => x.LayMatHangAsync("MH-1", default))
            .ReturnsAsync(new MatHang("MH-1", "NH-1", "Sữa", "Hộp", 20_000));
        repository.Setup(x => x.DonViDaDuocSuDungAsync("MH-1", default)).ReturnsAsync(donViDaDuocSuDung);
        return repository;
    }
}
