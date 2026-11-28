using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Services;

public sealed class ShiftCashService(
    ICaBanRepository caRepo, IPhieuThuChiRepository thuChiRepo, IInventoryReportQueryService inventoryReports,
    IAuthorizationService auth, IUnitOfWork work, ICounterAuthorizationRepository counterAuth,
    ICounterRequestRepository requests) : IShiftCashService
{
    public async Task<CaBan> MoCaAsync(string maTaiKhoan, decimal tienMatDauCa, DateTime lucUtc,
        CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.CA_MO, ct);
        return await work.ThucThiAsync(async () =>
        {
            if (await caRepo.LayCaMoAsync(maTaiKhoan, ct) is not null)
            {
                throw new ConcurrencyConflictException("Thu ngân đã có ca đang mở.");
            }
            var ca = new CaBan(await caRepo.SinhMaAsync(ct), maTaiKhoan, lucUtc, tienMatDauCa);
            // Unique filtered index chặn hai phiên cùng mở ca sau khi cả hai đã đọc.
            await caRepo.ThemAsync(ca, ct);
            return ca;
        }, ct);
    }

    public async Task<CaBan> DongCaAsync(string maCa, string maTaiKhoan, decimal tienThucDem,
        DateTime lucUtc, CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.CA_DONG, ct);
        return await work.ThucThiAsync(async () =>
        {
            var ca = await caRepo.KhoaCaMoAsync(maCa, ct);
            CounterTransaction.KiemTraChuCa(ca, maTaiKhoan);
            var ket = await caRepo.TongHopKetAsync(maCa, ct);
            var chiNcc = await inventoryReports.TinhTongChiNccTuKetTheoCaAsync(maCa, ct);
            var duKien = ca.TienMatDauCa + ket.ThuBan - ket.HoanTra + ket.ThuRieng - ket.ChiRieng - ket.DaoHuy - chiNcc;
            ca.Dong(maTaiKhoan, lucUtc, duKien, tienThucDem);
            await caRepo.CapNhatAsync(ca, ct);
            return ca;
        }, ct);
    }

    public async Task<PhieuThuChi> LapPhieuThuChiAsync(string maCa, string maTaiKhoan, string loai, string nguon,
        string phuongThuc, decimal tien, string lyDo, string? soChungTu, DateTime lucUtc,
        string maYeuCau, CancellationToken ct = default)
    {
        const string maQuyen = MaQuyenPos.QUY_LAP_THU_CHI;
        soChungTu = soChungTu?.Trim();
        var noiDung = new { maCa, loai, nguon, phuongThuc, Tien = CounterRequest.ChuanHoaTien(tien), lyDo, soChungTu };
        return await work.ThucThiAsync(async () =>
        {
            var vaiTro = await counterAuth.BatBuocCoQuyenAsync(maTaiKhoan, maQuyen, ct);
            var ca = await caRepo.KhoaTheoMaAsync(maCa, ct)
                ?? throw new EntityNotFoundException("Không tìm thấy ca bán.");
            if (ca.MaNguoiMo != maTaiKhoan && vaiTro != "QuanLy")
            {
                throw new UnauthorizedActionException(maQuyen);
            }

            return await requests.GhiMotLanAsync("LapPhieuThuChi", maTaiKhoan, maYeuCau,
                noiDung, () => GhiAsync(ca), p => p, p => p, ct);
        }, ct);

        async Task<PhieuThuChi> GhiAsync(CaBan ca)
        {
            if (ca.TrangThai != TrangThaiChungTu.DangMo)
            {
                throw new InvalidStateTransitionException("Ca bán đã đóng.");
            }
            if (lucUtc < ca.LucMo)
            {
                throw new BusinessRuleViolationException("Thời điểm lập phiếu phải từ lúc mở ca.");
            }
            var phieu = new PhieuThuChi(await thuChiRepo.SinhMaAsync(ct), maCa, maTaiKhoan, lucUtc,
                loai, nguon, phuongThuc, tien, lyDo, soChungTu);
            if (phieu.SoChungTuKemTheo is { } so && await thuChiRepo.DaHachToanAsync(so, ct))
            {
                throw new BusinessRuleViolationException("Chứng từ này đã được hạch toán. Không thể ghi lại khoản thu chi.");
            }
            await thuChiRepo.ThemAsync(phieu, ct);
            return phieu;
        }
    }

    public async Task<ThongTinCaMoDto> KiemTraVaKhoaCaMoChoChiNccAsync(string maCaChi, CancellationToken ct)
        => ToDto(await caRepo.KhoaCaMoAsync(maCaChi, ct));

    public async Task<IReadOnlyList<ThongTinCaMoDto>> LayDanhSachCaDangMoAsync(CancellationToken ct)
        => await work.ThucThiAsync<IReadOnlyList<ThongTinCaMoDto>>(async () =>
            (await caRepo.LayCaDangMoAsync(ct)).Select(ToDto).ToArray(), ct);

    private static ThongTinCaMoDto ToDto(CaBan ca) => new(ca.MaCa, ca.MaNguoiMo, ca.LucMo);
}
