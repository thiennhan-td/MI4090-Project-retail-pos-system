using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.CaBan;

namespace Pos.Web.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class CaBanController(BanHangDemoSessionProvider sessions) : QuayDemoController(sessions)
{
    [HttpGet]
    public IActionResult Index([FromQuery] LocCaViewModel loc) => Trang(loc, nameof(Index), xemSoQuy: false);

    [HttpGet]
    public IActionResult SoQuy([FromQuery] LocCaViewModel loc) => Trang(loc, nameof(SoQuy), xemSoQuy: true);

    private IActionResult Trang(LocCaViewModel loc, string view, bool xemSoQuy) =>
        TrongPhien(xemSoQuy ? "QUY_TRA_SO" : "CA_TRA_BAN_GIAO", (kho, _) =>
        {
            var (status, model) = kho.XemCa(loc, xemSoQuy);
            if (model is null)
            {
                Response.StatusCode = status;
                return View("~/Views/Shared/QuayDenied.cshtml", "Tài khoản hiện tại không có quyền xem màn hình này.");
            }

            if (!ModelState.IsValid || loc.Tu > loc.Den
                || loc.TrangThai is not (null or "DangMo" or "DaDong")
                || loc.PhuongThuc is not (null or "TienMat" or "ChuyenKhoan"))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                ViewData["F7FilterError"] = "Kiểm tra bộ lọc. Từ ngày phải trước hoặc bằng đến ngày.";
                model = model with { LichSu = [], So = [] };
            }
            else if (status != StatusCodes.Status200OK)
            {
                Response.StatusCode = status;
                ViewData["F7FilterError"] = status == StatusCodes.Status403Forbidden
                    ? "Bạn không có quyền xem ca đã chọn."
                    : "Không tìm thấy ca đã chọn.";
            }

            return View($"~/Views/nv_CaBanThuChi/{view}.cshtml", model);
        });

    [HttpPost]
    public IActionResult Mo(long? dau, Guid? maYeuCau) => GhiThaoTac("CA_MO",
        kho => ModelState.IsValid && dau.HasValue
            ? kho.MoCa(dau.Value, maYeuCau) : (400, "Nhập số tiền đầu ca hợp lệ."));

    [HttpPost]
    public IActionResult Dong(string ma, long? dem) => GhiThaoTac("CA_DONG",
        kho => ModelState.IsValid && dem.HasValue
            ? kho.DongCa(ma, dem.Value) : (400, "Kiểm tra mã ca và số tiền thực đếm."));

    [HttpPost]
    public IActionResult Ghi(PhieuQuyViewModel form) => GhiThaoTac("QUY_LAP_THU_CHI",
        kho => ModelState.IsValid
            ? kho.LapThuChi(form) : (400, "Kiểm tra số tiền, lý do và số chứng từ kèm theo."),
        thongBaoPhieu: true);

    private IActionResult GhiThaoTac(string quyen, Func<BanHangDemoStore, (int Status, string Message)> ghi,
        bool thongBaoPhieu = false) =>
        TrongPhien(quyen, (kho, _) =>
        {
            var (status, message) = ghi(kho);
            if (thongBaoPhieu && status == StatusCodes.Status200OK)
                TempData["F7Success"] = $"Đã lưu phiếu thu, chi {message}.";
            return StatusCode(status, new { message });
        }, json: true);
}
