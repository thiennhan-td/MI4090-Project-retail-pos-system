namespace Pos.Contracts;

public interface IShiftCashService
{
    // Caller keeps the shared transaction open until the supplier payment is recorded.
    Task<ThongTinCaMoDto> KiemTraVaKhoaCaMoChoChiNccAsync(string maCaChi, CancellationToken ct);
    Task<IReadOnlyList<ThongTinCaMoDto>> LayDanhSachCaDangMoAsync(CancellationToken ct);
}
