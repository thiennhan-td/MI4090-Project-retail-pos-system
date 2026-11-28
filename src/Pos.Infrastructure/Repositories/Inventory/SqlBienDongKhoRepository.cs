using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Inventory.InventorySql;

namespace Pos.Infrastructure.Repositories.Inventory;

public sealed class SqlBienDongKhoRepository(SqlUnitOfWork work, SqlConnectionFactory factory) : IBienDongKhoRepository
{
    private const string CotBienDong = """
        maBienDong, maHang, loaiSuKien, soLuongThayDoi, thoiDiemGhi,
        maDongBan, maDongTra, maDongNhap, maPhieuKiem
        """;
    private const string TongTon = "SELECT COALESCE(SUM(CONVERT(bigint, soLuongThayDoi)), 0) FROM dbo.BienDongKho";

    public bool CoGiaoDich => work.CoGiaoDich;

    public Task<string> SinhMaAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('BD-', NEXT VALUE FOR dbo.Seq_BienDongKho);", ct);

    public Task<BienDongKho?> LayTheoMaAsync(string ma, CancellationToken ct)
        => work.DocAsync(factory, $"SELECT {CotBienDong} FROM dbo.BienDongKho WHERE maBienDong = @ma;",
            (command, token) => DocMotAsync(command, AnhXa, token), ct, Ma("@ma", ma));

    public Task<BienDongKho?> LayTheoKhoaSuKienAsync(string khoaSuKien, CancellationToken ct)
        => work.DocAsync(factory, $"SELECT {CotBienDong} FROM dbo.BienDongKho WHERE khoaSuKien = @khoa;",
            (command, token) => DocMotAsync(command, AnhXa, token), ct, Khoa(khoaSuKien));

    public Task ThemAsync(BienDongKho entity, CancellationToken ct)
        => work.Ghi($"""
            INSERT INTO dbo.BienDongKho ({CotBienDong}, khoaSuKien)
            VALUES (@ma, @hang, @loai, @soLuong, @luc, @ban, @tra, @nhap, @kiem, @khoa);
            """, ct,
            Ma("@ma", entity.MaBienDong), Ma("@hang", entity.MaHang), Loai(entity.LoaiSuKien),
            So("@soLuong", entity.SoLuongThayDoi), Luc("@luc", entity.ThoiDiemGhi), Ma("@ban", entity.MaDongBan),
            Ma("@tra", entity.MaDongTra), Ma("@nhap", entity.MaDongNhap), Ma("@kiem", entity.MaPhieuKiem),
            Khoa(entity.KhoaSuKien));

    public Task<NguonBienDongKho?> LayNguonAsync(BienDongKho bienDong, CancellationToken ct)
    {
        var maNguon = bienDong.MaDongBan ?? bienDong.MaDongTra ?? bienDong.MaDongNhap ?? bienDong.MaPhieuKiem;
        return work.DocAsync(factory, $"""
            SELECT MaNguon, MaHang, MaChungTu, SoLuong, CoHieuLuc FROM ({TruyVanNguon(bienDong.LoaiSuKien)}) AS nguon
            WHERE MaNguon = @nguon AND MaHang = @hang;
            """, (command, token) => DocMotAsync(command, AnhXaNguon, token),
            ct, Ma("@nguon", maNguon), Ma("@hang", bienDong.MaHang));
    }

    public Task<IReadOnlyList<NguonBienDongKho>> LayCacNguonAsync(string loaiSuKien, string maChungTu, CancellationToken ct)
        => work.DocAsync(factory, $"""
            SELECT MaNguon, MaHang, MaChungTu, SoLuong, CoHieuLuc FROM ({TruyVanNguon(loaiSuKien)}) AS nguon
            WHERE MaChungTu = @chungTu;
            """, (command, token) => DocNhieuAsync(command, AnhXaNguon, token), ct, Ma("@chungTu", maChungTu));

    public Task<int> LayTonHienHanhAsync(string maHang, CancellationToken ct)
        => work.DocAsync(factory, $"{TongTon} WHERE maHang = @hang;", DocTonAsync, ct, Ma("@hang", maHang));

    public Task<int> LayTonTaiMocAsync(string maHang, DateTime mocUtc, CancellationToken ct)
        => work.DocAsync(factory, $"{TongTon} WHERE maHang = @hang AND thoiDiemGhi <= @moc;", DocTonAsync,
            ct, Ma("@hang", maHang), Luc("@moc", mocUtc));

    public Task<int> LayTonTruocMocAsync(string maHang, DateTime mocUtc, CancellationToken ct)
        => work.DocAsync(factory, $"{TongTon} WHERE maHang = @hang AND thoiDiemGhi < @moc;", DocTonAsync,
            ct, Ma("@hang", maHang), Luc("@moc", mocUtc));

    public Task<IReadOnlyList<BienDongKho>> LayTrongKyAsync(string maHang, DateTime tuUtc, DateTime denUtc, CancellationToken ct)
        => work.DocAsync(factory, $"""
            SELECT {CotBienDong} FROM dbo.BienDongKho
            WHERE maHang = @hang AND thoiDiemGhi >= @tu AND thoiDiemGhi < @den
            ORDER BY thoiDiemGhi, maBienDong;
            """, (command, token) => DocNhieuAsync(command, AnhXa, token),
            ct, Ma("@hang", maHang), Luc("@tu", tuUtc), Luc("@den", denUtc));

    public Task<DateTime?> LayMocDieuChinhCuoiAsync(string maHang, CancellationToken ct)
        => work.DocAsync<DateTime?>(factory, """
            SELECT MAX(thoiDiemGhi) FROM dbo.BienDongKho WHERE maHang = @hang AND loaiSuKien = @loai;
            """, async (command, token) => await command.ExecuteScalarAsync(token) is DateTime luc
                ? DateTime.SpecifyKind(luc, DateTimeKind.Utc) : null, ct,
            Ma("@hang", maHang), Loai(LoaiBienDongKho.DieuChinhKiemKho));

    private static async Task<int> DocTonAsync(SqlCommand command, CancellationToken ct)
        => checked((int)(long)(await command.ExecuteScalarAsync(ct))!);

    private static BienDongKho AnhXa(SqlDataReader reader) => BienDongKho.Tao(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.Utc(4),
        reader.ChuNull(5), reader.ChuNull(6), reader.ChuNull(7), reader.ChuNull(8));

    private static NguonBienDongKho AnhXaNguon(SqlDataReader reader)
        => new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetBoolean(4));

    private static string TruyVanNguon(string loaiSuKien) => loaiSuKien switch
    {
        LoaiBienDongKho.XuatBan => """
            SELECT d.maDongBan AS MaNguon, d.maHang AS MaHang, d.maHoaDon AS MaChungTu, -d.soLuongBan AS SoLuong,
                CONVERT(bit, CASE WHEN p.trangThai = 'DaXacNhan' THEN 1 ELSE 0 END) AS CoHieuLuc
            FROM dbo.DongBan AS d JOIN dbo.HoaDon AS p ON p.maHoaDon = d.maHoaDon
            """,
        LoaiBienDongKho.DaoXuatBan => """
            SELECT d.maDongBan AS MaNguon, d.maHang AS MaHang, d.maHoaDon AS MaChungTu, d.soLuongBan AS SoLuong,
                CONVERT(bit, CASE WHEN p.trangThai = 'DaHuy' AND p.lucXacNhan IS NOT NULL AND EXISTS (
                    SELECT 1 FROM dbo.BienDongKho AS goc WHERE goc.khoaSuKien = CONCAT('XuatBan:', d.maDongBan)
                        AND goc.maHang = d.maHang AND goc.soLuongThayDoi = -d.soLuongBan
                ) THEN 1 ELSE 0 END) AS CoHieuLuc
            FROM dbo.DongBan AS d JOIN dbo.HoaDon AS p ON p.maHoaDon = d.maHoaDon
            """,
        LoaiBienDongKho.NhapTraHang => """
            SELECT d.maDongTra AS MaNguon, ban.maHang AS MaHang, d.maPhieuTra AS MaChungTu, d.soLuongNhapLai AS SoLuong,
                CONVERT(bit, CASE WHEN p.trangThai = 'DaXacNhan' THEN 1 ELSE 0 END) AS CoHieuLuc
            FROM dbo.DongTra AS d JOIN dbo.DongBan AS ban ON ban.maDongBan = d.maDongBan
            JOIN dbo.PhieuTra AS p ON p.maPhieuTra = d.maPhieuTra
            WHERE d.soLuongNhapLai > 0
            """,
        LoaiBienDongKho.NhapKho => """
            SELECT d.maDongNhap AS MaNguon, d.maHang AS MaHang, d.maPhieuNhap AS MaChungTu, d.soLuongNhap AS SoLuong,
                CONVERT(bit, CASE WHEN p.trangThai = 'DaXacNhan' THEN 1 ELSE 0 END) AS CoHieuLuc
            FROM dbo.DongNhap AS d JOIN dbo.PhieuNhap AS p ON p.maPhieuNhap = d.maPhieuNhap
            """,
        LoaiBienDongKho.DaoNhapKho => """
            SELECT d.maDongNhap AS MaNguon, d.maHang AS MaHang, d.maPhieuNhap AS MaChungTu, -d.soLuongNhap AS SoLuong,
                CONVERT(bit, CASE WHEN p.trangThai = 'DaHuy' AND p.lucXacNhan IS NOT NULL AND EXISTS (
                    SELECT 1 FROM dbo.BienDongKho AS goc WHERE goc.khoaSuKien = CONCAT('NhapKho:', d.maDongNhap)
                        AND goc.maHang = d.maHang AND goc.soLuongThayDoi = d.soLuongNhap
                ) THEN 1 ELSE 0 END) AS CoHieuLuc
            FROM dbo.DongNhap AS d JOIN dbo.PhieuNhap AS p ON p.maPhieuNhap = d.maPhieuNhap
            """,
        LoaiBienDongKho.DieuChinhKiemKho => """
            SELECT d.maPhieuKiem AS MaNguon, d.maHang AS MaHang, d.maPhieuKiem AS MaChungTu,
                d.soLuongDem - d.tonSoTaiMoc AS SoLuong,
                CONVERT(bit, CASE WHEN p.trangThai = 'HoanTat' THEN 1 ELSE 0 END) AS CoHieuLuc
            FROM dbo.DongKiem AS d JOIN dbo.PhieuKiem AS p ON p.maPhieuKiem = d.maPhieuKiem
            WHERE d.soLuongDem <> d.tonSoTaiMoc
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(loaiSuKien))
    };

    private static SqlParameter Khoa(string value) => new("@khoa", SqlDbType.VarChar, 256) { Value = value };
    private static SqlParameter Loai(string value) => new("@loai", SqlDbType.VarChar, 24) { Value = value };
}
