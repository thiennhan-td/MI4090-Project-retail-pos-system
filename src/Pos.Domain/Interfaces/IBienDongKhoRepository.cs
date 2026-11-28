using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IBienDongKhoRepository
{
    Task<BienDongKho?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(BienDongKho entity, CancellationToken ct);
    Task<BienDongKho?> LayTheoKhoaSuKienAsync(string khoaSuKien, CancellationToken ct);
    Task<int> LayTonHienHanhAsync(string maHang, CancellationToken ct);
}
