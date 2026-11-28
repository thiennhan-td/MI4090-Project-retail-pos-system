using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.TraHang;

namespace Pos.Web.Controllers;

public sealed class TraHangController(BanHangDemoSessionProvider phien) : QuayDemoController(phien)
{
    [HttpGet]
    public IActionResult Index(string? maHoaDon, string? maNhap) => TrongPhien("TRA_LAP_PHIEU", (kho, danhTinh) =>
    {
        ViewData["PhienTra"] = HttpContext.Session.Id;
        ViewData["CaMo"] = kho.LayCaHienTai()?.MaCa;
        ViewData["ActorId"] = danhTinh.Ma;
        ViewData["ActorName"] = danhTinh.HoTen;
        return View("~/Views/nv_TraHang/Index.cshtml", kho.TraHoaDon(maHoaDon, maNhap));
    }, vaiTro: "ThuNgan");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Ghi([FromBody] YeuCauTraViewModel? request, bool xacNhan) =>
        TrongPhien(xacNhan ? "TRA_XAC_NHAN" : "TRA_LAP_PHIEU", (kho, _) =>
    {
        if (!ModelState.IsValid || request == null)
        {
            return BadRequest(new { message = "Dữ liệu phiếu trả không hợp lệ." });
        }

        var r = kho.GhiTra(request, xacNhan);
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
    }, json: true, vaiTro: "ThuNgan");

    [HttpGet]
    public IActionResult TraCuu([FromQuery] LocTraViewModel loc) => TrongPhien("TRA_TRA_CUU", (kho, danhTinh) =>
    {
        var ca = kho.LayCaHienTai()?.MaCa;
        ViewData["CaMo"] = ca;
        loc = loc with { VaiTro = danhTinh.VaiTro, CaMo = ca is not null };
        if (!ModelState.IsValid || loc.TuNgay > loc.DenNgay)
        {
            ViewData["FilterError"] = "Vui lòng kiểm tra ngày lọc và thứ tự từ ngày đến ngày.";
            Response.StatusCode = 400;
            return View("~/Views/nv_TraHang/TraCuu.cshtml", new TraCuuTraViewModel(loc, [], null, []));
        }
        var r = kho.TimPhieuTra(loc);
        if (r.Model is not null)
            return View("~/Views/nv_TraHang/TraCuu.cshtml", r.Model);

        Response.StatusCode = r.Status;
        ViewData["FilterError"] = r.Status == StatusCodes.Status403Forbidden
            ? "Bạn không có quyền xem phiếu trả đã chọn." : "Không tìm thấy phiếu trả đã chọn.";
        return View("~/Views/nv_TraHang/TraCuu.cshtml", kho.TimPhieuTra(loc with { Chon = null }).Model);
    });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Xoa(string ma, int? phienBan, [Bind(Prefix = "loc")] LocTraViewModel loc) =>
        TrongPhien("TRA_LAP_PHIEU", (kho, _) =>
    {
        var (status, message) = ModelState.IsValid
            ? kho.XoaNhap(ma, phienBan) : (400, "Thông tin phiếu trả không hợp lệ.");
        TempData[status == 200 ? "F4Success" : "F4Error"] = message;
        return RedirectToAction(nameof(TraCuu), new
        {
            loc.Ma, loc.HoaDon, tuNgay = loc.TuNgay?.ToString("yyyy-MM-dd"),
            denNgay = loc.DenNgay?.ToString("yyyy-MM-dd"), loc.Ca, loc.TrangThai,
            chon = status == 200 ? null : ma
        });
    }, vaiTro: "ThuNgan");
}
