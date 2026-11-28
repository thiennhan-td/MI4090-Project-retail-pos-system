using Moq;
using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Inventory.Services;

namespace Pos.Tests.Inventory;

public sealed class PurchaseServiceTests
{
    private const string MaPhieu = "PN-1";
    private const string MaNcc = "NCC-1";
    private const string MaHang = "MH-1";
    private const string MaDong = "DN-1";
    private const string MaNguoiLap = "TK-KHO";
    private const string MaQuanLy = "TK-QL";
    private const string MaCa = "CA-1";
    private static readonly DateTime LucLap = new(2026, 11, 28, 1, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LucXacNhan = LucLap.AddHours(1);
    private static readonly DateTime LucHienTai = LucLap.AddHours(2);
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly Guid MaYeuCau = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid MaYeuCauKhac = new("00112233-4455-6677-8899-aabbccddeeff");
    private const string MaKhoanTra = "TNCC-_____________________w";

    private readonly Mock<IPhieuNhapRepository> _phieus = new();
    private readonly Mock<IDanhMucRepository> _danhMuc = new();
    private readonly Mock<IAuthorizationService> _auth = new();
    private readonly Mock<IShiftCashService> _kets = new();
    private readonly Mock<IGhiBienDongKhoNoiBo> _ledger = new();
    private readonly Mock<IUnitOfWork> _work = new();

    private PurchaseService TaoService() => new(_phieus.Object, _danhMuc.Object, _auth.Object, _kets.Object,
        _ledger.Object, _work.Object, new DongHoCoDinh());

    [Fact]
    public async Task TraTuKet_KhoaCaTruocPhieuVaGhiTrongGiaoDich()
    {
        var phieu = TaoPhieu(TrangThaiChungTu.DaXacNhan);
        var thuTu = new List<string>();
        _auth.Setup(x => x.KiemTraQuyenAsync(MaQuanLy, MaQuyenPos.NCC_GHI_TRA_TIEN, Ct))
            .Callback(() => thuTu.Add("quyền"));
        _work.Setup(x => x.BeginTransactionAsync(Ct)).Callback(() => thuTu.Add("bắt đầu"));
        _kets.Setup(x => x.KiemTraVaKhoaCaMoChoChiNccAsync(MaCa, Ct))
            .Callback(() => thuTu.Add("khóa ca")).ReturnsAsync(new ThongTinCaMoDto(MaCa, "TK-THU-NGAN", LucLap));
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct))
            .Callback(() => thuTu.Add("khóa phiếu")).ReturnsAsync(phieu);
        _phieus.Setup(x => x.ThemKhoanTraAsync(It.IsAny<TraNhaCungCap>(), Ct))
            .Callback(() => thuTu.Add("ghi khoản trả"));
        _work.Setup(x => x.CommitAsync(Ct)).Callback(() => thuTu.Add("commit"));

        var khoan = await TaoService().GhiTraNhaCungCapAsync(MaPhieu, MaQuanLy, 40, PhuongThucThanhToan.TienMat,
            NguonQuy.KetCa, MaCa, MaYeuCau, Ct);

        Assert.Equal(["quyền", "bắt đầu", "khóa ca", "khóa phiếu", "ghi khoản trả", "commit"], thuTu);
        Assert.Equal((MaKhoanTra, MaPhieu, MaCa, 40m, LucHienTai),
            (khoan.MaKhoanTra, khoan.MaPhieuNhap, khoan.MaCaChi, khoan.SoTienTra, khoan.LucTra));
    }

    [Fact]
    public async Task TraTuKet_CaDaDong_ThiRollbackTruocKhiKhoaPhieu()
    {
        var loi = new InvalidStateTransitionException("Ca đã đóng.");
        _kets.Setup(x => x.KiemTraVaKhoaCaMoChoChiNccAsync(MaCa, Ct)).ThrowsAsync(loi);

        var ketQua = await Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            TaoService().GhiTraNhaCungCapAsync(MaPhieu, MaQuanLy, 40, PhuongThucThanhToan.TienMat,
                NguonQuy.KetCa, MaCa, MaYeuCau));

        Assert.Same(loi, ketQua);
        _phieus.Verify(x => x.KhoaTheoMaAsync(It.IsAny<string>(), Ct), Times.Never);
        _work.Verify(x => x.RollbackAsync(Ct), Times.Once);
        _work.Verify(x => x.CommitAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task TraTuKet_GuiLaiCungMaSauKhiCaDong_TraKhoanDaGhi()
    {
        var phieu = TaoPhieu(TrangThaiChungTu.DaXacNhan);
        TraNhaCungCap? daGhi = null;
        _phieus.Setup(x => x.LayKhoanTraTheoMaAsync(MaKhoanTra, Ct)).ReturnsAsync(() => daGhi);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _phieus.Setup(x => x.ThemKhoanTraAsync(It.IsAny<TraNhaCungCap>(), Ct))
            .Callback<TraNhaCungCap, CancellationToken>((khoan, _) => daGhi = khoan);
        _kets.SetupSequence(x => x.KiemTraVaKhoaCaMoChoChiNccAsync(MaCa, Ct))
            .ReturnsAsync(new ThongTinCaMoDto(MaCa, "TK-THU-NGAN", LucLap))
            .ThrowsAsync(new InvalidStateTransitionException("Ca đã đóng."));

        var service = TaoService();
        var lanMot = await service.GhiTraNhaCungCapAsync(MaPhieu, MaQuanLy, 40, PhuongThucThanhToan.TienMat,
            NguonQuy.KetCa, MaCa, MaYeuCau);
        var guiLai = await service.GhiTraNhaCungCapAsync(MaPhieu, MaQuanLy, 40, PhuongThucThanhToan.TienMat,
            NguonQuy.KetCa, MaCa, MaYeuCau);

        Assert.Same(lanMot, guiLai);
        Assert.Equal(60, phieu.DuNo);
        _kets.Verify(x => x.KiemTraVaKhoaCaMoChoChiNccAsync(MaCa, Ct), Times.Once);
        _phieus.Verify(x => x.KhoaTheoMaAsync(MaPhieu, Ct), Times.Exactly(2));
        _phieus.Verify(x => x.ThemKhoanTraAsync(It.IsAny<TraNhaCungCap>(), Ct), Times.Once);
    }

    [Fact]
    public async Task TraTuKet_KhoanTraXuatHienTrongLucKhoaCaDong_TraKhoanDaGhi()
    {
        var daGhi = new TraNhaCungCap(MaKhoanTra, MaPhieu, MaQuanLy, MaCa, LucHienTai, 40,
            PhuongThucThanhToan.TienMat, NguonQuy.KetCa);
        _phieus.SetupSequence(x => x.LayKhoanTraTheoMaAsync(MaKhoanTra, Ct))
            .ReturnsAsync((TraNhaCungCap?)null).ReturnsAsync(daGhi);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(TaoPhieu(TrangThaiChungTu.DaXacNhan, daGhi));
        _kets.Setup(x => x.KiemTraVaKhoaCaMoChoChiNccAsync(MaCa, Ct))
            .ThrowsAsync(new InvalidStateTransitionException("Ca vừa đóng."));

        var guiLai = await TaoService().GhiTraNhaCungCapAsync(MaPhieu, MaQuanLy, 40, PhuongThucThanhToan.TienMat,
            NguonQuy.KetCa, MaCa, MaYeuCau);

        Assert.Same(daGhi, guiLai);
        _phieus.Verify(x => x.ThemKhoanTraAsync(It.IsAny<TraNhaCungCap>(), Ct), Times.Never);
        _work.Verify(x => x.CommitAsync(Ct), Times.Once);
    }

    [Theory]
    [InlineData("PN-2", MaQuanLy, 40)]
    [InlineData(MaPhieu, "TK-QL-2", 40)]
    [InlineData(MaPhieu, MaQuanLy, 45)]
    public async Task TraNcc_GuiLaiMaVoiPhieuNguoiGhiHoacTienKhac_TuChoi(string maPhieu, string maNguoiGhi, int soTien)
    {
        var daGhi = KhoanTra(MaKhoanTra, 40);
        _phieus.Setup(x => x.LayKhoanTraTheoMaAsync(MaKhoanTra, Ct)).ReturnsAsync(daGhi);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(TaoPhieu(TrangThaiChungTu.DaXacNhan, daGhi));

        await Assert.ThrowsAsync<DuplicateRequestException>(() => TaoService().GhiTraNhaCungCapAsync(maPhieu,
            maNguoiGhi, soTien, PhuongThucThanhToan.ChuyenKhoan, NguonQuy.QuyCuaHang, null, MaYeuCau));

        _phieus.Verify(x => x.KhoaTheoMaAsync(MaPhieu, Ct), Times.Once);
        _phieus.Verify(x => x.ThemKhoanTraAsync(It.IsAny<TraNhaCungCap>(), Ct), Times.Never);
        _work.Verify(x => x.RollbackAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task TraNcc_MaVuaDuocDungTrenPhieuKhac_TuChoiTruocKhiGhi()
    {
        var daGhi = new TraNhaCungCap(MaKhoanTra, "PN-2", MaQuanLy, null, LucHienTai, 40,
            PhuongThucThanhToan.ChuyenKhoan, NguonQuy.QuyCuaHang);
        _phieus.SetupSequence(x => x.LayKhoanTraTheoMaAsync(MaKhoanTra, Ct))
            .ReturnsAsync((TraNhaCungCap?)null).ReturnsAsync(daGhi);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(TaoPhieu(TrangThaiChungTu.DaXacNhan));

        await Assert.ThrowsAsync<DuplicateRequestException>(() => TaoService().GhiTraNhaCungCapAsync(MaPhieu,
            MaQuanLy, 40, PhuongThucThanhToan.ChuyenKhoan, NguonQuy.QuyCuaHang, null, MaYeuCau));

        _phieus.Verify(x => x.KhoaTheoMaAsync(MaPhieu, Ct), Times.Once);
        _phieus.Verify(x => x.KhoaTheoMaAsync("PN-2", Ct), Times.Never);
        _phieus.Verify(x => x.ThemKhoanTraAsync(It.IsAny<TraNhaCungCap>(), Ct), Times.Never);
    }

    [Fact]
    public async Task TraTuQuy_DocLaiSoDaTraDuoiKhoaVaChanTraVuotDuNo()
    {
        var phieu = TaoPhieu(TrangThaiChungTu.DaXacNhan, KhoanTra("TNCC-1", 80));
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => TaoService().GhiTraNhaCungCapAsync(MaPhieu,
            MaQuanLy, 21, PhuongThucThanhToan.ChuyenKhoan, NguonQuy.QuyCuaHang, null, MaYeuCau));

        _kets.VerifyNoOtherCalls();
        Assert.Equal(20, phieu.DuNo);
        _phieus.Verify(x => x.ThemKhoanTraAsync(It.IsAny<TraNhaCungCap>(), Ct), Times.Never);
        _work.Verify(x => x.RollbackAsync(Ct), Times.Once);
        _work.Verify(x => x.CommitAsync(Ct), Times.Never);
    }

    [Theory]
    [InlineData(PhuongThucThanhToan.TienMat)]
    [InlineData(PhuongThucThanhToan.ChuyenKhoan)]
    public async Task TraTuQuy_PhuongThucHopLe_GhiHaiKhoanCungTienKhiConDuNo(string phuongThuc)
    {
        var phieu = TaoPhieu(TrangThaiChungTu.DaXacNhan, KhoanTra("TNCC-1", 50));
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        var service = TaoService();
        var lanMot = await service.GhiTraNhaCungCapAsync(MaPhieu, MaQuanLy, 25, phuongThuc, NguonQuy.QuyCuaHang,
            null, MaYeuCau);
        var lanHai = await service.GhiTraNhaCungCapAsync(MaPhieu, MaQuanLy, 25, phuongThuc, NguonQuy.QuyCuaHang,
            null, MaYeuCauKhac);

        Assert.Equal(MaKhoanTra, lanMot.MaKhoanTra);
        Assert.NotEqual(lanMot.MaKhoanTra, lanHai.MaKhoanTra);
        Assert.Equal(0, phieu.DuNo);
        _kets.VerifyNoOtherCalls();
        _phieus.Verify(x => x.ThemKhoanTraAsync(It.IsAny<TraNhaCungCap>(), Ct), Times.Exactly(2));
        _work.Verify(x => x.CommitAsync(Ct), Times.Exactly(2));
    }

    [Theory]
    [InlineData(TrangThaiChungTu.Nhap, LoaiBienDongKho.NhapKho, 2)]
    [InlineData(TrangThaiChungTu.DaXacNhan, LoaiBienDongKho.DaoNhapKho, -2)]
    public async Task XacNhanHoacHuy_LoiGhiTheKho_HoanTacTrangThai(string trangThai, string loai, int soLuong)
    {
        var phieu = TaoPhieu(trangThai);
        _phieus.Setup(x => x.LayTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _danhMuc.Setup(x => x.LayMatHangAsync(MaHang, Ct)).ReturnsAsync(new MatHang(MaHang, "NH-1", "Sữa", "Hộp", 60));
        var loi = new InsufficientStockException(MaHang, 1, 2);
        var bienDong = new YeuCauBienDongKho(MaHang, loai, soLuong, LucHienTai, MaDong);
        _ledger.Setup(x => x.GhiBienDongNoiBoAsync(It.Is<IReadOnlyList<YeuCauBienDongKho>>(
            entries => entries.Count == 1 && entries[0] == bienDong), Ct)).ThrowsAsync(loi);

        var service = TaoService();
        var chiTiet = Assert.IsType<PhieuNhapDto>(await service.LayPhieuNhapAsync(MaPhieu, MaNguoiLap));
        _work.Invocations.Clear();
        var ketQua = await Assert.ThrowsAsync<InsufficientStockException>(() => trangThai == TrangThaiChungTu.Nhap
            ? service.XacNhanNhapAsync(MaPhieu, MaNguoiLap, chiTiet.PhienBan)
            : service.HuyNhapAsync(MaPhieu, MaQuanLy, trangThai, chiTiet.PhienBan, "Nhập sai"));

        Assert.Same(loi, ketQua);
        _phieus.Verify(x => x.CapNhatTrangThaiAsync(phieu, trangThai, Ct), Times.Once);
        _work.Verify(x => x.RollbackAsync(Ct), Times.Once);
        _work.Verify(x => x.CommitAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task HuyNhap_DaTraNhaCungCap_ThiKhongDoiTrangThaiHayDaoKho()
    {
        var phieu = TaoPhieu(TrangThaiChungTu.DaXacNhan, KhoanTra("TNCC-1", 30));
        _phieus.Setup(x => x.LayTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);

        var service = TaoService();
        var chiTiet = Assert.IsType<PhieuNhapDto>(await service.LayPhieuNhapAsync(MaPhieu, MaQuanLy));
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.HuyNhapAsync(MaPhieu, MaQuanLy,
            chiTiet.TrangThai, chiTiet.PhienBan, "Nhập sai"));

        Assert.Equal(TrangThaiChungTu.DaXacNhan, phieu.TrangThai);
        _phieus.Verify(x => x.CapNhatTrangThaiAsync(It.IsAny<PhieuNhap>(), It.IsAny<string>(), Ct), Times.Never);
        _ledger.VerifyNoOtherCalls();
        _work.Verify(x => x.RollbackAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task XacNhan_PhienBanCuBiTuChoiVaYeuCauLapLaiChiGhiMotLan()
    {
        var phieu = TaoPhieu(TrangThaiChungTu.Nhap);
        _phieus.Setup(x => x.LayTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _danhMuc.Setup(x => x.LayNhaCungCapAsync(MaNcc, Ct)).ReturnsAsync(new NhaCungCap(MaNcc, "Nhà cung cấp", "Liên hệ"));
        _danhMuc.Setup(x => x.LayMatHangAsync(MaHang, Ct)).ReturnsAsync(new MatHang(MaHang, "NH-1", "Sữa", "Hộp", 60));
        _ledger.Setup(x => x.GhiBienDongNoiBoAsync(It.IsAny<IReadOnlyList<YeuCauBienDongKho>>(), Ct))
            .ReturnsAsync(new KetQuaGhiTheKhoDto(1, 0));
        var service = TaoService();

        var cu = Assert.IsType<PhieuNhapDto>(await service.LayPhieuNhapAsync(MaPhieu, MaNguoiLap));
        var daSua = await service.SuaNhapAsync(MaPhieu, MaNguoiLap, cu.PhienBan, MaNcc, null, [new(MaHang, 3, 50)]);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.XacNhanNhapAsync(MaPhieu, MaNguoiLap, cu.PhienBan));
        var xacNhan = await service.XacNhanNhapAsync(MaPhieu, MaNguoiLap, daSua.PhienBan);
        var lapLai = await service.XacNhanNhapAsync(MaPhieu, MaNguoiLap, daSua.PhienBan);

        Assert.Equal(MaDong, Assert.Single(daSua.DongNhaps).MaDongNhap);
        Assert.Equal(TrangThaiChungTu.DaXacNhan, lapLai.TrangThai);
        Assert.Equal(xacNhan.LucXacNhan, lapLai.LucXacNhan);
        _ledger.Verify(x => x.GhiBienDongNoiBoAsync(It.IsAny<IReadOnlyList<YeuCauBienDongKho>>(), Ct), Times.Once);
    }

    private static PhieuNhap TaoPhieu(string trangThai, params TraNhaCungCap[] khoanTras) =>
        PhieuNhap.KhoiPhuc(MaPhieu, MaNcc, MaNguoiLap, LucLap, null, trangThai,
            trangThai == TrangThaiChungTu.DaXacNhan ? LucXacNhan : null, null, null, null,
            [new DongNhap(MaDong, MaPhieu, MaHang, 2, 50)], khoanTras);

    private static TraNhaCungCap KhoanTra(string ma, decimal soTien) => new(ma, MaPhieu, MaQuanLy, null,
        LucXacNhan, soTien, PhuongThucThanhToan.ChuyenKhoan, NguonQuy.QuyCuaHang);

    private sealed class DongHoCoDinh : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(LucHienTai);
    }
}
