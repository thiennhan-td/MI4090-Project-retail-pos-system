using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

namespace Pos.Tests.Domain;

public sealed class ChungTuRegressionTests
{
    private static readonly DateTime LucLap = new(2026, 11, 28, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GhiSoLuongDem_GiuTonSoVaChanSuaSauKhiGuiDuyet()
    {
        var phieu = new PhieuKiem("PK-1", "TK-1", LucLap);
        phieu.ThemDongKiem(new("PK-1", "MH-1", 10, null, null));

        phieu.GhiSoLuongDem("MH-1", 8, LucLap, "Hỏng hai sản phẩm");
        phieu.GuiDuyet();

        var dong = Assert.Single(phieu.DongKiems);
        Assert.Equal(10, dong.TonSoTaiMoc);
        Assert.Equal(-2, dong.ChenhLech.GetValueOrDefault());
        Assert.Equal("Hỏng hai sản phẩm", dong.GiaiTrinh);
        Assert.Equal(TrangThaiChungTu.ChoDuyet, phieu.TrangThai);
        Assert.Throws<InvalidStateTransitionException>(() => phieu.GhiSoLuongDem("MH-1", 10, LucLap));
    }

    [Fact]
    public void GuiDuyet_ChanDongLechChuaGiaiTrinh()
    {
        var phieu = new PhieuKiem("PK-1", "TK-1", LucLap);
        phieu.ThemDongKiem(new("PK-1", "MH-1", 10, 8, null, LucLap));

        Assert.Throws<BusinessRuleViolationException>(() => phieu.GuiDuyet());

        Assert.Equal(TrangThaiChungTu.DangKiem, phieu.TrangThai);
    }

    [Fact]
    public void GuiDuyet_SoDemBangKhongKhacVoiChuaDem()
    {
        var phieu = new PhieuKiem("PK-1", "TK-1", LucLap);
        phieu.ThemDongKiem(new("PK-1", "MH-1", 0, null, null));
        Assert.Throws<BusinessRuleViolationException>(() => phieu.GuiDuyet());

        phieu.GhiSoLuongDem("MH-1", 0, LucLap);
        phieu.GuiDuyet();

        Assert.Equal(TrangThaiChungTu.ChoDuyet, phieu.TrangThai);
    }

    [Fact]
    public void PhienBanKiem_TheoNoiDungVaKhongPhuThuocThuTuDong()
    {
        var dongA = new DongKiem("PK-1", "MH-A", 3, 2, "Hỏng một", LucLap);
        var dongZ = new DongKiem("PK-1", "MH-Z", 1, 1, null, LucLap);
        PhieuKiem KhoiPhuc(string trangThai, params DongKiem[] dongs) => PhieuKiem.KhoiPhuc("PK-1", "TK-1",
            LucLap, trangThai, null, null, dongs);

        var goc = KhoiPhuc(TrangThaiChungTu.DangKiem, dongA, dongZ);
        Assert.Equal(goc.PhienBan, KhoiPhuc(TrangThaiChungTu.DangKiem, dongZ, dongA).PhienBan);
        Assert.NotEqual(goc.PhienBan, KhoiPhuc(TrangThaiChungTu.ChoDuyet, dongA, dongZ).PhienBan);
        Assert.NotEqual(goc.PhienBan, KhoiPhuc(TrangThaiChungTu.DangKiem,
            new("PK-1", "MH-A", 3, 2, "Mất một", LucLap), dongZ).PhienBan);
        Assert.NotEqual(goc.PhienBan, KhoiPhuc(TrangThaiChungTu.DangKiem,
            new("PK-1", "MH-A", 3, 2, "Hỏng một", LucLap.AddMilliseconds(1)), dongZ).PhienBan);
    }

    [Fact]
    public void KhoiPhucPhieuKiemHoanTatCu_GiuSoDemVaMocDemNull()
    {
        var phieu = PhieuKiem.KhoiPhuc("PK-1", "TK-1", LucLap, TrangThaiChungTu.HoanTat, "TK-2",
            LucLap.AddMinutes(1), [new("PK-1", "MH-1", 10, 8, "Hỏng hai sản phẩm")]);

        Assert.Equal(TrangThaiChungTu.HoanTat, phieu.TrangThai);
        var dong = Assert.Single(phieu.DongKiems);
        Assert.Equal(8, dong.SoLuongDem);
        Assert.Null(dong.LucDem);
    }

    [Theory]
    [InlineData(TrangThaiChungTu.DaXacNhan)]
    [InlineData(TrangThaiChungTu.DaHuy)]
    public void KhoiPhucHoaDon_GiuLichSuKhongChayLaiKiemTraThanhToan(string trangThai)
    {
        var lucXacNhan = LucLap.AddMinutes(1);
        DateTime? lucHuy = trangThai == TrangThaiChungTu.DaHuy ? LucLap.AddMinutes(2) : null;

        var hoaDon = HoaDon.KhoiPhuc("HD-1", "CA-1", "TK-1", LucLap, trangThai,
            lucXacNhan, lucHuy, lucHuy.HasValue ? "TK-2" : null, lucHuy.HasValue ? "Hủy lịch sử" : null,
            null, [new("DB-1", "HD-1", "MH-1", 2, 25_000m)], []);

        Assert.Equal(trangThai, hoaDon.TrangThai);
        Assert.Equal(lucXacNhan, hoaDon.LucXacNhan.GetValueOrDefault());
        Assert.Equal(lucHuy, hoaDon.LucHuy);
        Assert.Equal(50_000m, hoaDon.TinhGiaTriAnhHuong());
        Assert.Throws<InvalidStateTransitionException>(() => hoaDon.XacNhan(lucXacNhan));
    }

    [Fact]
    public void KhoiPhucPhieuTra_GiuTrangThaiVaDongDaLuu()
    {
        var lucXacNhan = LucLap.AddMinutes(1);

        var phieu = PhieuTra.KhoiPhuc("PT-1", "HD-1", "CA-1", "TK-1", LucLap,
            TrangThaiChungTu.DaXacNhan, lucXacNhan, [new("DT-1", "PT-1", "DB-1", 2, 1)],
            [new("KH-1", "PT-1", PhuongThucThanhToan.TienMat, 50_000m)]);

        Assert.Equal(TrangThaiChungTu.DaXacNhan, phieu.TrangThai);
        Assert.Equal(lucXacNhan, phieu.LucXacNhan.GetValueOrDefault());
        Assert.Equal(1, Assert.Single(phieu.DongTras).SoLuongNhapLai);
        Assert.Equal(-50_000m, phieu.TinhGiaTriAnhHuong());
    }
}
