namespace Pos.Web.Demo;

internal static class GioPos
{
    private static readonly TimeSpan MuiGio = TimeSpan.FromHours(7);

    internal static DateTime Now => Local(DateTimeOffset.UtcNow);
    internal static DateTime Local(DateTimeOffset luc) => luc.ToOffset(MuiGio).DateTime;
}
