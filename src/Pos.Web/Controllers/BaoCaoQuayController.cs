using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.BaoCaoQuay;
using Pos.Web.ViewModels.CaBan;

namespace Pos.Web.Controllers;

public sealed class BaoCaoQuayController(BanHangDemoSessionProvider sessions) : QuayDemoController(sessions)
{
    [HttpGet]
    public IActionResult CuoiNgay([FromQuery] LocBaoCaoQuayViewModel loc, bool csv = false)
    {
        var ngay = loc.Ngay ?? new DateOnly(2026, 10, 10);
        loc = loc with { Ngay = ngay };
        return HienThi(loc, csv, "BC_CUOI_NGAY", nameof(CuoiNgay), "Báo cáo cuối ngày",
            store => store.BaoCaoCuoiNgay(loc, ngay));
    }

    [HttpGet]
    public IActionResult BanTra([FromQuery] LocBaoCaoQuayViewModel loc, bool csv = false)
    {
        loc = loc with { Tu = loc.Tu ?? new(2026, 10, 1), Den = loc.Den ?? new(2026, 10, 31) };
        return HienThi(loc, csv, "BC_BAN_TRA", nameof(BanTra), "Thống kê bán hàng và trả hàng",
            store => store.BaoCaoBanTra(loc, loc.Tu!.Value, loc.Den!.Value));
    }

    [HttpGet]
    public IActionResult ThanhToanLechCa([FromQuery] LocBaoCaoQuayViewModel loc, bool csv = false)
    {
        loc = loc with { Tu = loc.Tu ?? new(2026, 10, 1), Den = loc.Den ?? new(2026, 11, 30) };
        return HienThi(loc, csv, "BC_THANH_TOAN_CA", nameof(ThanhToanLechCa), "Thanh toán và chênh lệch ca",
            store => store.BaoCaoThanhToanCa(loc, loc.Tu!.Value, loc.Den!.Value));
    }

    private IActionResult HienThi(
        LocBaoCaoQuayViewModel loc, bool csv, string maQuyen, string hanhDong, string tieuDe,
        Func<BanHangDemoStore, BaoCaoQuayViewModel> doc) =>
        TrongPhien(maQuyen, (quay, _) =>
        {
            var loi = LoiBoLoc(loc);
            if (loi is not null)
            {
                if (csv)
                    return BadRequest(loi);
                Response.StatusCode = StatusCodes.Status400BadRequest;
                ViewData["F8FilterError"] = loi;
                var cacCa = hanhDong == nameof(ThanhToanLechCa)
                    ? quay.LayDanhSachCaBanTheoVaiTro("QuanLy").Select(c => c.MaCa).ToArray()
                    : null;
                var boLoc = new BaoCaoQuayViewModel(loc, hanhDong, tieuDe, [], [], cacCa);
                return View($"~/Views/nv_BaoCao/{hanhDong}.cshtml", boLoc);
            }

            var model = doc(quay);
            return csv ? Csv(model) : View($"~/Views/nv_BaoCao/{hanhDong}.cshtml", model);
        });

    private string? LoiBoLoc(LocBaoCaoQuayViewModel loc)
    {
        if (!ModelState.IsValid)
            return "Bộ lọc không hợp lệ. Kiểm tra định dạng ngày và tham số xuất CSV.";
        if (loc.Tu > loc.Den)
            return "Từ ngày phải trước hoặc bằng đến ngày.";
        if (loc.TrangThaiCa is not (null or "DangMo" or "DaDong"))
            return "Trạng thái ca không hợp lệ.";
        return null;
    }

    private FileContentResult Csv(BaoCaoQuayViewModel model)
    {
        var csv = new StringBuilder();
        void GhiDong(IEnumerable<object?> cells) => csv.AppendJoin(',', cells.Select(TruongCsv)).Append("\r\n");
        if (model.ChiSo.Length > 0)
        {
            GhiDong(["Chỉ số", "Giá trị"]);
            foreach (var chiSo in model.ChiSo) GhiDong([chiSo.Nhan, chiSo.GiaTri]);
            csv.AppendLine();
        }
        foreach (var bang in model.Bang)
        {
            GhiDong([bang.TieuDe]);
            GhiDong(bang.Cot);
            foreach (var dong in bang.Dong) GhiDong(dong);
            if (bang.Tong is not null) GhiDong(bang.Tong);
            csv.AppendLine();
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return File([.. utf8.GetPreamble(), .. utf8.GetBytes(csv.ToString())], "text/csv; charset=utf-8",
            $"bao-cao-quay-{model.HanhDong.ToLowerInvariant()}.csv");
    }

    private static string TruongCsv(object? value)
    {
        var text = value switch
        {
            DateTime luc => luc.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            DongQuyViewModel chungTu => chungTu.Ma,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
        };
        if (value is (string or DongQuyViewModel) && text.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@')
            text = "'" + text;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
