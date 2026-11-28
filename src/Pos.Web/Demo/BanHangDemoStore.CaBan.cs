using Pos.Web.ViewModels.CaBan;

namespace Pos.Web.Demo;

public sealed partial class BanHangDemoStore
{
    private readonly Dictionary<string, BanGiaoViewModel> _banGiao = new()
    {
        ["S-CA-1010"] = new(500000,600000,598000,new(2026,10,10,17,0,0)),
        ["S-CA-1011"] = new(500000,414000,410000,new(2026,10,11,17,0,0)),
        ["S-CA-1112"] = new(500000,500000,501000,new(2026,11,12,17,0,0)),
        ["S-CA-1126"] = new(300000)
    };
    private readonly List<DongQuyViewModel> _thuChi =
    [
        new("S-CA-1010",new(2026,10,10,11,0,0),"S-TC-01","Chi riêng","KetCa","TienMat",-6000,"Mua bút viết","BB-01"),
        new("S-CA-1010",new(2026,10,10,12,0,0),"S-TC-02","Thu riêng","KetCa","TienMat",10000,"Thu lại tiền ứng mua vật tư"),
        new("S-CA-1010",new(2026,10,10,12,30,0),"S-TC-03","Thu riêng","QuyCuaHang","ChuyenKhoan",20000,"Thu ngoài két ca")
    ];
    private readonly Dictionary<string,(PhieuQuyViewModel Request,string Ma)> _yeuCauQuy = new();
    public string? MaCaMo
    {
        get { lock (_sync) return _caBans.Find(c => c.MaNguoiMo == "S-TK-TN1" && c.TrangThai == "DangMo")?.MaCa; }
    }
    internal CaChiDemo[] CaDangMoDeChi() =>
        _caBans.Where(c => c.TrangThai == "DangMo")
            .Select(c => new CaChiDemo(c.MaCa, _banGiao[c.MaCa].Dau, true)).ToArray();

    private static bool TienHopLe(decimal n) => n >= 0 && n <= 999999999999999m && decimal.Truncate(n) == n;
    private IEnumerable<DongQuyViewModel> DocSo()
    {
        foreach (var row in _thuChi) yield return row;
        foreach (var tra in Kho.CacKhoanTraTuKet())
        {
            yield return new(tra.MaCa!, tra.LucTra.LocalDateTime, tra.Ma, "Trả nhà cung cấp",
                tra.Nguon, tra.PhuongThuc, -tra.SoTien, "Thanh toán phiếu nhập", tra.MaPhieu);
        }
        foreach (var h in _hoaDons.Where(h => h.LucXacNhan.HasValue))
        {
            foreach (var t in h.ThanhToans)
                yield return new(h.MaCa,h.LucXacNhan!.Value,h.MaHoaDon,"Thu bán",
                    t.PhuongThuc == "TienMat" ? "KetCa" : "QuyCuaHang",t.PhuongThuc,t.SoTienThanhToan);
            if (h.TrangThai == "DaHuy" && h.LucHuy is { } lucHuy)
            {
                if (h.MaCaDao is { } caDao && h.TongTienMatPhanBo > 0)
                    yield return new(caDao, lucHuy, h.MaHoaDon, "Đảo hủy", "KetCa", "TienMat", -h.TongTienMatPhanBo);
                if (h.TongChuyenKhoan > 0)
                    yield return new(h.MaCa, lucHuy, h.MaHoaDon, "Đảo hủy", "QuyCuaHang", "ChuyenKhoan", -h.TongChuyenKhoan);
            }
        }
        foreach (var p in _phieuTra.Values.Where(p => p.XacNhan))
        {
            if (p.TienMat > 0) yield return new(p.Ca,p.Ngay,p.Ma,"Hoàn trả","KetCa","TienMat",-p.TienMat);
            if (p.ChuyenKhoan > 0) yield return new(p.Ca,p.Ngay,p.Ma,"Hoàn trả","QuyCuaHang","ChuyenKhoan",-p.ChuyenKhoan);
        }
    }
    public (int Status,CaBanViewModel? Model) XemCa(LocCaViewModel l)
    {
        lock (_sync)
        {
            var visible = LayDanhSachCaBanTheoVaiTro(l.VaiTro).ToArray();
            var codes = visible.Select(c=>c.MaCa).ToHashSet();
            var chosen = l.Chon == null ? visible.FirstOrDefault(c=>c.TrangThai=="DangMo") :
                visible.FirstOrDefault(c=>c.MaCa==l.Chon);
            if (l.Chon != null && chosen == null) return (_banGiao.ContainsKey(l.Chon)?403:404,null);
            bool TrongKy(DateTime time) => !(DateOnly.FromDateTime(time)<l.Tu || DateOnly.FromDateTime(time)>l.Den);
            var history = visible.Where(c=>TrongKy(c.LucMo) && c.MaCa.Contains(l.Ma??"",StringComparison.OrdinalIgnoreCase)
                && (l.TrangThai==null || c.TrangThai==l.TrangThai)).OrderByDescending(c=>c.LucMo).ToArray();
            var events = DocSo().Where(d=>codes.Contains(d.MaCa)).OrderByDescending(d=>d.Luc).ToArray();
            return (200,new(l,visible,history,chosen,chosen==null?null:_banGiao[chosen.MaCa],
                events.Where(d=>TrongKy(d.Luc) && (l.Chon==null || d.MaCa==l.Chon) &&
                    (l.PhuongThuc==null || d.PhuongThuc==l.PhuongThuc)).ToArray(),
                events.Where(d=>d.MaCa==chosen?.MaCa).ToArray()));
        }
    }
    public (int,string) MoCa(string vaiTro,decimal dau)
    {
        lock (_sync)
        {
            if(vaiTro!="ThuNgan") return (403,"Chỉ thu ngân được mở ca.");
            if(!TienHopLe(dau)) return (400,"Tiền đầu ca phải là số đồng nguyên không âm.");
            if(MaCaMo!=null) return (409,"Bạn đang có một ca mở.");
            var ma=$"CA-{_caBans.Count+1:D5}";
            _caBans.Add(new(ma,"S-TK-TN1","Lê Linh","DangMo",DateTime.Now));
            _banGiao.Add(ma,new(dau));
            return (200,ma);
        }
    }
    public (int,string) DongCa(string vaiTro,string ma,decimal dem)
    {
        lock(_sync)
        {
            var i=_caBans.FindIndex(c=>c.MaCa==ma);
            if(vaiTro!="ThuNgan" || (i>=0 && _caBans[i].MaNguoiMo!="S-TK-TN1")) return (403,"Chỉ người mở ca được đóng ca.");
            if(i<0 || _caBans[i].TrangThai!="DangMo") return (409,"Ca không còn mở.");
            if(!TienHopLe(dem)) return (400,"Tiền thực đếm không hợp lệ.");
            var b=_banGiao[ma];
            _banGiao[ma]=b with {Dem=dem,Chot=b.Dau+DocSo().Where(d=>d.MaCa==ma).Sum(d=>d.TienKet),Dong=DateTime.Now};
            _caBans[i]=_caBans[i] with {TrangThai="DaDong"};
            return (200,ma);
        }
    }
    public (int,string) LapThuChi(PhieuQuyViewModel q)
    {
        lock(_sync)
        {
            if(q.VaiTro is not ("ThuNgan" or "QuanLy")) return (403,"Vai trò không có quyền lập phiếu.");
            if(_yeuCauQuy.TryGetValue(q.MaYeuCau,out var cu))
                return cu.Request==q ? (200,cu.Ma) : (409,"Mã yêu cầu đã dùng với nội dung khác.");
            var ca=_caBans.Find(c=>c.MaCa==q.MaCa);
            if(ca!=null && q.VaiTro=="ThuNgan" && ca.MaNguoiMo!="S-TK-TN1") return (403,"Ca thuộc thu ngân khác.");
            if(ca?.TrangThai!="DangMo") return (400,"Chọn một ca đang mở.");
            if(!TienHopLe(q.SoTien) || q.SoTien==0 || string.IsNullOrWhiteSpace(q.LyDo) ||
                q.Loai is not ("Thu" or "Chi") || q.Nguon is not ("KetCa" or "QuyCuaHang") ||
                q.PhuongThuc is not ("TienMat" or "ChuyenKhoan") || (q.Nguon=="KetCa" && q.PhuongThuc!="TienMat"))
                return (400,"Kiểm tra nguồn quỹ, phương thức, số tiền và lý do.");
            var ma=$"TC-{_thuChi.Count+1:D5}";
            _thuChi.Add(new(q.MaCa,DateTime.Now,ma,q.Loai=="Thu"?"Thu riêng":"Chi riêng",q.Nguon,q.PhuongThuc,
                q.Loai=="Thu"?q.SoTien:-q.SoTien,q.LyDo,q.ChungTu));
            _yeuCauQuy.Add(q.MaYeuCau,(q,ma));
            return (200,ma);
        }
    }
}
