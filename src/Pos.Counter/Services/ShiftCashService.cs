using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Services;

public sealed class ShiftCashService(
    ICaBanRepository caRepo, IPhieuThuChiRepository thuChiRepo, IInventoryReportQueryService inventoryReports,
    IAuthorizationService auth, IUnitOfWork work) : IShiftCashService
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
        string phuongThuc, decimal tien, string lyDo, string? soChungTu, DateTime lucUtc, CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.QUY_LAP_THU_CHI, ct);
        return await work.ThucThiAsync(async () =>
        {
            var ca = await caRepo.KhoaCaMoAsync(maCa, ct);
            if (ca.MaNguoiMo != maTaiKhoan)
            {
                await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NCC_GHI_TRA_TIEN, ct);
            }
            if (lucUtc < ca.LucMo)
            {
                throw new BusinessRuleViolationException("Thời điểm lập phiếu phải từ lúc mở ca.");
            }
            var phieu = new PhieuThuChi(await thuChiRepo.SinhMaAsync(ct), maCa, maTaiKhoan, lucUtc,
                loai, nguon, phuongThuc, tien, lyDo, soChungTu);
            await thuChiRepo.ThemAsync(phieu, ct);
            return phieu;
        }, ct);
    }

    public async Task<ThongTinCaMoDto> KiemTraVaKhoaCaMoChoChiNccAsync(string maCaChi, CancellationToken ct)
        => ToDto(await caRepo.KhoaCaMoAsync(maCaChi, ct));

    public async Task<IReadOnlyList<ThongTinCaMoDto>> LayDanhSachCaDangMoAsync(CancellationToken ct)
        => await work.ThucThiAsync<IReadOnlyList<ThongTinCaMoDto>>(async () =>
            (await caRepo.LayCaDangMoAsync(ct)).Select(ToDto).ToArray(), ct);

    private static ThongTinCaMoDto ToDto(CaBan ca) => new(ca.MaCa, ca.MaNguoiMo, ca.LucMo);
}
