using Pos.Domain.Exceptions;

namespace Pos.Inventory.Factories;

internal static class MocGhiKho
{
    public static DateTime ChuanHoa(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleViolationException("Thời điểm thẻ kho phải theo UTC.");
        }

        // Lưu cùng độ chính xác datetime2(3) của SQL trước khi so sánh yêu cầu gửi lại.
        const long miliGiay = TimeSpan.TicksPerMillisecond;
        var ticks = (utc.Ticks + miliGiay / 2) / miliGiay * miliGiay;
        if (ticks > DateTime.MaxValue.Ticks)
        {
            throw new BusinessRuleViolationException("Thời điểm thẻ kho vượt phạm vi lưu trữ.");
        }

        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
