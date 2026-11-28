using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

namespace Pos.Inventory.Factories;

internal sealed record NguonDongBan(string MaDongBan);
internal sealed record NguonDongTra(string MaDongTra);
internal sealed record NguonDongNhap(string MaDongNhap);
internal sealed record NguonDongKiem(string MaPhieuKiem);

internal abstract class BienDongKhoCreator<TNguon>
{
    public BienDongKho Tao(string maBienDong, string maHang, TNguon nguon, int soLuong, DateTime lucUtc)
        => KhoiTao(maBienDong, maHang, nguon, soLuong, MocGhiKho.ChuanHoa(lucUtc));

    protected abstract BienDongKho KhoiTao(string maBienDong, string maHang, TNguon nguon, int soLuong, DateTime lucUtc);

    protected static int LaySoLuongXuatBan(int soLuong)
        => soLuong > 0 ? -soLuong : throw new BusinessRuleViolationException("Số lượng bán phải lớn hơn 0.");
}

internal sealed class XuatBanLedgerCreator : BienDongKhoCreator<NguonDongBan>
{
    protected override BienDongKho KhoiTao(string maBienDong, string maHang, NguonDongBan nguon, int soLuong, DateTime lucUtc)
        => BienDongKho.Tao(maBienDong, maHang, LoaiBienDongKho.XuatBan, LaySoLuongXuatBan(soLuong), lucUtc,
            maDongBan: nguon.MaDongBan);
}

internal sealed class DaoXuatBanLedgerCreator : BienDongKhoCreator<NguonDongBan>
{
    protected override BienDongKho KhoiTao(string maBienDong, string maHang, NguonDongBan nguon, int soLuong, DateTime lucUtc)
        => BienDongKho.Tao(maBienDong, maHang, LoaiBienDongKho.DaoXuatBan, soLuong, lucUtc, maDongBan: nguon.MaDongBan);
}

internal sealed class NhapTraLedgerCreator : BienDongKhoCreator<NguonDongTra>
{
    protected override BienDongKho KhoiTao(string maBienDong, string maHang, NguonDongTra nguon, int soLuong, DateTime lucUtc)
        => BienDongKho.Tao(maBienDong, maHang, LoaiBienDongKho.NhapTraHang, soLuong, lucUtc, maDongTra: nguon.MaDongTra);
}

internal sealed class NhapKhoLedgerCreator : BienDongKhoCreator<NguonDongNhap>
{
    protected override BienDongKho KhoiTao(string maBienDong, string maHang, NguonDongNhap nguon, int soLuong, DateTime lucUtc)
        => BienDongKho.Tao(maBienDong, maHang, LoaiBienDongKho.NhapKho, soLuong, lucUtc, maDongNhap: nguon.MaDongNhap);
}

internal sealed class DaoNhapKhoLedgerCreator : BienDongKhoCreator<NguonDongNhap>
{
    protected override BienDongKho KhoiTao(string maBienDong, string maHang, NguonDongNhap nguon, int soLuong, DateTime lucUtc)
        => BienDongKho.Tao(maBienDong, maHang, LoaiBienDongKho.DaoNhapKho, soLuong, lucUtc, maDongNhap: nguon.MaDongNhap);
}

internal sealed class DieuChinhKiemKhoLedgerCreator : BienDongKhoCreator<NguonDongKiem>
{
    protected override BienDongKho KhoiTao(string maBienDong, string maHang, NguonDongKiem nguon, int soLuong, DateTime lucUtc)
        => BienDongKho.Tao(maBienDong, maHang, LoaiBienDongKho.DieuChinhKiemKho, soLuong, lucUtc,
            maPhieuKiem: nguon.MaPhieuKiem);
}
