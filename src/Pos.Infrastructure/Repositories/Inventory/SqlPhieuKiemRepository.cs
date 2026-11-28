using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Entities;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Inventory.InventorySql;

namespace Pos.Infrastructure.Repositories.Inventory;

public sealed class SqlPhieuKiemRepository(SqlUnitOfWork work, SqlConnectionFactory factory) : IPhieuKiemRepository
{
    private const string CotPhieu = "p.maPhieuKiem, p.maNguoiLap, p.mocKiem, p.trangThai, p.maNguoiDuyet, p.lucHoanTat";
    private const string CotDong = "d.maHang, d.tonSoTaiMoc, d.soLuongDem, d.giaiTrinh";

    public Task<string> SinhMaAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('PK-', NEXT VALUE FOR dbo.Seq_PhieuKiem);", ct);

    public async Task<PhieuKiem?> LayTheoMaAsync(string ma, CancellationToken ct)
        => (await work.DocAsync(factory, $"""
            SELECT {CotPhieu}, {CotDong}
            FROM dbo.PhieuKiem AS p WITH (HOLDLOCK) LEFT JOIN dbo.DongKiem AS d ON d.maPhieuKiem = p.maPhieuKiem
            WHERE p.maPhieuKiem = @ma ORDER BY d.maHang;
            """, DocPhieuAsync, ct, Ma("@ma", ma))).SingleOrDefault();

    public async Task<PhieuKiem?> KhoaTheoMaAsync(string ma, CancellationToken ct)
    {
        await using var lenhKhoaPhieu = work.Lenh($"""
            SELECT {CotPhieu} FROM dbo.PhieuKiem AS p WITH (UPDLOCK, HOLDLOCK) WHERE p.maPhieuKiem = @ma;
            """, Ma("@ma", ma));
        var dauPhieu = await DocMotAsync(lenhKhoaPhieu, AnhXaDauPhieu, ct);
        if (dauPhieu is null)
        {
            return null;
        }

        await using var lenhKhoaDongs = work.Lenh($"""
            SELECT {CotDong} FROM dbo.DongKiem AS d WITH (UPDLOCK)
            WHERE d.maPhieuKiem = @ma ORDER BY d.maHang;
            """, Ma("@ma", ma));
        var dongs = await DocNhieuAsync(lenhKhoaDongs, reader => AnhXaDong(reader, dauPhieu.Ma, 0), ct);
        return dauPhieu.KhoiPhuc(dongs);
    }

    public Task<IReadOnlyList<PhieuKiem>> LayLichSuAsync(CancellationToken ct)
        => work.DocAsync(factory, $"""
            SELECT {CotPhieu}, {CotDong}
            FROM dbo.PhieuKiem AS p WITH (HOLDLOCK) LEFT JOIN dbo.DongKiem AS d ON d.maPhieuKiem = p.maPhieuKiem
            ORDER BY p.mocKiem DESC, p.maPhieuKiem DESC, d.maHang;
            """, DocPhieuAsync, ct);

    public Task<DateTime?> LayMocKiemDangMoMoiNhatAsync(string maHang, CancellationToken ct)
        => work.DocAsync<DateTime?>(factory, """
            SELECT MAX(p.mocKiem) FROM dbo.DongKiem AS d JOIN dbo.PhieuKiem AS p ON p.maPhieuKiem = d.maPhieuKiem
            WHERE d.maHang = @hang AND p.trangThai IN ('DangKiem', 'ChoDuyet');
            """, async (command, token) => await command.ExecuteScalarAsync(token) is DateTime luc
                ? DateTime.SpecifyKind(luc, DateTimeKind.Utc) : null, ct, Ma("@hang", maHang));

    public async Task ThemAsync(PhieuKiem entity, CancellationToken ct)
    {
        await work.Ghi("""
            INSERT INTO dbo.PhieuKiem (maPhieuKiem, maNguoiLap, mocKiem, trangThai)
            VALUES (@ma, @nguoiLap, @moc, @trangThai);
            """, ct, Ma("@ma", entity.MaPhieuKiem), Ma("@nguoiLap", entity.MaNguoiLap),
            Luc("@moc", entity.MocKiem), TrangThai(entity.TrangThai));
        foreach (var dong in entity.DongKiems)
        {
            await work.Ghi("""
                INSERT INTO dbo.DongKiem (maPhieuKiem, maHang, tonSoTaiMoc, soLuongDem, giaiTrinh)
                VALUES (@ma, @hang, @ton, @dem, @giaiTrinh);
                """, ct, Ma("@ma", dong.MaPhieuKiem), Ma("@hang", dong.MaHang), So("@ton", dong.TonSoTaiMoc),
                So("@dem", dong.SoLuongDem), Chu("@giaiTrinh", dong.GiaiTrinh, 500));
        }
    }

    public Task CapNhatAsync(PhieuKiem entity, CancellationToken ct)
        => work.SuaMot("""
            UPDATE dbo.PhieuKiem SET trangThai = @trangThai, maNguoiDuyet = @nguoiDuyet, lucHoanTat = @luc
            WHERE maPhieuKiem = @ma AND (
                (trangThai = 'DangKiem' AND @trangThai IN ('ChoDuyet', 'DaHuy')) OR
                (trangThai = 'ChoDuyet' AND @trangThai IN ('HoanTat', 'DaHuy'))
            );
            """, ct, Ma("@ma", entity.MaPhieuKiem), TrangThai(entity.TrangThai),
            Ma("@nguoiDuyet", entity.MaNguoiDuyet), Luc("@luc", entity.LucHoanTat));

    public Task CapNhatDemAsync(DongKiem dong, CancellationToken ct)
        => work.SuaMot("""
            UPDATE d SET soLuongDem = @dem, giaiTrinh = @giaiTrinh
            FROM dbo.DongKiem AS d JOIN dbo.PhieuKiem AS p ON p.maPhieuKiem = d.maPhieuKiem
            WHERE d.maPhieuKiem = @ma AND d.maHang = @hang AND p.trangThai = 'DangKiem';
            """, ct, Ma("@ma", dong.MaPhieuKiem), Ma("@hang", dong.MaHang),
            So("@dem", dong.SoLuongDem), Chu("@giaiTrinh", dong.GiaiTrinh, 500));

    private static async Task<IReadOnlyList<PhieuKiem>> DocPhieuAsync(SqlCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        var ketQua = new List<PhieuKiem>();
        DauPhieu? dauPhieu = null;
        var dongs = new List<DongKiem>();
        while (await reader.ReadAsync(ct))
        {
            var ma = reader.GetString(0);
            if (dauPhieu?.Ma != ma)
            {
                if (dauPhieu is not null)
                {
                    ketQua.Add(dauPhieu.KhoiPhuc(dongs));
                    dongs.Clear();
                }
                dauPhieu = AnhXaDauPhieu(reader);
            }
            if (!reader.IsDBNull(6))
            {
                dongs.Add(AnhXaDong(reader, ma, 6));
            }
        }
        if (dauPhieu is not null)
        {
            ketQua.Add(dauPhieu.KhoiPhuc(dongs));
        }
        return ketQua;
    }

    private static DauPhieu AnhXaDauPhieu(SqlDataReader reader)
        => new(reader.GetString(0), reader.GetString(1), reader.Utc(2), reader.GetString(3),
            reader.ChuNull(4), reader.LucNull(5));

    private static DongKiem AnhXaDong(SqlDataReader reader, string ma, int i)
        => new(ma, reader.GetString(i), reader.GetInt32(i + 1),
            reader.IsDBNull(i + 2) ? null : reader.GetInt32(i + 2), reader.ChuNull(i + 3));

    private static SqlParameter TrangThai(string value) => new("@trangThai", SqlDbType.VarChar, 24) { Value = value };

    private sealed record DauPhieu(string Ma, string MaNguoiLap, DateTime Moc, string TrangThai,
        string? MaNguoiDuyet, DateTime? LucHoanTat)
    {
        public PhieuKiem KhoiPhuc(IReadOnlyList<DongKiem> dongs)
            => PhieuKiem.KhoiPhuc(Ma, MaNguoiLap, Moc, TrangThai, MaNguoiDuyet, LucHoanTat, dongs);
    }
}
