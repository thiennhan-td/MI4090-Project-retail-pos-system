using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;

namespace Pos.Web.Controllers;

public abstract class QuayDemoController(BanHangDemoSessionProvider sessions) : Controller
{
    protected IActionResult TrongPhien(string quyen, Func<BanHangDemoStore, DanhTinhKhoDemo, IActionResult> action,
        bool json = false, string? vaiTro = null)
    {
        var store = sessions.LayKhoTheoPhien(HttpContext);
        lock (store.KhoaPhien)
        {
            var danhTinh = store.Kho.DanhTinh;
            ViewData["KhoDanhTinh"] = danhTinh;
            if (danhTinh is not null && danhTinh.CoQuyen(quyen, vaiTro))
                return action(store, danhTinh);

            if (danhTinh is null && HttpMethods.IsGet(Request.Method))
                return RedirectToAction("DangNhap", "HeThong", new { troVe = Request.Path + Request.QueryString });

            var status = danhTinh is null ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden;
            var message = danhTinh is null ? "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại."
                : "Tài khoản hiện tại không có quyền thực hiện thao tác này.";
            if (json)
                return StatusCode(status, new { ok = false, message });

            Response.StatusCode = status;
            return View("~/Views/Shared/QuayDenied.cshtml", message);
        }
    }
}
