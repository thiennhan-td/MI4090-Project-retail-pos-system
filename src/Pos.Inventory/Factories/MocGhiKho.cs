using Pos.Domain.Exceptions;

namespace Pos.Inventory.Factories;

internal static class MocGhiKho
{
    public static DateTime ChuanHoa(DateTime utc)
    {
        KiemTraUtc(utc);

        // SqlClient bỏ phần nhỏ hơn mili giây khi gửi tham số datetime2(3).
        const long miliGiay = TimeSpan.TicksPerMillisecond;
        return new DateTime(utc.Ticks - utc.Ticks % miliGiay, DateTimeKind.Utc);
    }

    public static void KiemTraUtc(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleViolationException("Thời điểm thẻ kho phải theo UTC.");
        }
    }
}
