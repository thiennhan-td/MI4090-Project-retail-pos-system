using Pos.Domain.Entities;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Counter.CounterSql;

namespace Pos.Infrastructure.Repositories.Counter;

public sealed class SqlPhieuThuChiRepository(SqlUnitOfWork work) : IPhieuThuChiRepository
{
    public Task<string> SinhMaAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('TC-', NEXT VALUE FOR dbo.Seq_PhieuThuChi);", ct);

    public async Task<PhieuThuChi?> LayTheoMaAsync(string ma, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT maPhieuThuChi, maCa, maNguoiLap, lucGhi, loaiThuChi, nguonQuy, phuongThuc, soTien, lyDo, soChungTuKemTheo
            FROM dbo.PhieuThuChi WHERE maPhieuThuChi = @ma;
            """, Ma("@ma", ma));
        await using var r = await command.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct)
            ? new(r.GetString(0), r.GetString(1), r.GetString(2), r.Utc(3), r.GetString(4), r.GetString(5),
                r.GetString(6), r.GetDecimal(7), r.GetString(8), r.ChuNull(9))
            : null;
    }

    public async Task<bool> DaHachToanAsync(string soChungTu, CancellationToken ct)
    {
        if (soChungTu.Length > 32)
        {
            return false;
        }

        await using var command = work.Lenh("""
            SELECT CAST(CASE WHEN
                EXISTS (SELECT 1 FROM dbo.HoaDon WITH (HOLDLOCK)
                    WHERE maHoaDon=@so AND lucXacNhan IS NOT NULL)
                OR EXISTS (SELECT 1 FROM dbo.PhieuTra WITH (HOLDLOCK)
                    WHERE maPhieuTra=@so AND lucXacNhan IS NOT NULL)
                OR EXISTS (SELECT 1 FROM dbo.TraNhaCungCap WITH (HOLDLOCK)
                    WHERE maKhoanTra=@so OR maPhieuNhap=@so)
                THEN 1 ELSE 0 END AS bit);
            """, Ma("@so", soChungTu));
        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }

    public Task ThemAsync(PhieuThuChi entity, CancellationToken ct) => work.Ghi("""
        INSERT dbo.PhieuThuChi(maPhieuThuChi, maCa, maNguoiLap, lucGhi, loaiThuChi, nguonQuy,
            phuongThuc, soTien, lyDo, soChungTuKemTheo)
        VALUES(@ma, @ca, @nguoi, @luc, @loai, @nguon, @pt, @tien, @lyDo, @kemTheo);
        """, ct, Ma("@ma", entity.MaPhieuThuChi), Ma("@ca", entity.MaCa), Ma("@nguoi", entity.MaNguoiLap),
        Luc("@luc", entity.LucGhi), Ma("@loai", entity.LoaiThuChi), Ma("@nguon", entity.NguonQuy),
        Ma("@pt", entity.PhuongThuc), Tien("@tien", entity.SoTien), Chu("@lyDo", entity.LyDo, 500),
        Chu("@kemTheo", entity.SoChungTuKemTheo, 64));
}
