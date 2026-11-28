namespace Pos.Contracts.Reports;

public interface IBaoCaoNghiepVu
{
    string MaQuyenYeuCau { get; }
    string TenBaoCao { get; }
    Task<KetQuaBaoCaoDto> ThucThiAsync(BoLocBaoCaoDto boLoc, string maTaiKhoan, CancellationToken ct);
}
