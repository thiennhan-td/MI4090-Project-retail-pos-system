namespace Pos.Domain.Interfaces;

public sealed record KetQuaYeuCauQuay(string DauVanTay, string NoiDungKetQua);

public interface ICounterRequestRepository
{
    Task<KetQuaYeuCauQuay?> KhoaVaDocAsync(string khoa, CancellationToken ct);
    Task ThemAsync(string khoa, KetQuaYeuCauQuay ketQua, CancellationToken ct);
}
