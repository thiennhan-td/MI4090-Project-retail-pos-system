using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;

namespace Pos.Counter.Strategies;

public sealed class ChuyenKhoanStrategy : IPhuongThucThanhToanStrategy
{
    public string LoaiPhuongThuc => PhuongThucThanhToan.ChuyenKhoan;

    public void KiemTraHopLe(decimal soTien, decimal? tienKhachDua)
    {
        if (!TienThanhToan.HopLe(soTien) || tienKhachDua is not null)
        {
            throw new BusinessRuleViolationException("Khoản chuyển khoản phải là số nguyên VND dương và không có tiền khách đưa.");
        }
    }

    public decimal TinhTienThua(decimal soTien, decimal? tienKhachDua)
    {
        KiemTraHopLe(soTien, tienKhachDua);
        return 0m;
    }

    public decimal TinhBienDongKetCa(decimal soTien, bool laKhoanThu) => 0m;
}
