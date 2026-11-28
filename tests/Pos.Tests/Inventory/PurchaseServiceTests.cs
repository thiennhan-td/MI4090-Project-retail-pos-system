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
    private const string MaQuanLyKhac = "TK-QL-2";
    private const string NhanVienKho = "NhanVienKho";
    private const string QuanLy = "QuanLy";
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
    private readonly Mock<IQuanTriTaiKhoanRepository> _taiKhoans = new();
    private readonly Mock<IAuthorizationService> _auth = new();
    private readonly Mock<IShiftCashService> _kets = new();
    private readonly Mock<IGhiBienDongKhoNoiBo> _ledger = new();
    private readonly Mock<IUnitOfWork> _work = new();

    public PurchaseServiceTests()
    {
        _taiKhoans.Setup(x => x.LayHoSoDangNhapAsync(MaNguoiLap, Ct))
            .ReturnsAsync(TaoHoSo(MaNguoiLap, NhanVienKho, MaQuyenPos.NHAP_XAC_NHAN));
        _taiKhoans.Setup(x => x.LayHoSoDangNhapAsync(MaQuanLy, Ct))
            .ReturnsAsync(TaoHoSo(MaQuanLy, QuanLy, MaQuyenPos.NCC_GHI_TRA_TIEN, MaQuyenPos.NHAP_HUY_XAC_NHAN));
        _taiKhoans.Setup(x => x.LayHoSoDangNhapAsync(MaQuanLyKhac, Ct))
            .ReturnsAsync(TaoHoSo(MaQuanLyKhac, QuanLy, MaQuyenPos.NCC_GHI_TRA_TIEN));
    }

    private PurchaseService TaoService() => new(_phieus.Object, _danhMuc.Object, _taiKhoans.Object,
        _auth.Object, _kets.Object, _ledger.Object, _work.Object, new DongHoCoDinh());

    [Theory]
    [InlineData(MaQuyenPos.NHAP_XAC_NHAN, MaQuanLy)]
    [InlineData(MaQuyenPos.NHAP_HUY_XAC_NHAN, MaNguoiLap)]
    [InlineData(MaQuyenPos.NCC_GHI_TRA_TIEN, MaNguoiLap)]
    public async Task GhiPhieuNhap_CoQuyenNhungSaiVaiTro_TuChoiTruocGiaoDich(string maQuyen, string maTaiKhoan)
    {
        var vaiTro = maTaiKhoan == MaQuanLy ? QuanLy : NhanVienKho;
        _auth.Setup(x => x.KiemTraQuyenAsync(maTaiKhoan, maQuyen, Ct)).Returns(Task.CompletedTask);
        _taiKhoans.Setup(x => x.LayHoSoDangNhapAsync(maTaiKhoan, Ct))
            .ReturnsAsync(TaoHoSo(maTaiKhoan, vaiTro, maQuyen));

        var loi = await Assert.ThrowsAsync<UnauthorizedActionException>(() => ThucHienThaoTacGhi(maQuyen, maTaiKhoan));

        Assert.Equal(maQuyen, loi.MaQuyen);
        _auth.Verify(x => x.KiemTraQuyenAsync(maTaiKhoan, maQuyen, Ct), Times.Once);
        _taiKhoans.Verify(x => x.LayHoSoDangNhapAsync(maTaiKhoan, Ct), Times.Once);
        _work.VerifyNoOtherCalls();
        _phieus.VerifyNoOtherCalls();
        _kets.VerifyNoOtherCalls();
        _ledger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task XacNhan_VaiTroHienTaiThieuQuyenSauKhiKiemTraQuyenBanDau_TuChoi()
    {
        _auth.Setup(x => x.KiemTraQuyenAsync(MaNguoiLap, MaQuyenPos.NHAP_XAC_NHAN, Ct)).Returns(Task.CompletedTask);
        _taiKhoans.Setup(x => x.LayHoSoDangNhapAsync(MaNguoiLap, Ct))
            .ReturnsAsync(TaoHoSo(MaNguoiLap, NhanVienKho));

        var loi = await Assert.ThrowsAsync<UnauthorizedActionException>(() =>
            TaoService().XacNhanNhapAsync(MaPhieu, MaNguoiLap, "phien-ban"));

        Assert.Equal(MaQuyenPos.NHAP_XAC_NHAN, loi.MaQuyen);
        _auth.VerifyAll();
        _taiKhoans.Verify(x => x.LayHoSoDangNhapAsync(MaNguoiLap, Ct), Times.Once);
        _work.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(MaQuyenPos.NHAP_XAC_NHAN, MaNguoiLap)]
    [InlineData(MaQuyenPos.NHAP_HUY_XAC_NHAN, MaQuanLy)]
    [InlineData(MaQuyenPos.NCC_GHI_TRA_TIEN, MaQuanLy)]
    public async Task GhiPhieuNhap_DungVaiTroNhungThieuQuyen_TuChoiTruocGiaoDich(string maQuyen, string maTaiKhoan)
    {
        var thieuQuyen = new UnauthorizedActionException(maQuyen);
        _auth.Setup(x => x.KiemTraQuyenAsync(maTaiKhoan, maQuyen, Ct)).ThrowsAsync(thieuQuyen);

        var loi = await Assert.ThrowsAsync<UnauthorizedActionException>(() => ThucHienThaoTacGhi(maQuyen, maTaiKhoan));

        Assert.Same(thieuQuyen, loi);
        _auth.Verify(x => x.KiemTraQuyenAsync(maTaiKhoan, maQuyen, Ct), Times.Once);
        _taiKhoans.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
        _phieus.VerifyNoOtherCalls();
        _kets.VerifyNoOtherCalls();
        _ledger.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task XacNhan_CoQuyenNhungTaiKhoanBiKhoaHoacKhongTonTai_TuChoi(bool biKhoa)
    {
        var hoSo = TaoHoSo(MaNguoiLap, NhanVienKho, MaQuyenPos.NHAP_XAC_NHAN);
        hoSo.TaiKhoan.Khoa();
        _taiKhoans.Setup(x => x.LayHoSoDangNhapAsync(MaNguoiLap, Ct)).ReturnsAsync(biKhoa ? hoSo : null);

        var loi = await Assert.ThrowsAsync<UnauthorizedActionException>(() =>
            TaoService().XacNhanNhapAsync(MaPhieu, MaNguoiLap, "phien-ban"));

        Assert.Equal(MaQuyenPos.NHAP_XAC_NHAN, loi.MaQuyen);
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LayCaChi_ChiQuanLyCoQuyenDuocTraCuu()
    {
        var ca = new ThongTinCaMoDto(MaCa, "TK-THU-NGAN", LucLap);
        _auth.Setup(x => x.KiemTraQuyenAsync(MaNguoiLap, MaQuyenPos.NCC_GHI_TRA_TIEN, Ct))
            .Returns(Task.CompletedTask);
        _taiKhoans.Setup(x => x.LayHoSoDangNhapAsync(MaNguoiLap, Ct))
            .ReturnsAsync(TaoHoSo(MaNguoiLap, NhanVienKho, MaQuyenPos.NCC_GHI_TRA_TIEN));
        var service = TaoService();

        var saiVaiTro = await Assert.ThrowsAsync<UnauthorizedActionException>(() =>
            service.LayDanhSachCaChiAsync(MaNguoiLap));
        Assert.Equal(MaQuyenPos.NCC_GHI_TRA_TIEN, saiVaiTro.MaQuyen);

        var thieuQuyen = new UnauthorizedActionException(MaQuyenPos.NCC_GHI_TRA_TIEN);
        _auth.Setup(x => x.KiemTraQuyenAsync(MaQuanLy, MaQuyenPos.NCC_GHI_TRA_TIEN, Ct)).ThrowsAsync(thieuQuyen);
        var loi = await Assert.ThrowsAsync<UnauthorizedActionException>(() => service.LayDanhSachCaChiAsync(MaQuanLy));
        Assert.Same(thieuQuyen, loi);
        _kets.VerifyNoOtherCalls();

        _auth.Setup(x => x.KiemTraQuyenAsync(MaQuanLy, MaQuyenPos.NCC_GHI_TRA_TIEN, Ct)).Returns(Task.CompletedTask);
        _kets.Setup(x => x.LayDanhSachCaDangMoAsync(Ct)).ReturnsAsync([ca]);
        Assert.Equal(ca, Assert.Single(await service.LayDanhSachCaChiAsync(MaQuanLy)));
    }

    [Fact]
    public async Task HuyNhapNhap_GiuQuyenDaCapChoVaiTroKhac()
    {
        const string maThuNgan = "TK-THU-NGAN";
        var phieu = TaoPhieu(TrangThaiChungTu.Nhap);
        _phieus.Setup(x => x.LayTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _auth.Setup(x => x.KiemTraQuyenAsync(maThuNgan, MaQuyenPos.NHAP_HUY_NHAP, Ct)).Returns(Task.CompletedTask);
        var service = TaoService();
        var nhap = Assert.IsType<PhieuNhapDto>(await service.LayPhieuNhapAsync(MaPhieu, maThuNgan));

        var daHuy = await service.HuyNhapAsync(MaPhieu, maThuNgan, nhap.TrangThai, nhap.PhienBan, "Nhập sai");

        Assert.Equal(TrangThaiChungTu.DaHuy, daHuy.TrangThai);
        _auth.Verify(x => x.KiemTraQuyenAsync(maThuNgan, MaQuyenPos.NHAP_HUY_NHAP, Ct), Times.Once);
        _taiKhoans.VerifyNoOtherCalls();
        _ledger.VerifyNoOtherCalls();
    }

    private Task ThucHienThaoTacGhi(string maQuyen, string maTaiKhoan)
    {
        var service = TaoService();
        return maQuyen switch
        {
            MaQuyenPos.NHAP_XAC_NHAN => service.XacNhanNhapAsync(MaPhieu, maTaiKhoan, "phien-ban"),
            MaQuyenPos.NHAP_HUY_XAC_NHAN => service.HuyNhapAsync(MaPhieu, maTaiKhoan, TrangThaiChungTu.DaXacNhan,
                "phien-ban", "Nhập sai"),
            MaQuyenPos.NCC_GHI_TRA_TIEN => service.GhiTraNhaCungCapAsync(MaPhieu, maTaiKhoan, 40,
                PhuongThucThanhToan.ChuyenKhoan, NguonQuy.QuyCuaHang, null, MaYeuCau),
            _ => throw new ArgumentOutOfRangeException(nameof(maQuyen))
        };
    }

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
    [InlineData(MaPhieu, MaQuanLyKhac, 40)]
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

    [Fact]
    public async Task XacNhan_DonGiaAmKhongVaGiaKhongDocTuSql_CungPhienBan()
    {
        var giaAmKhong = new decimal(0, 0, 0, true, 0);
        var phieu = TaoPhieu(TrangThaiChungTu.Nhap);
        _phieus.Setup(x => x.LayTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(phieu);
        _phieus.Setup(x => x.KhoaTheoMaAsync(MaPhieu, Ct)).ReturnsAsync(() => phieu);
        _danhMuc.Setup(x => x.LayNhaCungCapAsync(MaNcc, Ct)).ReturnsAsync(new NhaCungCap(MaNcc, "Nhà cung cấp", "Liên hệ"));
        _danhMuc.Setup(x => x.LayMatHangAsync(MaHang, Ct)).ReturnsAsync(new MatHang(MaHang, "NH-1", "Sữa", "Hộp", 60));
        var service = TaoService();
        var truocSua = Assert.IsType<PhieuNhapDto>(await service.LayPhieuNhapAsync(MaPhieu, MaNguoiLap));
        var daSua = await service.SuaNhapAsync(MaPhieu, MaNguoiLap, truocSua.PhienBan, MaNcc, null,
            [new(MaHang, 2, giaAmKhong)]);

        phieu = PhieuNhap.KhoiPhuc(MaPhieu, MaNcc, MaNguoiLap, LucLap, null, TrangThaiChungTu.Nhap,
            null, null, null, null, [new DongNhap(MaDong, MaPhieu, MaHang, 2, 0m)], []);
        var daXacNhan = await service.XacNhanNhapAsync(MaPhieu, MaNguoiLap, daSua.PhienBan);

        Assert.Equal(TrangThaiChungTu.DaXacNhan, daXacNhan.TrangThai);
        Assert.Equal(daSua.PhienBan, daXacNhan.PhienBan);
    }

    private static HoSoDangNhap TaoHoSo(string maTaiKhoan, string vaiTro, params string[] quyens) =>
        new(new TaiKhoan(maTaiKhoan, vaiTro, maTaiKhoan, maTaiKhoan, "hash"),
            quyens.ToHashSet(StringComparer.Ordinal));

    private static TraNhaCungCap KhoanTra(string ma, decimal soTien) => new(ma, MaPhieu, MaQuanLy, null,
        LucXacNhan, soTien, PhuongThucThanhToan.ChuyenKhoan, NguonQuy.QuyCuaHang);

    private sealed class DongHoCoDinh : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(LucHienTai);
    }
}
