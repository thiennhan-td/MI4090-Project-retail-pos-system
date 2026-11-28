using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.Kho;

namespace Pos.Web.Controllers;

public sealed class HeThongController(KhoDemoSessionProvider sessions) : KhoDemoController(sessions)
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(DangNhap));

    [HttpGet]
    public IActionResult DangNhap(string? troVe) => TrongPhien(_ =>
        View(new DangNhapKhoViewModel { TroVe = Url.IsLocalUrl(troVe) ? troVe : null }));

    [HttpPost]
    public IActionResult DangNhap(DangNhapKhoViewModel form) => TrongPhien(store =>
    {
        if (ModelState.IsValid && store.DangNhap(form.TenDangNhap, form.MatKhau))
        {
            if (Url.IsLocalUrl(form.TroVe))
                return LocalRedirect(form.TroVe!);
            if (store.CoQuyen("NHAP_TRA_CUU"))
                return RedirectToAction("Index", "NhapHang");
            return RedirectToAction("Index", "Home");
        }
        if (ModelState.IsValid)
            ModelState.AddModelError("", "Thông tin đăng nhập không đúng hoặc tài khoản đã bị khóa.");
        form.MatKhau = "";
        var saiDinhDangMatKhau = ModelState[nameof(form.MatKhau)]?.Errors.Count > 0;
        ModelState.Remove(nameof(form.MatKhau));
        if (saiDinhDangMatKhau)
            ModelState.AddModelError(nameof(form.MatKhau), "Nhập mật khẩu hợp lệ.");
        return View(form);
    });

    [HttpPost]
    public IActionResult DangXuat() => TrongPhien(store =>
    {
        store.DangXuat();
        return RedirectToAction(nameof(DangNhap));
    });

    [HttpGet]
    public IActionResult TaiKhoan(string? tuKhoa) => TrongPhien(store =>
    {
        ViewData["TuKhoa"] = tuKhoa;
        return View(store.TaiKhoans().Where(x => string.IsNullOrWhiteSpace(tuKhoa)
            || x.HoTen.Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase)
            || x.TenDangNhap.Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray());
    }, "HT_QL_TAI_KHOAN");

    [HttpGet]
    public IActionResult SuaTaiKhoan(string? id) => TrongPhien(store =>
    {
        if (id is null && !store.CoQuyen("HT_PHAN_QUYEN"))
            return ThieuQuyen();
        var account = id is null ? null : store.TaiKhoans().FirstOrDefault(x => x.Ma == id);
        if (id is not null && account is null) return NotFound();
        ViewData["Ma"] = id;
        ViewData["VaiTroHienTai"] = account?.VaiTro;
        return View(account is null ? new() : new TaiKhoanFormViewModel
        {
            HoTen = account.HoTen, TenDangNhap = account.TenDangNhap, VaiTro = account.VaiTro, HoatDong = account.HoatDong
        });
    }, "HT_QL_TAI_KHOAN");

    [HttpPost]
    public IActionResult SuaTaiKhoan([FromRoute] string? id, TaiKhoanFormViewModel form) => TrongPhien(store =>
    {
        var account = id is null ? null : store.TaiKhoans().FirstOrDefault(x => x.Ma == id);
        if (id is not null && account is null) return NotFound();
        if ((account is null || account.VaiTro != form.VaiTro) && !store.CoQuyen("HT_PHAN_QUYEN"))
            return ThieuQuyen();
        if (ModelState.IsValid)
        {
            try
            {
                store.CapNhatTaiKhoan(id, form.HoTen, form.TenDangNhap, form.VaiTro, form.MatKhau, form.HoatDong);
                return RedirectToAction(nameof(TaiKhoan));
            }
            catch (ThaoTacKhoDemoException ex) { ModelState.AddModelError("", ex.Message); }
        }
        ViewData["Ma"] = id;
        ViewData["VaiTroHienTai"] = account?.VaiTro;
        return View(form);
    }, "HT_QL_TAI_KHOAN");

    [HttpGet]
    public IActionResult PhanQuyen(string vaiTro = KhoDemoStore.QuanLy) => TrongPhien(store =>
    {
        if (!KhoDemoStore.VaiTros.ContainsKey(vaiTro)) return NotFound();
        return View(new PhanQuyenViewModel { VaiTro = vaiTro, Quyen = store.QuyenCuaVaiTro(vaiTro).ToArray() });
    }, "HT_PHAN_QUYEN");

    [HttpPost]
    public IActionResult PhanQuyen(PhanQuyenViewModel form) => TrongPhien(store =>
    {
        if (!KhoDemoStore.VaiTros.ContainsKey(form.VaiTro)) return BadRequest();
        if (ModelState.IsValid)
        {
            try
            {
                store.CapNhatQuyen(form.VaiTro, form.Quyen);
                TempData["KhoSuccess"] = "Đã lưu quyền của vai trò.";
                return RedirectToAction(nameof(PhanQuyen), new { vaiTro = form.VaiTro });
            }
            catch (ThaoTacKhoDemoException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(form);
    }, "HT_PHAN_QUYEN");
}
