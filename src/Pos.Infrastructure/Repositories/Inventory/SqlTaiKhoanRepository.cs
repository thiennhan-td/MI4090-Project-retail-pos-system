using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Inventory.InventorySql;

namespace Pos.Infrastructure.Repositories.Inventory;

public sealed class SqlTaiKhoanRepository(SqlUnitOfWork work, SqlConnectionFactory factory)
    : IQuanTriTaiKhoanRepository
{
    private const string Cot = "maTaiKhoan, maVaiTro, hoTen, tenDangNhap, matKhauBam, trangThai";

    public Task<TaiKhoan?> LayTheoMaAsync(string ma, CancellationToken ct)
        => work.DocAsync(factory, $"SELECT {Cot} FROM dbo.TaiKhoan WHERE maTaiKhoan = @ma;",
            (command, token) => DocMotAsync(command, DocTaiKhoan, token), ct, Ma("@ma", ma));

    public Task<TaiKhoan?> LayTheoTenDangNhapAsync(string tenDangNhap, CancellationToken ct)
        => work.DocAsync(factory, $"SELECT {Cot} FROM dbo.TaiKhoan WHERE tenDangNhap = @ten;",
            (command, token) => DocMotAsync(command, DocTaiKhoan, token), ct, Chu("@ten", tenDangNhap, 64));

    public Task<HoSoDangNhap?> LayHoSoDangNhapAsync(string maTaiKhoan, CancellationToken ct)
        => work.DocAsync(factory, """
            SELECT t.maTaiKhoan, t.maVaiTro, t.hoTen, t.tenDangNhap, t.matKhauBam, t.trangThai, q.maQuyen
            FROM dbo.TaiKhoan AS t WITH (HOLDLOCK)
            LEFT JOIN dbo.QuyenVaiTro AS q WITH (HOLDLOCK) ON q.maVaiTro = t.maVaiTro
            WHERE t.maTaiKhoan = @ma AND t.trangThai = 'HoatDong';
            """, DocHoSoAsync, ct, Ma("@ma", maTaiKhoan));

    public Task<IReadOnlyList<TaiKhoan>> TimTaiKhoanAsync(string? tuKhoa, CancellationToken ct)
        => work.DocAsync(factory, $"""
            SELECT {Cot} FROM dbo.TaiKhoan
            WHERE @tukhoa IS NULL OR CHARINDEX(@tukhoa, tenDangNhap) > 0 OR CHARINDEX(@tukhoa, hoTen) > 0
            ORDER BY tenDangNhap, maTaiKhoan;
            """, (command, token) => DocNhieuAsync(command, DocTaiKhoan, token), ct, Chu("@tukhoa", tuKhoa, 150));

    public Task<string> SinhMaTaiKhoanAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('TK-', NEXT VALUE FOR dbo.Seq_TaiKhoan);", ct);

    public Task ThemAsync(TaiKhoan entity, CancellationToken ct)
        => InventorySql.ChanTrungAsync(() => work.Ghi($"""
            INSERT INTO dbo.TaiKhoan ({Cot})
            VALUES (@ma, @vaiTro, @ho, @ten, @bam, @trangThai);
            """, ct, ThamSo(entity)), "Tên đăng nhập đã được sử dụng.");

    public Task CapNhatAsync(TaiKhoan entity, CancellationToken ct)
        => InventorySql.ChanTrungAsync(() => work.SuaMot("""
            UPDATE dbo.TaiKhoan SET maVaiTro = @vaiTro, hoTen = @ho, tenDangNhap = @ten,
                matKhauBam = @bam, trangThai = @trangThai WHERE maTaiKhoan = @ma;
            """, ct, ThamSo(entity)), "Tên đăng nhập đã được sử dụng.");

    public async Task<VaiTro?> KhoaVaiTroAsync(string maVaiTro, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT maVaiTro, tenVaiTro FROM dbo.VaiTro WITH (UPDLOCK, HOLDLOCK) WHERE maVaiTro = @ma;
            """, Ma("@ma", maVaiTro));
        return await DocMotAsync(command, DocVaiTro, ct);
    }

    public async Task<int> DemQuanLyKhacDangHoatDongAsync(string maTaiKhoan, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT COUNT(*) FROM dbo.TaiKhoan
            WHERE maVaiTro = 'QuanLy' AND trangThai = 'HoatDong' AND maTaiKhoan <> @ma;
            """, Ma("@ma", maTaiKhoan));
        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public Task<IReadOnlyList<VaiTro>> LayVaiTroAsync(CancellationToken ct)
        => work.DocAsync(factory, "SELECT maVaiTro, tenVaiTro FROM dbo.VaiTro ORDER BY tenVaiTro;",
            (command, token) => DocNhieuAsync(command, DocVaiTro, token), ct);

    public Task<IReadOnlyList<Quyen>> LayDanhSachQuyenAsync(CancellationToken ct)
        => work.DocAsync(factory, "SELECT maQuyen, tenQuyen FROM dbo.Quyen ORDER BY maQuyen;",
            (command, token) => DocNhieuAsync(command, r => new Quyen(r.GetString(0), r.GetString(1)), token), ct);

    public Task<IReadOnlySet<string>> LayTapQuyenAsync(string maTaiKhoan, CancellationToken ct)
        => work.DocAsync(factory, """
            SELECT v.maQuyen FROM dbo.TaiKhoan t
            JOIN dbo.QuyenVaiTro v ON v.maVaiTro = t.maVaiTro
            WHERE t.maTaiKhoan = @ma AND t.trangThai = 'HoatDong';
            """, DocQuyenAsync, ct, Ma("@ma", maTaiKhoan));

    public Task<IReadOnlySet<string>> LayQuyenVaiTroAsync(string maVaiTro, CancellationToken ct)
        => work.DocAsync(factory, "SELECT maQuyen FROM dbo.QuyenVaiTro WHERE maVaiTro = @ma;",
            DocQuyenAsync, ct, Ma("@ma", maVaiTro));

    public async Task ThayQuyenVaiTroAsync(string maVaiTro, IReadOnlySet<string> maQuyen, CancellationToken ct)
    {
        await work.Ghi("DELETE FROM dbo.QuyenVaiTro WHERE maVaiTro = @ma;", ct, Ma("@ma", maVaiTro));
        foreach (var quyen in maQuyen.Order(StringComparer.Ordinal))
        {
            await work.Ghi("INSERT INTO dbo.QuyenVaiTro (maVaiTro, maQuyen) VALUES (@ma, @quyen);",
                ct, Ma("@ma", maVaiTro), new SqlParameter("@quyen", SqlDbType.VarChar, 64) { Value = quyen });
        }
    }

    private static async Task<IReadOnlySet<string>> DocQuyenAsync(SqlCommand command, CancellationToken ct)
        => (await DocNhieuAsync(command, r => r.GetString(0), ct)).ToHashSet(StringComparer.Ordinal);

    private static async Task<HoSoDangNhap?> DocHoSoAsync(SqlCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        var taiKhoan = DocTaiKhoan(reader);
        var quyen = new HashSet<string>(StringComparer.Ordinal);
        do
        {
            if (!reader.IsDBNull(6))
            {
                quyen.Add(reader.GetString(6));
            }
        } while (await reader.ReadAsync(ct));

        return new(taiKhoan, quyen);
    }

    private static TaiKhoan DocTaiKhoan(SqlDataReader reader)
    {
        var entity = new TaiKhoan(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetString(4));
        if (reader.GetString(5) == TrangThaiChungTu.BiKhoa)
        {
            entity.Khoa();
        }

        return entity;
    }

    private static VaiTro DocVaiTro(SqlDataReader reader) => new(reader.GetString(0), reader.GetString(1));

    private static SqlParameter[] ThamSo(TaiKhoan entity) =>
    [
        Ma("@ma", entity.MaTaiKhoan), Ma("@vaiTro", entity.MaVaiTro), Chu("@ho", entity.HoTen, 150),
        Chu("@ten", entity.TenDangNhap, 64), new SqlParameter("@bam", SqlDbType.VarChar, 256) { Value = entity.MatKhauBam },
        new SqlParameter("@trangThai", SqlDbType.VarChar, 24) { Value = entity.TrangThai }
    ];
}
