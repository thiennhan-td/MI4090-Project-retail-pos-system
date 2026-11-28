using Microsoft.AspNetCore.Mvc;
using Pos.Web.Demo;
using Pos.Web.ViewModels.CaBan;

namespace Pos.Web.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class CaBanController(BanHangDemoSessionProvider sessions) : Controller
{
    private BanHangDemoStore Kho => sessions.LayKhoTheoPhien(HttpContext);
    private IActionResult Trang(LocCaViewModel loc,string view)
    {
        if(!ModelState.IsValid || loc.Tu>loc.Den) return BadRequest("Kiểm tra bộ lọc ngày.");
        if(loc.VaiTro is not ("ThuNgan" or "QuanLy")) return StatusCode(403);
        var (status,model)=Kho.XemCa(loc);
        return model==null ? StatusCode(status) : View($"~/Views/nv_CaBanThuChi/{view}.cshtml",model);
    }
    [HttpGet] public IActionResult Index([FromQuery] LocCaViewModel loc) => Trang(loc,"Index");
    [HttpGet] public IActionResult SoQuy([FromQuery] LocCaViewModel loc) => Trang(loc,"SoQuy");
    private IActionResult KetQua((int Status,string Message) r) => StatusCode(r.Status,new {message=r.Message});
    [HttpPost] public IActionResult Mo(string vaiTro,long? dau) => ModelState.IsValid && dau.HasValue ?
        KetQua(Kho.MoCa(vaiTro,dau.Value)) : BadRequest();
    [HttpPost] public IActionResult Dong(string vaiTro,string ma,long? dem) => ModelState.IsValid && dem.HasValue ?
        KetQua(Kho.DongCa(vaiTro,ma,dem.Value)) : BadRequest();
    [HttpPost] public IActionResult Ghi(PhieuQuyViewModel q) => ModelState.IsValid ? KetQua(Kho.LapThuChi(q)) : BadRequest();
}
