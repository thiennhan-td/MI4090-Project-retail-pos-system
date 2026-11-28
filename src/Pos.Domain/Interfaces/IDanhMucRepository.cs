using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IDanhMucRepository
{
    Task<MatHang?> LayMatHangAsync(string maHang, CancellationToken ct);
    Task KhoaMatHangAsync(string maHang, CancellationToken ct);
    Task<IReadOnlyList<MatHang>> TimMatHangAsync(string? tuKhoa, string? maNhom, CancellationToken ct);
    Task<IReadOnlyList<NhomHang>> LayNhomHangAsync(CancellationToken ct);
    Task<NhaCungCap?> LayNhaCungCapAsync(string maNhaCungCap, CancellationToken ct);
}
