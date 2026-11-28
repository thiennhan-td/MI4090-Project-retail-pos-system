using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Inventory.Factories;

namespace Pos.Inventory.Services;

public sealed record YeuCauBienDongKho(string MaHang, string LoaiSuKien, int SoLuongThayDoi,
    DateTime ThoiDiemUtc, string MaNguon);

public interface IGhiBienDongKhoNoiBo
{
    /// <summary>Ghi trong giao dịch đã mở của nghiệp vụ nhập hàng hoặc kiểm kho.</summary>
    Task<KetQuaGhiTheKhoDto> GhiBienDongNoiBoAsync(IReadOnlyList<YeuCauBienDongKho> yeuCau, CancellationToken ct);
}

public sealed record TheKhoDto(MatHang MatHang, int TonDauKy, int TonCuoiKy, int TonHienHanh,
    IReadOnlyList<BienDongKho> BienDongs);

public sealed class InventoryLedgerService(
    IBienDongKhoRepository bienDongRepo,
    IDanhMucRepository danhMucRepo,
    IAuthorizationService authorization,
    IUnitOfWork work) : IInventoryService, IGhiBienDongKhoNoiBo
{
    private static readonly XuatBanLedgerCreator XuatBan = new();
    private static readonly DaoXuatBanLedgerCreator DaoXuatBan = new();
    private static readonly NhapTraLedgerCreator NhapTra = new();
    private static readonly NhapKhoLedgerCreator NhapKho = new();
    private static readonly DaoNhapKhoLedgerCreator DaoNhapKho = new();
    private static readonly DieuChinhKiemKhoLedgerCreator DieuChinhKiemKho = new();

    public Task<KetQuaGhiTheKhoDto> GhiNhanXuatBanAsync(YeuCauXuatBanDto req, CancellationToken ct)
        => GhiTheoChungTuAsync(req.DongHang, req.MaHoaDon, (ma, dong) => XuatBan.Tao(ma, dong.MaHang,
            new(dong.MaDongBan), dong.SoLuongBan, req.LucXacNhanUtc), ct);

    public Task<KetQuaGhiTheKhoDto> DaoXuatBanAsync(YeuCauDaoXuatBanDto req, CancellationToken ct)
        => GhiTheoChungTuAsync(req.DongHang, req.MaHoaDon, (ma, dong) => DaoXuatBan.Tao(ma, dong.MaHang,
            new(dong.MaDongBan), dong.SoLuongBan, req.LucHuyUtc), ct);

    public Task<KetQuaGhiTheKhoDto> GhiNhanNhapTraHangAsync(YeuCauNhapTraDto req, CancellationToken ct)
        => GhiTheoChungTuAsync(req.DongHang, req.MaPhieuTra, (ma, dong) => NhapTra.Tao(ma, dong.MaHang,
            new(dong.MaDongTra), dong.SoLuongNhapLai, req.LucXacNhanUtc), ct);

    public Task<KetQuaGhiTheKhoDto> GhiBienDongNoiBoAsync(IReadOnlyList<YeuCauBienDongKho> yeuCau, CancellationToken ct)
    {
        if (yeuCau.Count != 0 && yeuCau.Any(dong => dong.LoaiSuKien != yeuCau[0].LoaiSuKien))
        {
            throw new BusinessRuleViolationException("Một lần ghi thẻ kho phải có cùng loại sự kiện.");
        }

        return GhiAsync(yeuCau, null, TaoBienDongNoiBo, ct);
    }

    public Task<int> LayTonHienHanhAsync(string maHang, CancellationToken ct)
        => DocTrongGiaoDichAsync(async () =>
        {
            await danhMucRepo.KhoaMatHangAsync(maHang, ct);
            return await bienDongRepo.LayTonHienHanhAsync(maHang, ct);
        }, ct);

    /// <summary>Tra thẻ kho theo khoảng UTC gồm đầu kỳ và loại trừ cuối kỳ.</summary>
    public async Task<TheKhoDto> TraTheKhoAsync(string maTaiKhoan, string maHang, DateTime tuUtc, DateTime denUtc,
        CancellationToken ct = default)
    {
        await authorization.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.KHO_TRA_SO_HANG, ct);
        MocGhiKho.KiemTraUtc(tuUtc);
        MocGhiKho.KiemTraUtc(denUtc);
        if (tuUtc >= denUtc)
        {
            throw new BusinessRuleViolationException("Đầu kỳ thẻ kho phải trước cuối kỳ.");
        }

        return await DocTrongGiaoDichAsync(async () =>
        {
            await danhMucRepo.KhoaMatHangAsync(maHang, ct);
            var hang = await danhMucRepo.LayMatHangAsync(maHang, ct)
                ?? throw new EntityNotFoundException($"Không tìm thấy mặt hàng {maHang}.");
            var tonDau = await bienDongRepo.LayTonTruocMocAsync(maHang, tuUtc, ct);
            var tonCuoi = await bienDongRepo.LayTonTruocMocAsync(maHang, denUtc, ct);
            var tonHienHanh = await bienDongRepo.LayTonHienHanhAsync(maHang, ct);
            var bienDongs = await bienDongRepo.LayTrongKyAsync(maHang, tuUtc, denUtc, ct);
            return new TheKhoDto(hang, tonDau, tonCuoi, tonHienHanh, bienDongs);
        }, ct);
    }

    private Task<T> DocTrongGiaoDichAsync<T>(Func<Task<T>> doc, CancellationToken ct)
        => bienDongRepo.CoGiaoDich ? doc() : InventoryTransaction.ChayAsync(work, doc, ct);

    private Task<KetQuaGhiTheKhoDto> GhiTheoChungTuAsync<TDong>(IReadOnlyList<TDong> dongHang, string maChungTu,
        Func<string, TDong, BienDongKho> taoBienDong, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(maChungTu) || maChungTu.Length > 32 || dongHang.Count == 0)
        {
            throw new BusinessRuleViolationException("Cần có mã chứng từ và các dòng phát sinh thẻ kho.");
        }
        return GhiAsync(dongHang, maChungTu, taoBienDong, ct);
    }

    private async Task<KetQuaGhiTheKhoDto> GhiAsync<TDong>(IReadOnlyList<TDong> dongHang, string? maChungTu,
        Func<string, TDong, BienDongKho> taoBienDong, CancellationToken ct)
    {
        if (!bienDongRepo.CoGiaoDich)
        {
            throw new InvalidOperationException("Cần mở giao dịch của chứng từ trước khi ghi thẻ kho.");
        }
        if (dongHang.Count == 0)
        {
            return new KetQuaGhiTheKhoDto(0, 0);
        }

        var bienDongs = new List<BienDongKho>(dongHang.Count);
        var khoaSuKiens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dong in dongHang)
        {
            var bienDong = taoBienDong(await bienDongRepo.SinhMaAsync(ct), dong);
            if (!khoaSuKiens.Add(bienDong.KhoaSuKien))
            {
                throw new DuplicateRequestException($"Khóa sự kiện {bienDong.KhoaSuKien} bị lặp trong yêu cầu.");
            }
            bienDongs.Add(bienDong);
        }

        var theoHang = bienDongs.GroupBy(dong => dong.MaHang, StringComparer.Ordinal)
            .OrderBy(nhom => nhom.Key, StringComparer.Ordinal).ToArray();
        foreach (var nhom in theoHang)
        {
            await danhMucRepo.KhoaMatHangAsync(nhom.Key, ct);
        }

        var soDongDaTonTai = 0;
        foreach (var bienDong in bienDongs)
        {
            var daCo = await bienDongRepo.LayTheoKhoaSuKienAsync(bienDong.KhoaSuKien, ct);
            if (daCo is not null)
            {
                if (!TrungNoiDung(bienDong, daCo))
                {
                    throw new DuplicateRequestException($"Khóa sự kiện {bienDong.KhoaSuKien} đã được ghi với nội dung khác.");
                }
                soDongDaTonTai++;
            }
        }

        if (soDongDaTonTai != 0 && soDongDaTonTai != bienDongs.Count)
        {
            throw new DuplicateRequestException("Yêu cầu chứa cả sự kiện đã ghi và sự kiện mới.");
        }
        await KiemTraCacNguonAsync(bienDongs, maChungTu, soDongDaTonTai != 0, ct);
        if (soDongDaTonTai != 0)
        {
            return new KetQuaGhiTheKhoDto(0, soDongDaTonTai);
        }

        var xuatBan = bienDongs[0].LoaiSuKien == LoaiBienDongKho.XuatBan;
        foreach (var nhom in theoHang)
        {
            if (xuatBan && (await danhMucRepo.LayMatHangAsync(nhom.Key, ct))?.TrangThai != TrangThaiChungTu.HoatDong)
            {
                throw new BusinessRuleViolationException($"Mặt hàng {nhom.Key} không hoạt động.");
            }

            var thayDoi = nhom.Sum(dong => (long)dong.SoLuongThayDoi);
            var ton = await bienDongRepo.LayTonHienHanhAsync(nhom.Key, ct);
            if (thayDoi < -int.MaxValue || thayDoi + ton > int.MaxValue)
            {
                throw new BusinessRuleViolationException($"Số lượng của mặt hàng {nhom.Key} vượt phạm vi lưu trữ.");
            }
            if (thayDoi + ton < 0)
            {
                throw new InsufficientStockException(nhom.Key, ton, checked((int)Math.Max(0, -thayDoi)));
            }
        }

        foreach (var nhom in theoHang)
        {
            foreach (var bienDong in nhom)
            {
                await bienDongRepo.ThemAsync(bienDong, ct);
            }
        }
        return new KetQuaGhiTheKhoDto(bienDongs.Count, 0);
    }

    private async Task KiemTraCacNguonAsync(IReadOnlyList<BienDongKho> bienDongs, string? maChungTu,
        bool daGhi, CancellationToken ct)
    {
        var dauTien = bienDongs[0];
        maChungTu ??= (await bienDongRepo.LayNguonAsync(dauTien, ct))?.MaChungTu
            ?? throw new BusinessRuleViolationException($"Không tìm thấy nguồn {dauTien.KhoaSuKien}.");

        var cacNguon = await bienDongRepo.LayCacNguonAsync(dauTien.LoaiSuKien, maChungTu, ct);
        if (cacNguon.Count != bienDongs.Count)
        {
            if (daGhi)
            {
                throw new DuplicateRequestException($"Yêu cầu gửi lại không có đủ các dòng nguồn của chứng từ {maChungTu}.");
            }
            throw new BusinessRuleViolationException($"Danh sách dòng ghi thẻ kho không khớp chứng từ {maChungTu}.");
        }

        var theoNguon = cacNguon.ToDictionary(nguon => (nguon.MaNguon, nguon.MaHang));
        foreach (var bienDong in bienDongs)
        {
            var maNguon = bienDong.MaDongBan ?? bienDong.MaDongTra ?? bienDong.MaDongNhap ?? bienDong.MaPhieuKiem!;
            if (!theoNguon.TryGetValue((maNguon, bienDong.MaHang), out var nguon)
                || nguon.MaChungTu != maChungTu || nguon.SoLuongThayDoi != bienDong.SoLuongThayDoi)
            {
                throw new BusinessRuleViolationException($"Mặt hàng, số lượng hoặc chứng từ không khớp nguồn {bienDong.KhoaSuKien}.");
            }
            if (!daGhi && !nguon.CoHieuLuc)
            {
                throw new InvalidStateTransitionException($"Nguồn {bienDong.KhoaSuKien} chưa đủ điều kiện để ghi thẻ kho.");
            }
            if (!daGhi && nguon.LucHieuLucUtc != bienDong.ThoiDiemGhi)
            {
                throw new BusinessRuleViolationException($"Thời điểm ghi thẻ kho không khớp nguồn {bienDong.KhoaSuKien}.");
            }
        }
    }

    private static bool TrungNoiDung(BienDongKho a, BienDongKho b)
        => a.KhoaSuKien == b.KhoaSuKien && a.MaHang == b.MaHang && a.LoaiSuKien == b.LoaiSuKien
        && a.SoLuongThayDoi == b.SoLuongThayDoi && a.ThoiDiemGhi == b.ThoiDiemGhi
        && a.MaDongBan == b.MaDongBan && a.MaDongTra == b.MaDongTra
        && a.MaDongNhap == b.MaDongNhap && a.MaPhieuKiem == b.MaPhieuKiem;

    private static BienDongKho TaoBienDongNoiBo(string ma, YeuCauBienDongKho dong)
        => dong.LoaiSuKien switch
        {
            LoaiBienDongKho.NhapKho => NhapKho.Tao(ma, dong.MaHang, new(dong.MaNguon), dong.SoLuongThayDoi, dong.ThoiDiemUtc),
            LoaiBienDongKho.DaoNhapKho => DaoNhapKho.Tao(ma, dong.MaHang, new(dong.MaNguon), dong.SoLuongThayDoi, dong.ThoiDiemUtc),
            LoaiBienDongKho.DieuChinhKiemKho => DieuChinhKiemKho.Tao(ma, dong.MaHang, new(dong.MaNguon),
                dong.SoLuongThayDoi, dong.ThoiDiemUtc),
            _ => throw new BusinessRuleViolationException("Loại biến động kho nội bộ không hợp lệ.")
        };
}
