using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.Kho;

namespace Pos.Web.Controllers;

public sealed class BaoCaoController(KhoDemoSessionProvider sessions) : KhoDemoController(sessions)
{
    [HttpGet]
    public IActionResult XuatNhapTon(KyBaoCaoViewModel ky, bool csv = false) => TrongPhien(store =>
    {
        var nhomHangs = store.NhomHangs();
        if (ky.MaNhom is not null && !nhomHangs.Any(x => string.Equals(x.Ma, ky.MaNhom, StringComparison.OrdinalIgnoreCase)))
            return NotFound();
        KiemTraKy(ky);
        XuatNhapTonDemo[] dongs = ModelState.IsValid ? store.XuatNhapTon(ky.Tu, ky.Den, ky.MaNhom) : [];
        if (csv && !ModelState.IsValid) return BadRequest("Khoảng ngày báo cáo không hợp lệ.");
        return csv
            ? Csv("xuat-nhap-ton", ["Mã hàng", "Mặt hàng", "Nhóm hàng", "Đơn vị", "Tồn đầu", "Nhập mua ròng", "Xuất bán ròng",
                    "Trả nhập lại", "Điều chỉnh tồn", "Tổng nhập", "Tổng xuất", "Tồn cuối"],
                dongs.Select(x => new object[] { x.Hang.Ma, x.Hang.Ten, x.TenNhom, x.Hang.DonVi, x.TonDau,
                    x.NhapMuaRong, x.XuatBanRong, x.TraNhapLai, x.DieuChinhTon, x.Nhap, x.Xuat, x.TonCuoi }))
            : View("~/Views/nv_BaoCao/XuatNhapTon.cshtml", new XuatNhapTonViewModel(ky, nhomHangs, dongs));
    }, "BC_XUAT_NHAP_TON");

    [HttpGet]
    public IActionResult CongNoNcc(KyBaoCaoViewModel ky, bool csv = false) => TrongPhien(store =>
    {
        var nhaCungCaps = store.NhaCungCaps();
        if (ky.MaNcc is not null && !nhaCungCaps.Any(x => string.Equals(x.Ma, ky.MaNcc, StringComparison.OrdinalIgnoreCase)))
            return NotFound();
        KiemTraKy(ky);
        CongNoNccDemo[] dongs = ModelState.IsValid ? store.CongNoNcc(ky.Tu, ky.Den, ky.MaNcc) : [];
        if (csv && !ModelState.IsValid) return BadRequest("Khoảng ngày báo cáo không hợp lệ.");
        return csv
            ? Csv("cong-no-ncc", ["Mã NCC", "Nhà cung cấp", "Dư đầu", "Nhập", "Đảo nhập", "Đã trả", "Dư cuối"],
                dongs.Select(x => new object[] { x.Ncc.Ma, x.Ncc.Ten, x.Dau, x.Nhap, x.Dao, x.Tra, x.Cuoi }))
            : View("~/Views/nv_BaoCao/CongNoNcc.cshtml", new CongNoNccViewModel(ky, nhaCungCaps, dongs));
    }, "BC_NHAP_CONG_NO");

    private void KiemTraKy(KyBaoCaoViewModel ky)
    {
        if (ky.Tu > ky.Den) ModelState.AddModelError("", "Từ ngày phải trước hoặc bằng đến ngày.");
    }

    private FileContentResult Csv(string ten, object[] headers, IEnumerable<object[]> dongs)
    {
        var csv = new StringBuilder();
        foreach (var dong in dongs.Prepend(headers))
            csv.AppendJoin(',', dong.Select(TruongCsv)).Append("\r\n");
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return File([.. utf8.GetPreamble(), .. utf8.GetBytes(csv.ToString())], "text/csv; charset=utf-8", $"{ten}.csv");
    }

    private static string TruongCsv(object value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        if (value is string && text.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@')
            text = "'" + text;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
