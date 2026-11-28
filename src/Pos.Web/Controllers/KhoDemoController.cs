using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;

namespace Pos.Web.Controllers;

[AutoValidateAntiforgeryToken]
public abstract class KhoDemoController(KhoDemoSessionProvider sessions) : Controller
{
    protected IActionResult TrongPhien(Func<KhoDemoStore, IActionResult> operation, string? quyen = null, string? vaiTro = null) =>
        sessions.TrongPhien(HttpContext, store =>
        {
            var danhTinh = store.DanhTinh;
            ViewData["KhoDanhTinh"] = danhTinh;
            if (quyen is not null)
            {
                if (danhTinh is null)
                {
                    if (!HttpMethods.IsGet(Request.Method)) return ThieuQuyen();
                    return RedirectToAction("DangNhap", "HeThong", new { troVe = Request.Path + Request.QueryString });
                }
                if (!store.CoQuyen(quyen, vaiTro)) return ThieuQuyen();
            }
            try
            {
                return operation(store);
            }
            catch (QuyenKhoDemoException)
            {
                return ThieuQuyen();
            }
        });

    protected IActionResult Luu(string action, Action save, string? id = null)
    {
        if (ModelState.IsValid)
        {
            try
            {
                save();
                TempData["KhoSuccess"] = "Đã cập nhật dữ liệu.";
                return RedirectToAction(action, new { id });
            }
            catch (ThaoTacKhoDemoException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("~/Views/Shared/KhoRejected.cshtml", Url.Action(action, new { id }));
    }

    protected IActionResult ThieuQuyen() => StatusCode(StatusCodes.Status403Forbidden);
}
