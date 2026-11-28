using System.Security.Cryptography;
using System.Text.Json;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Services;

internal static class CounterRequest
{
    public static async Task<T> GhiMotLanAsync<T, TSnapshot>(this ICounterRequestRepository repo,
        string thaoTac, string actor, string maYeuCau, object noiDung, Func<Task<T>> ghi,
        Func<T, TSnapshot> chup, Func<TSnapshot, T> khoiPhuc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(maYeuCau) || maYeuCau.Length > 128)
        {
            throw new BusinessRuleViolationException("Mã yêu cầu phải có từ 1 đến 128 ký tự.");
        }

        var khoa = Bam(new { thaoTac, actor, maYeuCau });
        var dauVanTay = Bam(noiDung);
        var daGhi = await repo.KhoaVaDocAsync(khoa, ct);
        if (daGhi is not null)
        {
            if (daGhi.DauVanTay != dauVanTay)
            {
                throw new DuplicateRequestException("Mã yêu cầu đã dùng cho nội dung khác.");
            }
            return khoiPhuc(JsonSerializer.Deserialize<TSnapshot>(daGhi.NoiDungKetQua)!);
        }

        var ketQua = await ghi();
        await repo.ThemAsync(khoa, new(dauVanTay, JsonSerializer.Serialize(chup(ketQua))), ct);
        return ketQua;
    }

    internal static string? ChuanHoaTien(decimal? tien)
        => tien?.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);

    internal static string Bam<T>(T noiDung) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(noiDung)));
}

internal sealed record HoaDonDaGhi(string MaHoaDon, string MaCa, string MaNguoiLap, DateTime LucLap,
    string TrangThai, DateTime? LucXacNhan, DongBan[] DongBan, ThanhToanBan[] ThanhToan)
{
    public static HoaDonDaGhi Chup(HoaDon h) => new(h.MaHoaDon, h.MaCa, h.MaNguoiLap, h.LucLap,
        h.TrangThai, h.LucXacNhan, h.DongBans.ToArray(), h.ThanhToanBans.ToArray());

    public HoaDon KhoiPhuc() => HoaDon.KhoiPhuc(MaHoaDon, MaCa, MaNguoiLap, LucLap,
        TrangThai, LucXacNhan, null, null, null, null, DongBan, ThanhToan);
}
