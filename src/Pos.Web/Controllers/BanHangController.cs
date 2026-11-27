using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.BanHang;

namespace Pos.Web.Controllers;

public sealed class BanHangController : Controller
{
    private readonly BanHangDemoSessionProvider _sessionProvider;

    public BanHangController(BanHangDemoSessionProvider sessionProvider)
    {
        _sessionProvider = sessionProvider;
    }

    private BanHangDemoStore LayKhoPhien() => _sessionProvider.LayKhoTheoPhien(HttpContext);

    private static string ChuanHoaVaiTro(string? vaiTro) =>
        string.Equals(vaiTro, "QuanLy", StringComparison.OrdinalIgnoreCase) ? "QuanLy" : "ThuNgan";

    [HttpGet]
    public IActionResult Index()
    {
        var store = LayKhoPhien();
        var matHangs = store.LayDanhSachMatHang();
        var vm = new BanHangPosViewModel
        {
            MaCaHienTai = "S-CA-1126",
            TenThuNgan = "Lê Linh",
            MaYeuCauMacDinh = Guid.NewGuid().ToString("N"),
            DanhSachNhomHang = matHangs.Select(m => m.TenNhom).Distinct().ToList(),
            DanhSachMatHang = matHangs
        };

        return View("~/Views/nv_BanHang/Index.cshtml", vm);
    }

    [HttpPost]
    public IActionResult XacNhanBan([FromBody] XacNhanBanRequestViewModel? req)
    {
        if (req is null)
        {
            return BadRequest(new { ok = false, field = "form", message = "Dữ liệu xác nhận hóa đơn không hợp lệ." });
        }

        if (!req.CaDangMo)
        {
            return BadRequest(new { ok = false, field = "shift", message = "Ca bán hiện tại đã đóng. Vui lòng mở ca mới trước khi xác nhận bán hàng." });
        }

        if (string.IsNullOrWhiteSpace(req.MaYeuCau))
        {
            return BadRequest(new { ok = false, field = "form", message = "Thiếu mã yêu cầu giao dịch." });
        }

        if (req.DongBans is null || req.DongBans.Count == 0)
        {
            return BadRequest(new { ok = false, field = "cart", message = "Giỏ hàng đang trống. Vui lòng chọn ít nhất một mặt hàng." });
        }

        var store = LayKhoPhien();
        var catalog = store.LayDanhSachMatHang().ToDictionary(m => m.MaHang, StringComparer.OrdinalIgnoreCase);
        var dongBans = new List<DongBanDemoRecord>();

        foreach (var line in req.DongBans)
        {
            if (!catalog.TryGetValue(line.MaHang, out var item))
            {
                return BadRequest(new { ok = false, field = "cart", message = $"Mặt hàng {line.MaHang} không tồn tại trong danh mục bán." });
            }

            if (line.SoLuong <= 0)
            {
                return BadRequest(new { ok = false, field = "cart", message = $"Số lượng của {item.TenHang} phải là số nguyên lớn hơn 0." });
            }

            if (line.SoLuong > item.TonMau)
            {
                return BadRequest(new { ok = false, field = "cart", message = $"Số lượng bán của {item.TenHang} vượt tồn mẫu hiện có ({item.TonMau} {item.DonViTinh})." });
            }

            dongBans.Add(new DongBanDemoRecord(item.MaHang, item.TenHang, item.DonViTinh, line.SoLuong, item.GiaBanHienHanh));
        }

        var tongPhaiThu = dongBans.Sum(d => d.ThanhTien);

        if (req.TienMatPhanBo < 0 || req.TienChuyenKhoan < 0 || decimal.Truncate(req.TienMatPhanBo) != req.TienMatPhanBo || decimal.Truncate(req.TienChuyenKhoan) != req.TienChuyenKhoan)
        {
            return BadRequest(new { ok = false, field = "payment", message = "Số tiền thanh toán phải là số nguyên VND không âm." });
        }

        var thanhToans = new List<ThanhToanDemoRecord>();
        if (req.TienMatPhanBo > 0)
        {
            if (!req.TienKhachDua.HasValue || decimal.Truncate(req.TienKhachDua.Value) != req.TienKhachDua.Value)
            {
                return BadRequest(new { ok = false, field = "cash-given", message = "Vui lòng nhập số tiền mặt khách đưa hợp lệ." });
            }

            if (req.TienKhachDua.Value < req.TienMatPhanBo)
            {
                return BadRequest(new { ok = false, field = "cash-given", message = "Tiền mặt khách đưa phải lớn hơn hoặc bằng số tiền mặt phân bổ vào hóa đơn." });
            }

            thanhToans.Add(new ThanhToanDemoRecord("TienMat", req.TienMatPhanBo, req.TienKhachDua.Value));
        }

        if (req.TienChuyenKhoan > 0)
        {
            thanhToans.Add(new ThanhToanDemoRecord("ChuyenKhoan", req.TienChuyenKhoan, null));
        }

        var tongPhanBo = req.TienMatPhanBo + req.TienChuyenKhoan;
        if (tongPhanBo != tongPhaiThu)
        {
            return BadRequest(new
            {
                ok = false,
                field = "payment",
                message = $"Tổng phân bổ ({tongPhanBo:N0} đ) phải bằng đúng giá trị hóa đơn ({tongPhaiThu:N0} đ)."
            });
        }

        var ketQua = store.TaoHoacLayHoaDonDaXacNhan(req.MaYeuCau, dongBans, thanhToans);
        var hoaDon = ketQua.HoaDon;
        var traCuuUrl = Url.Action(nameof(TraCuu), "BanHang", new { chon = hoaDon.MaHoaDon, vaiTro = "ThuNgan" });

        if (ketQua.TrangThai == KetQuaIdempotency.XungDotNoiDung)
        {
            return Conflict(new
            {
                ok = false,
                conflict = true,
                field = "payment",
                maHoaDonDaGhi = hoaDon.MaHoaDon,
                traCuuUrl,
                message = $"Mã yêu cầu đã được dùng cho hóa đơn {hoaDon.MaHoaDon} với nội dung khác. Hóa đơn gốc được giữ nguyên; vui lòng mở Tra cứu hóa đơn để kiểm tra."
            });
        }

        var inUrl = Url.Action(nameof(InHoaDon), "BanHang", new { maHoaDon = hoaDon.MaHoaDon, vaiTro = "ThuNgan" });

        return Ok(new
        {
            ok = true,
            maHoaDon = hoaDon.MaHoaDon,
            tongTien = hoaDon.TongTienHang,
            tienMatPhanBo = hoaDon.TongTienMatPhanBo,
            tienChuyenKhoan = hoaDon.TongChuyenKhoan,
            tienKhachDua = hoaDon.TongTienKhachDua,
            tienThua = hoaDon.TienThua,
            daTonTaiTruocDo = ketQua.TrangThai == KetQuaIdempotency.GuiLaiTrungKhop,
            traCuuUrl,
            inUrl
        });
    }

    [HttpGet]
    public IActionResult TraCuu(
        string? maHoaDon,
        DateOnly? tuNgay,
        DateOnly? denNgay,
        string? maCa,
        string? trangThai,
        string? vaiTro,
        bool? coCaDaoMo,
        string? chon)
    {
        var store = LayKhoPhien();
        var vaiTroHienTai = ChuanHoaVaiTro(vaiTro);
        var trangThaiLoc = string.IsNullOrWhiteSpace(trangThai) ? "TatCa" : trangThai;
        var caDaoMo = coCaDaoMo ?? true;

        var caThuocVaiTro = store.LayDanhSachCaBanTheoVaiTro(vaiTroHienTai);
        var query = store.LayDanhSachHoaDonTheoPhamVi(vaiTroHienTai).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(maHoaDon))
        {
            var keyword = maHoaDon.Trim();
            query = query.Where(h => h.MaHoaDon.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (tuNgay.HasValue)
        {
            query = query.Where(h => DateOnly.FromDateTime(h.LucLap) >= tuNgay.Value);
        }

        if (denNgay.HasValue)
        {
            query = query.Where(h => DateOnly.FromDateTime(h.LucLap) <= denNgay.Value);
        }

        if (!string.IsNullOrWhiteSpace(maCa))
        {
            query = query.Where(h => string.Equals(h.MaCa, maCa, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.Equals(trangThaiLoc, "TatCa", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(h => string.Equals(h.TrangThai, trangThaiLoc, StringComparison.OrdinalIgnoreCase));
        }

        var danhSach = query.ToList();
        var dangChon = !string.IsNullOrWhiteSpace(chon)
            ? danhSach.FirstOrDefault(h => string.Equals(h.MaHoaDon, chon, StringComparison.OrdinalIgnoreCase)) ?? danhSach.FirstOrDefault()
            : danhSach.FirstOrDefault();

        var vm = new TraCuuHoaDonViewModel
        {
            MaHoaDon = maHoaDon,
            TuNgay = tuNgay,
            DenNgay = denNgay,
            MaCa = maCa,
            TrangThai = trangThaiLoc,
            VaiTroDemo = vaiTroHienTai,
            CoCaDaoMo = caDaoMo,
            MaHoaDonDangChon = dangChon?.MaHoaDon,
            ThongBaoThanhCong = TempData["F3Success"] as string,
            ThongBaoLoi = TempData["F3Error"] as string,
            DanhSachCaBan = caThuocVaiTro,
            DanhSachHoaDon = danhSach,
            HoaDonDangChon = dangChon
        };

        return View("~/Views/nv_BanHang/TraCuu.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult HuyHoaDon(HuyHoaDonRequestViewModel req)
    {
        if (!ModelState.IsValid)
        {
            TempData["F3Error"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Thông tin hủy hóa đơn chưa hợp lệ.";
        }
        else
        {
            var store = LayKhoPhien();
            var vaiTroHienTai = ChuanHoaVaiTro(req.VaiTroDemo);
            var (thanhCong, thongBao) = store.HuyHoaDon(req.MaHoaDon, req.LyDoHuy, vaiTroHienTai, req.CoCaDaoMo);
            if (thanhCong)
            {
                TempData["F3Success"] = thongBao;
            }
            else
            {
                TempData["F3Error"] = thongBao;
            }
        }

        return RedirectToAction(nameof(TraCuu), new
        {
            chon = req.MaHoaDon,
            vaiTro = ChuanHoaVaiTro(req.VaiTroDemo),
            coCaDaoMo = req.CoCaDaoMo
        });
    }

    [HttpGet]
    public IActionResult InHoaDon(string maHoaDon, string? vaiTro)
    {
        var store = LayKhoPhien();
        var vaiTroHienTai = ChuanHoaVaiTro(vaiTro);
        var (hoaDon, biChanPhamVi) = store.TimHoaDonTheoPhamVi(maHoaDon, vaiTroHienTai);
        if (biChanPhamVi)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (hoaDon is null)
        {
            return NotFound();
        }

        var vm = new InHoaDonViewModel
        {
            HoaDon = hoaDon,
            QuayLaiUrl = Url.Action(nameof(TraCuu), "BanHang", new { chon = hoaDon.MaHoaDon, vaiTro = vaiTroHienTai })
        };

        return View("~/Views/nv_BanHang/InHoaDon.cshtml", vm);
    }
}
