using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IPhieuTraRepository
{
    Task<string> SinhMaAsync(CancellationToken ct);
    Task<string> SinhMaDongAsync(CancellationToken ct);
    Task<string> SinhMaHoanAsync(CancellationToken ct);
    Task<bool> CoPhieuTraHieuLucAsync(string maHoaDon, CancellationToken ct);
    Task<PhieuTra?> LayTheoMaAsync(string ma, CancellationToken ct);
    /// <summary>Khóa đầu phiếu trong giao dịch hiện tại trước khi kiểm tra trạng thái để xác nhận hoặc xóa nháp.</summary>
    Task<PhieuTra?> KhoaTheoMaAsync(string maPhieuTra, CancellationToken ct);
    Task ThemAsync(PhieuTra entity, CancellationToken ct);
    Task CapNhatAsync(PhieuTra entity, CancellationToken ct);
    Task XoaNhapAsync(string maPhieuTra, CancellationToken ct);
    Task<int> TinhLuongDaTraAsync(string maDongBan, CancellationToken ct);
}
