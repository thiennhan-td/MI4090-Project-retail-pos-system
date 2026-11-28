using Microsoft.Data.SqlClient;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Counter.CounterSql;

namespace Pos.Infrastructure.Repositories.Counter;

public sealed class SqlCaBanRepository(SqlUnitOfWork work) : ICaBanRepository
{
    public Task<string> SinhMaAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('CA-', NEXT VALUE FOR dbo.Seq_CaBan);", ct);

    public Task<CaBan?> LayTheoMaAsync(string ma, CancellationToken ct)
        => DocMot("""
            SELECT maCa, maNguoiMo, lucMo, tienMatDauCa, lucDong, maNguoiChot, tienDuKienKhiChot, tienThucDem
            FROM dbo.CaBan WHERE maCa = @ma;
            """, Ma("@ma", ma), ct);

    public Task<CaBan?> KhoaTheoMaAsync(string maCa, CancellationToken ct)
        => DocMot("""
            SELECT maCa, maNguoiMo, lucMo, tienMatDauCa, lucDong, maNguoiChot, tienDuKienKhiChot, tienThucDem
            FROM dbo.CaBan WITH (UPDLOCK, HOLDLOCK) WHERE maCa = @ma;
            """, Ma("@ma", maCa), ct);

    public Task<CaBan?> LayCaMoAsync(string maNguoiMo, CancellationToken ct)
        => DocMot("""
            SELECT maCa, maNguoiMo, lucMo, tienMatDauCa, lucDong, maNguoiChot, tienDuKienKhiChot, tienThucDem
            FROM dbo.CaBan WHERE maNguoiMo = @ma AND trangThai = 'DangMo';
            """, Ma("@ma", maNguoiMo), ct);

    public Task<IReadOnlyList<CaBan>> LayCaDangMoAsync(CancellationToken ct)
        => DocNhieu("""
            SELECT maCa, maNguoiMo, lucMo, tienMatDauCa, lucDong, maNguoiChot, tienDuKienKhiChot, tienThucDem
            FROM dbo.CaBan WHERE trangThai = 'DangMo' ORDER BY maCa;
            """, ct);

    public Task<IReadOnlyList<CaBan>> LayTrongKyAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct)
        => DocNhieu("""
            SELECT maCa, maNguoiMo, lucMo, tienMatDauCa, lucDong, maNguoiChot, tienDuKienKhiChot, tienThucDem
            FROM dbo.CaBan WHERE lucMo < @den AND (lucDong IS NULL OR lucDong >= @tu) ORDER BY lucMo, maCa;
            """, ct, Luc("@tu", tuUtc), Luc("@den", denUtc));

    public async Task ThemAsync(CaBan entity, CancellationToken ct)
    {
        try
        {
            await work.Ghi("""
                INSERT dbo.CaBan(maCa, maNguoiMo, lucMo, tienMatDauCa)
                VALUES (@ma, @nguoi, @luc, @tien);
                """, ct, Ma("@ma", entity.MaCa), Ma("@nguoi", entity.MaNguoiMo),
                Luc("@luc", entity.LucMo), Tien("@tien", entity.TienMatDauCa));
        }
        catch (SqlException ex) when (ex.Number == 2601 && ex.Message.Contains("UX_CaBan_NguoiMo_DangMo", StringComparison.Ordinal))
        {
            throw new ConcurrencyConflictException("Thu ngân đã có ca đang mở.");
        }
    }

    public Task CapNhatAsync(CaBan entity, CancellationToken ct) => work.SuaMot("""
        UPDATE dbo.CaBan SET trangThai = @trangThai, maNguoiChot = @nguoi, lucDong = @luc,
            tienDuKienKhiChot = @duKien, tienThucDem = @thucDem
        WHERE maCa = @ma AND trangThai = 'DangMo';
        """, ct, Ma("@ma", entity.MaCa), Ma("@trangThai", entity.TrangThai), Ma("@nguoi", entity.MaNguoiChot),
        Luc("@luc", entity.LucDong), Tien("@duKien", entity.TienDuKienKhiChot), Tien("@thucDem", entity.TienThucDem));

    public async Task<SoQuyCa> TongHopKetAsync(string maCa, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT
              COALESCE((SELECT SUM(t.soTienThanhToan) FROM dbo.ThanhToanBan t JOIN dbo.HoaDon h ON h.maHoaDon=t.maHoaDon
                  WHERE h.maCa=@ma AND h.lucXacNhan IS NOT NULL AND t.phuongThuc='TienMat'),0),
              COALESCE((SELECT SUM(k.soTienHoan) FROM dbo.KhoanHoan k JOIN dbo.PhieuTra p ON p.maPhieuTra=k.maPhieuTra
                  WHERE p.maCaNhan=@ma AND p.trangThai='DaXacNhan' AND k.phuongThuc='TienMat'),0),
              COALESCE((SELECT SUM(soTien) FROM dbo.PhieuThuChi WHERE maCa=@ma AND nguonQuy='KetCa'
                  AND phuongThuc='TienMat' AND loaiThuChi='Thu'),0),
              COALESCE((SELECT SUM(soTien) FROM dbo.PhieuThuChi WHERE maCa=@ma AND nguonQuy='KetCa'
                  AND phuongThuc='TienMat' AND loaiThuChi='Chi'),0),
              COALESCE((SELECT SUM(t.soTienThanhToan) FROM dbo.ThanhToanBan t JOIN dbo.HoaDon h ON h.maHoaDon=t.maHoaDon
                  WHERE h.maCaDao=@ma AND h.trangThai='DaHuy' AND h.lucXacNhan IS NOT NULL AND t.phuongThuc='TienMat'),0);
            """, Ma("@ma", maCa));
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new(reader.GetDecimal(0), reader.GetDecimal(1), reader.GetDecimal(2), reader.GetDecimal(3), reader.GetDecimal(4));
    }

    private async Task<CaBan?> DocMot(string sql, SqlParameter parameter, CancellationToken ct)
        => (await DocNhieu(sql, ct, parameter)).SingleOrDefault();

    private async Task<IReadOnlyList<CaBan>> DocNhieu(string sql, CancellationToken ct, params SqlParameter[] parameters)
    {
        await using var command = work.Lenh(sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<CaBan>();
        while (await reader.ReadAsync(ct))
        {
            var ca = new CaBan(reader.GetString(0), reader.GetString(1), reader.Utc(2), reader.GetDecimal(3));
            if (reader.LucNull(4) is { } lucDong)
            {
                ca.Dong(reader.GetString(5), lucDong, reader.GetDecimal(6), reader.GetDecimal(7));
            }
            rows.Add(ca);
        }
        return rows;
    }
}
