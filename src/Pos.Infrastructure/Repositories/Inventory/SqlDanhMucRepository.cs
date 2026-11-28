using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Inventory.InventorySql;

namespace Pos.Infrastructure.Repositories.Inventory;

public sealed class SqlDanhMucRepository(SqlUnitOfWork work, SqlConnectionFactory factory)
    : IQuanLyDanhMucRepository
{
    private const string CotHang = "maHang, maNhom, tenHang, donViTinh, giaBanHienHanh, trangThai";
    private const string CotNhom = "maNhom, tenNhom, trangThai";
    private const string CotNhaCungCap = "maNhaCungCap, tenNhaCungCap, thongTinLienHe";

    public Task<MatHang?> LayMatHangAsync(string maHang, CancellationToken ct)
        => work.DocAsync(factory, $"SELECT {CotHang} FROM dbo.MatHang WHERE maHang = @ma;",
            (command, token) => DocMotAsync(command, DocHang, token), ct, Ma("@ma", maHang));

    public async Task KhoaMatHangAsync(string maHang, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT maHang FROM dbo.MatHang WITH (UPDLOCK, HOLDLOCK) WHERE maHang = @ma;
            """, Ma("@ma", maHang));
        if (await command.ExecuteScalarAsync(ct) is null)
        {
            throw new EntityNotFoundException($"Không tìm thấy mặt hàng {maHang}.");
        }
    }

    public Task<IReadOnlyList<MatHang>> TimMatHangAsync(string? tuKhoa, string? maNhom, CancellationToken ct)
        => work.DocAsync(factory, $"""
            SELECT {CotHang} FROM dbo.MatHang
            WHERE (@nhom IS NULL OR maNhom = @nhom)
              AND (@tukhoa IS NULL OR CHARINDEX(@tukhoa, maHang COLLATE DATABASE_DEFAULT) > 0
                   OR CHARINDEX(@tukhoa, tenHang) > 0)
            ORDER BY tenHang, maHang;
            """, (command, token) => DocNhieuAsync(command, DocHang, token), ct,
            Ma("@nhom", maNhom), Chu("@tukhoa", tuKhoa, 150));

    public Task<IReadOnlyList<NhomHang>> LayNhomHangAsync(CancellationToken ct)
        => work.DocAsync(factory, $"SELECT {CotNhom} FROM dbo.NhomHang ORDER BY tenNhom, maNhom;",
            (command, token) => DocNhieuAsync(command, DocNhom, token), ct);

    public Task<NhaCungCap?> LayNhaCungCapAsync(string maNhaCungCap, CancellationToken ct)
        => work.DocAsync(factory, $"SELECT {CotNhaCungCap} FROM dbo.NhaCungCap WHERE maNhaCungCap = @ma;",
            (command, token) => DocMotAsync(command, DocNhaCungCap, token), ct, Ma("@ma", maNhaCungCap));

    public Task<IReadOnlyList<NhaCungCap>> LayNhaCungCapAsync(CancellationToken ct)
        => work.DocAsync(factory, $"SELECT {CotNhaCungCap} FROM dbo.NhaCungCap ORDER BY tenNhaCungCap, maNhaCungCap;",
            (command, token) => DocNhieuAsync(command, DocNhaCungCap, token), ct);

    public async Task<NhomHang?> KhoaNhomHangAsync(string maNhom, CancellationToken ct)
    {
        await using var command = work.Lenh($"""
            SELECT {CotNhom} FROM dbo.NhomHang WITH (UPDLOCK, HOLDLOCK) WHERE maNhom = @ma;
            """, Ma("@ma", maNhom));
        return await DocMotAsync(command, DocNhom, ct);
    }

    public async Task<bool> CoMatHangDangHoatDongAsync(string maNhom, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT TOP (1) 1 FROM dbo.MatHang WHERE maNhom = @ma AND trangThai = 'HoatDong';
            """, Ma("@ma", maNhom));
        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<bool> DonViDaDuocSuDungAsync(string maHang, CancellationToken ct)
    {
        await using var command = work.Lenh("""
            SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.BienDongKho WHERE maHang = @ma)
                OR EXISTS (
                    SELECT 1 FROM dbo.DongNhap AS d
                    JOIN dbo.PhieuNhap AS p ON p.maPhieuNhap = d.maPhieuNhap
                    WHERE d.maHang = @ma AND p.trangThai = 'Nhap'
                )
                OR EXISTS (
                    SELECT 1 FROM dbo.DongKiem AS d
                    JOIN dbo.PhieuKiem AS p ON p.maPhieuKiem = d.maPhieuKiem
                    WHERE d.maHang = @ma AND d.soLuongDem IS NOT NULL AND p.trangThai <> 'DaHuy'
                )
                THEN 1 ELSE 0 END AS bit);
            """, Ma("@ma", maHang));
        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }

    public Task<string> SinhMaNhomAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('NH-', NEXT VALUE FOR dbo.Seq_NhomHang);", ct);

    public Task<string> SinhMaHangAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('MH-', NEXT VALUE FOR dbo.Seq_MatHang);", ct);

    public Task<string> SinhMaNhaCungCapAsync(CancellationToken ct)
        => work.SinhMa("SELECT CONCAT('NCC-', NEXT VALUE FOR dbo.Seq_NhaCungCap);", ct);

    public Task ThemNhomAsync(NhomHang nhom, CancellationToken ct)
        => ChanTrungAsync(() => work.Ghi($"INSERT INTO dbo.NhomHang ({CotNhom}) VALUES (@ma, @ten, @trangThai);",
            ct, Ma("@ma", nhom.MaNhom), Chu("@ten", nhom.TenNhom, 150), TrangThai(nhom.TrangThai)),
            "Tên nhóm hàng đã được sử dụng.");

    public Task CapNhatNhomAsync(NhomHang nhom, CancellationToken ct)
        => ChanTrungAsync(() => work.SuaMot("""
            UPDATE dbo.NhomHang SET tenNhom = @ten, trangThai = @trangThai WHERE maNhom = @ma;
            """, ct, Ma("@ma", nhom.MaNhom), Chu("@ten", nhom.TenNhom, 150), TrangThai(nhom.TrangThai)),
            "Tên nhóm hàng đã được sử dụng.");

    public Task ThemHangAsync(MatHang hang, CancellationToken ct)
        => ChanTrungAsync(() => work.Ghi($"""
            INSERT INTO dbo.MatHang ({CotHang}) VALUES (@ma, @nhom, @ten, @donVi, @gia, @trangThai);
            """, ct, ThamSoHang(hang)), "Tên mặt hàng đã được sử dụng.");

    public Task CapNhatHangAsync(MatHang hang, CancellationToken ct)
        => ChanTrungAsync(() => work.SuaMot("""
            UPDATE dbo.MatHang SET maNhom = @nhom, tenHang = @ten, donViTinh = @donVi,
                giaBanHienHanh = @gia, trangThai = @trangThai WHERE maHang = @ma;
            """, ct, ThamSoHang(hang)), "Tên mặt hàng đã được sử dụng.");

    public Task ThemNhaCungCapAsync(NhaCungCap nhaCungCap, CancellationToken ct)
        => ChanTrungAsync(() => work.Ghi($"""
            INSERT INTO dbo.NhaCungCap ({CotNhaCungCap}) VALUES (@ma, @ten, @lienHe);
            """, ct, ThamSoNhaCungCap(nhaCungCap)), "Tên nhà cung cấp đã được sử dụng.");

    public Task CapNhatNhaCungCapAsync(NhaCungCap nhaCungCap, CancellationToken ct)
        => ChanTrungAsync(() => work.SuaMot("""
            UPDATE dbo.NhaCungCap SET tenNhaCungCap = @ten, thongTinLienHe = @lienHe WHERE maNhaCungCap = @ma;
            """, ct, ThamSoNhaCungCap(nhaCungCap)), "Tên nhà cung cấp đã được sử dụng.");

    private static MatHang DocHang(SqlDataReader reader)
    {
        var hang = new MatHang(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetDecimal(4));
        if (reader.GetString(5) == TrangThaiChungTu.NgungSuDung)
        {
            hang.NgungSuDung();
        }

        return hang;
    }

    private static NhomHang DocNhom(SqlDataReader reader)
    {
        var nhom = new NhomHang(reader.GetString(0), reader.GetString(1));
        if (reader.GetString(2) == TrangThaiChungTu.NgungSuDung)
        {
            nhom.NgungSuDung();
        }

        return nhom;
    }

    private static NhaCungCap DocNhaCungCap(SqlDataReader reader)
        => new(reader.GetString(0), reader.GetString(1), reader.GetString(2));

    private static SqlParameter TrangThai(string value) => new("@trangThai", SqlDbType.VarChar, 24) { Value = value };

    private static SqlParameter[] ThamSoHang(MatHang entity) =>
    [
        Ma("@ma", entity.MaHang), Ma("@nhom", entity.MaNhom), Chu("@ten", entity.TenHang, 150),
        Chu("@donVi", entity.DonViTinh, 32), Tien("@gia", entity.GiaBanHienHanh), TrangThai(entity.TrangThai)
    ];

    private static SqlParameter[] ThamSoNhaCungCap(NhaCungCap entity) =>
    [
        Ma("@ma", entity.MaNhaCungCap), Chu("@ten", entity.TenNhaCungCap, 150), Chu("@lienHe", entity.ThongTinLienHe, 500)
    ];
}
