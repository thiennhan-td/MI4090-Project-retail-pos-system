using Pos.Domain.Interfaces;
using Pos.Domain.Reports;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Inventory.InventorySql;

namespace Pos.Infrastructure.Repositories.Inventory;

public sealed class SqlInventoryReportRepository(SqlUnitOfWork work, SqlConnectionFactory factory)
    : IInventoryReportRepository
{
    public Task<IReadOnlyList<DongXuatNhapTonDto>> LayXuatNhapTonAsync(
        DateTime tuUtc, DateTime denUtc, string? maNhom, string? maHang, CancellationToken ct)
        => work.DocAsync(factory, """
            WITH HangLoc AS (
                SELECT maHang, tenHang, maNhom, donViTinh
                FROM dbo.MatHang
                WHERE (@maNhom IS NULL OR maNhom = @maNhom)
                  AND (@maHang IS NULL OR maHang = @maHang)
            ),
            TongHopTheKho AS (
                SELECT bd.maHang,
                    SUM(CASE WHEN bd.thoiDiemGhi < @tuUtc THEN sl.soLuong ELSE 0 END) AS tonDau,
                    SUM(CASE WHEN bd.thoiDiemGhi >= @tuUtc AND bd.loaiSuKien IN ('NhapKho', 'DaoNhapKho')
                        THEN sl.soLuong ELSE 0 END) AS nhapMuaRong,
                    SUM(CASE WHEN bd.thoiDiemGhi >= @tuUtc AND bd.loaiSuKien IN ('XuatBan', 'DaoXuatBan')
                        THEN -sl.soLuong ELSE 0 END) AS xuatBanRong,
                    SUM(CASE WHEN bd.thoiDiemGhi >= @tuUtc AND bd.loaiSuKien = 'NhapTraHang'
                        THEN sl.soLuong ELSE 0 END) AS traNhapLai,
                    SUM(CASE WHEN bd.thoiDiemGhi >= @tuUtc AND bd.loaiSuKien = 'DieuChinhKiemKho'
                        THEN sl.soLuong ELSE 0 END) AS dieuChinhTon,
                    SUM(CASE WHEN bd.thoiDiemGhi >= @tuUtc AND sl.soLuong > 0
                        THEN sl.soLuong ELSE 0 END) AS tongNhap,
                    SUM(CASE WHEN bd.thoiDiemGhi >= @tuUtc AND sl.soLuong < 0
                        THEN -sl.soLuong ELSE 0 END) AS tongXuat
                FROM dbo.BienDongKho AS bd
                INNER JOIN HangLoc AS h ON h.maHang = bd.maHang
                CROSS APPLY (VALUES (CONVERT(bigint, bd.soLuongThayDoi))) AS sl(soLuong)
                WHERE bd.thoiDiemGhi < @denUtc
                GROUP BY bd.maHang
            )
            SELECT h.maHang, h.tenHang, n.tenNhom, h.donViTinh,
                COALESCE(t.tonDau, 0), COALESCE(t.nhapMuaRong, 0), COALESCE(t.xuatBanRong, 0),
                COALESCE(t.traNhapLai, 0), COALESCE(t.dieuChinhTon, 0),
                COALESCE(t.tongNhap, 0), COALESCE(t.tongXuat, 0)
            FROM HangLoc AS h
            INNER JOIN dbo.NhomHang AS n ON n.maNhom = h.maNhom
            LEFT JOIN TongHopTheKho AS t ON t.maHang = h.maHang
            ORDER BY h.maHang;
            """,
            (command, token) => DocNhieuAsync(command, reader => new DongXuatNhapTonDto(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetInt64(4), reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7),
                reader.GetInt64(8), reader.GetInt64(9), reader.GetInt64(10)), token),
            ct, Luc("@tuUtc", tuUtc, 7), Luc("@denUtc", denUtc, 7), Ma("@maNhom", maNhom), Ma("@maHang", maHang));

    public Task<IReadOnlyList<DongNhapCongNoDto>> LayNhapCongNoAsync(
        DateTime tuUtc, DateTime denUtc, string? maNhaCungCap, CancellationToken ct)
        => work.DocAsync(factory, """
            WITH TheoNhaCungCap AS (
                SELECT p.maNhaCungCap,
                    SUM(CASE WHEN p.lucXacNhan < @tuUtc THEN nhap.giaTri ELSE 0 END) AS nhapTruocKy,
                    SUM(CASE WHEN p.lucHuy < @tuUtc THEN nhap.giaTri ELSE 0 END) AS daoTruocKy,
                    SUM(CASE WHEN p.lucXacNhan >= @tuUtc THEN nhap.giaTri ELSE 0 END) AS nhapTrongKy,
                    SUM(CASE WHEN p.lucHuy >= @tuUtc AND p.lucHuy < @denUtc
                        THEN nhap.giaTri ELSE 0 END) AS daoTrongKy,
                    SUM(tra.traTruocKy) AS traTruocKy,
                    SUM(tra.traTrongKy) AS traTrongKy
                FROM dbo.PhieuNhap AS p
                OUTER APPLY (
                    SELECT COALESCE(SUM(d.soLuongNhap * d.donGiaNhap), 0) AS giaTri
                    FROM dbo.DongNhap AS d
                    WHERE d.maPhieuNhap = p.maPhieuNhap
                ) AS nhap
                OUTER APPLY (
                    SELECT COALESCE(SUM(CASE WHEN t.lucTra < @tuUtc THEN t.soTienTra ELSE 0 END), 0) AS traTruocKy,
                        COALESCE(SUM(CASE WHEN t.lucTra >= @tuUtc THEN t.soTienTra ELSE 0 END), 0) AS traTrongKy
                    FROM dbo.TraNhaCungCap AS t
                    WHERE t.maPhieuNhap = p.maPhieuNhap AND t.lucTra < @denUtc
                ) AS tra
                WHERE p.lucXacNhan < @denUtc
                  AND (@maNhaCungCap IS NULL OR p.maNhaCungCap = @maNhaCungCap)
                GROUP BY p.maNhaCungCap
            )
            SELECT ncc.maNhaCungCap, ncc.tenNhaCungCap,
                COALESCE(t.nhapTruocKy, 0) - COALESCE(t.daoTruocKy, 0) - COALESCE(t.traTruocKy, 0),
                COALESCE(t.nhapTrongKy, 0), COALESCE(t.daoTrongKy, 0), COALESCE(t.traTrongKy, 0)
            FROM dbo.NhaCungCap AS ncc
            LEFT JOIN TheoNhaCungCap AS t ON t.maNhaCungCap = ncc.maNhaCungCap
            WHERE @maNhaCungCap IS NULL OR ncc.maNhaCungCap = @maNhaCungCap
            ORDER BY ncc.maNhaCungCap;
            """,
            (command, token) => DocNhieuAsync(command, reader => new DongNhapCongNoDto(
                reader.GetString(0), reader.GetString(1), reader.GetDecimal(2), reader.GetDecimal(3),
                reader.GetDecimal(4), reader.GetDecimal(5)), token),
            ct, Luc("@tuUtc", tuUtc, 7), Luc("@denUtc", denUtc, 7), Ma("@maNhaCungCap", maNhaCungCap));

    public Task<IReadOnlyList<DongChiTietNhapCongNoDto>> LayChiTietNhapCongNoAsync(
        DateTime tuUtc, DateTime denUtc, string maNhaCungCap, CancellationToken ct)
        => work.DocAsync(factory, """
            WITH PhieuCoHieuLuc AS (
                SELECT maPhieuNhap, maNhaCungCap, lucXacNhan, lucHuy
                FROM dbo.PhieuNhap WITH (HOLDLOCK)
                WHERE lucXacNhan < @denUtc AND maNhaCungCap = @maNhaCungCap
            ),
            SuKien AS (
                SELECT sk.loai, sk.thoiDiem, p.maPhieuNhap, p.maNhaCungCap,
                    CAST(NULL AS varchar(32)) AS maKhoanTra, nhap.giaTri AS soTien
                FROM PhieuCoHieuLuc AS p
                OUTER APPLY (
                    SELECT COALESCE(SUM(d.soLuongNhap * d.donGiaNhap), 0) AS giaTri
                    FROM dbo.DongNhap AS d
                    WHERE d.maPhieuNhap = p.maPhieuNhap
                ) AS nhap
                CROSS APPLY (VALUES (@xacNhanNhap, p.lucXacNhan), (@daoNhap, p.lucHuy)) AS sk(loai, thoiDiem)
                WHERE sk.thoiDiem < @denUtc

                UNION ALL

                SELECT @traNhaCungCap, t.lucTra, p.maPhieuNhap, p.maNhaCungCap, t.maKhoanTra, t.soTienTra
                FROM PhieuCoHieuLuc AS p
                INNER JOIN dbo.TraNhaCungCap AS t ON t.maPhieuNhap = p.maPhieuNhap
                WHERE t.lucTra < @denUtc
            )
            SELECT loai, thoiDiem, maPhieuNhap, maNhaCungCap, maKhoanTra, soTien,
                CAST(CASE WHEN thoiDiem < @tuUtc THEN 1 ELSE 0 END AS bit) AS truocKy
            FROM SuKien
            ORDER BY thoiDiem, loai, maPhieuNhap, maKhoanTra;
            """,
            (command, token) => DocNhieuAsync(command, reader => new DongChiTietNhapCongNoDto(
                (LoaiSuKienNhapCongNo)reader.GetInt32(0), reader.Utc(1), reader.GetString(2), reader.GetString(3),
                reader.ChuNull(4), reader.GetDecimal(5), reader.GetBoolean(6)), token),
            ct, Luc("@tuUtc", tuUtc, 7), Luc("@denUtc", denUtc, 7), Ma("@maNhaCungCap", maNhaCungCap),
            So("@xacNhanNhap", (int)LoaiSuKienNhapCongNo.XacNhanNhap), So("@daoNhap", (int)LoaiSuKienNhapCongNo.DaoNhap),
            So("@traNhaCungCap", (int)LoaiSuKienNhapCongNo.TraNhaCungCap));

    public Task<decimal> TinhTongChiNccTuKetTheoCaAsync(string maCa, CancellationToken ct)
        => work.DocAsync(factory, """
            SELECT COALESCE(SUM(soTienTra), 0)
            FROM dbo.TraNhaCungCap
            WHERE maCaChi = @maCa AND nguonChi = 'KetCa' AND phuongThuc = 'TienMat';
            """,
            async (command, token) => (decimal)(await command.ExecuteScalarAsync(token))!,
            ct, Ma("@maCa", maCa));

    public Task<ChiNccTheoKyDto> LayTongHopChiNccTheoKyAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct)
        => work.DocAsync(factory, """
            SELECT
                COALESCE(SUM(CASE WHEN nguonChi = 'KetCa' AND phuongThuc = 'TienMat'
                    THEN soTienTra ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN nguonChi = 'QuyCuaHang' AND phuongThuc = 'TienMat'
                    THEN soTienTra ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN nguonChi = 'QuyCuaHang' AND phuongThuc = 'ChuyenKhoan'
                    THEN soTienTra ELSE 0 END), 0)
            FROM dbo.TraNhaCungCap
            WHERE lucTra >= @tuUtc AND lucTra < @denUtc;
            """,
            async (command, token) => (await DocMotAsync(command, reader =>
                new ChiNccTheoKyDto(reader.GetDecimal(0), reader.GetDecimal(1), reader.GetDecimal(2)), token))!,
            ct, Luc("@tuUtc", tuUtc, 7), Luc("@denUtc", denUtc, 7));
}
