using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.Kho;

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

    protected IActionResult Luu(string action, Action save, string? id = null, Func<IActionResult>? hienLoi = null)
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
        return hienLoi is not null ? hienLoi() : View("~/Views/Shared/KhoRejected.cshtml",
            new KhoRejectedViewModel("Dữ liệu cần kiểm tra", null, Url.Action(action, new { id }), "Quay lại"));
    }

    protected IActionResult ThieuQuyen()
    {
        if (ViewData["KhoDanhTinh"] is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return View("~/Views/Shared/KhoRejected.cshtml", new KhoRejectedViewModel("Cần đăng nhập lại",
                "Phiên đăng nhập đã kết thúc. Quay lại biểu mẫu để kiểm tra nội dung vừa nhập và đăng nhập lại trước khi lưu.",
                Url.Action("DangNhap", "HeThong"), "Đăng nhập lại"));
        }

        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/Shared/KhoRejected.cshtml", new KhoRejectedViewModel("Bạn chưa có quyền thao tác",
            "Hãy kiểm tra quyền của tài khoản đang đăng nhập trước khi thực hiện lại.",
            Url.Action("Index", "Home"), "Về trang chủ"));
    }
}
