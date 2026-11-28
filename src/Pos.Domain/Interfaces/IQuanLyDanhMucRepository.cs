using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IQuanLyDanhMucRepository : IDanhMucRepository
{
    Task<NhomHang?> KhoaNhomHangAsync(string maNhom, CancellationToken ct);
    Task<bool> CoMatHangDangHoatDongAsync(string maNhom, CancellationToken ct);
    Task<IReadOnlyList<NhaCungCap>> LayNhaCungCapAsync(CancellationToken ct);
    Task<string> SinhMaNhomAsync(CancellationToken ct);
    Task<string> SinhMaHangAsync(CancellationToken ct);
    Task<string> SinhMaNhaCungCapAsync(CancellationToken ct);
    Task ThemNhomAsync(NhomHang nhom, CancellationToken ct);
    Task CapNhatNhomAsync(NhomHang nhom, CancellationToken ct);
    Task ThemHangAsync(MatHang hang, CancellationToken ct);
    Task CapNhatHangAsync(MatHang hang, CancellationToken ct);
    Task ThemNhaCungCapAsync(NhaCungCap nhaCungCap, CancellationToken ct);
    Task CapNhatNhaCungCapAsync(NhaCungCap nhaCungCap, CancellationToken ct);
}
