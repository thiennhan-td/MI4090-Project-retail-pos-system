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
    private readonly Mock<IQuanTriTaiKhoanRepository> _taiKhoan = new();
    private readonly Mock<IAuthorizationService> _auth = new();
    private readonly Mock<IUnitOfWork> _work = new();

    public StocktakeServiceTests()
    {
        _taiKhoan.Setup(repo => repo.LayHoSoDangNhapAsync("TK-QL", default)).ReturnsAsync(
            new HoSoDangNhap(new TaiKhoan("TK-QL", "QuanLy", "Quản lý", "quanly", "hash"),
                new HashSet<string> { MaQuyenPos.KHO_DUYET_DIEU_CHINH, MaQuyenPos.KHO_HUY_KIEM }));
        _taiKhoan.Setup(repo => repo.LayHoSoDangNhapAsync("TK-KHO", default)).ReturnsAsync(
            new HoSoDangNhap(new TaiKhoan("TK-KHO", "NhanVienKho", "Nhân viên kho", "kho", "hash"),
                new HashSet<string> { MaQuyenPos.KHO_LAP_KIEM, MaQuyenPos.KHO_GIAI_TRINH_KIEM, MaQuyenPos.KHO_HUY_KIEM }));
    }

    [Fact]
    public async Task LapPhieu_ChupCaPhieuNhapDaCommitMangMocSauDongHoMayChu()
    {
        var nhap = BienDongKho.Tao("BD-1", "MH-A", LoaiBienDongKho.NhapKho, 6, Moc.AddHours(1), maDongNhap: "DN-1");
        _danhMuc.Setup(repo => repo.LayMatHangAsync(nhap.MaHang, default))
            .ReturnsAsync(new MatHang(nhap.MaHang, "NH-1", "Mặt hàng A", "hộp", 0));
        _phieuKiem.Setup(repo => repo.SinhMaAsync(default)).ReturnsAsync("PK-1");
        _bienDong.Setup(repo => repo.LayMocGhiAsync(nhap.MaHang, default))
            .ReturnsAsync(new MocGhiBienDongKho(nhap.ThoiDiemGhi, null, null));
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync(nhap.MaHang, It.IsAny<DateTime>(), default))
            .ReturnsAsync((string _, DateTime mocKiem, CancellationToken _) =>
                10 + (mocKiem >= nhap.ThoiDiemGhi ? nhap.SoLuongThayDoi : 0));

        var phieu = await TaoService().LapPhieuKiemAsync("TK-KHO", [nhap.MaHang]);

        Assert.Equal(nhap.ThoiDiemGhi, phieu.MocKiem);
        Assert.Equal(16, Assert.Single(phieu.DongKiems).TonSoTaiMoc);
    }

    [Fact]
    public async Task LapPhieu_TuChoiKhiHoSoVaiTroVuaTaiThieuQuyenDuKiemTraBanDauThanhCong()
    {
        _auth.Setup(auth => auth.KiemTraQuyenAsync("TK-KHO", MaQuyenPos.KHO_LAP_KIEM, default))
            .Returns(Task.CompletedTask);
        _taiKhoan.Setup(repo => repo.LayHoSoDangNhapAsync("TK-KHO", default)).ReturnsAsync(
            new HoSoDangNhap(new TaiKhoan("TK-KHO", "NhanVienKho", "Nhân viên kho", "kho", "hash"),
                new HashSet<string> { MaQuyenPos.KHO_GIAI_TRINH_KIEM }));

        var loi = await Assert.ThrowsAsync<UnauthorizedActionException>(() => TaoService().LapPhieuKiemAsync("TK-KHO", ["MH-A"]));

        Assert.Equal(MaQuyenPos.KHO_LAP_KIEM, loi.MaQuyen);
        _auth.Verify(auth => auth.KiemTraQuyenAsync("TK-KHO", MaQuyenPos.KHO_LAP_KIEM, default), Times.Once);
        _work.Verify(work => work.BeginTransactionAsync(default), Times.Never);
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
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(26);

        var lucDem = Moc.AddMilliseconds(1);
        var service = TaoService(lucDem.AddTicks(9_000));
        await service.GhiDemAsync("TK-KHO", phieu.MaPhieuKiem, phieu.PhienBan, [new("MH-A", 25, "  Vỡ một chai  ")]);
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.GuiDuyetAsync("TK-KHO", phieu.MaPhieuKiem, phieu.PhienBan));

        Assert.Equal(TrangThaiChungTu.DangKiem, phieu.TrangThai);
        Assert.Collection(phieu.DongKiems, dong =>
        {
            Assert.Equal(26, dong.TonSoTaiMoc);
            Assert.Equal(25, dong.SoLuongDem);
            Assert.Equal("Vỡ một chai", dong.GiaiTrinh);
            Assert.Equal(lucDem, dong.LucDem);
            Assert.Equal(DateTimeKind.Utc, dong.LucDem.GetValueOrDefault().Kind);
        }, dong =>
        {
            Assert.Null(dong.SoLuongDem);
            Assert.Null(dong.LucDem);
        });
        _phieuKiem.Verify(repo => repo.CapNhatDemAsync(It.IsAny<DongKiem>(), default), Times.Once);
        _auth.Verify(auth => auth.KiemTraQuyenAsync("TK-KHO", MaQuyenPos.KHO_GIAI_TRINH_KIEM, default), Times.Exactly(2));
        _work.Verify(work => work.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task GhiDem_TuChoiBanSauKhiChupTonCaKhiThoiDiemBanTrungMocHoacNamTrongTuongLai(int lechMiliGiay)
    {
        var phieu = new PhieuKiem("PK-1", "TK-KHO", Moc);
        phieu.ThemDongKiem(new("PK-1", "MH-A", 10, null, null));
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        var daKhoaMatHang = false;
        _danhMuc.Setup(repo => repo.KhoaMatHangAsync("MH-A", default))
            .Callback(() => daKhoaMatHang = true).Returns(Task.CompletedTask);
        _bienDong.Setup(repo => repo.LayMocGhiAsync("MH-A", default)).ReturnsAsync(() =>
        {
            Assert.True(daKhoaMatHang);
            return new MocGhiBienDongKho(Moc.AddMilliseconds(lechMiliGiay), null, Moc);
        });
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(lechMiliGiay == 0 ? 8 : 10);

        var loi = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => TaoService(Moc.AddMilliseconds(2))
            .GhiDemAsync("TK-KHO", phieu.MaPhieuKiem, phieu.PhienBan, [new("MH-A", 8)]));

        Assert.Contains("lập phiếu mới", loi.Message);
        var dong = Assert.Single(phieu.DongKiems);
        Assert.Equal(10, dong.TonSoTaiMoc);
        Assert.Null(dong.SoLuongDem);
        Assert.Null(dong.LucDem);
        _phieuKiem.Verify(repo => repo.CapNhatDemAsync(It.IsAny<DongKiem>(), default), Times.Never);
        _work.Verify(work => work.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GhiDem_DemLaiCungSoLuongNeuDongCuChuaCoMoc()
    {
        var phieu = new PhieuKiem("PK-1", "TK-KHO", Moc);
        phieu.ThemDongKiem(new("PK-1", "MH-A", 10, 8, "Hỏng hai hộp"));
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(10);
        var lucDem = Moc.AddMilliseconds(1);

        await TaoService(lucDem).GhiDemAsync("TK-KHO", phieu.MaPhieuKiem, phieu.PhienBan, [new("MH-A", 8)]);

        var dong = Assert.Single(phieu.DongKiems);
        Assert.Equal(lucDem, dong.LucDem);
        Assert.Equal("Hỏng hai hộp", dong.GiaiTrinh);
    }

    [Fact]
    public async Task GhiDem_TuChoiDoiSoDemKhiDongHoLuiTruocMocDemDaLuu()
    {
        var lucDem = Moc.AddMilliseconds(2);
        var phieu = new PhieuKiem("PK-1", "TK-KHO", Moc);
        phieu.ThemDongKiem(new("PK-1", "MH-A", 10, 8, "Hỏng hai hộp", lucDem));
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(10);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => TaoService(Moc.AddMilliseconds(1))
            .GhiDemAsync("TK-KHO", phieu.MaPhieuKiem, phieu.PhienBan, [new("MH-A", 7)]));

        Assert.Equal(lucDem, Assert.Single(phieu.DongKiems).LucDem);
        _phieuKiem.Verify(repo => repo.CapNhatDemAsync(It.IsAny<DongKiem>(), default), Times.Never);
    }

    [Fact]
    public async Task GhiDemVaGuiDuyet_TuChoiPhienBanDaCuSauKhiGhiDem()
    {
        var phieu = new PhieuKiem("PK-1", "TK-KHO", Moc);
        phieu.ThemDongKiem(new("PK-1", "MH-A", 10, null, null));
        var phienBanCu = phieu.PhienBan;
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(10);
        var service = TaoService();

        await service.GhiDemAsync("TK-KHO", phieu.MaPhieuKiem, phienBanCu, [new("MH-A", 10)]);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            service.GhiDemAsync("TK-KHO", phieu.MaPhieuKiem, phienBanCu, [new("MH-A", 9, "Mất một hộp")]));
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            service.GuiDuyetAsync("TK-KHO", phieu.MaPhieuKiem, phienBanCu));

        Assert.NotEqual(phienBanCu, phieu.PhienBan);
        Assert.Equal(10, Assert.Single(phieu.DongKiems).SoLuongDem);
        Assert.Equal(TrangThaiChungTu.DangKiem, phieu.TrangThai);
        _phieuKiem.Verify(repo => repo.CapNhatDemAsync(It.IsAny<DongKiem>(), default), Times.Once);
        _phieuKiem.Verify(repo => repo.CapNhatAsync(It.IsAny<PhieuKiem>(), default), Times.Never);
        _work.Verify(work => work.RollbackAsync(CancellationToken.None), Times.Exactly(2));
    }

    [Theory]
    [InlineData(TrangThaiChungTu.DangKiem)]
    [InlineData(TrangThaiChungTu.ChoDuyet)]
    public async Task GuiHoacDuyet_TuChoiPhieuCuCoSoDemNhungThieuMoc(string trangThai)
    {
        var phieu = PhieuKiem.KhoiPhuc("PK-1", "TK-KHO", Moc, trangThai, null, null,
            [new("PK-1", "MH-A", 10, 8, "Hỏng hai hộp")]);
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        var service = TaoService();

        var loi = await Assert.ThrowsAsync<BusinessRuleViolationException>(() => trangThai == TrangThaiChungTu.DangKiem
            ? service.GuiDuyetAsync("TK-KHO", phieu.MaPhieuKiem, phieu.PhienBan)
            : service.DuyetAsync("TK-QL", phieu.MaPhieuKiem));

        Assert.Contains("thiếu thời điểm đếm", loi.Message);
        Assert.Equal(trangThai, phieu.TrangThai);
        Assert.Null(Assert.Single(phieu.DongKiems).LucDem);
        _phieuKiem.Verify(repo => repo.CapNhatAsync(It.IsAny<PhieuKiem>(), default), Times.Never);
        _work.Verify(work => work.RollbackAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Duyet_GiuDungChenhLechTuMocKiemVaGhiCungGiaoDich()
    {
        var phieu = PhieuChoDuyet();
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(10);
        var lucDemPhieuKhac = Moc.AddMilliseconds(2);
        _bienDong.Setup(repo => repo.LayMocGhiAsync("MH-A", default))
            .ReturnsAsync(new MocGhiBienDongKho(Moc, lucDemPhieuKhac, Moc.AddMilliseconds(1)));
        _ghiBienDong.Setup(ledger => ledger.GhiBienDongNoiBoAsync(It.IsAny<IReadOnlyList<YeuCauBienDongKho>>(), default))
            .ReturnsAsync(new KetQuaGhiTheKhoDto(1, 0));

        var daDuyet = await TaoService(lucDemPhieuKhac).DuyetAsync("TK-QL", phieu.MaPhieuKiem);

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
    public async Task Duyet_ChiDieuChinhChenhLechSauKhiBanDuocGhiSauLuotDem()
    {
        var phieu = new PhieuKiem("PK-1", "TK-KHO", Moc);
        phieu.ThemDongKiem(new("PK-1", "MH-A", 10, null, null));
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        _danhMuc.Setup(repo => repo.LayMatHangAsync("MH-A", default))
            .ReturnsAsync(new MatHang("MH-A", "NH-1", "Mặt hàng A", "hộp", 0));
        _bienDong.SetupGet(repo => repo.CoGiaoDich).Returns(true);
        var soThe = 0;
        _bienDong.Setup(repo => repo.SinhMaAsync(default)).ReturnsAsync(() => $"BD-{++soThe}");
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(10);
        var daGhi = new List<BienDongKho>();
        _bienDong.Setup(repo => repo.LayMocGhiAsync("MH-A", default)).ReturnsAsync(() =>
            new MocGhiBienDongKho(daGhi.LastOrDefault()?.ThoiDiemGhi, phieu.DongKiems.Single().LucDem, phieu.MocKiem));
        _bienDong.Setup(repo => repo.LayTonHienHanhAsync("MH-A", default))
            .ReturnsAsync(() => 10 + daGhi.Sum(dong => dong.SoLuongThayDoi));
        _bienDong.Setup(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default))
            .Callback<BienDongKho, CancellationToken>((dong, _) => daGhi.Add(dong)).Returns(Task.CompletedTask);

        var lucDem = Moc.AddMilliseconds(1);
        var lucBan = Moc.AddMilliseconds(2);
        var nguonBan = new NguonBienDongKho("DB-1", "MH-A", "HD-1", -3, true, lucBan);
        var ledger = new InventoryLedgerService(_bienDong.Object, _danhMuc.Object, _auth.Object, _work.Object);
        _bienDong.Setup(repo => repo.LayCacNguonAsync(LoaiBienDongKho.XuatBan, "HD-1", default)).ReturnsAsync([nguonBan]);
        _bienDong.Setup(repo => repo.LayNguonAsync(It.IsAny<BienDongKho>(), default))
            .ReturnsAsync(() => new NguonBienDongKho("PK-1", "MH-A", "PK-1", -2,
                phieu.TrangThai == TrangThaiChungTu.HoanTat, phieu.LucHoanTat));
        _bienDong.Setup(repo => repo.LayCacNguonAsync(LoaiBienDongKho.DieuChinhKiemKho, "PK-1", default))
            .ReturnsAsync(() => [new NguonBienDongKho("PK-1", "MH-A", "PK-1", -2,
                phieu.TrangThai == TrangThaiChungTu.HoanTat, phieu.LucHoanTat)]);

        await TaoService(lucDem, ledger).GhiDemAsync("TK-KHO", "PK-1", phieu.PhienBan, [new("MH-A", 8)]);
        Assert.Equal(new KetQuaGhiTheKhoDto(1, 0),
            await ledger.GhiNhanXuatBanAsync(new("HD-1", lucBan, [new("DB-1", "MH-A", 3)]), default));
        var service = TaoService(lucBan, ledger);
        await service.GhiDemAsync("TK-KHO", "PK-1", phieu.PhienBan, [new("MH-A", 8, "Hỏng hai hộp")]);
        await service.GuiDuyetAsync("TK-KHO", "PK-1", phieu.PhienBan);
        var daDuyet = await service.DuyetAsync("TK-QL", "PK-1");

        Assert.Equal(TrangThaiChungTu.HoanTat, daDuyet.TrangThai);
        var dong = Assert.Single(daDuyet.DongKiems);
        Assert.Equal(lucDem, dong.LucDem);
        Assert.Equal("Hỏng hai hộp", dong.GiaiTrinh);
        Assert.Equal([-3, -2], daGhi.Select(dong => dong.SoLuongThayDoi));
        Assert.Equal(lucBan, daGhi[^1].ThoiDiemGhi);
        Assert.Equal(5, await _bienDong.Object.LayTonHienHanhAsync("MH-A", default));
    }

    [Fact]
    public async Task Duyet_TuChoiKhiThoiDiemTheKhoMoiNhatSauDongHoMayChu()
    {
        var phieu = PhieuChoDuyet();
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(10);
        _bienDong.Setup(repo => repo.LayMocGhiAsync("MH-A", default))
            .ReturnsAsync(new MocGhiBienDongKho(Moc.AddDays(1), Moc, Moc));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => TaoService().DuyetAsync("TK-QL", phieu.MaPhieuKiem));

        Assert.Equal(TrangThaiChungTu.ChoDuyet, phieu.TrangThai);
        _phieuKiem.Verify(repo => repo.CapNhatAsync(It.IsAny<PhieuKiem>(), default), Times.Never);
        _ghiBienDong.VerifyNoOtherCalls();
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
    public async Task Duyet_TuChoiKhiBanGhiSauLucChupTonNhungCoThoiDiemTruocMocKiem()
    {
        var phieu = PhieuChoDuyet();
        var dong = Assert.Single(phieu.DongKiems);
        var banGhiMuon = BienDongKho.Tao("BD-1", dong.MaHang, LoaiBienDongKho.XuatBan, -2,
            Moc.AddMilliseconds(-1), maDongBan: "DB-1");
        _phieuKiem.Setup(repo => repo.KhoaTheoMaAsync(phieu.MaPhieuKiem, default)).ReturnsAsync(phieu);
        var daKhoaMatHang = false;
        _danhMuc.Setup(repo => repo.KhoaMatHangAsync(dong.MaHang, default))
            .Callback(() => daKhoaMatHang = true).Returns(Task.CompletedTask);
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync(dong.MaHang, Moc, default)).ReturnsAsync(() =>
        {
            Assert.True(daKhoaMatHang);
            return dong.TonSoTaiMoc + banGhiMuon.SoLuongThayDoi;
        });

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => TaoService().DuyetAsync("TK-QL", phieu.MaPhieuKiem));

        Assert.Equal(TrangThaiChungTu.ChoDuyet, phieu.TrangThai);
        Assert.Equal(10, dong.TonSoTaiMoc);
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
        _bienDong.Setup(repo => repo.LayTonTaiMocAsync("MH-A", Moc, default)).ReturnsAsync(10);
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

    private StocktakeService TaoService(DateTime? utc = null, IGhiBienDongKhoNoiBo? ledger = null)
        => new(_phieuKiem.Object, _danhMuc.Object, _bienDong.Object, ledger ?? _ghiBienDong.Object,
            _taiKhoan.Object, _auth.Object, _work.Object, new DongHoCoDinh(utc ?? Moc));

    private static PhieuKiem PhieuChoDuyet() => PhieuKiem.KhoiPhuc("PK-1", "TK-KHO", Moc,
        TrangThaiChungTu.ChoDuyet, null, null, [new("PK-1", "MH-A", 10, 7, "Hỏng ba hộp", Moc)]);

    private sealed class DongHoCoDinh(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utc);
    }
}
