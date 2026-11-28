using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;

namespace Pos.Inventory.Decorators;

/// <summary>
/// Chặng 3 cung cấp mã tài khoản đã xác thực từ phiên hoặc danh tính của request hiện tại.
/// Giá trị phải ổn định trong phạm vi request và không lấy từ form hay DTO biến động kho.
/// </summary>
public interface IInventoryCallerContext
{
    string? MaTaiKhoan { get; }
}

public sealed record NhatKyGoiTheKho(string? MaTaiKhoan, string LoaiSuKien, string MaChungTu,
    DateTimeOffset LucGoiUtc, KetQuaGhiTheKhoDto? KetQua, Exception? Loi);

/// <summary>Ghi kết quả gọi thẻ kho. Việc commit do service điều phối chứng từ thực hiện.</summary>
public interface IInventoryAuditSink
{
    void Ghi(NhatKyGoiTheKho nhatKy);
}

public abstract class InventoryServiceDecoratorBase(IInventoryService inner) : IInventoryService
{
    public virtual Task<KetQuaGhiTheKhoDto> GhiNhanXuatBanAsync(YeuCauXuatBanDto req, CancellationToken ct)
        => inner.GhiNhanXuatBanAsync(req, ct);

    public virtual Task<KetQuaGhiTheKhoDto> DaoXuatBanAsync(YeuCauDaoXuatBanDto req, CancellationToken ct)
        => inner.DaoXuatBanAsync(req, ct);

    public virtual Task<KetQuaGhiTheKhoDto> GhiNhanNhapTraHangAsync(YeuCauNhapTraDto req, CancellationToken ct)
        => inner.GhiNhanNhapTraHangAsync(req, ct);

    public Task<int> LayTonHienHanhAsync(string maHang, CancellationToken ct)
        => inner.LayTonHienHanhAsync(maHang, ct);
}

public sealed class AuthorizedInventoryServiceDecorator(IInventoryService inner, IInventoryCallerContext caller,
    IAuthorizationService authorization) : InventoryServiceDecoratorBase(inner)
{
    public override async Task<KetQuaGhiTheKhoDto> GhiNhanXuatBanAsync(YeuCauXuatBanDto req, CancellationToken ct)
    {
        await KiemTraQuyenAsync(MaQuyenPos.BAN_XAC_NHAN, ct);
        return await base.GhiNhanXuatBanAsync(req, ct);
    }

    public override async Task<KetQuaGhiTheKhoDto> DaoXuatBanAsync(YeuCauDaoXuatBanDto req, CancellationToken ct)
    {
        await KiemTraQuyenAsync(MaQuyenPos.BAN_HUY_HD_XAC_NHAN, ct);
        return await base.DaoXuatBanAsync(req, ct);
    }

    public override async Task<KetQuaGhiTheKhoDto> GhiNhanNhapTraHangAsync(YeuCauNhapTraDto req, CancellationToken ct)
    {
        await KiemTraQuyenAsync(MaQuyenPos.TRA_XAC_NHAN, ct);
        return await base.GhiNhanNhapTraHangAsync(req, ct);
    }

    private Task KiemTraQuyenAsync(string maQuyen, CancellationToken ct)
    {
        var maTaiKhoan = caller.MaTaiKhoan;
        if (string.IsNullOrWhiteSpace(maTaiKhoan))
        {
            throw new UnauthorizedActionException(maQuyen);
        }
        return authorization.KiemTraQuyenAsync(maTaiKhoan, maQuyen, ct);
    }
}

/// <summary>
/// Chặng 3 đăng ký IInventoryService với lớp kiểm toán bọc lớp kiểm quyền, lớp kiểm quyền bọc InventoryLedgerService.
/// Dùng cùng IInventoryCallerContext cho hai lớp và cùng SqlUnitOfWork với service chứng từ.
/// </summary>
public sealed class AuditingInventoryServiceDecorator(IInventoryService inner, IInventoryCallerContext caller,
    IInventoryAuditSink audit, TimeProvider? clock = null) : InventoryServiceDecoratorBase(inner)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public override Task<KetQuaGhiTheKhoDto> GhiNhanXuatBanAsync(YeuCauXuatBanDto req, CancellationToken ct)
        => GhiNhatKyAsync(LoaiBienDongKho.XuatBan, req.MaHoaDon, () => base.GhiNhanXuatBanAsync(req, ct));

    public override Task<KetQuaGhiTheKhoDto> DaoXuatBanAsync(YeuCauDaoXuatBanDto req, CancellationToken ct)
        => GhiNhatKyAsync(LoaiBienDongKho.DaoXuatBan, req.MaHoaDon, () => base.DaoXuatBanAsync(req, ct));

    public override Task<KetQuaGhiTheKhoDto> GhiNhanNhapTraHangAsync(YeuCauNhapTraDto req, CancellationToken ct)
        => GhiNhatKyAsync(LoaiBienDongKho.NhapTraHang, req.MaPhieuTra, () => base.GhiNhanNhapTraHangAsync(req, ct));

    private async Task<KetQuaGhiTheKhoDto> GhiNhatKyAsync(string loaiSuKien, string maChungTu,
        Func<Task<KetQuaGhiTheKhoDto>> ghiTheKho)
    {
        var maTaiKhoan = caller.MaTaiKhoan;
        var lucGoi = _clock.GetUtcNow();
        KetQuaGhiTheKhoDto ketQua;
        try
        {
            ketQua = await ghiTheKho();
        }
        catch (Exception loi)
        {
            try
            {
                audit.Ghi(new NhatKyGoiTheKho(maTaiKhoan, loaiSuKien, maChungTu, lucGoi, null, loi));
            }
            catch (Exception loiKiemToan)
            {
                loi.Data["LoiKiemToan"] = loiKiemToan;
            }
            throw;
        }

        audit.Ghi(new NhatKyGoiTheKho(maTaiKhoan, loaiSuKien, maChungTu, lucGoi, ketQua, null));
        return ketQua;
    }
}
