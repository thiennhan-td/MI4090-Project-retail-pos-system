using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.Kho;

namespace Pos.Web.Controllers;

public sealed class KiemKhoController(KhoDemoSessionProvider sessions) : KhoDemoController(sessions)
{
    [HttpGet]
    public IActionResult TheKho(KyBaoCaoViewModel ky) => TrongPhien(store =>
    {
        var matHangs = store.MatHangs();
        ky.MaHang ??= matHangs.FirstOrDefault(x => x.Ma == "S-SUA")?.Ma ?? matHangs.FirstOrDefault()?.Ma;
        if (ky.MaHang is not null && !matHangs.Any(x => string.Equals(x.Ma, ky.MaHang, StringComparison.OrdinalIgnoreCase)))
            return NotFound();
        if (ky.Tu > ky.Den) ModelState.AddModelError("", "Từ ngày phải trước hoặc bằng đến ngày.");
        var the = ModelState.IsValid && ky.MaHang is not null ? store.TheKho(ky.MaHang, ky.Tu, ky.Den) : null;
        return View("~/Views/nv_KiemKho/TheKho.cshtml", new TheKhoViewModel(ky, matHangs, the));
    }, "KHO_TRA_SO_HANG");

    [HttpGet]
    public IActionResult Index() => TrongPhien(store => View("~/Views/nv_KiemKho/Index.cshtml", store.PhieuKiems()),
        "KHO_XEM_XET_KIEM");

    [HttpGet]
    public IActionResult Lap() => TrongPhien(store => HienFormLap(store),
        "KHO_LAP_KIEM", KhoDemoStore.NhanVienKho);

    [HttpPost]
    public IActionResult Lap(LapKiemFormViewModel form) => TrongPhien(store =>
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = store.LapPhieuKiem(form.MaHangs);
                return RedirectToAction(nameof(ChiTiet), new { id });
            }
            catch (ThaoTacKhoDemoException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return HienFormLap(store, form.MaHangs);
    }, "KHO_LAP_KIEM", KhoDemoStore.NhanVienKho);

    [HttpGet]
    public IActionResult ChiTiet(string? id) => TrongPhien(store =>
    {
        var phieu = id is null ? null : store.PhieuKiem(id);
        return phieu is null ? NotFound() : HienChiTiet(store, phieu);
    }, "KHO_XEM_XET_KIEM");

    [HttpPost]
    public IActionResult GhiDem([FromRoute] string id, GhiDemFormViewModel form) => TrongPhien(store =>
    {
        if (form.GuiDuyet && !store.CoQuyen("KHO_GIAI_TRINH_KIEM", KhoDemoStore.NhanVienKho))
            return ThieuQuyen();
        var phieu = store.PhieuKiem(id);
        if (phieu is null) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                store.GhiDem(id, form.PhienBan!.Value,
                    form.Dongs.Select(x => (x.MaHang, x.SoLuong, x.GiaiTrinh)), form.GuiDuyet);
                TempData["KhoSuccess"] = form.GuiDuyet ? "Đã gửi duyệt phiếu kiểm." : "Đã lưu số đếm.";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }
            catch (XungDotPhienBanKhoDemoException ex)
            {
                ModelState.AddModelError("", ex.Message);
                Response.StatusCode = StatusCodes.Status409Conflict;
                ViewData["PhienBanDaNhap"] = form.PhienBan;
                return HienChiTiet(store, phieu, xungDot: true);
            }
            catch (ThaoTacKhoDemoException ex) { ModelState.AddModelError("", ex.Message); }
        }
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return HienChiTiet(store, phieu);
    }, "KHO_LAP_KIEM", KhoDemoStore.NhanVienKho);

    [HttpPost]
    public IActionResult Duyet([FromRoute] string id) => TrongPhien(store =>
        Luu(nameof(ChiTiet), () => store.DuyetKiem(id), id), "KHO_DUYET_DIEU_CHINH", KhoDemoStore.QuanLy);

    [HttpPost]
    public IActionResult Huy([FromRoute] string id) => TrongPhien(store =>
    {
        if (store.PhieuKiem(id)?.TrangThai == KhoDemoStore.ChoDuyet &&
            !store.CoQuyen("KHO_HUY_KIEM", KhoDemoStore.QuanLy))
            return ThieuQuyen();
        return Luu(nameof(ChiTiet), () => store.HuyKiem(id), id);
    }, "KHO_HUY_KIEM");

    private IActionResult HienFormLap(KhoDemoStore store, string[]? selection = null)
    {
        ViewData["DaChon"] = selection ?? Array.Empty<string>();
        return View("~/Views/nv_KiemKho/Lap.cshtml", store.MatHangDangDung());
    }

    private IActionResult HienChiTiet(KhoDemoStore store, PhieuKiemDemo phieu, bool xungDot = false)
    {
        ViewData["XungDot"] = xungDot;
        return View("~/Views/nv_KiemKho/ChiTiet.cshtml",
            new ChiTietKiemViewModel(phieu, phieu.Dongs.ToDictionary(x => x.MaHang, x => store.TenHang(x.MaHang))));
    }
}
