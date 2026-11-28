namespace Pos.Domain.Exceptions;

public sealed class InsufficientStockException(string maHang, int tonKhaDung, int soLuongYeuCau)
    : PosDomainException($"Mặt hàng {maHang} còn {tonKhaDung}, yêu cầu {soLuongYeuCau}.")
{
    public string MaHang { get; } = maHang;
    public int TonKhaDung { get; } = tonKhaDung;
    public int SoLuongYeuCau { get; } = soLuongYeuCau;
}
