using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.TraHang;

namespace Pos.Web.Controllers;

public sealed class TraHangController(BanHangDemoSessionProvider phien) : Controller
{
    private BanHangDemoStore Kho => phien.LayKhoTheoPhien(HttpContext);

    [HttpGet]
    public IActionResult Index(string? maHoaDon, string? maNhap)
    {
        ViewData["PhienTra"] = HttpContext.Session.Id;
        ViewData["CaMo"] = Kho.MaCaMo;
        return View("~/Views/nv_TraHang/Index.cshtml", Kho.TraHoaDon(maHoaDon, maNhap));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Ghi([FromBody] YeuCauTraViewModel? request, bool xacNhan)
    {
        if (!ModelState.IsValid || request == null)
        {
            return BadRequest(new { message = "Dữ liệu phiếu trả không hợp lệ." });
        }

        var r = Kho.GhiTra(request, xacNhan);
        var lookupUrl = string.IsNullOrWhiteSpace(r.Ma)
            ? null
            : Url.Action(nameof(TraCuu), "TraHang", new { chon = r.Ma });

        return StatusCode(r.Status, new
        {
            message = r.Message,
            ma = r.Ma,
            phienBan = r.PhienBan,
            conflict = r.Status == StatusCodes.Status409Conflict,
            traCuuUrl = lookupUrl
        });
    }

    [HttpGet]
    public IActionResult TraCuu([FromQuery] LocTraViewModel loc)
    {
        if (!ModelState.IsValid || loc.TuNgay > loc.DenNgay)
        {
            ViewData["FilterError"] = "Vui lòng kiểm tra ngày lọc và thứ tự từ ngày đến ngày.";
            Response.StatusCode = 400;
            return View("~/Views/nv_TraHang/TraCuu.cshtml", new TraCuuTraViewModel(loc, [], null, []));
        }
        ViewData["CaMo"] = Kho.MaCaMo;
        var r = Kho.TimPhieuTra(loc);
        return r.Model == null
            ? StatusCode(r.Status)
            : View("~/Views/nv_TraHang/TraCuu.cshtml", r.Model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Xoa(string ma, bool caMo, string? vaiTro, [Bind(Prefix = "loc")] LocTraViewModel loc)
    {
        var (status, message) = Kho.XoaNhap(ma, caMo, vaiTro);
        TempData[status == 200 ? "F4Success" : "F4Error"] = message;
        return RedirectToAction(nameof(TraCuu), new
        {
            loc.Ma, loc.HoaDon, tuNgay = loc.TuNgay?.ToString("yyyy-MM-dd"),
            denNgay = loc.DenNgay?.ToString("yyyy-MM-dd"), loc.Ca, loc.TrangThai,
            vaiTro, caMo = caMo ? "true" : "false"
        });
    }
}
