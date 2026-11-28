using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.BaoCaoQuay;

namespace Pos.Web.Controllers;

public sealed class BaoCaoQuayController(BanHangDemoSessionProvider sessions) : Controller
{
    [HttpGet]
    public IActionResult CuoiNgay([FromQuery] LocBaoCaoQuayViewModel loc, bool csv = false)
    {
        var ngay = loc.Ngay ?? new DateOnly(2026, 10, 10);
        return HienThi(loc with { Ngay = ngay }, csv, store => store.BaoCaoCuoiNgay(loc with { Ngay = ngay }, ngay));
    }

    [HttpGet]
    public IActionResult BanTra([FromQuery] LocBaoCaoQuayViewModel loc, bool csv = false)
    {
        loc = loc with { Tu = loc.Tu ?? new(2026, 10, 1), Den = loc.Den ?? new(2026, 10, 31) };
        return HienThi(loc, csv, store => store.BaoCaoBanTra(loc, loc.Tu!.Value, loc.Den!.Value));
    }

    [HttpGet]
    public IActionResult ThanhToanLechCa([FromQuery] LocBaoCaoQuayViewModel loc, bool csv = false)
    {
        loc = loc with { Tu = loc.Tu ?? new(2026, 10, 1), Den = loc.Den ?? new(2026, 11, 30) };
        return HienThi(loc, csv, store => store.BaoCaoThanhToanCa(loc, loc.Tu!.Value, loc.Den!.Value));
    }

    private IActionResult HienThi(
        LocBaoCaoQuayViewModel loc, bool csv, Func<BanHangDemoStore, BaoCaoQuayViewModel> doc)
    {
        if (loc.VaiTro != "QuanLy")
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("~/Views/nv_BaoCao/TuChoiQuay.cshtml", loc);
        }
        if (!ModelState.IsValid)
            return BadRequest("Bộ lọc không hợp lệ. Kiểm tra định dạng ngày và tham số xuất CSV.");
        if (loc.Tu > loc.Den)
            return BadRequest("Từ ngày phải trước hoặc bằng đến ngày.");
        if (loc.TrangThaiCa is not (null or "DangMo" or "DaDong"))
            return BadRequest("Trạng thái ca không hợp lệ.");

        var model = doc(sessions.LayKhoTheoPhien(HttpContext));
        return csv ? Csv(model) : View($"~/Views/nv_BaoCao/{model.HanhDong}.cshtml", model);
    }

    private FileContentResult Csv(BaoCaoQuayViewModel model)
    {
        var csv = new StringBuilder();
        void GhiDong(IEnumerable<object?> cells) => csv.AppendJoin(',', cells.Select(TruongCsv)).Append("\r\n");
        if (model.ChiSo.Length > 0)
        {
            GhiDong(["Chỉ số", "Giá trị"]);
            foreach (var chiSo in model.ChiSo) GhiDong([chiSo.Nhan, chiSo.GiaTri]);
            csv.AppendLine();
        }
        foreach (var bang in model.Bang)
        {
            GhiDong([bang.TieuDe]);
            GhiDong(bang.Cot);
            foreach (var dong in bang.Dong) GhiDong(dong);
            if (bang.Tong is not null) GhiDong(bang.Tong);
            csv.AppendLine();
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return File([.. utf8.GetPreamble(), .. utf8.GetBytes(csv.ToString())], "text/csv; charset=utf-8",
            $"bao-cao-quay-{model.HanhDong.ToLowerInvariant()}.csv");
    }

    private static string TruongCsv(object? value)
    {
        var text = value is DateTime luc
            ? luc.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        if (value is string && text.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@')
            text = "'" + text;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
