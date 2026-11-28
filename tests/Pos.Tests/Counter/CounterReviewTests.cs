using Moq;
using Pos.Contracts;
using Pos.Counter.Reports;
using Pos.Counter.Services;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using static Pos.Tests.Counter.CounterFixture;

namespace Pos.Tests.Counter;

public sealed class CounterReviewTests
{
    [Fact]
    public async Task XacNhanBan_TuChoiBanNhapDaThayDoi()
    {
        var f = new CounterFixture();
        var draft = f.TaoHoaDon();
        var daXem = PhienBanNhap.CuaHoaDon(draft);
        draft.ThemDongBan(new("DB-2", MaHoaDon, "MH-2", 1, 10_000));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            f.Sale().XacNhanBanAsync(MaHoaDon, MaCa, Actor, Luc, daXem, "ban"));
        f.HoaDon.Verify(r => r.CapNhatAsync(It.IsAny<HoaDon>(), default), Times.Never);
        f.Kho.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task XacNhanBan_GuiLaiSauDongCaTraKetQuaGocVaChanNoiDungLech()
    {
        var f = new CounterFixture();
        LuuKetQua(f);
        var draft = f.TaoHoaDon();
        var version = PhienBanNhap.CuaHoaDon(draft);
        var sale = f.Sale();
        var first = await sale.XacNhanBanAsync(MaHoaDon, MaCa, Actor, Luc, version, "ban");
        f.Ca.Setup(r => r.KhoaTheoMaAsync(MaCa, default)).ThrowsAsync(new InvalidStateTransitionException("Ca đã đóng."));

        var replay = await sale.XacNhanBanAsync(MaHoaDon, MaCa, Actor, Luc.AddMinutes(1), version, "ban");
        await Assert.ThrowsAsync<DuplicateRequestException>(() =>
            sale.XacNhanBanAsync(MaHoaDon, MaCa, Actor, Luc, "le-phien-ban", "ban"));

        Assert.Equal(first.LucXacNhan, replay.LucXacNhan);
        Assert.Equal(first.DongBans[0].MaDongBan, replay.DongBans[0].MaDongBan);
        Assert.Equal(first.ThanhToanBans[0].TienKhachDua, replay.ThanhToanBans[0].TienKhachDua);
        f.Kho.Verify(k => k.GhiNhanXuatBanAsync(It.IsAny<YeuCauXuatBanDto>(), default), Times.Once);
    }

    [Fact]
    public async Task LuuNhap_GuiLaiKhongTaoHoaDonMoi()
    {
        var f = new CounterFixture();
        LuuKetQua(f);
        f.HoaDon.Setup(r => r.SinhMaAsync(default)).ReturnsAsync(MaHoaDon);
        var sale = f.Sale();

        var first = await sale.LuuNhapAsync(Actor, MaCa, null, [], [], Luc, "nhap");
        var replay = await sale.LuuNhapAsync(Actor, MaCa, null, [], [], Luc.AddMinutes(1), "nhap");

        Assert.Equal(first.MaHoaDon, replay.MaHoaDon);
        Assert.Equal(first.LucLap, replay.LucLap);
        f.HoaDon.Verify(r => r.ThemAsync(It.IsAny<HoaDon>(), default), Times.Once);
    }

    [Fact]
    public async Task LuuNhap_XoaThanhToanCuVanPhaiCoQuyen()
    {
        var f = new CounterFixture();
        f.TaoHoaDon();
        f.Auth.Setup(s => s.KiemTraQuyenAsync(Actor, MaQuyenPos.BAN_THANH_TOAN, default))
            .ThrowsAsync(new UnauthorizedActionException(MaQuyenPos.BAN_THANH_TOAN));

        await Assert.ThrowsAsync<UnauthorizedActionException>(() =>
            f.Sale().LuuNhapAsync(Actor, MaCa, MaHoaDon, [], [], Luc, "sua"));
        f.HoaDon.Verify(r => r.CapNhatAsync(It.IsAny<HoaDon>(), default), Times.Never);
    }

    [Fact]
    public async Task HuyNhap_ChanThuNganKhiCaGocDaDong()
    {
        var f = new CounterFixture();
        var draft = f.TaoHoaDon();
        var ca = new CaBan(MaCa, Actor, Luc, 300_000);
        ca.Dong(Actor, Luc.AddMinutes(1), 300_000, 300_000);
        f.Ca.Setup(r => r.KhoaTheoMaAsync(MaCa, default)).ReturnsAsync(ca);

        await Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            f.Sale().HuyHoaDonAsync(MaHoaDon, Actor, "Hủy nháp", null, Luc.AddMinutes(2)));
        Assert.Equal(TrangThaiChungTu.Nhap, draft.TrangThai);
    }

    [Fact]
    public async Task HuyNhap_QuanLyCoQuyenVanHuyDuocSauKhiCaDong()
    {
        var f = new CounterFixture();
        f.TaoHoaDon();
        const string quanLy = "TK-QL";
        f.TaiKhoan.Setup(r => r.LayTheoMaAsync(quanLy, default))
            .ReturnsAsync(new TaiKhoan(quanLy, "QuanLy", "Quản lý", "manager", "hash"));

        var result = await f.Sale().HuyHoaDonAsync(MaHoaDon, quanLy, "Hủy nháp", null, Luc);

        Assert.Equal(TrangThaiChungTu.DaHuy, result.TrangThai);
        f.Ca.Verify(r => r.KhoaTheoMaAsync(It.IsAny<string>(), default), Times.Never);
        f.Auth.Verify(a => a.KiemTraQuyenAsync(quanLy, MaQuyenPos.BAN_HUY_HD_NHAP, default), Times.Once);
    }

    [Fact]
    public async Task HuyBan_ChanNeuTruocGioMoCaDao()
    {
        var f = new CounterFixture();
        f.TaoHoaDon(true);
        f.Ca.Setup(r => r.KhoaTheoMaAsync(MaCa, default))
            .ReturnsAsync(new CaBan(MaCa, Actor, Luc.AddMinutes(2), 300_000));

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            f.Sale().HuyHoaDonAsync(MaHoaDon, Actor, "Hủy bán", MaCa, Luc.AddMinutes(1)));
        f.Kho.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("ThuNgan", false)]
    [InlineData("QuanLy", true)]
    public async Task ThuChi_CaKhacChiDanhChoQuanLy(string vaiTro, bool duocGhi)
    {
        var f = new CounterFixture();
        const string nguoiLap = "TK-2";
        f.TaiKhoan.Setup(r => r.LayTheoMaAsync(nguoiLap, default))
            .ReturnsAsync(new TaiKhoan(nguoiLap, vaiTro, "Nhân viên", "user-2", "hash"));
        f.ThuChi.Setup(r => r.SinhMaAsync(default)).ReturnsAsync("TC-1");

        Task<PhieuThuChi> Ghi() => f.Shift().LapPhieuThuChiAsync(MaCa, nguoiLap, "Thu", NguonQuy.KetCa,
            PhuongThucThanhToan.TienMat, 10_000, "Thu bổ sung", null, Luc, "thu");
        if (duocGhi)
        {
            Assert.Equal("TC-1", (await Ghi()).MaPhieuThuChi);
        }
        else
        {
            await Assert.ThrowsAsync<UnauthorizedActionException>(Ghi);
        }

        f.Auth.Verify(a => a.KiemTraQuyenAsync(nguoiLap, MaQuyenPos.NCC_GHI_TRA_TIEN, default), Times.Never);
    }

    [Fact]
    public async Task ThuChi_ChanChungTuDaHachToanVaGuiLaiKhongGhiTrung()
    {
        var f = new CounterFixture();
        LuuKetQua(f);
        f.ThuChi.Setup(r => r.SinhMaAsync(default)).ReturnsAsync("TC-1");
        f.ThuChi.Setup(r => r.DaHachToanAsync(MaHoaDon, default)).ReturnsAsync(true);
        Task<PhieuThuChi> Ghi(string? chungTu, decimal tien = 10_000) =>
            f.Shift().LapPhieuThuChiAsync(MaCa, Actor, "Thu", NguonQuy.KetCa,
                PhuongThucThanhToan.TienMat, tien, "Thu bổ sung", chungTu, Luc, "thu");

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => Ghi(MaHoaDon));
        var first = await Ghi(null);
        var replay = await Ghi(null, 10_000.00m);
        await Assert.ThrowsAsync<DuplicateRequestException>(() => Ghi(null, 20_000));

        Assert.Equal(first.MaPhieuThuChi, replay.MaPhieuThuChi);
        Assert.Equal(first.SoTien, replay.SoTien);
        f.ThuChi.Verify(r => r.ThemAsync(It.IsAny<PhieuThuChi>(), default), Times.Once);
    }

    [Fact]
    public async Task SuaNhapTra_GiuMaVaNgayLap_ChanLuuHoacXacNhanBanCu()
    {
        var f = new CounterFixture();
        f.TaoHoaDon(true);
        var draft = f.TaoPhieuTra();
        var version = PhienBanNhap.CuaPhieuTra(draft);
        f.Tra.Setup(r => r.SinhMaDongAsync(default)).ReturnsAsync("DT-2");
        f.Tra.Setup(r => r.SinhMaHoanAsync(default)).ReturnsAsync("KH-2");
        var service = f.Return();

        var updated = await service.CapNhatNhapAsync(MaPhieu, MaCa, Actor, version,
            [new(MaDong, 2, 1)], [new(PhuongThucThanhToan.TienMat, 50_000)]);
        f.Tra.Setup(r => r.KhoaTheoMaAsync(MaPhieu, default)).ReturnsAsync(updated);

        Assert.Equal(MaPhieu, updated.MaPhieuTra);
        Assert.Equal(draft.LucLap, updated.LucLap);
        Assert.Equal(2, Assert.Single(updated.DongTras).SoLuongTra);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            service.CapNhatNhapAsync(MaPhieu, MaCa, Actor, version, [], []));
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            service.XacNhanTraAsync(MaPhieu, MaCa, Actor, Luc, version));
        f.Tra.Verify(r => r.CapNhatAsync(It.IsAny<PhieuTra>(), default), Times.Once);
    }

    [Fact]
    public async Task BaoCaoLechCa_LocCaChoThanhToanChiNccVaTongChenhLech()
    {
        var f = new CounterFixture();
        var ca = new CaBan(MaCa, Actor, Luc, 300_000);
        ca.Dong(Actor, Luc, 300_000, 298_000);
        var caKhac = new CaBan("CA-2", "TK-2", Luc, 300_000);
        caKhac.Dong("TK-2", Luc, 300_000, 299_000);
        var ngay = DateOnly.FromDateTime(Luc);
        var tu = Luc.Date;
        var den = tu.AddDays(1);
        f.Ca.Setup(r => r.LayTrongKyAsync(tu, den, default)).ReturnsAsync(new[] { ca, caKhac });
        f.BaoCao.Setup(r => r.LaySuKienAsync(tu, den, default)).ReturnsAsync(new SuKienBanTra[]
        {
            new(Luc, MaHoaDon, MaCa, "DaoThanhToan", null, 0, 10_000, PhuongThucThanhToan.TienMat),
            new(Luc, MaHoaDon, caKhac.MaCa, "DaoThanhToan", null, 0, 20_000, PhuongThucThanhToan.ChuyenKhoan)
        });
        f.BaoCao.Setup(r => r.TinhChiNccTuKetTrongKyAsync(MaCa, tu, den, default)).ReturnsAsync(15_000m);
        var report = new BaoCaoThanhToanLechCa(f.Auth.Object, TimeZoneInfo.Utc, f.BaoCao.Object, f.Ca.Object,
            f.BaoCaoKho.Object, f.Work.Object);

        var result = await report.ThucThiAsync(new BoLocThanhToanLechCaDto(ngay, ngay, MaCa), Actor, default);

        Assert.Equal(4, result.Dong.Count);
        Assert.Equal(10_000m, result.Dong[0][3]);
        Assert.Equal(15_000m, result.Dong[0][6]);
        Assert.Equal(0m, result.Dong[1][3]);
        Assert.Equal(MaCa, result.Dong[2][0]);
        Assert.Equal(-2_000m, result.Dong[3][10]);
    }

    private static void LuuKetQua(CounterFixture f)
    {
        var store = new Dictionary<string, KetQuaYeuCauQuay>();
        f.Requests.Setup(r => r.KhoaVaDocAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string khoa, CancellationToken _) => store.GetValueOrDefault(khoa));
        f.Requests.Setup(r => r.ThemAsync(It.IsAny<string>(), It.IsAny<KetQuaYeuCauQuay>(), default))
            .Callback((string khoa, KetQuaYeuCauQuay ketQua, CancellationToken _) => store.Add(khoa, ketQua))
            .Returns(Task.CompletedTask);
    }
}
