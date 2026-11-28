using System.Security.Cryptography;
using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Inventory.Factories;

namespace Pos.Inventory.Services;

public sealed record DuLieuDongNhapDto(string MaHang, int SoLuongNhap, decimal DonGiaNhap);

public sealed record PhieuNhapDto(string MaPhieuNhap, string MaNhaCungCap, string MaNguoiLap, DateTime LucLap,
    DateOnly? HanThanhToan, string TrangThai, DateTime? LucXacNhan, string? MaNguoiHuy, DateTime? LucHuy,
    string? LyDoHuy, IReadOnlyList<DongNhap> DongNhaps, decimal TongTien, string PhienBan);

public sealed record LichSuTraNhaCungCapDto(decimal DaTra, decimal DuNo, IReadOnlyList<TraNhaCungCap> KhoanTras);

public sealed class PurchaseService(IPhieuNhapRepository phieuNhapRepo, IDanhMucRepository danhMucRepo,
    IAuthorizationService auth, IShiftCashService shiftCash, IGhiBienDongKhoNoiBo ledger, IUnitOfWork work,
    TimeProvider? dongHo = null)
{
    private readonly TimeProvider _dongHo = dongHo ?? TimeProvider.System;

    public async Task<PhieuNhapDto> LapNhapAsync(string maTaiKhoan, string maNhaCungCap, DateOnly? hanThanhToan,
        IReadOnlyList<DuLieuDongNhapDto> dongNhaps, CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NHAP_LAP_NHAP, ct);
        var dongs = dongNhaps.ToArray();
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            await KiemTraDanhMucAsync(maNhaCungCap, dongs, ct);
            var maPhieuNhap = await phieuNhapRepo.SinhMaPhieuNhapAsync(ct);
            var phieu = new PhieuNhap(maPhieuNhap, maNhaCungCap, maTaiKhoan, LayLucUtc(), hanThanhToan);
            phieu.LuuNhap(maNhaCungCap, hanThanhToan, await TaoDongsAsync(phieu, dongs, ct));
            await phieuNhapRepo.ThemAsync(phieu, ct);
            return ToDto(phieu);
        }, ct);
    }

    public async Task<PhieuNhapDto> SuaNhapAsync(string maPhieuNhap, string maTaiKhoan, string phienBan,
        string maNhaCungCap, DateOnly? hanThanhToan, IReadOnlyList<DuLieuDongNhapDto> dongNhaps,
        CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NHAP_LAP_NHAP, ct);
        var dongs = dongNhaps.ToArray();
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = await KhoaPhieuAsync(maPhieuNhap, ct);
            KiemTraPhienBan(phieu, phienBan);
            if (phieu.TrangThai != TrangThaiChungTu.Nhap)
            {
                throw new InvalidStateTransitionException("Chỉ sửa được phiếu nhập nháp.");
            }

            await KiemTraDanhMucAsync(maNhaCungCap, dongs, ct);
            var dongCu = phieu.DongNhaps.ToArray();
            phieu.LuuNhap(maNhaCungCap, hanThanhToan, await TaoDongsAsync(phieu, dongs, ct));
            await phieuNhapRepo.CapNhatNhapAsync(phieu, dongCu, ct);
            return ToDto(phieu);
        }, ct);
    }

    public async Task<PhieuNhapDto> XacNhanNhapAsync(string maPhieuNhap, string maTaiKhoan, string phienBan,
        CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NHAP_XAC_NHAN, ct);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = await KhoaPhieuAsync(maPhieuNhap, ct);
            KiemTraPhienBan(phieu, phienBan);
            if (phieu.TrangThai == TrangThaiChungTu.DaXacNhan)
            {
                return ToDto(phieu);
            }

            if (phieu.TrangThai != TrangThaiChungTu.Nhap)
            {
                throw new InvalidStateTransitionException("Chỉ xác nhận được phiếu nhập nháp.");
            }

            await KhoaHangAsync(phieu.DongNhaps.Select(dong => dong.MaHang), kiemTraHoatDong: true, ct);
            var luc = LayLucUtc();
            phieu.XacNhan(luc);
            await phieuNhapRepo.CapNhatTrangThaiAsync(phieu, TrangThaiChungTu.Nhap, ct);
            await ledger.GhiBienDongNoiBoAsync(phieu.DongNhaps.Select(dong =>
                new YeuCauBienDongKho(dong.MaHang, LoaiBienDongKho.NhapKho, dong.SoLuongNhap, luc, dong.MaDongNhap))
                .ToArray(), ct);
            return ToDto(phieu);
        }, ct);
    }

    public async Task<PhieuNhapDto> HuyNhapAsync(string maPhieuNhap, string maTaiKhoan, string trangThaiTruoc,
        string phienBan, string lyDoHuy, CancellationToken ct = default)
    {
        var maQuyen = trangThaiTruoc switch
        {
            TrangThaiChungTu.Nhap => MaQuyenPos.NHAP_HUY_NHAP,
            TrangThaiChungTu.DaXacNhan => MaQuyenPos.NHAP_HUY_XAC_NHAN,
            _ => throw new InvalidStateTransitionException("Chỉ hủy được phiếu nháp hoặc phiếu đã xác nhận.")
        };
        await KiemTraQuyenAsync(maTaiKhoan, maQuyen, ct);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = await KhoaPhieuAsync(maPhieuNhap, ct);
            KiemTraPhienBan(phieu, phienBan);
            if (phieu.TrangThai != trangThaiTruoc)
            {
                throw new ConcurrencyConflictException("Phiếu nhập đã đổi trạng thái. Hãy tải lại phiếu.");
            }

            var daoKho = trangThaiTruoc == TrangThaiChungTu.DaXacNhan;
            if (daoKho)
            {
                await KhoaHangAsync(phieu.DongNhaps.Select(dong => dong.MaHang), kiemTraHoatDong: false, ct);
            }

            var luc = LayLucUtc();
            phieu.Huy(maTaiKhoan, luc, lyDoHuy);
            await phieuNhapRepo.CapNhatTrangThaiAsync(phieu, trangThaiTruoc, ct);
            if (daoKho)
            {
                await ledger.GhiBienDongNoiBoAsync(phieu.DongNhaps.Select(dong =>
                    new YeuCauBienDongKho(dong.MaHang, LoaiBienDongKho.DaoNhapKho, -dong.SoLuongNhap, luc,
                        dong.MaDongNhap)).ToArray(), ct);
            }
            return ToDto(phieu);
        }, ct);
    }

    /// <param name="maYeuCau">Mã của một lần trả tiền. Giữ nguyên mã khi gửi lại cùng yêu cầu.</param>
    public async Task<TraNhaCungCap> GhiTraNhaCungCapAsync(string maPhieuNhap, string maTaiKhoan, decimal soTienTra,
        string phuongThuc, string nguonChi, string? maCaChi, Guid maYeuCau, CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NCC_GHI_TRA_TIEN, ct);
        if (maYeuCau == Guid.Empty)
        {
            throw new BusinessRuleViolationException("Cần mã yêu cầu ghi trả nhà cung cấp.");
        }
        var maKhoanTra = "TNCC-" + Convert.ToBase64String(maYeuCau.ToByteArray())
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var daGhi = await phieuNhapRepo.LayKhoanTraTheoMaAsync(maKhoanTra, ct);
            if (daGhi is not null)
            {
                return await KhoaVaDoiChieuKhoanDaGhiAsync(daGhi.MaPhieuNhap);
            }

            KiemTraMa(maPhieuNhap);
            TraNhaCungCap.KiemTraKhoanTra(soTienTra, phuongThuc, nguonChi, maCaChi);
            ThongTinCaMoDto? ca = null;
            if (maCaChi is not null)
            {
                KiemTraMa(maCaChi);
                try
                {
                    // Khoản chi mới khóa ca trước phiếu, cùng thứ tự với các giao dịch két.
                    ca = await shiftCash.KiemTraVaKhoaCaMoChoChiNccAsync(maCaChi, ct);
                    if (ca?.MaCa != maCaChi)
                    {
                        throw new EntityNotFoundException("Không tìm thấy ca đang mở để chi nhà cung cấp.");
                    }
                }
                catch (PosDomainException loi) when (loi is EntityNotFoundException or InvalidStateTransitionException)
                {
                    // Khoản chi có thể vừa được ghi trong lúc chờ khóa ca, trước khi ca đóng.
                    daGhi = await phieuNhapRepo.LayKhoanTraTheoMaAsync(maKhoanTra, ct);
                    if (daGhi is null)
                    {
                        throw;
                    }
                    return await KhoaVaDoiChieuKhoanDaGhiAsync(daGhi.MaPhieuNhap);
                }
            }

            var phieu = await KhoaPhieuAsync(maPhieuNhap, ct);
            daGhi = phieu.KhoanTras.FirstOrDefault(khoan => khoan.MaKhoanTra == maKhoanTra)
                ?? await phieuNhapRepo.LayKhoanTraTheoMaAsync(maKhoanTra, ct);
            if (daGhi is not null)
            {
                return DoiChieuKhoanDaGhi(daGhi);
            }

            var luc = LayLucUtc();
            if (ca is not null && luc < ca.LucMoUtc)
            {
                throw new BusinessRuleViolationException("Thời điểm chi nhà cung cấp phải từ lúc mở ca.");
            }

            var khoanTra = new TraNhaCungCap(maKhoanTra, phieu.MaPhieuNhap,
                maTaiKhoan, maCaChi, luc, soTienTra, phuongThuc, nguonChi);
            phieu.ThemTraNhaCungCap(khoanTra);
            await phieuNhapRepo.ThemKhoanTraAsync(khoanTra, ct);
            return khoanTra;
        }, ct);

        async Task<TraNhaCungCap> KhoaVaDoiChieuKhoanDaGhiAsync(string maPhieuGoc)
        {
            var phieu = await KhoaPhieuAsync(maPhieuGoc, ct);
            var khoan = phieu.KhoanTras.FirstOrDefault(khoan => khoan.MaKhoanTra == maKhoanTra)
                ?? throw new ConcurrencyConflictException("Khoản trả đã thay đổi. Hãy tải lại phiếu nhập.");
            return DoiChieuKhoanDaGhi(khoan);
        }

        TraNhaCungCap DoiChieuKhoanDaGhi(TraNhaCungCap khoan)
        {
            if (khoan.MaPhieuNhap != maPhieuNhap || khoan.MaNguoiGhi != maTaiKhoan || khoan.SoTienTra != soTienTra
                || khoan.PhuongThuc != phuongThuc || khoan.NguonChi != nguonChi || khoan.MaCaChi != maCaChi)
            {
                throw new DuplicateRequestException("Mã yêu cầu ghi trả đã được dùng với nội dung khác.");
            }
            return khoan;
        }
    }

    public async Task<PhieuNhapDto?> LayPhieuNhapAsync(string maPhieuNhap, string maTaiKhoan,
        CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NHAP_TRA_CUU, ct);
        KiemTraMa(maPhieuNhap);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = await phieuNhapRepo.LayTheoMaAsync(maPhieuNhap, ct);
            return phieu?.MaPhieuNhap == maPhieuNhap ? ToDto(phieu) : null;
        }, ct);
    }

    public async Task<IReadOnlyList<TomTatPhieuNhap>> TraCuuAsync(string maTaiKhoan, string? maNhaCungCap = null,
        string? trangThai = null, DateTime? tuUtc = null, DateTime? denUtc = null, bool xemCongNo = false,
        CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NHAP_TRA_CUU, ct);
        if (xemCongNo)
        {
            await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NCC_TRA_NO, ct);
        }
        if (maNhaCungCap is not null)
        {
            KiemTraMa(maNhaCungCap);
        }
        if (trangThai is not (null or TrangThaiChungTu.Nhap or TrangThaiChungTu.DaXacNhan or TrangThaiChungTu.DaHuy))
        {
            throw new BusinessRuleViolationException("Trạng thái phiếu nhập không hợp lệ.");
        }
        if (tuUtc is { Kind: not DateTimeKind.Utc } || denUtc is { Kind: not DateTimeKind.Utc } || tuUtc >= denUtc)
        {
            throw new BusinessRuleViolationException("Khoảng thời gian tra cứu phiếu nhập không hợp lệ.");
        }
        return await phieuNhapRepo.TraCuuAsync(maNhaCungCap, trangThai, tuUtc, denUtc, xemCongNo, ct);
    }

    public async Task<LichSuTraNhaCungCapDto> LayLichSuTraAsync(string maPhieuNhap, string maTaiKhoan,
        CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NCC_TRA_NO, ct);
        KiemTraMa(maPhieuNhap);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var phieu = YeuCauPhieu(maPhieuNhap, await phieuNhapRepo.LayTheoMaAsync(maPhieuNhap, ct));
            return new LichSuTraNhaCungCapDto(phieu.DaTra, phieu.DuNo, phieu.KhoanTras.ToArray());
        }, ct);
    }

    public async Task<IReadOnlyList<ThongTinCaMoDto>> LayDanhSachCaChiAsync(string maTaiKhoan,
        CancellationToken ct = default)
    {
        await KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.NCC_GHI_TRA_TIEN, ct);
        return await shiftCash.LayDanhSachCaDangMoAsync(ct);
    }

    private async Task KiemTraQuyenAsync(string maTaiKhoan, string maQuyen, CancellationToken ct)
    {
        KiemTraMa(maTaiKhoan);
        await auth.KiemTraQuyenAsync(maTaiKhoan, maQuyen, ct);
    }

    private async Task<PhieuNhap> KhoaPhieuAsync(string maPhieuNhap, CancellationToken ct)
    {
        KiemTraMa(maPhieuNhap);
        return YeuCauPhieu(maPhieuNhap, await phieuNhapRepo.KhoaTheoMaAsync(maPhieuNhap, ct));
    }

    private static PhieuNhap YeuCauPhieu(string maPhieuNhap, PhieuNhap? phieu) => phieu?.MaPhieuNhap == maPhieuNhap
        ? phieu
        : throw new EntityNotFoundException("Không tìm thấy phiếu nhập.");

    private async Task KiemTraDanhMucAsync(string maNhaCungCap, IReadOnlyList<DuLieuDongNhapDto> dongs,
        CancellationToken ct)
    {
        KiemTraMa(maNhaCungCap);
        var nhaCungCap = await danhMucRepo.LayNhaCungCapAsync(maNhaCungCap, ct);
        if (nhaCungCap?.MaNhaCungCap != maNhaCungCap)
        {
            throw new EntityNotFoundException("Không tìm thấy nhà cung cấp.");
        }
        await KhoaHangAsync(dongs.Select(dong => dong.MaHang), kiemTraHoatDong: true, ct);
    }

    private async Task KhoaHangAsync(IEnumerable<string> maHangs, bool kiemTraHoatDong, CancellationToken ct)
    {
        foreach (var maHang in maHangs.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            KiemTraMa(maHang);
            await danhMucRepo.KhoaMatHangAsync(maHang, ct);
            if (!kiemTraHoatDong)
            {
                continue;
            }

            var hang = await danhMucRepo.LayMatHangAsync(maHang, ct);
            if (hang?.MaHang != maHang || hang.TrangThai != TrangThaiChungTu.HoatDong)
            {
                throw new BusinessRuleViolationException($"Mặt hàng {maHang} phải đang hoạt động.");
            }
        }
    }

    private async Task<IReadOnlyList<DongNhap>> TaoDongsAsync(PhieuNhap phieu, IReadOnlyList<DuLieuDongNhapDto> dongs,
        CancellationToken ct)
    {
        var maDongCu = phieu.DongNhaps.ToLookup(dong => dong.MaHang, StringComparer.Ordinal);
        var ketQua = new List<DongNhap>(dongs.Count);
        foreach (var dong in dongs)
        {
            var maDong = maDongCu[dong.MaHang].FirstOrDefault()?.MaDongNhap ?? await phieuNhapRepo.SinhMaDongNhapAsync(ct);
            ketQua.Add(new DongNhap(maDong, phieu.MaPhieuNhap, dong.MaHang, dong.SoLuongNhap, dong.DonGiaNhap));
        }
        return ketQua;
    }

    private static void KiemTraMa(string ma)
    {
        if (string.IsNullOrWhiteSpace(ma) || ma.Length > 32 || ma != ma.Trim())
        {
            throw new BusinessRuleViolationException("Mã tham chiếu không hợp lệ.");
        }
    }

    private DateTime LayLucUtc() => MocGhiKho.ChuanHoa(_dongHo.GetUtcNow().UtcDateTime);

    private static void KiemTraPhienBan(PhieuNhap phieu, string phienBan)
    {
        if (phienBan != TaoPhienBan(phieu))
        {
            throw new ConcurrencyConflictException("Phiếu nhập đã thay đổi. Hãy tải lại phiếu.");
        }
    }

    private static string TaoPhienBan(PhieuNhap phieu)
    {
        // Phiên bản gắn với nội dung phiếu. Xác nhận lại cùng nội dung sử dụng phiên bản cũ.
        using var duLieu = new MemoryStream();
        using var ghi = new BinaryWriter(duLieu);
        ghi.Write(phieu.MaPhieuNhap);
        ghi.Write(phieu.MaNhaCungCap);
        ghi.Write(phieu.HanThanhToan?.DayNumber ?? -1);
        foreach (var dong in phieu.DongNhaps.OrderBy(dong => dong.MaDongNhap, StringComparer.Ordinal))
        {
            ghi.Write(dong.MaDongNhap);
            ghi.Write(dong.MaHang);
            ghi.Write(dong.SoLuongNhap);
            ghi.Write(decimal.Truncate(dong.DonGiaNhap));
        }
        ghi.Flush();
        return Convert.ToHexString(SHA256.HashData(duLieu.GetBuffer().AsSpan(0, checked((int)duLieu.Length))));
    }

    private static PhieuNhapDto ToDto(PhieuNhap phieu) => new(phieu.MaPhieuNhap, phieu.MaNhaCungCap, phieu.MaNguoiLap,
        phieu.LucLap, phieu.HanThanhToan, phieu.TrangThai, phieu.LucXacNhan, phieu.MaNguoiHuy, phieu.LucHuy,
        phieu.LyDoHuy, phieu.DongNhaps.ToArray(), phieu.TinhGiaTriAnhHuong(), TaoPhienBan(phieu));
}
