using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Pos.Web.Demo;
using Pos.Web.ViewModels.Kho;

namespace Pos.Web.Controllers;

public sealed class NhapHangController(KhoDemoSessionProvider sessions) : KhoDemoController(sessions)
{
    [HttpGet]
    public IActionResult Index(string? tuKhoa, string? maNcc, string? trangThai,
        [ModelBinder(BinderType = typeof(NgayKhoModelBinder))] DateOnly? tu,
        [ModelBinder(BinderType = typeof(NgayKhoModelBinder))] DateOnly? den) => TrongPhien(store =>
    {
        if (tu > den) ModelState.AddModelError("", "Từ ngày phải trước hoặc bằng đến ngày.");
        var nhaps = store.PhieuNhaps().Where(x => (maNcc is null || Trung(x.MaNcc, maNcc))
            && (trangThai is null || Trung(x.TrangThai, trangThai))
            && (tu is null || KhoDemoStore.NgayDiaPhuong(x.LucLap) >= tu)
            && (den is null || KhoDemoStore.NgayDiaPhuong(x.LucLap) <= den)
            && (string.IsNullOrWhiteSpace(tuKhoa) || x.Ma.Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase)
                || store.TenNcc(x.MaNcc).Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase)));
        var xemNo = store.CoQuyen("NCC_TRA_NO");
        PhieuNhapDongViewModel[] dongs = ModelState.IsValid ? nhaps.Select(x => Dong(store, x, xemNo)).ToArray() : [];
        return View("~/Views/nv_NhapHang/Index.cshtml",
            new SoNhapViewModel(tuKhoa, maNcc, trangThai, tu, den, store.NhaCungCaps(), dongs, xemNo ? dongs.Sum(x => x.DuNo) : null));
    }, "NHAP_TRA_CUU");

    [HttpGet]
    public IActionResult ChiTiet(string? id) => TrongPhien(store =>
    {
        var phieu = id is null ? null : store.PhieuNhap(id);
        return phieu is null ? NotFound() : HienChiTiet(store, phieu);
    }, "NHAP_TRA_CUU");

    [HttpGet]
    public IActionResult Lap(string? id) => TrongPhien(store =>
    {
        if (id is null)
            return HienForm(store, null, new PhieuNhapFormViewModel { Dongs = [new()] });
        var phieu = store.PhieuNhap(id);
        if (phieu is null) return NotFound();
        if (phieu.TrangThai != KhoDemoStore.Nhap) return RedirectToAction(nameof(ChiTiet), new { id });
        return HienForm(store, id, new PhieuNhapFormViewModel
        {
            PhienBan = phieu.PhienBan,
            MaNcc = phieu.MaNcc, HanThanhToan = phieu.HanThanhToan,
            Dongs = phieu.Dongs.Select(x => new DongNhapFormViewModel
            { MaHang = x.MaHang, SoLuong = x.SoLuong, DonGia = x.DonGia }).ToList()
        });
    }, "NHAP_LAP_NHAP");

    [HttpPost]
    public IActionResult Lap([FromRoute] string? id, PhieuNhapFormViewModel form) => TrongPhien(store =>
    {
        if (id is not null && store.PhieuNhap(id) is null) return NotFound();
        if (id is not null && form.PhienBan is null)
            ModelState.AddModelError(nameof(form.PhienBan), "Mở lại phiếu nhập để lấy phiên bản hiện tại.");
        if (form.Dongs.Count is < 1 or > 100)
            ModelState.AddModelError("", "Phiếu nhập cần từ 1 đến 100 dòng.");
        if (ModelState.IsValid)
        {
            try
            {
                var ma = store.LuuNhap(id, form.PhienBan, form.MaNcc, form.HanThanhToan,
                    form.Dongs.Select(x => (x.MaHang, x.SoLuong!.Value, x.DonGia!.Value)));
                TempData["KhoSuccess"] = "Đã lưu phiếu nhập nháp.";
                return RedirectToAction(nameof(ChiTiet), new { id = ma });
            }
            catch (XungDotPhienBanKhoDemoException ex)
            {
                ModelState.AddModelError("", ex.Message);
                Response.StatusCode = StatusCodes.Status409Conflict;
                return HienForm(store, id, form, xungDot: true);
            }
            catch (ThaoTacKhoDemoException ex) { ModelState.AddModelError("", ex.Message); }
        }
        if (form.Dongs.Count == 0) form.Dongs.Add(new());
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return HienForm(store, id, form);
    }, "NHAP_LAP_NHAP");

    [HttpPost]
    public IActionResult XacNhan([FromRoute] string id, [FromForm, BindRequired] int phienBan) => TrongPhien(store =>
        Luu(nameof(ChiTiet), () => store.XacNhanNhap(id, phienBan), id), "NHAP_XAC_NHAN", KhoDemoStore.NhanVienKho);

    [HttpPost]
    public IActionResult Huy([FromRoute] string id, HuyNhapFormViewModel form) => TrongPhien(store =>
    {
        if (store.DanhTinh is null || (!store.CoQuyen("NHAP_HUY_NHAP") && !store.CoQuyen("NHAP_HUY_XAC_NHAN", KhoDemoStore.QuanLy)))
            return ThieuQuyen();
        var phieu = store.PhieuNhap(id);
        if (phieu is null) return NotFound();
        var duocHuy = phieu.LucXacNhan is null ? store.CoQuyen("NHAP_HUY_NHAP")
            : store.CoQuyen("NHAP_HUY_XAC_NHAN", KhoDemoStore.QuanLy);
        if (!duocHuy) return ThieuQuyen();
        return Luu(nameof(ChiTiet), () => store.HuyNhap(id, form.LyDo), id,
            () => HienChiTiet(store, phieu, huyDaNhap: form));
    });

    [HttpPost]
    public IActionResult TraTien([FromRoute] string id, TraTienNccViewModel form) => TrongPhien(store =>
    {
        var phieu = store.PhieuNhap(id);
        if (phieu is null) return NotFound();
        if (form.KhoaYeuCau == Guid.Empty) ModelState.AddModelError("", "Tải lại trang trước khi ghi khoản trả.");
        return Luu(nameof(ChiTiet), () => store.TraNcc(id, form.SoTien!.Value, form.PhuongThuc, form.Nguon,
            form.MaCa, form.KhoaYeuCau!.Value), id,
            () => HienChiTiet(store, phieu, traTienDaNhap: form));
    }, "NCC_GHI_TRA_TIEN", KhoDemoStore.QuanLy);

    private IActionResult HienChiTiet(KhoDemoStore store, PhieuNhapDemo phieu,
        TraTienNccViewModel? traTienDaNhap = null, HuyNhapFormViewModel? huyDaNhap = null)
    {
        var xemNo = store.CoQuyen("NCC_TRA_NO");
        var traTien = store.CoQuyen("NCC_GHI_TRA_TIEN", KhoDemoStore.QuanLy);
        return View("~/Views/nv_NhapHang/ChiTiet.cshtml", new ChiTietNhapViewModel(Dong(store, phieu, xemNo),
            phieu.Dongs.ToDictionary(x => x.MaHang, x => store.TenHang(x.MaHang)),
            xemNo ? store.LichSuTra(phieu.Ma) : [], traTien ? store.CaDangMo() : [],
            store.CoTheDaoNhap(phieu), traTien ? store.DuNo(phieu) : null, Guid.NewGuid(), traTienDaNhap, huyDaNhap));
    }

    private IActionResult HienForm(KhoDemoStore store, string? id, PhieuNhapFormViewModel form, bool xungDot = false)
    {
        ViewData["Ma"] = id;
        ViewData["XungDot"] = xungDot;
        ViewData["NhaCungCaps"] = store.NhaCungCaps();
        ViewData["MatHangs"] = store.MatHangDangDung();
        return View("~/Views/nv_NhapHang/Lap.cshtml", form);
    }

    private static bool Trung(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static PhieuNhapDongViewModel Dong(KhoDemoStore store, PhieuNhapDemo phieu, bool xemNo) =>
        new(phieu, store.TenNcc(phieu.MaNcc), xemNo ? store.DaTra(phieu.Ma) : null, xemNo ? store.DuNo(phieu) : null);
}
