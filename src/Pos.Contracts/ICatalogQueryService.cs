namespace Pos.Contracts;

public interface ICatalogQueryService
{
    Task<MatHangBanDto?> LayMatHangDangHoatDongAsync(string maHang, CancellationToken ct);
    Task<IReadOnlyList<MatHangBanDto>> TimKiemMatHangBanAsync(string? tuKhoa, string? maNhom, CancellationToken ct);
    Task<IReadOnlyList<NhomHangDto>> LayNhomHangHoatDongAsync(CancellationToken ct);
}
