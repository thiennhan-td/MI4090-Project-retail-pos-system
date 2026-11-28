using Pos.Domain.Constants;
using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

internal static class QuyTac
{
    public static void Dat(bool dung, string thongBao)
    {
        if (!dung)
        {
            throw new BusinessRuleViolationException(thongBao);
        }
    }

    public static void TrangThai(bool dung)
    {
        if (!dung)
        {
            throw new InvalidStateTransitionException("Thao tác không hợp lệ ở trạng thái hiện tại.");
        }
    }

    public static string Chuoi(string giaTri, int toiDa)
    {
        Dat(!string.IsNullOrWhiteSpace(giaTri) && giaTri.Length <= toiDa, "Chuỗi rỗng hoặc quá dài.");
        return giaTri;
    }

    public static decimal TienCoDau(decimal giaTri)
    {
        Dat(decimal.Truncate(giaTri) == giaTri && decimal.Abs(giaTri) <= 9999999999999999999m,
            "Số tiền phải là số nguyên VND trong phạm vi decimal(19,0).");
        return giaTri;
    }

    public static decimal Tien(decimal giaTri)
    {
        Dat(giaTri >= 0, "Số tiền không được âm.");
        return TienCoDau(giaTri);
    }

    public static decimal TienDuong(decimal giaTri)
    {
        Dat(giaTri > 0, "Số tiền phải lớn hơn 0.");
        return TienCoDau(giaTri);
    }

    public static int Luong(int giaTri)
    {
        Dat(giaTri >= 0, "Số lượng không được âm.");
        return giaTri;
    }

    public static int LuongDuong(int giaTri)
    {
        Dat(giaTri > 0, "Số lượng phải lớn hơn 0.");
        return giaTri;
    }

    public static DateTime Utc(DateTime luc, DateTime? tu = null)
    {
        Dat(luc.Kind == DateTimeKind.Utc && (tu is null || luc >= tu),
            "Thời điểm phải theo UTC và đúng thứ tự nghiệp vụ.");
        return luc;
    }

    public static string PhuongThuc(string giaTri)
    {
        Dat(giaTri is PhuongThucThanhToan.TienMat or PhuongThucThanhToan.ChuyenKhoan,
            "Phương thức thanh toán không hợp lệ.");
        return giaTri;
    }
}
