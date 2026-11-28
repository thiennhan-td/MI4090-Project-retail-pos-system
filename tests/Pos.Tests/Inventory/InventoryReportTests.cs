using Moq;
using Pos.Contracts;
using Pos.Contracts.Reports;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Domain.Reports;
using Pos.Inventory.Reports;
using Pos.Inventory.Services;

namespace Pos.Tests.Inventory;

public sealed class InventoryReportTests
{
    private static readonly TimeZoneInfo MuiGioCuaHang =
        TimeZoneInfo.CreateCustomTimeZone("UTC+07", TimeSpan.FromHours(7), "UTC+07", "UTC+07");
    private static readonly DateOnly DauThangMuoi = new(2026, 10, 1);
    private static readonly DateOnly CuoiThangMuoi = new(2026, 10, 31);
    private static readonly DateTime TuUtc = new(2026, 9, 30, 17, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DenUtc = new(2026, 10, 31, 17, 0, 0, DateTimeKind.Utc);
    private const string MaTaiKhoan = "S-TK-QL";

    [Fact]
    public async Task XuatNhapTon_ThangMuoi_KhopTheKhoSuaVaBanh()
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        auth.Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_XUAT_NHAP_TON, ct))
            .Returns(Task.CompletedTask);
        // Tháng 10 trong seed: sữa nhập 40, khách trả nhập 1, bán 5, kiểm giảm 2;
        // bánh bán 3 rồi đảo 3. Hàng không có thẻ kho vẫn có thể xuất hiện trong báo cáo.
        repository.Setup(x => x.LayXuatNhapTonAsync(
            It.Is<DateTime>(tu => tu == TuUtc && tu.Kind == DateTimeKind.Utc),
            It.Is<DateTime>(den => den == DenUtc && den.Kind == DateTimeKind.Utc), null, null, ct)).ReturnsAsync(
            [
                new("S-SUA", "Sữa tươi 180 ml", "Sữa", "Hộp", 100, 40, 5, 1, -2, 41, 7),
                new("S-BANH", "Bánh mì đóng gói", "Bánh", "Gói", 50, 0, 0, 0, 0, 3, 3),
                new("H-CHUA-NHAP", "Hàng chưa nhập", "Bánh", "Gói", 0, 0, 0, 0, 0, 0, 0)
            ]);
        var report = new BaoCaoXuatNhapTon(repository.Object, auth.Object, MuiGioCuaHang);

        var bang = await report.ThucThiAsync(new BoLocBaoCaoDto(DauThangMuoi, CuoiThangMuoi), MaTaiKhoan, ct);

        Assert.Equal(3, bang.Dong.Count);
        Assert.Equal(bang.Cot.Count, bang.Dong[0].Count);
        Assert.Equal(new object?[]
            { "S-SUA", "Sữa tươi 180 ml", "Sữa", "Hộp", 100L, 40L, 5L, 1L, -2L, 41L, 7L, 134L }, bang.Dong[0]);
        Assert.Equal(new object?[]
            { "S-BANH", "Bánh mì đóng gói", "Bánh", "Gói", 50L, 0L, 0L, 0L, 0L, 3L, 3L, 50L }, bang.Dong[1]);
        Assert.Equal(0L, Assert.IsType<long>(bang.Dong[2][^1]));
        auth.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task XuatNhapTon_TruyenBoLocNhomVaHang_GiuKieuSoLuongLong()
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        var ct = CancellationToken.None;
        auth.Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_XUAT_NHAP_TON, ct))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.LayXuatNhapTonAsync(TuUtc, DenUtc, "S-N-SUA", "S-SUA", ct)).ReturnsAsync(
            [new("S-SUA", "Sữa tươi 180 ml", "Sữa", "Hộp", int.MaxValue, 40, 0, 0, 0, 40, 0)]);
        var report = new BaoCaoXuatNhapTon(repository.Object, auth.Object, MuiGioCuaHang);

        var bang = await report.ThucThiAsync(new BoLocXuatNhapTonDto(DauThangMuoi, CuoiThangMuoi, "S-N-SUA", "S-SUA"),
            MaTaiKhoan, ct);

        Assert.Equal((long)int.MaxValue + 40, Assert.IsType<long>(Assert.Single(bang.Dong)[^1]));
        auth.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task NhapCongNo_ThangMuoi_KhopDuNoNhaCungCapSua()
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        var ct = CancellationToken.None;
        auth.Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, ct))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.LayNhapCongNoAsync(TuUtc, DenUtc, "S-NCC-SUA", ct)).ReturnsAsync(
            [new("S-NCC-SUA", "NCC sữa", 1_500_000m - 300_000m, 600_000m, 0m, 15_000m + 100_000m)]);
        var report = new BaoCaoNhapCongNo(repository.Object, auth.Object, MuiGioCuaHang);

        var bang = await report.ThucThiAsync(new BoLocNhapCongNoDto(DauThangMuoi, CuoiThangMuoi, "S-NCC-SUA"),
            MaTaiKhoan, ct);

        Assert.Equal(new object?[] { "S-NCC-SUA", "NCC sữa", 1_200_000m, 600_000m, 0m, 115_000m, 1_685_000m },
            Assert.Single(bang.Dong));
        auth.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task NhapCongNo_ThangMuoiMot_DaoPhieuNhapBanhKhongLamTangDuCuoi()
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        var ct = CancellationToken.None;
        var denUtc = new DateTime(2026, 11, 30, 17, 0, 0, DateTimeKind.Utc);
        auth.Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, ct))
            .Returns(Task.CompletedTask);
        // S-PN-NOV-BANH được xác nhận và hủy trong cùng kỳ, không có khoản trả.
        repository.Setup(x => x.LayNhapCongNoAsync(DenUtc, denUtc, "S-NCC-BANH", ct)).ReturnsAsync(
            [new("S-NCC-BANH", "NCC bánh", 400_000m, 32_000m, 32_000m, 0m)]);
        var report = new BaoCaoNhapCongNo(repository.Object, auth.Object, MuiGioCuaHang);

        var bang = await report.ThucThiAsync(new BoLocNhapCongNoDto(new(2026, 11, 1), new(2026, 11, 30), "S-NCC-BANH"),
            MaTaiKhoan, ct);

        Assert.Equal(new object?[] { "S-NCC-BANH", "NCC bánh", 400_000m, 32_000m, 32_000m, 0m, 400_000m },
            Assert.Single(bang.Dong));
        auth.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task NhapCongNo_ChiTietDungNhaCungCapVaKhoangUtcCuaBaoCao()
    {
        const string maNcc = "S-NCC-SUA";
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        var baoCao = new BaoCaoNhapCongNo(repository.Object, auth.Object, MuiGioCuaHang);
        var boLoc = new BoLocNhapCongNoDto(DauThangMuoi, CuoiThangMuoi, maNcc);
        DongChiTietNhapCongNoDto[] suKiens =
        [
            new(LoaiSuKienNhapCongNo.XacNhanNhap, TuUtc.AddDays(-5), "PN-CU", maNcc, null, 100m, TruocKy: true),
            new(LoaiSuKienNhapCongNo.TraNhaCungCap, TuUtc.AddTicks(-1), "PN-CU", maNcc, "TNCC-CU", 40m, TruocKy: true),
            new(LoaiSuKienNhapCongNo.XacNhanNhap, TuUtc, "PN-DAO", maNcc, null, 50m, TruocKy: false),
            new(LoaiSuKienNhapCongNo.DaoNhap, TuUtc.AddDays(1), "PN-DAO", maNcc, null, 50m, TruocKy: false),
            new(LoaiSuKienNhapCongNo.TraNhaCungCap, DenUtc.AddTicks(-1), "PN-CU", maNcc, "TNCC-MOI", 20m, TruocKy: false)
        ];
        var thuTu = new MockSequence();
        auth.InSequence(thuTu).Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, ct))
            .Returns(Task.CompletedTask);
        repository.InSequence(thuTu).Setup(x => x.LayNhapCongNoAsync(
            It.Is<DateTime>(tu => tu == TuUtc && tu.Kind == DateTimeKind.Utc),
            It.Is<DateTime>(den => den == DenUtc && den.Kind == DateTimeKind.Utc), maNcc, ct))
            .ReturnsAsync([new DongNhapCongNoDto(maNcc, "NCC sữa", 60m, 50m, 50m, 20m)]);
        auth.InSequence(thuTu).Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, ct))
            .Returns(Task.CompletedTask);
        repository.InSequence(thuTu).Setup(x => x.LayChiTietNhapCongNoAsync(
            It.Is<DateTime>(tu => tu == TuUtc && tu.Kind == DateTimeKind.Utc),
            It.Is<DateTime>(den => den == DenUtc && den.Kind == DateTimeKind.Utc), maNcc, ct))
            .ReturnsAsync(suKiens);

        var tongHop = await baoCao.ThucThiAsync(boLoc, MaTaiKhoan, ct);
        var chiTiet = await baoCao.LayChiTietAsync(boLoc, MaTaiKhoan, ct);

        Assert.Equal(new object?[] { maNcc, "NCC sữa", 60m, 50m, 50m, 20m, 40m }, Assert.Single(tongHop.Dong));
        Assert.Equal(suKiens, chiTiet);
        auth.Verify(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, ct), Times.Exactly(2));
        repository.VerifyAll();
    }

    [Fact]
    public async Task NhapCongNo_ChiTietThieuQuyen_TuChoiTruocKhiKiemTraBoLoc()
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        var thieuQuyen = new UnauthorizedActionException(MaQuyenPos.BC_NHAP_CONG_NO);
        auth.Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, CancellationToken.None))
            .ThrowsAsync(thieuQuyen);
        var baoCao = new BaoCaoNhapCongNo(repository.Object, auth.Object, MuiGioCuaHang);

        var loi = await Assert.ThrowsAsync<UnauthorizedActionException>(() =>
            baoCao.LayChiTietAsync(new BoLocNhapCongNoDto(CuoiThangMuoi, DauThangMuoi), MaTaiKhoan));

        Assert.Same(thieuQuyen, loi);
        auth.VerifyAll();
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NhapCongNo_ChiTietThieuNhaCungCap_KhongTruyVan()
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        auth.Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, CancellationToken.None))
            .Returns(Task.CompletedTask);
        var baoCao = new BaoCaoNhapCongNo(repository.Object, auth.Object, MuiGioCuaHang);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            baoCao.LayChiTietAsync(new BoLocNhapCongNoDto(DauThangMuoi, CuoiThangMuoi), MaTaiKhoan));

        auth.VerifyAll();
        repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("NCC có dấu")]
    [InlineData("123456789012345678901234567890123")]
    public async Task NhapCongNo_MaNhaCungCapSaiDinhDang_KhongTruyVan(string maNcc)
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        auth.Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, CancellationToken.None))
            .Returns(Task.CompletedTask);
        var baoCao = new BaoCaoNhapCongNo(repository.Object, auth.Object, MuiGioCuaHang);
        var boLoc = new BoLocNhapCongNoDto(DauThangMuoi, CuoiThangMuoi, maNcc);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            baoCao.ThucThiAsync(boLoc, MaTaiKhoan, CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => baoCao.LayChiTietAsync(boLoc, MaTaiKhoan));

        auth.Verify(x => x.KiemTraQuyenAsync(MaTaiKhoan, MaQuyenPos.BC_NHAP_CONG_NO, CancellationToken.None),
            Times.Exactly(2));
        repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(MaQuyenPos.BC_XUAT_NHAP_TON)]
    [InlineData(MaQuyenPos.BC_NHAP_CONG_NO)]
    public async Task BaoCao_ThieuQuyen_KhongTruyVanDuLieu(string maQuyen)
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        var ct = CancellationToken.None;
        auth.Setup(x => x.KiemTraQuyenAsync(MaTaiKhoan, maQuyen, ct))
            .ThrowsAsync(new UnauthorizedActionException(maQuyen));
        IBaoCaoNghiepVu report = maQuyen == MaQuyenPos.BC_XUAT_NHAP_TON
            ? new BaoCaoXuatNhapTon(repository.Object, auth.Object, MuiGioCuaHang)
            : new BaoCaoNhapCongNo(repository.Object, auth.Object, MuiGioCuaHang);

        var loi = await Assert.ThrowsAsync<UnauthorizedActionException>(() =>
            report.ThucThiAsync(new BoLocBaoCaoDto(DauThangMuoi, CuoiThangMuoi), MaTaiKhoan, ct));

        Assert.Equal(maQuyen, loi.MaQuyen);
        auth.VerifyAll();
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChiNcc_TraKetQuaTheoCaVaTheoNguonTrongKy()
    {
        var repository = new Mock<IInventoryReportRepository>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        var dauNgayUtc = new DateTime(2026, 10, 9, 17, 0, 0, DateTimeKind.Utc);
        var cuoiNgayUtc = dauNgayUtc.AddDays(1);
        repository.Setup(x => x.TinhTongChiNccTuKetTheoCaAsync("S-CA-1010", ct)).ReturnsAsync(15_000m);
        repository.Setup(x => x.LayTongHopChiNccTheoKyAsync(dauNgayUtc, cuoiNgayUtc, ct))
            .ReturnsAsync(new ChiNccTheoKyDto(15_000m, 0m, 0m));
        repository.Setup(x => x.LayTongHopChiNccTheoKyAsync(TuUtc, DenUtc, ct))
            .ReturnsAsync(new ChiNccTheoKyDto(15_000m, 0m, 100_000m));
        IInventoryReportQueryService service = new InventoryReportQueryService(repository.Object);

        Assert.Equal(15_000m, await service.TinhTongChiNccTuKetTheoCaAsync("S-CA-1010", ct));
        Assert.Equal(new TongHopChiNccTrongKyDto(15_000m, 0m, 0m),
            await service.LayTongHopChiNccTheoKyAsync(dauNgayUtc, cuoiNgayUtc, ct));
        Assert.Equal(new TongHopChiNccTrongKyDto(15_000m, 0m, 100_000m),
            await service.LayTongHopChiNccTheoKyAsync(TuUtc, DenUtc, ct));
        repository.VerifyAll();
    }
}
