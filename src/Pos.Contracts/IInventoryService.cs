namespace Pos.Contracts;

public interface IInventoryService
{
    Task<KetQuaGhiTheKhoDto> GhiNhanXuatBanAsync(YeuCauXuatBanDto req, CancellationToken ct);
    Task<KetQuaGhiTheKhoDto> DaoXuatBanAsync(YeuCauDaoXuatBanDto req, CancellationToken ct);
    Task<KetQuaGhiTheKhoDto> GhiNhanNhapTraHangAsync(YeuCauNhapTraDto req, CancellationToken ct);
    Task<int> LayTonHienHanhAsync(string maHang, CancellationToken ct);

    Task<KetQuaGhiTheKhoDto> GhiNhanDaoHuyBanAsync(YeuCauDaoHuyBanDto req, CancellationToken ct)
        => DaoXuatBanAsync(new(req.MaHoaDon, req.LucHuyUtc, req.DongHang), ct);

    Task<KetQuaGhiTheKhoDto> GhiNhanNhapTraAsync(YeuCauNhapTraDto req, CancellationToken ct)
        => GhiNhanNhapTraHangAsync(req, ct);

    Task<int> LayTonKhaDungAsync(string maHang, CancellationToken ct)
        => LayTonHienHanhAsync(maHang, ct);
}
