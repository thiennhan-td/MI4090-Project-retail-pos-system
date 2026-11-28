using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.BanHang;

namespace Pos.Web.Controllers;

public sealed class BanHangController : Controller
{
    private const decimal SoTienToiDaTrongBanNhap = 9007199254740991m;
    private readonly BanHangDemoSessionProvider _sessionProvider;

    public BanHangController(BanHangDemoSessionProvider sessionProvider)
    {
        _sessionProvider = sessionProvider;
    }

    private BanHangDemoStore LayKhoPhien() => _sessionProvider.LayKhoTheoPhien(HttpContext);

    private static string ChuanHoaVaiTro(string? vaiTro) =>
        string.Equals(vaiTro, "QuanLy", StringComparison.OrdinalIgnoreCase) ? "QuanLy" : "ThuNgan";

    [HttpGet]
    public IActionResult Index(string? maNhap)
    {
        var store = LayKhoPhien();
        var ca = store.LayCaHienTai();
        var matHangs = store.LayDanhSachMatHang();
        HoaDonDemoRecord? hoaDonNhap = null;
        string? thongBaoLoi = null;
        string? traCuuUrl = null;
        if (!string.IsNullOrWhiteSpace(maNhap))
        {
            var (hoaDon, biChan) = store.TimHoaDonTheoPhamVi(maNhap.Trim(), "ThuNgan");
            if (biChan || (hoaDon is not null && !store.CoQuyenSuaNhap(hoaDon)))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                thongBaoLoi = "Thu ngân chỉ được sửa hóa đơn nháp thuộc ca đang mở của chính mình.";
            }
            else if (hoaDon is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                thongBaoLoi = "Không tìm thấy hóa đơn nháp trong phiên bán hàng này.";
            }
            else if (hoaDon.TrangThai != "Nhap")
            {
                Response.StatusCode = StatusCodes.Status409Conflict;
                thongBaoLoi = $"Hóa đơn {hoaDon.MaHoaDon} đã xác nhận hoặc đã hủy.";
                traCuuUrl = Url.Action(nameof(TraCuu), new { chon = hoaDon.MaHoaDon });
            }
            else
            {
                hoaDonNhap = hoaDon;
            }
        }

        var vm = new BanHangPosViewModel
        {
            MaCaHienTai = ca?.MaCa ?? string.Empty,
            TenThuNgan = ca?.TenNguoiMo ?? "Lê Linh",
            MaYeuCauMacDinh = Guid.NewGuid().ToString("N"),
            HoaDonNhap = hoaDonNhap,
            ThongBaoLoi = thongBaoLoi,
            TraCuuUrl = traCuuUrl,
            DanhSachNhomHang = matHangs.Select(m => m.TenNhom).Distinct().ToList(),
            DanhSachMatHang = matHangs
        };

        return View("~/Views/nv_BanHang/Index.cshtml", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult LuuNhap([FromBody] GhiHoaDonRequestViewModel? req) => GhiHoaDon(req, xacNhan: false);

    [HttpPost]
    public async Task<IActionResult> XacNhanBan(
        [FromBody] GhiHoaDonRequestViewModel? req,
        [FromServices] IAntiforgery antiforgery)
    {
        if (!string.IsNullOrWhiteSpace(req?.MaNhap) && !await antiforgery.IsRequestValidAsync(HttpContext))
        {
            return Loi("form", "Phiên bảo mật đã hết hạn. Vui lòng mở lại hóa đơn trước khi xác nhận.");
        }

        return GhiHoaDon(req, xacNhan: true);
    }

    private IActionResult GhiHoaDon(GhiHoaDonRequestViewModel? req, bool xacNhan)
    {
        if (req is null || !ModelState.IsValid)
        {
            var loi = ModelState.FirstOrDefault(entry => entry.Value?.Errors.Count > 0);
            var field = loi.Key?.Contains(nameof(GhiHoaDonRequestViewModel.DongBans), StringComparison.OrdinalIgnoreCase) == true
                ? "cart"
                : "form";
            return Loi(field, "Dữ liệu hóa đơn không hợp lệ. Vui lòng kiểm tra số lượng, phiên bản và các khoản thanh toán.");
        }

        var maNhap = string.IsNullOrWhiteSpace(req.MaNhap) ? null : req.MaNhap.Trim();
        if ((maNhap is null) == req.PhienBan.HasValue)
        {
            return Loi("form", "Cần gửi đúng mã và phiên bản của hóa đơn nháp.");
        }

        if (req.DongBans is null || req.DongBans.Count == 0)
        {
            return Loi("cart", "Giỏ hàng đang trống. Vui lòng chọn ít nhất một mặt hàng.");
        }

        var store = LayKhoPhien();
        var catalog = store.LayDanhSachMatHang().ToDictionary(m => m.MaHang, StringComparer.OrdinalIgnoreCase);
        var dongTheoMa = new Dictionary<string, DongBanDemoRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in req.DongBans)
        {
            if (line is null || string.IsNullOrWhiteSpace(line.MaHang))
            {
                return Loi("cart", "Thiếu mã mặt hàng.");
            }

            if (!catalog.TryGetValue(line.MaHang.Trim(), out var item))
            {
                return Loi("cart", $"Mặt hàng {line.MaHang} không tồn tại trong danh mục bán.");
            }

            if (line.SoLuong <= 0)
            {
                return Loi("cart", $"Số lượng của {item.TenHang} phải là số nguyên lớn hơn 0.");
            }

            var soLuong = (long)(dongTheoMa.GetValueOrDefault(item.MaHang)?.SoLuongBan ?? 0) + line.SoLuong;
            if (soLuong > item.TonMau)
            {
                return Loi("cart", $"Tổng số lượng bán của {item.TenHang} vượt tồn mẫu hiện có ({item.TonMau} {item.DonViTinh}).");
            }

            dongTheoMa[item.MaHang] = new(item.MaHang, item.TenHang, item.DonViTinh, (int)soLuong, item.GiaBanHienHanh);
        }

        if (req.CheDoThanhToan is not ("TienMat" or "ChuyenKhoan" or "HonHop"))
        {
            return Loi("payment", "Vui lòng chọn phương thức thanh toán hợp lệ.");
        }

        var soTienToiDa = !xacNhan || maNhap is not null ? SoTienToiDaTrongBanNhap : decimal.MaxValue;
        if (!LaSoTienHopLe(req.TienMatPhanBo, soTienToiDa) || !LaSoTienHopLe(req.TienChuyenKhoan, soTienToiDa))
        {
            return Loi("payment", "Số tiền thanh toán phải là số nguyên VND không âm trong phạm vi hợp lệ.");
        }

        if (!LaSoTienHopLe(req.TienKhachDua, soTienToiDa))
        {
            return Loi("cash-given", "Vui lòng nhập số tiền mặt khách đưa hợp lệ.");
        }

        var dongBans = dongTheoMa.Values.ToArray();
        var tongPhaiThu = dongBans.Sum(d => d.ThanhTien);
        var thanhToans = new List<ThanhToanDemoRecord>();
        ThanhToanNhapDemoRecord? thanhToanNhap = null;
        if (xacNhan)
        {
            var tienMat = req.TienMatPhanBo.GetValueOrDefault();
            var chuyenKhoan = req.TienChuyenKhoan.GetValueOrDefault();
            if (tienMat > tongPhaiThu || chuyenKhoan > tongPhaiThu || tienMat + chuyenKhoan != tongPhaiThu)
            {
                return Loi("payment", $"Tổng phân bổ phải bằng đúng giá trị hóa đơn ({tongPhaiThu:N0} đ).");
            }

            if (tienMat > 0)
            {
                if (req.TienKhachDua is null || req.TienKhachDua < tienMat)
                {
                    return Loi("cash-given", "Tiền mặt khách đưa phải lớn hơn hoặc bằng số tiền mặt phân bổ vào hóa đơn.");
                }

                thanhToans.Add(new("TienMat", tienMat, req.TienKhachDua));
            }

            if (chuyenKhoan > 0)
            {
                thanhToans.Add(new("ChuyenKhoan", chuyenKhoan, null));
            }
        }
        else
        {
            thanhToanNhap = req.CheDoThanhToan switch
            {
                "TienMat" => new("TienMat", tongPhaiThu, req.TienKhachDua, null),
                "ChuyenKhoan" => new("ChuyenKhoan", null, null, tongPhaiThu),
                _ => new("HonHop", req.TienMatPhanBo, req.TienKhachDua, req.TienChuyenKhoan)
            };
        }

        var ketQua = store.GhiHoaDon(req.MaYeuCau, maNhap, req.PhienBan, req.CaDangMo, req.MaCa, dongBans, thanhToanNhap, thanhToans);
        var hoaDon = ketQua.HoaDon;
        if (hoaDon is null)
        {
            var status = ketQua.TrangThai switch
            {
                TrangThaiGhiHoaDonDemo.KhongTimThay => StatusCodes.Status404NotFound,
                TrangThaiGhiHoaDonDemo.KhongCoQuyen => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            var field = ketQua.TrangThai == TrangThaiGhiHoaDonDemo.CaDaDong ? "shift" : "form";
            return StatusCode(status, new { ok = false, field, message = ketQua.ThongBao });
        }

        var traCuuUrl = Url.Action(nameof(TraCuu), new { chon = hoaDon.MaHoaDon });
        var suaUrl = hoaDon.TrangThai == "Nhap"
            ? Url.Action(nameof(Index), new { maNhap = hoaDon.MaHoaDon })
            : null;

        if (ketQua.TrangThai == TrangThaiGhiHoaDonDemo.XungDot)
        {
            return Conflict(new
            {
                ok = false,
                conflict = true,
                field = "form",
                maHoaDonDaGhi = hoaDon.MaHoaDon,
                phienBan = hoaDon.PhienBan,
                suaUrl,
                traCuuUrl,
                message = ketQua.ThongBao
            });
        }

        var inUrl = xacNhan ? Url.Action(nameof(InHoaDon), new { maHoaDon = hoaDon.MaHoaDon }) : null;
        return Ok(new
        {
            ok = true,
            maHoaDon = hoaDon.MaHoaDon,
            phienBan = hoaDon.PhienBan,
            suaUrl,
            tongTien = hoaDon.TongTienHang,
            tienMatPhanBo = hoaDon.TongTienMatPhanBo,
            tienChuyenKhoan = hoaDon.TongChuyenKhoan,
            tienKhachDua = hoaDon.TongTienKhachDua,
            tienThua = hoaDon.TienThua,
            daTonTaiTruocDo = ketQua.TrangThai == TrangThaiGhiHoaDonDemo.GuiLaiTrungKhop,
            traCuuUrl,
            inUrl
        });
    }

    private BadRequestObjectResult Loi(string field, string message) => BadRequest(new { ok = false, field, message });

    private static bool LaSoTienHopLe(decimal? soTien, decimal soTienToiDa) =>
        soTien is null || (soTien >= 0 && soTien <= soTienToiDa && decimal.Truncate(soTien.Value) == soTien.Value);

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
        var maHuyLoi = TempData.Peek("F3CancelInvoice") as string;
        var hienLoiHuyTaiHoaDon = dangChon is not null && string.Equals(dangChon.MaHoaDon, maHuyLoi, StringComparison.OrdinalIgnoreCase);
        if (hienLoiHuyTaiHoaDon)
        {
            TempData.Remove("F3CancelInvoice");
        }

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
            ThongBaoLoiHuy = hienLoiHuyTaiHoaDon ? TempData["F3CancelError"] as string : null,
            LyDoHuyNhap = hienLoiHuyTaiHoaDon ? TempData["F3CancelReason"] as string : null,
            CoTheSuaNhap = vaiTroHienTai == "ThuNgan" && dangChon?.TrangThai == "Nhap" && store.CoQuyenSuaNhap(dangChon),
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
        string? thongBaoLoi = null;
        if (!ModelState.IsValid)
        {
            thongBaoLoi = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Thông tin hủy hóa đơn chưa hợp lệ.";
        }
        else
        {
            var store = LayKhoPhien();
            var vaiTroHienTai = ChuanHoaVaiTro(req.VaiTroDemo);
            var (thanhCong, thongBao) = store.HuyHoaDon(req.MaHoaDon, req.PhienBan, req.LyDoHuy, vaiTroHienTai, req.CoCaDaoMo);
            if (thanhCong)
            {
                TempData["F3Success"] = thongBao;
            }
            else
            {
                thongBaoLoi = thongBao;
            }
        }

        if (thongBaoLoi is not null)
        {
            TempData["F3CancelError"] = thongBaoLoi;
            TempData["F3CancelInvoice"] = req.MaHoaDon;
            TempData["F3CancelReason"] = req.LyDoHuy is { Length: <= 500 } ? req.LyDoHuy : null;
        }

        return RedirectToAction(nameof(TraCuu), null, new
        {
            chon = req.MaHoaDon,
            vaiTro = ChuanHoaVaiTro(req.VaiTroDemo),
            coCaDaoMo = req.CoCaDaoMo
        }, thongBaoLoi is null ? null : "huy-hoa-don");
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

        if (hoaDon.LucXacNhan is null)
        {
            TempData["F3Error"] = hoaDon.TrangThai == "Nhap"
                ? $"Hóa đơn {hoaDon.MaHoaDon} đang là bản nháp. Vui lòng xác nhận bán hàng trước khi in."
                : $"Bản nháp {hoaDon.MaHoaDon} đã hủy trước khi xác nhận. Xem thông tin hủy trong tra cứu.";
            return RedirectToAction(nameof(TraCuu), new { chon = hoaDon.MaHoaDon, vaiTro = vaiTroHienTai });
        }

        var vm = new InHoaDonViewModel
        {
            HoaDon = hoaDon,
            QuayLaiUrl = Url.Action(nameof(TraCuu), "BanHang", new { chon = hoaDon.MaHoaDon, vaiTro = vaiTroHienTai })
        };

        return View("~/Views/nv_BanHang/InHoaDon.cshtml", vm);
    }
}
