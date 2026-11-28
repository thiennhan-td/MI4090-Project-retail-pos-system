using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.Kho;

namespace Pos.Web.Controllers;

public abstract class DanhMucController(KhoDemoSessionProvider sessions) : KhoDemoController(sessions)
{
    protected static bool KhopTuKhoa(string ma, string ten, string? tuKhoa) =>
        string.IsNullOrWhiteSpace(tuKhoa) || ma.Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase)
        || ten.Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase);

    protected static bool KhopTrangThai(bool hoatDong, string? trangThai) =>
        trangThai is null || (hoatDong ? "HoatDong" : "NgungSuDung") == trangThai;

    protected IActionResult LuuDanhMuc(string view, object form, Action save)
    {
        if (ModelState.IsValid)
        {
            try
            {
                save();
                TempData["KhoSuccess"] = "Đã lưu danh mục.";
                return RedirectToAction("Index");
            }
            catch (ThaoTacKhoDemoException ex) { ModelState.AddModelError("", ex.Message); }
        }
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View(view, form);
    }
}

public sealed class NhomHangController(KhoDemoSessionProvider sessions) : DanhMucController(sessions)
{
    private const string FormView = "~/Views/dm_NhomHang/Sua.cshtml";

    [HttpGet]
    public IActionResult Index(string? tuKhoa, string? trangThai) => TrongPhien(store =>
    {
        var dongs = store.NhomHangs().Where(x => KhopTuKhoa(x.Ma, x.Ten, tuKhoa) && KhopTrangThai(x.HoatDong, trangThai))
            .Select(x => new DanhMucDongViewModel(x.Ma, x.Ten, HoatDong: x.HoatDong)).ToArray();
        return View("~/Views/dm_NhomHang/Index.cshtml", new DanhMucViewModel("Nhóm hàng", tuKhoa, trangThai,
            null, false, false, true, store.CoQuyen("DM_NHOM_QL"), dongs, []));
    }, "DM_NHOM_XEM");

    [HttpGet]
    public IActionResult Sua(string? id) => TrongPhien(store =>
    {
        var nhom = store.NhomHangs().FirstOrDefault(x => x.Ma == id);
        if (id is not null && nhom is null) return NotFound();
        ViewData["Ma"] = id;
        return View(FormView, new NhomHangFormViewModel { Ten = nhom?.Ten ?? "" });
    }, "DM_NHOM_QL");

    [HttpPost]
    public IActionResult Sua([FromRoute] string? id, NhomHangFormViewModel form) => TrongPhien(store =>
    {
        ViewData["Ma"] = id;
        return LuuDanhMuc(FormView, form, () => store.LuuNhom(id, form.Ten));
    }, "DM_NHOM_QL");

    [HttpPost]
    public IActionResult Ngung([FromRoute] string id) => TrongPhien(store =>
        Luu(nameof(Index), () => store.NgungNhom(id)), "DM_NHOM_QL");
}

public sealed class MatHangController(KhoDemoSessionProvider sessions) : DanhMucController(sessions)
{
    private const string FormView = "~/Views/dm_MatHang/Sua.cshtml";

    [HttpGet]
    public IActionResult Index(string? tuKhoa, string? trangThai, string? maNhom) => TrongPhien(store =>
    {
        var nhomHangs = store.NhomHangs();
        var nhoms = nhomHangs.ToDictionary(x => x.Ma, x => x.Ten);
        var dongs = store.MatHangs().Where(x => KhopTuKhoa(x.Ma, x.Ten, tuKhoa) && KhopTrangThai(x.HoatDong, trangThai)
            && (maNhom is null || x.MaNhom == maNhom))
            .Select(x => new DanhMucDongViewModel(x.Ma, x.Ten, nhoms[x.MaNhom], x.DonVi, x.GiaBan, x.HoatDong)).ToArray();
        return View("~/Views/dm_MatHang/Index.cshtml", new DanhMucViewModel("Mặt hàng", tuKhoa, trangThai,
            maNhom, true, false, true, store.CoQuyen("DM_HANG_QL"), dongs, nhomHangs));
    }, "DM_HANG_XEM");

    [HttpGet]
    public IActionResult Sua(string? id) => TrongPhien(store =>
    {
        var matHang = store.MatHangs().FirstOrDefault(x => x.Ma == id);
        if (id is not null && matHang is null) return NotFound();
        NapForm(store, id);
        return View(FormView, matHang is null ? new() : new MatHangFormViewModel
        { Ten = matHang.Ten, MaNhom = matHang.MaNhom, DonVi = matHang.DonVi, GiaBan = matHang.GiaBan });
    }, "DM_HANG_QL");

    [HttpPost]
    public IActionResult Sua([FromRoute] string? id, MatHangFormViewModel form) => TrongPhien(store =>
    {
        NapForm(store, id);
        return LuuDanhMuc(FormView, form, () => store.LuuHang(id, form.MaNhom, form.Ten, form.DonVi, form.GiaBan!.Value));
    }, "DM_HANG_QL");

    [HttpPost]
    public IActionResult Ngung([FromRoute] string id) => TrongPhien(store =>
        Luu(nameof(Index), () => store.NgungHang(id)), "DM_HANG_QL");

    private void NapForm(KhoDemoStore store, string? id)
    {
        ViewData["Ma"] = id;
        ViewData["NhomHangs"] = store.NhomHangs().Where(x => x.HoatDong).ToArray();
    }
}

public sealed class NhaCungCapController(KhoDemoSessionProvider sessions) : DanhMucController(sessions)
{
    private const string FormView = "~/Views/dm_NhaCungCap/Sua.cshtml";

    [HttpGet]
    public IActionResult Index(string? tuKhoa) => TrongPhien(store =>
    {
        var dongs = store.NhaCungCaps().Where(x => KhopTuKhoa(x.Ma, x.Ten, tuKhoa))
            .Select(x => new DanhMucDongViewModel(x.Ma, x.Ten, LienHe: x.LienHe)).ToArray();
        return View("~/Views/dm_NhaCungCap/Index.cshtml", new DanhMucViewModel("Nhà cung cấp", tuKhoa, null,
            null, false, true, false, store.CoQuyen("DM_NCC_QL"), dongs, []));
    }, "DM_NCC_XEM");

    [HttpGet]
    public IActionResult Sua(string? id) => TrongPhien(store =>
    {
        var ncc = store.NhaCungCaps().FirstOrDefault(x => x.Ma == id);
        if (id is not null && ncc is null) return NotFound();
        ViewData["Ma"] = id;
        return View(FormView, new NhaCungCapFormViewModel { Ten = ncc?.Ten ?? "", LienHe = ncc?.LienHe });
    }, "DM_NCC_QL");

    [HttpPost]
    public IActionResult Sua([FromRoute] string? id, NhaCungCapFormViewModel form) => TrongPhien(store =>
    {
        ViewData["Ma"] = id;
        return LuuDanhMuc(FormView, form, () => store.LuuNcc(id, form.Ten, form.LienHe));
    }, "DM_NCC_QL");
}
