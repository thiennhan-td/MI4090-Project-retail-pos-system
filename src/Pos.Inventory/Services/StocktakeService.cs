using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Inventory.Factories;

namespace Pos.Inventory.Services;

/// <summary>Giải trình null giữ nguyên nội dung đã lưu. Chuỗi trống xóa giải trình.</summary>
public sealed record SoLuongDemDto(string MaHang, int? SoLuongDem, string? GiaiTrinh = null);

public sealed class StocktakeService(
    IPhieuKiemRepository phieuKiemRepo,
    IDanhMucRepository danhMucRepo,
    IBienDongKhoRepository bienDongRepo,
    IGhiBienDongKhoNoiBo ghiBienDong,
    IQuanTriTaiKhoanRepository taiKhoanRepo,
    IAuthorizationService authorization,
    IUnitOfWork work,
    TimeProvider? clock = null)
{
    private const string NhanVienKho = "NhanVienKho";
    private const string QuanLy = "QuanLy";
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<PhieuKiem> LapPhieuKiemAsync(string maTaiKhoan, IReadOnlyList<string> maHangs,
        CancellationToken ct = default)
    {
        await KiemTraVaiTroAsync(maTaiKhoan, MaQuyenPos.KHO_LAP_KIEM, NhanVienKho, ct);
        if (maHangs.Count == 0 || maHangs.Any(ma => string.IsNullOrWhiteSpace(ma) || ma.Length > 32))
        {
            throw new BusinessRuleViolationException("Chọn ít nhất một mặt hàng hợp lệ cho phiếu kiểm.");
        }

        var hangCanKiem = maHangs.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            await KhoaMatHangAsync(hangCanKiem, ct);
            var mocKiem = LayLucUtc();
            foreach (var maHang in hangCanKiem)
            {
                var hang = await danhMucRepo.LayMatHangAsync(maHang, ct);
                if (hang?.TrangThai != TrangThaiChungTu.HoatDong)
                {
                    throw new BusinessRuleViolationException($"Mặt hàng {maHang} không hoạt động.");
                }

                var moc = await bienDongRepo.LayMocGhiAsync(maHang, ct);
                if (moc.LucGhiCuoi is DateTime lucGhiCuoi && lucGhiCuoi > mocKiem)
                {
                    mocKiem = lucGhiCuoi;
                }

                // Mốc mới sắp sau mọi điều chỉnh đã thấy, kể cả điều chỉnh cùng mili giây.
                if (await bienDongRepo.LayMocDieuChinhCuoiAsync(maHang, ct) is DateTime dieuChinh && dieuChinh >= mocKiem)
                {
                    mocKiem = dieuChinh.AddMilliseconds(1);
                }
            }

            var phieu = new PhieuKiem(await phieuKiemRepo.SinhMaAsync(ct), maTaiKhoan, mocKiem);
            foreach (var maHang in hangCanKiem)
            {
                var ton = await bienDongRepo.LayTonTaiMocAsync(maHang, phieu.MocKiem, ct);
                phieu.ThemDongKiem(new DongKiem(phieu.MaPhieuKiem, maHang, ton, null, null));
            }
            await phieuKiemRepo.ThemAsync(phieu, ct);
            return phieu;
        }, ct);
    }

    public async Task<PhieuKiem> GhiDemAsync(string maTaiKhoan, string maPhieuKiem, string phienBan,
        IReadOnlyList<SoLuongDemDto> soDems, CancellationToken ct = default)
    {
        var quyenGiaiTrinh = soDems.Any(dong => dong.GiaiTrinh is not null) ? MaQuyenPos.KHO_GIAI_TRINH_KIEM : null;
        await KiemTraVaiTroAsync(maTaiKhoan, MaQuyenPos.KHO_LAP_KIEM, NhanVienKho, ct, quyenGiaiTrinh);
        if (soDems.Select(dong => dong.MaHang).Distinct(StringComparer.Ordinal).Count() != soDems.Count)
        {
            throw new BusinessRuleViolationException("Mỗi mặt hàng chỉ được ghi một lần trong yêu cầu.");
        }

        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = await KhoaPhieuKiemAsync(maPhieuKiem, ct);
            KiemTraNguoiLap(phieu, maTaiKhoan, MaQuyenPos.KHO_LAP_KIEM);
            KiemTraPhienBan(phieu, phienBan);
            if (phieu.TrangThai != TrangThaiChungTu.DangKiem)
            {
                throw new InvalidStateTransitionException("Chỉ ghi số đếm trên phiếu đang kiểm.");
            }

            var theoHang = phieu.DongKiems.ToDictionary(dong => dong.MaHang, StringComparer.Ordinal);
            var theoMaHang = soDems.OrderBy(dem => dem.MaHang, StringComparer.Ordinal).ToArray();
            foreach (var dem in theoMaHang)
            {
                if (!theoHang.ContainsKey(dem.MaHang))
                {
                    throw new EntityNotFoundException($"Mặt hàng {dem.MaHang} không có trong phiếu kiểm.");
                }
            }
            await KhoaMatHangAsync(theoMaHang.Select(dem => dem.MaHang), ct);

            var lucDem = LayLucUtc();
            foreach (var dem in theoMaHang.Where(dem => dem.SoLuongDem.HasValue))
            {
                var dong = theoHang[dem.MaHang];
                if (dem.SoLuongDem == dong.SoLuongDem && dong.LucDem.HasValue)
                {
                    continue;
                }
                if (lucDem < phieu.MocKiem || lucDem < dong.LucDem)
                {
                    throw new ConcurrencyConflictException(
                        "Thời điểm hiện tại trước mốc kiểm hoặc mốc đếm đã lưu. Hãy thử ghi số đếm lại.");
                }

                // Thẻ đã commit có thể mang mốc tương lai nếu thời gian hệ thống thay đổi.
                var moc = await bienDongRepo.LayMocGhiAsync(dong.MaHang, ct);
                if (moc.LucGhiCuoi > phieu.MocKiem
                    || await bienDongRepo.LayTonTaiMocAsync(dong.MaHang, phieu.MocKiem, ct) != dong.TonSoTaiMoc)
                {
                    throw new ConcurrencyConflictException(
                        $"Mặt hàng {dong.MaHang} đã phát sinh biến động sau khi chụp tồn. Hãy lập phiếu mới và đếm lại.");
                }
            }
            foreach (var dem in theoMaHang)
            {
                var dong = theoHang[dem.MaHang];
                var giaiTrinh = dem.GiaiTrinh is null ? dong.GiaiTrinh
                    : string.IsNullOrWhiteSpace(dem.GiaiTrinh) ? null : dem.GiaiTrinh.Trim();
                var mocDem = dem.SoLuongDem == dong.SoLuongDem ? dong.LucDem : null;
                if (dem.SoLuongDem.HasValue)
                {
                    mocDem ??= lucDem;
                }
                phieu.GhiSoLuongDem(dem.MaHang, dem.SoLuongDem, mocDem, giaiTrinh);
            }
            foreach (var dong in phieu.DongKiems)
            {
                var cu = theoHang[dong.MaHang];
                if (dong.SoLuongDem != cu.SoLuongDem || dong.GiaiTrinh != cu.GiaiTrinh || dong.LucDem != cu.LucDem)
                {
                    await phieuKiemRepo.CapNhatDemAsync(dong, ct);
                }
            }
            return phieu;
        }, ct);
    }

    public async Task<PhieuKiem> GuiDuyetAsync(string maTaiKhoan, string maPhieuKiem, string phienBan,
        CancellationToken ct = default)
    {
        await KiemTraVaiTroAsync(maTaiKhoan, MaQuyenPos.KHO_GIAI_TRINH_KIEM, NhanVienKho, ct);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = await KhoaPhieuKiemAsync(maPhieuKiem, ct);
            KiemTraNguoiLap(phieu, maTaiKhoan, MaQuyenPos.KHO_GIAI_TRINH_KIEM);
            KiemTraPhienBan(phieu, phienBan);
            await KhoaMatHangAsync(phieu.DongKiems.Select(dong => dong.MaHang).Order(StringComparer.Ordinal), ct);
            phieu.GuiDuyet();
            await phieuKiemRepo.CapNhatAsync(phieu, ct);
            return phieu;
        }, ct);
    }

    public async Task<PhieuKiem> DuyetAsync(string maTaiKhoan, string maPhieuKiem, CancellationToken ct = default)
    {
        await KiemTraVaiTroAsync(maTaiKhoan, MaQuyenPos.KHO_DUYET_DIEU_CHINH, QuanLy, ct);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = await KhoaPhieuKiemAsync(maPhieuKiem, ct);
            phieu.KiemTraCoTheDuyet();
            await KhoaMatHangAsync(phieu.DongKiems.Select(dong => dong.MaHang).Order(StringComparer.Ordinal), ct);

            var lucDuyet = LayLucUtc();
            var mocGanNhat = phieu.DongKiems.Max(dong => dong.LucDem) ?? phieu.MocKiem;
            foreach (var dong in phieu.DongKiems)
            {
                if (await bienDongRepo.LayMocDieuChinhCuoiAsync(dong.MaHang, ct) is DateTime luc && luc >= phieu.MocKiem)
                {
                    throw new ConcurrencyConflictException($"Mặt hàng {dong.MaHang} đã được điều chỉnh sau khi chụp tồn. Hãy lập phiếu mới.");
                }

                // Giao dịch ghi sau khi chụp tồn có thể mang thời điểm nghiệp vụ trước mốc kiểm.
                if (await bienDongRepo.LayTonTaiMocAsync(dong.MaHang, phieu.MocKiem, ct) != dong.TonSoTaiMoc)
                {
                    throw new ConcurrencyConflictException($"Tồn sổ tại mốc kiểm của mặt hàng {dong.MaHang} đã thay đổi. Hãy lập phiếu mới.");
                }

                // Các dòng có chênh lệch được kiểm tra tồn lại trong cùng khóa khi ghi thẻ.
                if (dong.ChenhLech == 0 && await bienDongRepo.LayTonHienHanhAsync(dong.MaHang, ct) < 0)
                {
                    throw new BusinessRuleViolationException($"Tồn hiện hành của mặt hàng {dong.MaHang} đang âm.");
                }
                if (dong.ChenhLech != 0)
                {
                    var moc = await bienDongRepo.LayMocGhiAsync(dong.MaHang, ct);
                    if (moc.LucGhiCuoi > lucDuyet)
                    {
                        throw new ConcurrencyConflictException(
                            $"Thẻ kho mới nhất của mặt hàng {dong.MaHang} sau thời điểm hiện tại. Hãy thử duyệt lại.");
                    }
                    if (moc.LucDemDuocBaoVe is DateTime lucDemDuocBaoVe && lucDemDuocBaoVe > mocGanNhat)
                    {
                        mocGanNhat = lucDemDuocBaoVe;
                    }
                    if (moc.MocKiemDangMoMoiNhat is DateTime mocKiemDangMo && mocKiemDangMo > mocGanNhat)
                    {
                        mocGanNhat = mocKiemDangMo;
                    }
                }
            }

            // Điều chỉnh nằm sau các mốc kiểm được bảo vệ, kể cả mốc đếm của chính phiếu.
            if (lucDuyet < mocGanNhat)
            {
                throw new ConcurrencyConflictException("Thời điểm hiện tại trước mốc kiểm được bảo vệ. Hãy thử duyệt lại.");
            }
            if (lucDuyet == mocGanNhat)
            {
                lucDuyet = mocGanNhat.AddMilliseconds(1);
            }
            phieu.Duyet(maTaiKhoan, lucDuyet);
            await phieuKiemRepo.CapNhatAsync(phieu, ct);
            var dieuChinhs = phieu.DongKiems.Where(dong => dong.ChenhLech.HasValue && dong.ChenhLech.Value != 0)
                .Select(dong => new YeuCauBienDongKho(dong.MaHang, LoaiBienDongKho.DieuChinhKiemKho,
                    dong.ChenhLech!.Value, lucDuyet, phieu.MaPhieuKiem)).ToArray();
            if (dieuChinhs.Length > 0)
            {
                await ghiBienDong.GhiBienDongNoiBoAsync(dieuChinhs, ct);
            }
            return phieu;
        }, ct);
    }

    public async Task<PhieuKiem> HuyAsync(string maTaiKhoan, string maPhieuKiem, CancellationToken ct = default)
    {
        var vaiTro = await KiemTraVaiTroAsync(maTaiKhoan, MaQuyenPos.KHO_HUY_KIEM, null, ct);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = await KhoaPhieuKiemAsync(maPhieuKiem, ct);
            if (vaiTro != QuanLy && (vaiTro != NhanVienKho || phieu.TrangThai == TrangThaiChungTu.ChoDuyet))
            {
                throw new UnauthorizedActionException(MaQuyenPos.KHO_HUY_KIEM);
            }
            if (vaiTro == NhanVienKho)
            {
                KiemTraNguoiLap(phieu, maTaiKhoan, MaQuyenPos.KHO_HUY_KIEM);
            }

            await KhoaMatHangAsync(phieu.DongKiems.Select(dong => dong.MaHang).Order(StringComparer.Ordinal), ct);
            phieu.Huy();
            await phieuKiemRepo.CapNhatAsync(phieu, ct);
            return phieu;
        }, ct);
    }

    public async Task<PhieuKiem> LayPhieuKiemAsync(string maTaiKhoan, string maPhieuKiem, CancellationToken ct = default)
    {
        await authorization.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.KHO_XEM_XET_KIEM, ct);
        return await phieuKiemRepo.LayTheoMaAsync(maPhieuKiem, ct)
            ?? throw new EntityNotFoundException($"Không tìm thấy phiếu kiểm {maPhieuKiem}.");
    }

    public async Task<IReadOnlyList<PhieuKiem>> LayLichSuAsync(string maTaiKhoan, CancellationToken ct = default)
    {
        await authorization.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.KHO_XEM_XET_KIEM, ct);
        return await phieuKiemRepo.LayLichSuAsync(ct);
    }

    private DateTime LayLucUtc() => MocGhiKho.ChuanHoa(_clock.GetUtcNow().UtcDateTime);

    private async Task KhoaMatHangAsync(IEnumerable<string> maHangs, CancellationToken ct)
    {
        foreach (var maHang in maHangs)
        {
            await danhMucRepo.KhoaMatHangAsync(maHang, ct);
        }
    }

    private async Task<PhieuKiem> KhoaPhieuKiemAsync(string maPhieuKiem, CancellationToken ct)
        => await phieuKiemRepo.KhoaTheoMaAsync(maPhieuKiem, ct)
            ?? throw new EntityNotFoundException($"Không tìm thấy phiếu kiểm {maPhieuKiem}.");

    private async Task<string> KiemTraVaiTroAsync(string maTaiKhoan, string maQuyen, string? vaiTro,
        CancellationToken ct, string? quyenBoSung = null)
    {
        await authorization.KiemTraQuyenAsync(maTaiKhoan, maQuyen, ct);
        if (quyenBoSung is not null)
        {
            await authorization.KiemTraQuyenAsync(maTaiKhoan, quyenBoSung, ct);
        }

        var hoSo = await taiKhoanRepo.LayHoSoDangNhapAsync(maTaiKhoan, ct);
        if (hoSo?.TaiKhoan is not { TrangThai: TrangThaiChungTu.HoatDong } taiKhoan
            || (vaiTro is not null && taiKhoan.MaVaiTro != vaiTro) || !hoSo.Quyen.Contains(maQuyen))
        {
            throw new UnauthorizedActionException(maQuyen);
        }
        if (quyenBoSung is not null && !hoSo.Quyen.Contains(quyenBoSung))
        {
            throw new UnauthorizedActionException(quyenBoSung);
        }
        return taiKhoan.MaVaiTro;
    }

    private static void KiemTraNguoiLap(PhieuKiem phieu, string maTaiKhoan, string maQuyen)
    {
        if (phieu.MaNguoiLap != maTaiKhoan)
        {
            throw new UnauthorizedActionException(maQuyen);
        }
    }

    private static void KiemTraPhienBan(PhieuKiem phieu, string phienBan)
    {
        if (!string.Equals(phienBan, phieu.PhienBan, StringComparison.Ordinal))
        {
            throw new ConcurrencyConflictException("Phiếu kiểm đã thay đổi. Hãy tải lại phiếu.");
        }
    }
}
