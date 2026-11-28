namespace Pos.Web.Demo;

public sealed partial class KhoDemoStore
{
    internal MatHangDemoItem[] DanhMucBanHang() => MatHangDangDung()
        .Select(hang => new MatHangDemoItem(hang.Ma, hang.Ten, _nhomHangs[hang.MaNhom].Ten,
            hang.DonVi, hang.GiaBan, TonHienTai(hang.Ma)))
        .ToArray();

    // Quầy và Kho dùng cùng khóa phiên. GhiBienDong kiểm tra toàn bộ lô trước khi thêm dòng nào.
    internal void GhiBienDongQuay(string loai, string maChungTu, DateTimeOffset luc,
        IEnumerable<(string MaHang, int SoLuong)> dongs)
    {
        GhiBienDong(dongs.Select((dong, i) =>
            BienDong(dong.MaHang, loai, dong.SoLuong, luc, maChungTu, $":{i + 1}")));
    }
}
