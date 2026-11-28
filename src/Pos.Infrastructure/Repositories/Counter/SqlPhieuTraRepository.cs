using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Counter.CounterSql;

namespace Pos.Infrastructure.Repositories.Counter;

public sealed class SqlPhieuTraRepository(SqlUnitOfWork work) : IPhieuTraRepository
{
    public Task<string> SinhMaAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('PT-', NEXT VALUE FOR dbo.Seq_PhieuTra);", ct);

    public Task<string> SinhMaDongAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('DT-', NEXT VALUE FOR dbo.Seq_DongTra);", ct);

    public Task<string> SinhMaHoanAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('KH-', NEXT VALUE FOR dbo.Seq_KhoanHoan);", ct);

    public Task<PhieuTra?> LayTheoMaAsync(string ma, CancellationToken ct) => DocAsync(ma, false, ct);

    public Task<PhieuTra?> KhoaTheoMaAsync(string maPhieuTra, CancellationToken ct) => DocAsync(maPhieuTra, true, ct);

    public async Task<bool> CoPhieuTraHieuLucAsync(string maHoaDon, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.PhieuTra WHERE maHoaDon=@ma AND trangThai='DaXacNhan')
                THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """, Ma("@ma", maHoaDon));
        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<int> TinhLuongDaTraAsync(string maDongBan, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT COALESCE(SUM(d.soLuongTra), 0) FROM dbo.DongTra d
            JOIN dbo.PhieuTra p ON d.maPhieuTra=p.maPhieuTra
            WHERE d.maDongBan=@ma AND p.trangThai='DaXacNhan';
            """, Ma("@ma", maDongBan));
        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task XoaNhapAsync(string maPhieuTra, CancellationToken ct)
    {
        var entity = await KhoaTheoMaAsync(maPhieuTra, ct)
            ?? throw new EntityNotFoundException("Không tìm thấy phiếu trả.");
        if (!entity.CoTheHuy())
        {
            throw new InvalidStateTransitionException("Chỉ được xóa phiếu trả nháp.");
        }

        await XoaDongAsync(maPhieuTra, ct);
        await work.SuaMot("DELETE dbo.PhieuTra WHERE maPhieuTra=@ma AND trangThai='Nhap';", ct, Ma("@ma", maPhieuTra));
    }

    private async Task<PhieuTra?> DocAsync(string ma, bool khoa, CancellationToken ct)
    {
        const string doc = """
            SELECT maPhieuTra, maHoaDon, maCaNhan, maNguoiNhan, lucLap, lucXacNhan, trangThai
            FROM dbo.PhieuTra WHERE maPhieuTra=@ma;
            SELECT maDongTra, maPhieuTra, maDongBan, soLuongTra, soLuongNhapLai FROM dbo.DongTra WHERE maPhieuTra=@ma;
            SELECT maKhoanHoan, maPhieuTra, phuongThuc, soTienHoan FROM dbo.KhoanHoan WHERE maPhieuTra=@ma;
            """;
        const string docKhoa = """
            SELECT maPhieuTra, maHoaDon, maCaNhan, maNguoiNhan, lucLap, lucXacNhan, trangThai
            FROM dbo.PhieuTra WITH (UPDLOCK, HOLDLOCK) WHERE maPhieuTra=@ma;
            SELECT maDongTra, maPhieuTra, maDongBan, soLuongTra, soLuongNhapLai FROM dbo.DongTra WHERE maPhieuTra=@ma;
            SELECT maKhoanHoan, maPhieuTra, phuongThuc, soTienHoan FROM dbo.KhoanHoan WHERE maPhieuTra=@ma;
            """;
        await using var command = work.Lenh(khoa ? docKhoa : doc, Ma("@ma", ma));
        await using var r = await command.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
        {
            return null;
        }

        var maPhieuTra = r.GetString(0);
        var maHoaDon = r.GetString(1);
        var maCaNhan = r.GetString(2);
        var maNguoiNhan = r.GetString(3);
        var lucLap = r.Utc(4);
        var xacNhan = r.LucNull(5);
        var trangThai = r.GetString(6);

        var dongHang = new List<DongTra>();
        await r.NextResultAsync(ct);
        while (await r.ReadAsync(ct))
        {
            dongHang.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetInt32(4)));
        }

        var thanhToan = new List<KhoanHoan>();
        await r.NextResultAsync(ct);
        while (await r.ReadAsync(ct))
        {
            thanhToan.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetDecimal(3)));
        }

        return PhieuTra.KhoiPhuc(maPhieuTra, maHoaDon, maCaNhan, maNguoiNhan, lucLap, trangThai, xacNhan, dongHang, thanhToan);
    }

    public async Task ThemAsync(PhieuTra entity, CancellationToken ct)
    {
        await work.Ghi("""
            INSERT dbo.PhieuTra(maPhieuTra, maHoaDon, maCaNhan, maNguoiNhan, lucLap)
            VALUES(@maPhieuTra, @maHoaDon, @maCaNhan, @maNguoiNhan, @lucLap);
            """, ct, Ma("@maPhieuTra", entity.MaPhieuTra), Ma("@maHoaDon", entity.MaHoaDon), Ma("@maCaNhan", entity.MaCaNhan), Ma("@maNguoiNhan", entity.MaNguoiNhan),
            Luc("@lucLap", entity.LucLap));
        await GhiDongAsync(entity, ct);

        if (entity.TrangThai != TrangThaiChungTu.Nhap)
        {
            await GhiTrangThaiAsync(entity, ct);
        }
    }

    public async Task CapNhatAsync(PhieuTra entity, CancellationToken ct)
    {
        await GhiTrangThaiAsync(entity, ct);
        if (entity.TrangThai == TrangThaiChungTu.Nhap)
        {
            await XoaDongAsync(entity.MaPhieuTra, ct);
            await GhiDongAsync(entity, ct);
        }
    }

    private Task XoaDongAsync(string ma, CancellationToken ct) => work.Ghi("""
        DELETE dbo.KhoanHoan WHERE maPhieuTra=@ma;
        DELETE dbo.DongTra WHERE maPhieuTra=@ma;
        """, ct, Ma("@ma", ma));

    private Task GhiTrangThaiAsync(PhieuTra entity, CancellationToken ct) => work.SuaMot("""
        UPDATE dbo.PhieuTra SET trangThai=@trangThai, lucXacNhan=@xacNhan
        WHERE maPhieuTra=@ma AND (trangThai='Nhap');
        """, ct, Ma("@ma", entity.MaPhieuTra), Ma("@trangThai", entity.TrangThai), Luc("@xacNhan", entity.LucXacNhan));

    private async Task GhiDongAsync(PhieuTra entity, CancellationToken ct)
    {
        foreach (var dong in entity.DongTras)
        {
            await work.Ghi("""
                INSERT dbo.DongTra(maDongTra, maPhieuTra, maDongBan, soLuongTra, soLuongNhapLai)
                VALUES(@maDongTra, @maPhieuTra, @maDongBan, @soLuongTra, @soLuongNhapLai);
                """, ct,
                Ma("@maDongTra", dong.MaDongTra),
                Ma("@maPhieuTra", dong.MaPhieuTra),
                Ma("@maDongBan", dong.MaDongBan),
                So("@soLuongTra", dong.SoLuongTra),
                So("@soLuongNhapLai", dong.SoLuongNhapLai));
        }

        foreach (var dong in entity.KhoanHoans)
        {
            await work.Ghi("""
                INSERT dbo.KhoanHoan(maKhoanHoan, maPhieuTra, phuongThuc, soTienHoan)
                VALUES(@maKhoanHoan, @maPhieuTra, @phuongThuc, @soTienHoan);
                """, ct,
                Ma("@maKhoanHoan", dong.MaKhoanHoan),
                Ma("@maPhieuTra", dong.MaPhieuTra),
                Ma("@phuongThuc", dong.PhuongThuc),
                Tien("@soTienHoan", dong.SoTienHoan));
        }
    }
}
