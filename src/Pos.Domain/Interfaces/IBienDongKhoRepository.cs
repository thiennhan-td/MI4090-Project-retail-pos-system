using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IBienDongKhoRepository
{
    bool CoGiaoDich { get; }
    Task<string> SinhMaAsync(CancellationToken ct);
    Task<BienDongKho?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(BienDongKho entity, CancellationToken ct);
    Task<BienDongKho?> LayTheoKhoaSuKienAsync(string khoaSuKien, CancellationToken ct);
    Task<NguonBienDongKho?> LayNguonAsync(BienDongKho bienDong, CancellationToken ct);
    Task<IReadOnlyList<NguonBienDongKho>> LayCacNguonAsync(string loaiSuKien, string maChungTu, CancellationToken ct);
    Task<int> LayTonHienHanhAsync(string maHang, CancellationToken ct);
    Task<int> LayTonTaiMocAsync(string maHang, DateTime mocUtc, CancellationToken ct);
    Task<int> LayTonTruocMocAsync(string maHang, DateTime mocUtc, CancellationToken ct);
    Task<IReadOnlyList<BienDongKho>> LayTrongKyAsync(string maHang, DateTime tuUtc, DateTime denUtc, CancellationToken ct);
    Task<DateTime?> LayMocDieuChinhCuoiAsync(string maHang, CancellationToken ct);
}

public sealed record NguonBienDongKho(string MaNguon, string MaHang, string MaChungTu, int SoLuongThayDoi,
    bool CoHieuLuc, DateTime? LucHieuLucUtc);
