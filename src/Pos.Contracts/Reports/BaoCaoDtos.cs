namespace Pos.Contracts.Reports;

public record BoLocBaoCaoDto(DateOnly TuNgay, DateOnly DenNgay);
public sealed record KhoangBaoCaoUtcDto(DateTime TuUtc, DateTime DenUtc);
public sealed record KetQuaBaoCaoDto(string TenBaoCao, IReadOnlyList<string> Cot,
    IReadOnlyList<IReadOnlyList<object?>> Dong);
