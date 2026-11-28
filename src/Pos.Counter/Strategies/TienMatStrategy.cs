using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;

namespace Pos.Counter.Strategies;

public sealed class TienMatStrategy : IPhuongThucThanhToanStrategy
{
    public string LoaiPhuongThuc => PhuongThucThanhToan.TienMat;

    public void KiemTraHopLe(decimal soTien, decimal? tienKhachDua)
    {
        if (!TienThanhToan.HopLe(soTien) || tienKhachDua is not { } tien ||
            !TienThanhToan.HopLe(tien) || tien < soTien)
        {
            throw new BusinessRuleViolationException("Tiền khách đưa phải đủ và là số nguyên VND.");
        }
    }

    public decimal TinhTienThua(decimal soTien, decimal? tienKhachDua)
    {
        KiemTraHopLe(soTien, tienKhachDua);
        return tienKhachDua!.Value - soTien;
    }

    public decimal TinhBienDongKetCa(decimal soTien, bool laKhoanThu) => laKhoanThu ? soTien : -soTien;
}

internal static class TienThanhToan
{
    public static bool HopLe(decimal value) => value > 0 && value <= 9999999999999999999m
        && decimal.Truncate(value) == value;
}
