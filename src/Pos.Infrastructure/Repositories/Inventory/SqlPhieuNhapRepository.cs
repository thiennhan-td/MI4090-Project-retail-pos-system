using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Entities;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Inventory.InventorySql;

namespace Pos.Infrastructure.Repositories.Inventory;

public sealed class SqlPhieuNhapRepository(SqlUnitOfWork work, SqlConnectionFactory factory) : IPhieuNhapRepository
{
    private const string DocDongsVaKhoanTra = """
        SELECT maDongNhap, maHang, soLuongNhap, donGiaNhap
        FROM dbo.DongNhap WHERE maPhieuNhap = @ma ORDER BY maDongNhap;

        SELECT maKhoanTra, maPhieuNhap, maNguoiGhi, maCaChi, lucTra, soTienTra, phuongThuc, nguonChi
        FROM dbo.TraNhaCungCap WHERE maPhieuNhap = @ma ORDER BY lucTra, maKhoanTra;
        """;

    public Task<string> SinhMaPhieuNhapAsync(CancellationToken ct) =>
        work.SinhMa("SELECT CONCAT('PN-', NEXT VALUE FOR dbo.Seq_PhieuNhap);", ct);

    public Task<string> SinhMaDongNhapAsync(CancellationToken ct) =>
        work.SinhMa("SELECT CONCAT('DN-', NEXT VALUE FOR dbo.Seq_DongNhap);", ct);

    public Task<PhieuNhap?> LayTheoMaAsync(string ma, CancellationToken ct) => DocPhieuAsync("""
        SELECT maPhieuNhap, maNhaCungCap, maNguoiLap, lucLap, hanThanhToan, trangThai,
               lucXacNhan, maNguoiHuy, lucHuy, lyDoHuy
        FROM dbo.PhieuNhap WITH (HOLDLOCK) WHERE maPhieuNhap = @ma;
        """, ma, ct);

    public Task<PhieuNhap?> KhoaTheoMaAsync(string maPhieuNhap, CancellationToken ct) => DocPhieuAsync("""
        SELECT maPhieuNhap, maNhaCungCap, maNguoiLap, lucLap, hanThanhToan, trangThai,
               lucXacNhan, maNguoiHuy, lucHuy, lyDoHuy
        FROM dbo.PhieuNhap WITH (UPDLOCK, HOLDLOCK) WHERE maPhieuNhap = @ma;
        """, maPhieuNhap, ct);

    public async Task ThemAsync(PhieuNhap entity, CancellationToken ct)
    {
        await work.Ghi("""
            INSERT dbo.PhieuNhap (maPhieuNhap, maNhaCungCap, maNguoiLap, lucLap, hanThanhToan)
            VALUES (@ma, @maNcc, @maNguoiLap, @lucLap, @han);
            """, ct, Ma("@ma", entity.MaPhieuNhap), Ma("@maNcc", entity.MaNhaCungCap),
            Ma("@maNguoiLap", entity.MaNguoiLap), Luc("@lucLap", entity.LucLap), Ngay("@han", entity.HanThanhToan));

        foreach (var dong in entity.DongNhaps)
        {
            await ThemDongAsync(dong, ct);
        }
    }

    public async Task CapNhatNhapAsync(PhieuNhap entity, IReadOnlyList<DongNhap> dongCu, CancellationToken ct)
    {
        await work.SuaMot("""
            UPDATE dbo.PhieuNhap SET maNhaCungCap = @maNcc, hanThanhToan = @han
            WHERE maPhieuNhap = @ma AND trangThai = 'Nhap';
            """, ct, Ma("@ma", entity.MaPhieuNhap), Ma("@maNcc", entity.MaNhaCungCap), Ngay("@han", entity.HanThanhToan));

        var maDongCu = dongCu.ToDictionary(dong => dong.MaDongNhap, StringComparer.Ordinal);
        var maDongMoi = entity.DongNhaps.Select(dong => dong.MaDongNhap).ToHashSet(StringComparer.Ordinal);
        foreach (var dong in dongCu.Where(dong => !maDongMoi.Contains(dong.MaDongNhap)))
        {
            await work.SuaMot("""
                DELETE dbo.DongNhap WHERE maPhieuNhap = @ma AND maDongNhap = @maDong;
                """, ct, Ma("@ma", entity.MaPhieuNhap), Ma("@maDong", dong.MaDongNhap));
        }
        foreach (var dong in entity.DongNhaps)
        {
            if (!maDongCu.TryGetValue(dong.MaDongNhap, out var cu))
            {
                await ThemDongAsync(dong, ct);
            }
            else if (cu.MaHang != dong.MaHang || cu.SoLuongNhap != dong.SoLuongNhap || cu.DonGiaNhap != dong.DonGiaNhap)
            {
                await work.SuaMot("""
                    UPDATE dbo.DongNhap SET maHang = @maHang, soLuongNhap = @soLuong, donGiaNhap = @donGia
                    WHERE maPhieuNhap = @ma AND maDongNhap = @maDong;
                    """, ct, Ma("@ma", entity.MaPhieuNhap), Ma("@maDong", dong.MaDongNhap),
                    Ma("@maHang", dong.MaHang), So("@soLuong", dong.SoLuongNhap), Tien("@donGia", dong.DonGiaNhap));
            }
        }
    }

    public Task CapNhatTrangThaiAsync(PhieuNhap entity, string trangThaiTruoc, CancellationToken ct) => work.SuaMot("""
        UPDATE dbo.PhieuNhap SET trangThai = @trangThai, lucXacNhan = @lucXacNhan,
            maNguoiHuy = @maNguoiHuy, lucHuy = @lucHuy, lyDoHuy = @lyDoHuy
        WHERE maPhieuNhap = @ma AND trangThai = @trangThaiTruoc;
        """, ct, Ma("@ma", entity.MaPhieuNhap), Ma("@trangThai", entity.TrangThai),
        Ma("@trangThaiTruoc", trangThaiTruoc), Luc("@lucXacNhan", entity.LucXacNhan),
        Ma("@maNguoiHuy", entity.MaNguoiHuy), Luc("@lucHuy", entity.LucHuy), Chu("@lyDoHuy", entity.LyDoHuy, 500));

    public Task<TraNhaCungCap?> LayKhoanTraTheoMaAsync(string maKhoanTra, CancellationToken ct) => work.DocAsync(factory, """
        SELECT maKhoanTra, maPhieuNhap, maNguoiGhi, maCaChi, lucTra, soTienTra, phuongThuc, nguonChi
        FROM dbo.TraNhaCungCap WHERE maKhoanTra = @ma;
        """, (command, token) => DocMotAsync(command, DocKhoanTra, token), ct, Ma("@ma", maKhoanTra));

    public Task ThemKhoanTraAsync(TraNhaCungCap khoanTra, CancellationToken ct) => ChanTrungAsync(() => work.Ghi("""
            INSERT dbo.TraNhaCungCap (maKhoanTra, maPhieuNhap, maNguoiGhi, maCaChi, lucTra,
                soTienTra, phuongThuc, nguonChi)
            VALUES (@maKhoanTra, @maPhieuNhap, @maNguoiGhi, @maCaChi, @lucTra, @soTienTra, @phuongThuc, @nguonChi);
            """, ct, Ma("@maKhoanTra", khoanTra.MaKhoanTra), Ma("@maPhieuNhap", khoanTra.MaPhieuNhap),
            Ma("@maNguoiGhi", khoanTra.MaNguoiGhi), Ma("@maCaChi", khoanTra.MaCaChi), Luc("@lucTra", khoanTra.LucTra),
            Tien("@soTienTra", khoanTra.SoTienTra), Ma("@phuongThuc", khoanTra.PhuongThuc), Ma("@nguonChi", khoanTra.NguonChi)),
        "Mã yêu cầu ghi trả đã được sử dụng.");

    public Task<IReadOnlyList<TomTatPhieuNhap>> TraCuuAsync(string? maNhaCungCap, string? trangThai,
        DateTime? tuUtc, DateTime? denUtc, bool xemCongNo, CancellationToken ct) => work.DocAsync(factory, """
        SELECT phieu.maPhieuNhap, phieu.maNhaCungCap, phieu.maNguoiLap, phieu.lucLap, phieu.hanThanhToan,
            phieu.trangThai, COALESCE(hang.tongTien, 0), CASE WHEN @xemCongNo = 1 THEN COALESCE(tra.daTra, 0) END
        FROM dbo.PhieuNhap AS phieu
        OUTER APPLY (
            SELECT SUM(dong.soLuongNhap * dong.donGiaNhap) AS tongTien
            FROM dbo.DongNhap AS dong WHERE dong.maPhieuNhap = phieu.maPhieuNhap
        ) AS hang
        OUTER APPLY (
            SELECT SUM(khoan.soTienTra) AS daTra FROM dbo.TraNhaCungCap AS khoan
            WHERE @xemCongNo = 1 AND khoan.maPhieuNhap = phieu.maPhieuNhap
        ) AS tra
        WHERE (@maNcc IS NULL OR phieu.maNhaCungCap = @maNcc)
          AND (@trangThai IS NULL OR phieu.trangThai = @trangThai)
          AND (@tu IS NULL OR phieu.lucLap >= @tu) AND (@den IS NULL OR phieu.lucLap < @den)
        ORDER BY phieu.lucLap DESC, phieu.maPhieuNhap;
        """, (command, token) => DocNhieuAsync(command, reader => new TomTatPhieuNhap(reader.GetString(0),
            reader.GetString(1), reader.GetString(2), reader.Utc(3), DocHanThanhToan(reader, 4), reader.GetString(5),
            reader.GetDecimal(6), reader.IsDBNull(7) ? null : reader.GetDecimal(7)), token), ct,
        Ma("@maNcc", maNhaCungCap), Ma("@trangThai", trangThai), MocTraCuuUtc("@tu", tuUtc), MocTraCuuUtc("@den", denUtc),
        new SqlParameter("@xemCongNo", SqlDbType.Bit) { Value = xemCongNo });

    private Task ThemDongAsync(DongNhap dong, CancellationToken ct) => work.Ghi("""
        INSERT dbo.DongNhap (maDongNhap, maPhieuNhap, maHang, soLuongNhap, donGiaNhap)
        VALUES (@maDong, @ma, @maHang, @soLuong, @donGia);
        """, ct, Ma("@maDong", dong.MaDongNhap), Ma("@ma", dong.MaPhieuNhap), Ma("@maHang", dong.MaHang),
        So("@soLuong", dong.SoLuongNhap), Tien("@donGia", dong.DonGiaNhap));

    private async Task<PhieuNhap?> DocPhieuAsync(string sqlDauPhieu, string ma, CancellationToken ct)
    {
        // Giữ khóa đầu phiếu qua cả ba tập kết quả trong giao dịch do service mở.
        await using var command = work.Lenh(sqlDauPhieu + "\n" + DocDongsVaKhoanTra, Ma("@ma", ma));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        var maPhieu = reader.GetString(0);
        var maNcc = reader.GetString(1);
        var maNguoiLap = reader.GetString(2);
        var lucLap = reader.Utc(3);
        var han = DocHanThanhToan(reader, 4);
        var trangThai = reader.GetString(5);
        var lucXacNhan = reader.LucNull(6);
        var maNguoiHuy = reader.ChuNull(7);
        var lucHuy = reader.LucNull(8);
        var lyDoHuy = reader.ChuNull(9);

        await reader.NextResultAsync(ct);
        var dongs = new List<DongNhap>();
        while (await reader.ReadAsync(ct))
        {
            dongs.Add(new DongNhap(reader.GetString(0), maPhieu, reader.GetString(1), reader.GetInt32(2),
                reader.GetDecimal(3)));
        }

        await reader.NextResultAsync(ct);
        var khoanTras = new List<TraNhaCungCap>();
        while (await reader.ReadAsync(ct))
        {
            khoanTras.Add(DocKhoanTra(reader));
        }

        return PhieuNhap.KhoiPhuc(maPhieu, maNcc, maNguoiLap, lucLap, han, trangThai, lucXacNhan, maNguoiHuy,
            lucHuy, lyDoHuy, dongs, khoanTras);
    }

    private static DateOnly? DocHanThanhToan(SqlDataReader reader, int i) =>
        reader.IsDBNull(i) ? null : DateOnly.FromDateTime(reader.GetDateTime(i));

    private static TraNhaCungCap DocKhoanTra(SqlDataReader reader) => new(reader.GetString(0), reader.GetString(1),
        reader.GetString(2), reader.ChuNull(3), reader.Utc(4), reader.GetDecimal(5), reader.GetString(6), reader.GetString(7));

    private static SqlParameter MocTraCuuUtc(string ten, DateTime? moc)
        => new(ten, SqlDbType.DateTime2) { Scale = 7, Value = (object?)moc ?? DBNull.Value };
}
