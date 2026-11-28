using Moq;
using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Inventory.Services;

namespace Pos.Tests.Inventory;

public sealed class StocktakeServiceTests
{
    private static readonly DateTime Moc = new(2026, 11, 28, 8, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IPhieuKiemRepository> _phieuKiem = new();
    private readonly Mock<IDanhMucRepository> _danhMuc = new();
    private readonly Mock<IBienDongKhoRepository> _bienDong = new();
    private readonly Mock<IGhiBienDongKhoNoiBo> _ghiBienDong = new();
    private readonly Mock<ITaiKhoanRepository> _taiKhoan = new();
    private readonly Mock<IAuthorizationService> _auth = new();
    private readonly Mock<IUnitOfWork> _work = new();

    public StocktakeServiceTests()
    {
        _taiKhoan.Setup(repo => repo.LayTheoMaAsync("TK-QL", default)).ReturnsAsync(
            new TaiKhoan("TK-QL", "QuanLy", "Quản lý", "quanly", "hash"));
        _taiKhoan.Setup(repo => repo.LayTheoMaAsync("TK-KHO", default)).ReturnsAsync(
            new TaiKhoan("TK-KHO", "NhanVienKho", "Nhân viên kho", "kho", "hash"));
    }

    [Fact]
    public async Task LapPhieu_ChupTonSauKhiKhoaVaChonMocSauDieuChinhCungMiliGiay()
    {
        var mocMoi = Moc.AddMilliseconds(1);
        var calls = new MockSequence();
        _work.InSequence(calls).Setup(work => work.BeginTransactionAsync(default)).Returns(Task.CompletedTask);
        _danhMuc.InSequence(calls).Setup(repo => repo.KhoaMatHangAsync("MH-A", default)).Returns(Task.CompletedTask);
        _danhMuc.InSequence(calls).Setup(repo => repo.KhoaMatHangAsync("MH-Z", default)).Returns(Task.CompletedTask);
        _bienDong.InSequence(calls).Setup(repo => repo.LayMocDieuChinhCuoiAsync("MH-A", default)).ReturnsAsync(Moc);
        _bienDong.InSequence(calls).Setup(repo => repo.LayTonTaiMocAsync("MH-A", mocMoi, default)).ReturnsAsync(26);
        _bienDong.InSequence(calls).Setup(repo => repo.LayTonTaiMocAsync("MH-Z", mocMoi, default)).ReturnsAsync(10);
        _phieuKiem.InSequence(calls).Setup(repo => repo.ThemAsync(It.IsAny<PhieuKiem>(), default)).Returns(Task.CompletedTask);
        _work.InSequence(calls).Setup(work => work.CommitAsync(default)).Returns(Task.CompletedTask);
        _danhMuc.Setup(repo => repo.LayMatHangAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string ma, CancellationToken _) => new MatHang(ma, "NH-1", ma, "cái", 0));
        _phieuKiem.Setup(repo => repo.SinhMaAsync(default)).ReturnsAsync("PK-1");

        var phieu = await TaoService().LapPhieuKiemAsync("TK-KHO", ["MH-Z", "MH-A", "MH-A"]);

        Assert.Equal(mocMoi, phieu.MocKiem);
        Assert.Equal([26, 10], phieu.DongKiems.Select(dong => dong.TonSoTaiMoc));
        Assert.All(phieu.DongKiems, dong => Assert.Null(dong.SoLuongDem));
        _work.Verify(work => work.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task GhiDem_TungPhanGiuTonTaiMocVaChuaDuDieuKienGuiDuyet()
    {
        var phieu = new PhieuKiem("PK-1", "TK-KHO", Moc);
        phieu.ThemDongKiem(new("PK-1", "MH-A", 26, null, null));
        phieu.ThemDongKiem(new("PK-1", "MH-Z", 12, null, null));
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);

        var service = TaoService();
        await service.GhiDemAsync("TK-KHO", phieu.MaPhieuKiem, [new("MH-A", 25, "  Vỡ một chai  ")]);
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.GuiDuyetAsync("TK-KHO", phieu.MaPhieuKiem));

        Assert.Equal(TrangThaiChungTu.DangKiem, phieu.TrangThai);
        Assert.Collection(phieu.DongKiems, dong =>
        {
            Assert.Equal(26, dong.TonSoTaiMoc);
            Assert.Equal(25, dong.SoLuongDem);
            Assert.Equal("Vỡ một chai", dong.GiaiTrinh);
        }, dong => Assert.Null(dong.SoLuongDem));
        _phieuKiem.Verify(repo => repo.CapNhatDemAsync(It.IsAny<DongKiem>(), default), Times.Once);
        _auth.Verify(auth => auth.KiemTraQuyenAsync("TK-KHO", MaQuyenPos.KHO_GIAI_TRINH_KIEM, default), Times.Exactly(2));
        _work.Verify(work => work.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Duyet_GiuDungChenhLechTuMocKiemVaGhiCungGiaoDich()
    {
        var phieu = PhieuChoDuyet();
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        _phieuKiem.Setup(repo => repo.LayMocKiemDangMoMoiNhatAsync("MH-A", default)).ReturnsAsync(Moc.AddMilliseconds(2));
        _ghiBienDong.Setup(ledger => ledger.GhiBienDongNoiBoAsync(It.IsAny<IReadOnlyList<YeuCauBienDongKho>>(), default))
            .ReturnsAsync(new KetQuaGhiTheKhoDto(1, 0));

        var daDuyet = await TaoService().DuyetAsync("TK-QL", phieu.MaPhieuKiem);

        Assert.Equal(TrangThaiChungTu.HoanTat, daDuyet.TrangThai);
        Assert.Equal("TK-QL", daDuyet.MaNguoiDuyet);
        Assert.Equal(Moc.AddMilliseconds(3), daDuyet.LucHoanTat);
        _ghiBienDong.Verify(ledger => ledger.GhiBienDongNoiBoAsync(It.Is<IReadOnlyList<YeuCauBienDongKho>>(ds =>
            ds.Count == 1 && ds[0] == new YeuCauBienDongKho("MH-A", LoaiBienDongKho.DieuChinhKiemKho, -3,
                Moc.AddMilliseconds(3), "PK-1")), default), Times.Once);
        _work.Verify(work => work.BeginTransactionAsync(default), Times.Once);
        _work.Verify(work => work.CommitAsync(default), Times.Once);
        _work.Verify(work => work.RollbackAsync(default), Times.Never);
    }

    [Fact]
    public async Task Duyet_TuChoiDieuChinhTuPhieuKhacTrungMiliGiayVoiMocKiem()
    {
        var phieu = PhieuChoDuyet();
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        _bienDong.Setup(repo => repo.LayMocDieuChinhCuoiAsync("MH-A", default)).ReturnsAsync(Moc);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => TaoService().DuyetAsync("TK-QL", phieu.MaPhieuKiem));

        Assert.Equal(TrangThaiChungTu.ChoDuyet, phieu.TrangThai);
        _phieuKiem.Verify(repo => repo.CapNhatAsync(It.IsAny<PhieuKiem>(), default), Times.Never);
        _ghiBienDong.VerifyNoOtherCalls();
        _work.Verify(work => work.RollbackAsync(CancellationToken.None), Times.Once);
        _work.Verify(work => work.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task Duyet_LoiThieuTonTuTheKhoHoanTacCapNhatPhieu()
    {
        var phieu = PhieuChoDuyet();
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        var loi = new InsufficientStockException("MH-A", 2, 3);
        _ghiBienDong.Setup(ledger => ledger.GhiBienDongNoiBoAsync(It.IsAny<IReadOnlyList<YeuCauBienDongKho>>(), default))
            .ThrowsAsync(loi);

        Assert.Same(loi, await Assert.ThrowsAsync<InsufficientStockException>(() =>
            TaoService().DuyetAsync("TK-QL", phieu.MaPhieuKiem)));

        _work.Verify(work => work.RollbackAsync(CancellationToken.None), Times.Once);
        _work.Verify(work => work.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task HuyChoDuyet_ChiQuanLyCoQuyen()
    {
        var phieu = PhieuChoDuyet();
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        var service = TaoService();

        await Assert.ThrowsAsync<UnauthorizedActionException>(() => service.HuyAsync("TK-KHO", phieu.MaPhieuKiem));
        Assert.Equal(TrangThaiChungTu.ChoDuyet, phieu.TrangThai);
        Assert.Equal(TrangThaiChungTu.DaHuy, (await service.HuyAsync("TK-QL", phieu.MaPhieuKiem)).TrangThai);
        Assert.Equal(10, Assert.Single(phieu.DongKiems).TonSoTaiMoc);
    }

    private StocktakeService TaoService() => new(_phieuKiem.Object, _danhMuc.Object, _bienDong.Object,
        _ghiBienDong.Object, _taiKhoan.Object, _auth.Object, _work.Object, new DongHoCoDinh(Moc));

    private static PhieuKiem PhieuChoDuyet() => PhieuKiem.KhoiPhuc("PK-1", "TK-KHO", Moc,
        TrangThaiChungTu.ChoDuyet, null, null, [new("PK-1", "MH-A", 10, 7, "Hỏng ba hộp")]);

    private sealed class DongHoCoDinh(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utc);
    }
}
