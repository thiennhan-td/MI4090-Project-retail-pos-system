using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Counter.CounterSql;

namespace Pos.Infrastructure.Repositories.Counter;

public sealed class SqlCounterReportRepository(SqlUnitOfWork work) : ICounterReportRepository
{
    public async Task<IReadOnlyList<SuKienBanTra>> LaySuKienAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT h.lucXacNhan, h.maHoaDon, h.maCa, 'Ban', d.maHang, d.soLuongBan,
                   CAST(d.soLuongBan * d.donGiaThucBan AS decimal(29,0)), CAST(NULL AS varchar(24))
            FROM dbo.HoaDon h JOIN dbo.DongBan d ON h.maHoaDon=d.maHoaDon
            WHERE h.lucXacNhan >= @tu AND h.lucXacNhan < @den
            UNION ALL
            SELECT h.lucHuy, h.maHoaDon, h.maCa, 'DaoBan', d.maHang, d.soLuongBan,
                   CAST(d.soLuongBan * d.donGiaThucBan AS decimal(29,0)), CAST(NULL AS varchar(24))
            FROM dbo.HoaDon h JOIN dbo.DongBan d ON h.maHoaDon=d.maHoaDon
            WHERE h.lucHuy >= @tu AND h.lucHuy < @den AND h.lucXacNhan IS NOT NULL AND h.trangThai='DaHuy'
            UNION ALL
            SELECT p.lucXacNhan, p.maPhieuTra, p.maCaNhan, 'Tra', b.maHang, d.soLuongTra,
                   CAST(d.soLuongTra * b.donGiaThucBan AS decimal(29,0)), CAST(NULL AS varchar(24))
            FROM dbo.PhieuTra p JOIN dbo.DongTra d ON p.maPhieuTra=d.maPhieuTra
            JOIN dbo.DongBan b ON b.maDongBan=d.maDongBan
            WHERE p.lucXacNhan >= @tu AND p.lucXacNhan < @den AND p.trangThai='DaXacNhan'
            UNION ALL
            SELECT h.lucXacNhan, h.maHoaDon, h.maCa, 'ThuBan', NULL, 0, t.soTienThanhToan, t.phuongThuc
            FROM dbo.HoaDon h JOIN dbo.ThanhToanBan t ON h.maHoaDon=t.maHoaDon
            WHERE h.lucXacNhan >= @tu AND h.lucXacNhan < @den
            UNION ALL
            SELECT h.lucHuy, h.maHoaDon, COALESCE(h.maCaDao,h.maCa), 'DaoThanhToan', NULL, 0,
                   t.soTienThanhToan, t.phuongThuc
            FROM dbo.HoaDon h JOIN dbo.ThanhToanBan t ON h.maHoaDon=t.maHoaDon
            WHERE h.lucHuy >= @tu AND h.lucHuy < @den AND h.lucXacNhan IS NOT NULL AND h.trangThai='DaHuy'
            UNION ALL
            SELECT p.lucXacNhan, p.maPhieuTra, p.maCaNhan, 'HoanTra', NULL, 0, k.soTienHoan, k.phuongThuc
            FROM dbo.PhieuTra p JOIN dbo.KhoanHoan k ON p.maPhieuTra=k.maPhieuTra
            WHERE p.lucXacNhan >= @tu AND p.lucXacNhan < @den AND p.trangThai='DaXacNhan'
            UNION ALL
            SELECT q.lucGhi, q.maPhieuThuChi, q.maCa,
                   CASE WHEN q.nguonQuy='KetCa' AND q.loaiThuChi='Thu' THEN 'ThuKet'
                        WHEN q.nguonQuy='KetCa' THEN 'ChiKet'
                        WHEN q.loaiThuChi='Thu' THEN 'ThuQuy' ELSE 'ChiQuy' END,
                   NULL, 0, q.soTien, q.phuongThuc
            FROM dbo.PhieuThuChi q WHERE q.lucGhi >= @tu AND q.lucGhi < @den
            ORDER BY 1, 2, 4;
            """, Luc("@tu", tuUtc), Luc("@den", denUtc));
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<SuKienBanTra>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new(reader.Utc(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.ChuNull(4), reader.GetInt32(5), reader.GetDecimal(6), reader.ChuNull(7)));
        }
        return rows;
    }
}
