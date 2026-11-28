using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Counter.CounterSql;

namespace Pos.Infrastructure.Repositories.Counter;

public sealed class SqlHoaDonRepository(SqlUnitOfWork work) : IHoaDonRepository
{
    public Task<string> SinhMaAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('HD-', NEXT VALUE FOR dbo.Seq_HoaDon);", ct);

    public Task<string> SinhMaDongAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('DB-', NEXT VALUE FOR dbo.Seq_DongBan);", ct);

    public Task<string> SinhMaThanhToanAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('TT-', NEXT VALUE FOR dbo.Seq_ThanhToanBan);", ct);

    public Task<HoaDon?> LayTheoMaAsync(string ma, CancellationToken ct) => DocAsync(ma, false, ct);

    public Task<HoaDon?> KhoaTheoMaAsync(string maHoaDon, CancellationToken ct) => DocAsync(maHoaDon, true, ct);

    private async Task<HoaDon?> DocAsync(string ma, bool khoa, CancellationToken ct)
    {
        const string doc = """
            SELECT maHoaDon, maCa, maNguoiLap, lucLap, lucXacNhan, lucHuy, maNguoiHuy, lyDoHuy, maCaDao, trangThai
            FROM dbo.HoaDon WHERE maHoaDon=@ma;
            SELECT maDongBan, maHoaDon, maHang, soLuongBan, donGiaThucBan FROM dbo.DongBan WHERE maHoaDon=@ma;
            SELECT maThanhToan, maHoaDon, phuongThuc, soTienThanhToan, tienKhachDua FROM dbo.ThanhToanBan WHERE maHoaDon=@ma;
            """;
        const string docKhoa = """
            SELECT maHoaDon, maCa, maNguoiLap, lucLap, lucXacNhan, lucHuy, maNguoiHuy, lyDoHuy, maCaDao, trangThai
            FROM dbo.HoaDon WITH (UPDLOCK, HOLDLOCK) WHERE maHoaDon=@ma;
            SELECT maDongBan, maHoaDon, maHang, soLuongBan, donGiaThucBan FROM dbo.DongBan WHERE maHoaDon=@ma;
            SELECT maThanhToan, maHoaDon, phuongThuc, soTienThanhToan, tienKhachDua FROM dbo.ThanhToanBan WHERE maHoaDon=@ma;
            """;
        await using var command = work.Lenh(khoa ? docKhoa : doc, Ma("@ma", ma));
        await using var r = await command.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
        {
            return null;
        }

        var maHoaDon = r.GetString(0);
        var maCa = r.GetString(1);
        var maNguoiLap = r.GetString(2);
        var lucLap = r.Utc(3);
        var xacNhan = r.LucNull(4);
        var huy = r.LucNull(5);
        var nguoiHuy = r.ChuNull(6);
        var lyDo = r.ChuNull(7);
        var maCaDao = r.ChuNull(8);
        var trangThai = r.GetString(9);

        var dongHang = new List<DongBan>();
        await r.NextResultAsync(ct);
        while (await r.ReadAsync(ct))
        {
            dongHang.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetDecimal(4)));
        }

        var thanhToan = new List<ThanhToanBan>();
        await r.NextResultAsync(ct);
        while (await r.ReadAsync(ct))
        {
            thanhToan.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.IsDBNull(4) ? null : r.GetDecimal(4)));
        }

        return HoaDon.KhoiPhuc(maHoaDon, maCa, maNguoiLap, lucLap, trangThai, xacNhan, huy, nguoiHuy, lyDo,
            maCaDao, dongHang, thanhToan);
    }

    public async Task ThemAsync(HoaDon entity, CancellationToken ct)
    {
        await work.Ghi("""
            INSERT dbo.HoaDon(maHoaDon, maCa, maNguoiLap, lucLap)
            VALUES(@maHoaDon, @maCa, @maNguoiLap, @lucLap);
            """, ct, Ma("@maHoaDon", entity.MaHoaDon), Ma("@maCa", entity.MaCa), Ma("@maNguoiLap", entity.MaNguoiLap),
            Luc("@lucLap", entity.LucLap));
        await GhiDongAsync(entity, ct);

        if (entity.TrangThai != TrangThaiChungTu.Nhap)
        {
            await GhiTrangThaiAsync(entity, ct);
        }
    }

    public async Task CapNhatAsync(HoaDon entity, CancellationToken ct)
    {
        await GhiTrangThaiAsync(entity, ct);
        if (entity.TrangThai == TrangThaiChungTu.Nhap)
        {
            await XoaDongAsync(entity.MaHoaDon, ct);
            await GhiDongAsync(entity, ct);
        }
    }

    private Task XoaDongAsync(string ma, CancellationToken ct) => work.Ghi("""
        DELETE dbo.ThanhToanBan WHERE maHoaDon=@ma;
        DELETE dbo.DongBan WHERE maHoaDon=@ma;
        """, ct, Ma("@ma", ma));

    private Task GhiTrangThaiAsync(HoaDon entity, CancellationToken ct) => work.SuaMot("""
        UPDATE dbo.HoaDon SET trangThai=@trangThai, lucXacNhan=@xacNhan,
            lucHuy=@huy, maNguoiHuy=@nguoiHuy, lyDoHuy=@lyDo, maCaDao=@caDao
        WHERE maHoaDon=@ma AND (trangThai='Nhap' OR (trangThai='DaXacNhan' AND @trangThai='DaHuy'));
        """, ct, Ma("@ma", entity.MaHoaDon), Ma("@trangThai", entity.TrangThai), Luc("@xacNhan", entity.LucXacNhan),
        Luc("@huy", entity.LucHuy), Ma("@nguoiHuy", entity.MaNguoiHuy), Chu("@lyDo", entity.LyDoHuy, 500),
        Ma("@caDao", entity.MaCaDao));

    private async Task GhiDongAsync(HoaDon entity, CancellationToken ct)
    {
        foreach (var dong in entity.DongBans)
        {
            await work.Ghi("""
                INSERT dbo.DongBan(maDongBan, maHoaDon, maHang, soLuongBan, donGiaThucBan)
                VALUES(@maDongBan, @maHoaDon, @maHang, @soLuongBan, @donGiaThucBan);
                """, ct,
                Ma("@maDongBan", dong.MaDongBan),
                Ma("@maHoaDon", dong.MaHoaDon),
                Ma("@maHang", dong.MaHang),
                So("@soLuongBan", dong.SoLuongBan),
                Tien("@donGiaThucBan", dong.DonGiaThucBan));
        }

        foreach (var dong in entity.ThanhToanBans)
        {
            await work.Ghi("""
                INSERT dbo.ThanhToanBan(maThanhToan, maHoaDon, phuongThuc, soTienThanhToan, tienKhachDua)
                VALUES(@maThanhToan, @maHoaDon, @phuongThuc, @soTienThanhToan, @tienKhachDua);
                """, ct,
                Ma("@maThanhToan", dong.MaThanhToan),
                Ma("@maHoaDon", dong.MaHoaDon),
                Ma("@phuongThuc", dong.PhuongThuc),
                Tien("@soTienThanhToan", dong.SoTienThanhToan),
                Tien("@tienKhachDua", dong.TienKhachDua));
        }
    }
}
