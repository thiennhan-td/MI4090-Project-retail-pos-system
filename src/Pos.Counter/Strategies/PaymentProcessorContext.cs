using Pos.Contracts;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

namespace Pos.Counter.Strategies;

public sealed class PaymentProcessorContext(IEnumerable<IPhuongThucThanhToanStrategy> strategies)
{
    private readonly IReadOnlyDictionary<string, IPhuongThucThanhToanStrategy> _strategies =
        strategies.ToDictionary(item => item.LoaiPhuongThuc, StringComparer.Ordinal);

    public ThanhToanBan TaoThanhToan(string ma, string maHoaDon, string phuongThuc, decimal soTien, decimal? tienKhachDua)
    {
        LayStrategy(phuongThuc).KiemTraHopLe(soTien, tienKhachDua);
        return new(ma, maHoaDon, phuongThuc, soTien, tienKhachDua);
    }

    public decimal TinhTienThua(ThanhToanBan thanhToan) => LayStrategy(thanhToan.PhuongThuc)
        .TinhTienThua(thanhToan.SoTienThanhToan, thanhToan.TienKhachDua);

    public decimal TinhBienDongKetCa(string phuongThuc, decimal soTien, bool laKhoanThu)
        => LayStrategy(phuongThuc).TinhBienDongKetCa(soTien, laKhoanThu);

    public void KiemTraThanhToan(ThanhToanBan thanhToan) => LayStrategy(thanhToan.PhuongThuc)
        .KiemTraHopLe(thanhToan.SoTienThanhToan, thanhToan.TienKhachDua);

    private IPhuongThucThanhToanStrategy LayStrategy(string phuongThuc)
        => _strategies.TryGetValue(phuongThuc, out var strategy)
            ? strategy
            : throw new BusinessRuleViolationException("Phương thức thanh toán không được hỗ trợ.");
}
