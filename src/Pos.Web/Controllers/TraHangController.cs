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
            conflict = r.Status == StatusCodes.Status409Conflict,
            traCuuUrl = lookupUrl
        });
    }

    [HttpGet]
    public IActionResult TraCuu([FromQuery] LocTraViewModel loc)
    {
        var r = Kho.TimPhieuTra(loc);
        return r.Model == null
            ? StatusCode(r.Status)
            : View("~/Views/nv_TraHang/TraCuu.cshtml", r.Model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Xoa(string ma, bool caMo, string? vaiTro)
    {
        var (status, message) = Kho.XoaNhap(ma, caMo, vaiTro);
        if (status != StatusCodes.Status200OK)
        {
            return StatusCode(status, message);
        }

        TempData["F4Success"] = message;
        return RedirectToAction(nameof(TraCuu), new
        {
            vaiTro = string.Equals(vaiTro, "QuanLy", StringComparison.OrdinalIgnoreCase) ? "QuanLy" : "ThuNgan",
            caMo = caMo ? "true" : "false"
        });
    }
}
