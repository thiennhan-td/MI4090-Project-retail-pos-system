using System.Globalization;

namespace Pos.Web.Demo;

public static class HienThiKho
{
    public static string Tien(decimal value) => value.ToString("#,##0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    public static string So(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));

    public static string Nhan(string value) => value switch
    {
        "Nhap" => "Nháp",
        "DaXacNhan" => "Đã xác nhận",
        "DaHuy" => "Đã hủy",
        "DangKiem" => "Đang kiểm",
        "ChoDuyet" => "Chờ duyệt",
        "HoanTat" => "Hoàn tất",
        "NhapKho" => "Nhập kho",
        "DaoNhapKho" => "Đảo nhập kho",
        "XuatBan" => "Xuất bán",
        "DaoXuatBan" => "Đảo xuất bán",
        "NhapTraHang" => "Nhập lại hàng trả",
        "DieuChinhKiemKho" => "Điều chỉnh kiểm kho",
        "ChuyenKhoan" => "Chuyển khoản",
        "TienMat" => "Tiền mặt",
        "KetCa" => "Két ca",
        "QuyCuaHang" => "Quỹ cửa hàng",
        _ => value
    };

    public static string Mau(string value) => value switch
    {
        "DaXacNhan" or "HoanTat" => "kho-badge kho-badge-ok",
        "DangKiem" or "ChoDuyet" or "Nhap" => "kho-badge kho-badge-wait",
        _ => "kho-badge"
    };
}
