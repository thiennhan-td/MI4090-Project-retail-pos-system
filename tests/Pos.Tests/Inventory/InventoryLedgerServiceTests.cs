using Moq;
using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Inventory.Decorators;
using Pos.Inventory.Services;

namespace Pos.Tests.Inventory;

public sealed class InventoryLedgerServiceTests
{
    private static readonly DateTime Luc = new(2026, 11, 28, 8, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IBienDongKhoRepository> _bienDong = new();
    private readonly Mock<IDanhMucRepository> _danhMuc = new();
    private readonly Mock<IAuthorizationService> _auth = new();
    private readonly Mock<IUnitOfWork> _work = new(MockBehavior.Strict);

    public InventoryLedgerServiceTests()
    {
        var so = 0;
        _bienDong.SetupGet(repo => repo.CoGiaoDich).Returns(true);
        _bienDong.Setup(repo => repo.SinhMaAsync(default)).ReturnsAsync(() => $"BD-{++so}");
        _danhMuc.Setup(repo => repo.LayMatHangAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string ma, CancellationToken _) => new MatHang(ma, "NH-1", ma, "cái", 0));
    }

    [Fact]
    public async Task XuatBan_KhoaHangTheoMaVaKiemTongCacDongTruocKhiGhi()
    {
        var locks = new List<string>();
        _danhMuc.Setup(repo => repo.KhoaMatHangAsync(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((ma, _) => locks.Add(ma)).Returns(Task.CompletedTask);
        var yeuCau = new YeuCauXuatBanDto("HD-1", Luc, [new("DB-3", "MH-Z", 1), new("DB-1", "MH-A", 4), new("DB-2", "MH-A", 3)]);
        CaiNguonBan(yeuCau);
        _bienDong.Setup(repo => repo.LayTonHienHanhAsync("MH-A", default)).ReturnsAsync(6);

        var loi = await Assert.ThrowsAsync<InsufficientStockException>(() => TaoService().GhiNhanXuatBanAsync(yeuCau, default));

        Assert.Equal("MH-A", loi.MaHang);
        Assert.Equal(7, loi.SoLuongYeuCau);
        Assert.Equal(["MH-A", "MH-Z"], locks);
        _bienDong.Verify(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default), Times.Never);
    }

    [Fact]
    public async Task XuatBan_GuiLaiDuNoiDungVanThanhCongKhiChungTuDaHuy()
    {
        var yeuCau = new YeuCauXuatBanDto("HD-1", Luc.AddTicks(100), [new("DB-1", "MH-A", 2)]);
        CaiNguonBan(yeuCau, coHieuLuc: false);
        _bienDong.Setup(repo => repo.LayTheoKhoaSuKienAsync("XuatBan:DB-1", default))
            .ReturnsAsync(BienDongKho.Tao("BD-100", "MH-A", LoaiBienDongKho.XuatBan, -2, Luc, maDongBan: "DB-1"));

        Assert.Equal(new KetQuaGhiTheKhoDto(0, 1), await TaoService().GhiNhanXuatBanAsync(yeuCau, default));
        _danhMuc.Verify(repo => repo.LayMatHangAsync(It.IsAny<string>(), default), Times.Never);
        _bienDong.Verify(repo => repo.LayTonHienHanhAsync(It.IsAny<string>(), default), Times.Never);
        _bienDong.Verify(repo => repo.LayMocGhiAsync(It.IsAny<string>(), default), Times.Never);
        _bienDong.Verify(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default), Times.Never);
    }

    [Fact]
    public async Task XuatBan_TuChoiGhiLuiTruocLanNhapDaCommit()
    {
        var yeuCau = new YeuCauXuatBanDto("HD-1", Luc, [new("DB-1", "MH-A", 2)]);
        CaiNguonBan(yeuCau);
        var nhapSau = BienDongKho.Tao("BD-1", "MH-A", LoaiBienDongKho.NhapKho, 10,
            Luc.AddMilliseconds(1), maDongNhap: "DN-1");
        var daKhoaHang = false;
        _danhMuc.Setup(repo => repo.KhoaMatHangAsync("MH-A", default))
            .Callback(() => daKhoaHang = true).Returns(Task.CompletedTask);
        _bienDong.Setup(repo => repo.LayMocGhiAsync("MH-A", default)).ReturnsAsync(() =>
        {
            Assert.True(daKhoaHang);
            return new MocGhiBienDongKho(nhapSau.ThoiDiemGhi, null, null);
        });
        _bienDong.Setup(repo => repo.LayTonHienHanhAsync("MH-A", default)).ReturnsAsync(nhapSau.SoLuongThayDoi);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => TaoService().GhiNhanXuatBanAsync(yeuCau, default));

        _bienDong.Verify(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default), Times.Never);
    }

    [Fact]
    public async Task XuatBan_TuChoiCungMiliGiayVoiMocChupTonCuaPhieuChuaDem()
    {
        var yeuCau = new YeuCauXuatBanDto("HD-1", Luc.AddTicks(9_000), [new("DB-1", "MH-A", 2)]);
        CaiNguonBan(yeuCau);
        _bienDong.Setup(repo => repo.LayMocGhiAsync("MH-A", default))
            .ReturnsAsync(new MocGhiBienDongKho(Luc, null, Luc));
        _bienDong.Setup(repo => repo.LayTonHienHanhAsync("MH-A", default)).ReturnsAsync(9);

        var loi = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => TaoService().GhiNhanXuatBanAsync(yeuCau, default));

        Assert.Contains("mốc chụp tồn", loi.Message);
        _bienDong.Verify(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default), Times.Never);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task XuatBan_TuChoiThoiDiemTruocHoacTrungMocDemDuocBaoVe(int lechMiliGiay, bool biTuChoi)
    {
        var lucBan = Luc.AddMilliseconds(lechMiliGiay);
        var yeuCau = new YeuCauXuatBanDto("HD-1", lucBan.AddTicks(9_000), [new("DB-1", "MH-A", 2)]);
        CaiNguonBan(yeuCau, lucNguonUtc: lucBan);
        _bienDong.Setup(repo => repo.LayMocGhiAsync("MH-A", default))
            .ReturnsAsync(new MocGhiBienDongKho(Luc.AddMilliseconds(-2), Luc, null));
        _bienDong.Setup(repo => repo.LayTonHienHanhAsync("MH-A", default)).ReturnsAsync(5);

        if (biTuChoi)
        {
            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => TaoService().GhiNhanXuatBanAsync(yeuCau, default));
        }
        else
        {
            Assert.Equal(new KetQuaGhiTheKhoDto(1, 0), await TaoService().GhiNhanXuatBanAsync(yeuCau, default));
        }

        _bienDong.Verify(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default), biTuChoi ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public async Task XuatBan_TuChoiGuiLaiThieuDongHonHopHoacDoiNoiDung(int soDong, bool doiSoLuong)
    {
        var goc = new YeuCauXuatBanDto("HD-1", Luc, [new("DB-1", "MH-A", 2), new("DB-2", "MH-A", 3)]);
        CaiNguonBan(goc);
        _bienDong.Setup(repo => repo.LayTheoKhoaSuKienAsync("XuatBan:DB-1", default))
            .ReturnsAsync(BienDongKho.Tao("BD-100", "MH-A", LoaiBienDongKho.XuatBan, -2, Luc, maDongBan: "DB-1"));
        var dongHang = goc.DongHang.Take(soDong).ToArray();
        if (soDong == 2 && doiSoLuong)
        {
            dongHang[0] = dongHang[0] with { SoLuongBan = 1 };
        }
        var thuLai = goc with { DongHang = dongHang };

        await Assert.ThrowsAsync<DuplicateRequestException>(() => TaoService().GhiNhanXuatBanAsync(thuLai, default));
        _bienDong.Verify(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default), Times.Never);
    }

    [Fact]
    public async Task XuatBan_MoiChiGhiKhiNguonDaDuocXacNhan()
    {
        var yeuCau = new YeuCauXuatBanDto("HD-1", Luc, [new("DB-1", "MH-A", 2)]);
        CaiNguonBan(yeuCau, coHieuLuc: false);

        await Assert.ThrowsAsync<InvalidStateTransitionException>(() => TaoService().GhiNhanXuatBanAsync(yeuCau, default));
        _bienDong.Verify(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default), Times.Never);
    }

    [Fact]
    public async Task XuatBan_GiuCungKyVoiHoaDonKhiThoiDiemGanNuaDemDiaPhuong()
    {
        var dauNgayMoiUtc = new DateTime(2026, 11, 28, 17, 0, 0, DateTimeKind.Utc);
        var yeuCau = new YeuCauXuatBanDto("HD-1", dauNgayMoiUtc.AddTicks(-4_000), [new("DB-1", "MH-A", 2)]);
        var lucNguonUtc = dauNgayMoiUtc.AddMilliseconds(-1);
        CaiNguonBan(yeuCau, lucNguonUtc: lucNguonUtc);
        _bienDong.Setup(repo => repo.LayMocGhiAsync("MH-A", default))
            .ReturnsAsync(new MocGhiBienDongKho(lucNguonUtc, null, null));
        _bienDong.Setup(repo => repo.LayTonHienHanhAsync("MH-A", default)).ReturnsAsync(2);

        Assert.Equal(new KetQuaGhiTheKhoDto(1, 0), await TaoService().GhiNhanXuatBanAsync(yeuCau, default));

        _bienDong.Verify(repo => repo.ThemAsync(It.Is<BienDongKho>(dong => dong.ThoiDiemGhi == lucNguonUtc), default), Times.Once);
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => TaoService().GhiNhanXuatBanAsync(
            yeuCau with { LucXacNhanUtc = dauNgayMoiUtc }, default));
    }

    [Fact]
    public async Task XuatBan_TuChoiMatHangDaNgungSuDungKhiXacNhan()
    {
        var yeuCau = new YeuCauXuatBanDto("HD-1", Luc, [new("DB-1", "MH-A", 2)]);
        CaiNguonBan(yeuCau);
        var matHang = new MatHang("MH-A", "NH-1", "Mặt hàng A", "cái", 0);
        matHang.NgungSuDung();
        _danhMuc.Setup(repo => repo.LayMatHangAsync(matHang.MaHang, default)).ReturnsAsync(matHang);
        _bienDong.Setup(repo => repo.LayTonHienHanhAsync(matHang.MaHang, default)).ReturnsAsync(5);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => TaoService().GhiNhanXuatBanAsync(yeuCau, default));

        _bienDong.Verify(repo => repo.ThemAsync(It.IsAny<BienDongKho>(), default), Times.Never);
    }

    [Fact]
    public async Task NhapKhoNoiBo_DungNguonVaSoLuongThiGhiTrongGiaoDichCuaChungTu()
    {
        var nguon = new NguonBienDongKho("DN-1", "MH-A", "PN-1", 5, true, Luc);
        _bienDong.Setup(repo => repo.LayNguonAsync(It.IsAny<BienDongKho>(), default)).ReturnsAsync(nguon);
        _bienDong.Setup(repo => repo.LayCacNguonAsync(LoaiBienDongKho.NhapKho, "PN-1", default)).ReturnsAsync([nguon]);
        var yeuCau = new YeuCauBienDongKho("MH-A", LoaiBienDongKho.NhapKho, 5, Luc, "DN-1");

        Assert.Equal(new KetQuaGhiTheKhoDto(1, 0), await TaoService().GhiBienDongNoiBoAsync([yeuCau], default));
        _bienDong.Verify(repo => repo.ThemAsync(It.Is<BienDongKho>(dong => dong.KhoaSuKien == "NhapKho:DN-1"
            && dong.MaDongNhap == "DN-1" && dong.SoLuongThayDoi == 5), default), Times.Once);
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => TaoService().GhiBienDongNoiBoAsync(
            [yeuCau with { SoLuongThayDoi = 6 }], default));
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TheKho_TraDungSoTonTheoKyVaTonHienHanhTrongMotGiaoDich()
    {
        var tu = Luc.AddTicks(1_234);
        var den = Luc.AddMilliseconds(1).AddTicks(5_678);
        var hang = new MatHang("MH-A", "NH-1", "Mặt hàng A", "hộp", 0);
        var dong = BienDongKho.Tao("BD-1", hang.MaHang, LoaiBienDongKho.XuatBan, -3, Luc.AddMilliseconds(1), maDongBan: "DB-1");
        _bienDong.SetupGet(repo => repo.CoGiaoDich).Returns(false);
        var thuTu = new MockSequence();
        _work.InSequence(thuTu).Setup(work => work.BeginTransactionAsync(default)).Returns(Task.CompletedTask);
        _danhMuc.InSequence(thuTu).Setup(repo => repo.KhoaMatHangAsync(hang.MaHang, default)).Returns(Task.CompletedTask);
        _bienDong.InSequence(thuTu).Setup(repo => repo.LayTonTruocMocAsync(hang.MaHang, tu, default)).ReturnsAsync(10);
        _bienDong.InSequence(thuTu).Setup(repo => repo.LayTonTruocMocAsync(hang.MaHang, den, default)).ReturnsAsync(7);
        _bienDong.InSequence(thuTu).Setup(repo => repo.LayTonHienHanhAsync(hang.MaHang, default)).ReturnsAsync(11);
        _bienDong.InSequence(thuTu).Setup(repo => repo.LayTrongKyAsync(hang.MaHang, tu, den, default)).ReturnsAsync([dong]);
        _work.InSequence(thuTu).Setup(work => work.CommitAsync(default)).Returns(Task.CompletedTask);
        _danhMuc.Setup(repo => repo.LayMatHangAsync(hang.MaHang, default)).ReturnsAsync(hang);

        var the = await TaoService().TraTheKhoAsync("TK-KHO", hang.MaHang, tu, den);

        Assert.Equal(10, the.TonDauKy);
        Assert.Equal(7, the.TonCuoiKy);
        Assert.Equal(11, the.TonHienHanh);
        Assert.Same(dong, Assert.Single(the.BienDongs));
        _auth.Verify(auth => auth.KiemTraQuyenAsync("TK-KHO", MaQuyenPos.KHO_TRA_SO_HANG, default), Times.Once);
    }

    [Fact]
    public async Task DaoBan_DungDanhTinhDaXacThucVaLuuLoiKhiBiTuChoi()
    {
        var caller = Mock.Of<IInventoryCallerContext>(context => context.MaTaiKhoan == "TK-QL");
        var noiDung = new List<NhatKyGoiTheKho>();
        var audit = new Mock<IInventoryAuditSink>();
        audit.Setup(sink => sink.Ghi(It.IsAny<NhatKyGoiTheKho>())).Callback<NhatKyGoiTheKho>(noiDung.Add);
        var loi = new UnauthorizedActionException(MaQuyenPos.BAN_HUY_HD_XAC_NHAN);
        _auth.Setup(auth => auth.KiemTraQuyenAsync("TK-QL", MaQuyenPos.BAN_HUY_HD_XAC_NHAN, default)).ThrowsAsync(loi);
        var ledger = new Mock<IInventoryService>(MockBehavior.Strict);
        IInventoryService service = new AuditingInventoryServiceDecorator(
            new AuthorizedInventoryServiceDecorator(ledger.Object, caller, _auth.Object), caller, audit.Object);

        Assert.Same(loi, await Assert.ThrowsAsync<UnauthorizedActionException>(() =>
            service.GhiNhanDaoHuyBanAsync(new("HD-1", Luc, [new("DB-1", "MH-A", 2)]), default)));
        var daGhi = Assert.Single(noiDung);
        Assert.Equal("TK-QL", daGhi.MaTaiKhoan);
        Assert.Equal(LoaiBienDongKho.DaoXuatBan, daGhi.LoaiSuKien);
        Assert.Same(loi, daGhi.Loi);
        ledger.VerifyNoOtherCalls();
    }

    private InventoryLedgerService TaoService() => new(_bienDong.Object, _danhMuc.Object, _auth.Object, _work.Object);

    private void CaiNguonBan(YeuCauXuatBanDto yeuCau, bool coHieuLuc = true, DateTime? lucNguonUtc = null)
        => _bienDong.Setup(repo => repo.LayCacNguonAsync(LoaiBienDongKho.XuatBan, yeuCau.MaHoaDon, default))
            .ReturnsAsync(yeuCau.DongHang.Select(dong =>
                new NguonBienDongKho(dong.MaDongBan, dong.MaHang, yeuCau.MaHoaDon, -dong.SoLuongBan,
                    coHieuLuc, lucNguonUtc ?? Luc)).ToArray());
}
