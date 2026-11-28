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
    private readonly Dictionary<Guid, (string NguoiMo, decimal Dau, string MaCa)> _yeuCauMoCa = new();
    private readonly Dictionary<Guid, (string NguoiLap, PhieuQuyViewModel Request, string Ma)> _yeuCauQuy = new();
    internal CaChiDemo[] CaDangMoDeChi() =>
        _caBans.Where(c => c.TrangThai == "DangMo")
            .Select(c => new CaChiDemo(c.MaCa, _banGiao[c.MaCa].Dau, true)).ToArray();

    private static bool TienHopLe(decimal n) => n >= 0 && n <= 999999999999999m && decimal.Truncate(n) == n;
    private IEnumerable<DongQuyViewModel> DocSo()
    {
        foreach (var row in _thuChi) yield return row;
        foreach (var tra in Kho.CacKhoanTraTuKet())
        {
            yield return new(tra.MaCa!, GioPos.Local(tra.LucTra), tra.Ma, "Trả nhà cung cấp",
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
                    yield return new(caDao, lucHuy, h.MaHoaDon, "Đảo hủy", "KetCa", "TienMat",
                        -h.TongTienMatPhanBo, h.LyDoHuy ?? "", MaNguoiLapHoaDon: h.MaNguoiLap);
                if (h.TongChuyenKhoan > 0)
                    yield return new(h.MaCa, lucHuy, h.MaHoaDon, "Đảo hủy", "QuyCuaHang", "ChuyenKhoan", -h.TongChuyenKhoan, h.LyDoHuy ?? "");
            }
        }
        foreach (var p in _phieuTra.Values.Where(p => p.XacNhan))
        {
            if (p.TienMat > 0) yield return new(p.Ca,p.LucXacNhan ?? p.Ngay,p.Ma,"Hoàn trả","KetCa","TienMat",-p.TienMat);
            if (p.ChuyenKhoan > 0) yield return new(p.Ca,p.LucXacNhan ?? p.Ngay,p.Ma,"Hoàn trả","QuyCuaHang","ChuyenKhoan",-p.ChuyenKhoan);
        }
    }
    public (int Status, CaBanViewModel? Model) XemCa(LocCaViewModel loc, bool xemSoQuy)
    {
        lock (_sync)
        {
            var actor = Kho.DanhTinh;
            var quyen = xemSoQuy ? "QUY_TRA_SO" : "CA_TRA_BAN_GIAO";
            if (actor is null || !actor.CoQuyen(quyen) || actor.VaiTro is not ("ThuNgan" or "QuanLy"))
                return (403, null);

            var cacCa = LayDanhSachCaBanTheoVaiTro(actor.VaiTro).ToArray();
            var maCaDuocXem = cacCa.Select(c => c.MaCa).ToHashSet();
            var chon = loc.Chon is null
                ? cacCa.FirstOrDefault(c => c.TrangThai == "DangMo")
                : cacCa.FirstOrDefault(c => string.Equals(c.MaCa, loc.Chon, StringComparison.OrdinalIgnoreCase));
            var status = loc.Chon is null || chon is not null
                ? 200
                : _banGiao.Keys.Contains(loc.Chon, StringComparer.OrdinalIgnoreCase) ? 403 : 404;
            bool TrongKy(DateTime luc) => !(DateOnly.FromDateTime(luc) < loc.Tu || DateOnly.FromDateTime(luc) > loc.Den);
            var lichSu = cacCa.Where(c => TrongKy(c.LucMo)
                && c.MaCa.Contains(loc.Ma ?? "", StringComparison.OrdinalIgnoreCase)
                && (loc.TrangThai is null || c.TrangThai == loc.TrangThai)).OrderByDescending(c => c.LucMo).ToArray();
            var butToan = DocSo().Where(d => maCaDuocXem.Contains(d.MaCa)).OrderByDescending(d => d.Luc).ToArray();
            var so = status == 200
                ? butToan.Where(d => TrongKy(d.Luc) && (loc.Chon is null || d.MaCa == chon?.MaCa)
                    && (loc.PhuongThuc is null || d.PhuongThuc == loc.PhuongThuc)).ToArray()
                : [];
            return (status, new(loc, cacCa, lichSu, chon, chon is null ? null : _banGiao[chon.MaCa],
                so, butToan.Where(d => d.MaCa == chon?.MaCa).ToArray(),
                actor.CoQuyen("CA_MO", "ThuNgan") && cacCa.All(c => c.TrangThai != "DangMo"),
                actor.CoQuyen("CA_DONG", "ThuNgan") && chon?.TrangThai == "DangMo" && chon.MaNguoiMo == actor.Ma,
                actor.CoQuyen("QUY_LAP_THU_CHI")));
        }
    }
    public (int Status, string Message) MoCa(decimal dau, Guid? maYeuCau = null)
    {
        lock (_sync)
        {
            var actor = Kho.DanhTinh;
            if (actor is null || !actor.CoQuyen("CA_MO", "ThuNgan"))
                return (403, "Tài khoản hiện tại không có quyền mở ca.");
            if (maYeuCau == Guid.Empty)
                return (400, "Mã yêu cầu không hợp lệ. Tải lại biểu mẫu.");
            if (maYeuCau is Guid maCu && _yeuCauMoCa.TryGetValue(maCu, out var cu))
                return cu.NguoiMo == actor.Ma && cu.Dau == dau
                    ? (200, cu.MaCa) : (409, "Mã yêu cầu đã dùng với nội dung khác.");
            if (!TienHopLe(dau))
                return (400, "Tiền đầu ca phải là số đồng nguyên không âm.");
            if (LayCaHienTai() is { } ca)
                return (409, $"Bạn đang có ca mở {ca.MaCa}. Kiểm tra ca hiện tại trước khi mở ca mới.");

            var ma = $"CA-{_caBans.Count + 1:D5}";
            _caBans.Add(new(ma, actor.Ma, actor.HoTen, "DangMo", GioPos.Now));
            _banGiao.Add(ma, new(dau));
            if (maYeuCau is Guid maMoi)
                _yeuCauMoCa.Add(maMoi, (actor.Ma, dau, ma));
            return (200, ma);
        }
    }
    public (int Status, string Message) DongCa(string ma, decimal dem)
    {
        lock (_sync)
        {
            var actor = Kho.DanhTinh;
            var i = _caBans.FindIndex(c => c.MaCa == ma);
            if (actor is null || !actor.CoQuyen("CA_DONG", "ThuNgan") || (i >= 0 && _caBans[i].MaNguoiMo != actor.Ma))
                return (403, "Chỉ người mở ca có quyền đóng ca của mình.");
            if (i < 0)
                return (404, "Không tìm thấy ca.");
            var banGiao = _banGiao[ma];
            if (_caBans[i].TrangThai != "DangMo")
                return banGiao.Dem == dem ? (200, ma) : (409, "Ca đã đóng. Xem lại số tiền bàn giao.");
            if (!TienHopLe(dem))
                return (400, "Tiền thực đếm không hợp lệ.");

            _banGiao[ma] = banGiao with
            {
                Dem = dem,
                Chot = banGiao.Dau + DocSo().Where(d => d.MaCa == ma).Sum(d => d.TienKet),
                Dong = GioPos.Now
            };
            _caBans[i] = _caBans[i] with { TrangThai = "DaDong" };
            return (200, ma);
        }
    }
    public (int Status, string Message) LapThuChi(PhieuQuyViewModel form)
    {
        lock (_sync)
        {
            var actor = Kho.DanhTinh;
            if (actor is null || !actor.CoQuyen("QUY_LAP_THU_CHI") || actor.VaiTro is not ("ThuNgan" or "QuanLy"))
                return (403, "Tài khoản hiện tại không có quyền lập phiếu thu, chi.");
            if (form.MaYeuCau == Guid.Empty)
                return (400, "Mã yêu cầu không hợp lệ. Tải lại biểu mẫu.");
            if (_yeuCauQuy.TryGetValue(form.MaYeuCau, out var cu))
                return cu.NguoiLap == actor.Ma && cu.Request == form
                    ? (200, cu.Ma) : (409, "Mã yêu cầu đã dùng với nội dung khác.");

            var ca = _caBans.Find(c => c.MaCa == form.MaCa);
            if (ca is not null && actor.VaiTro == "ThuNgan" && ca.MaNguoiMo != actor.Ma)
                return (403, "Ca thuộc thu ngân khác.");
            if (ca?.TrangThai != "DangMo")
                return (400, "Chọn một ca đang mở.");
            if (!TienHopLe(form.SoTien) || form.SoTien == 0 || string.IsNullOrWhiteSpace(form.LyDo) || form.LyDo.Length > 500
                || form.ChungTu is { Length: > 64 }
                || form.Loai is not ("Thu" or "Chi") || form.Nguon is not ("KetCa" or "QuyCuaHang")
                || form.PhuongThuc is not ("TienMat" or "ChuyenKhoan") || (form.Nguon == "KetCa" && form.PhuongThuc != "TienMat"))
                return (400, "Kiểm tra nguồn quỹ, phương thức, số tiền và lý do.");
            var chungTu = form.ChungTu?.Trim();
            if (!string.IsNullOrWhiteSpace(chungTu) && DaGhiChungTu(chungTu))
                return (400, "Chứng từ này đã có bút toán từ nghiệp vụ gốc. Chỉ lập phiếu cho khoản thu, chi riêng.");

            var ma = $"TC-{_thuChi.Count + 1:D5}";
            _thuChi.Add(new(ca.MaCa, GioPos.Now, ma, form.Loai == "Thu" ? "Thu riêng" : "Chi riêng",
                form.Nguon, form.PhuongThuc, form.Loai == "Thu" ? form.SoTien : -form.SoTien, form.LyDo.Trim(), chungTu));
            _yeuCauQuy.Add(form.MaYeuCau, (actor.Ma, form, ma));
            return (200, ma);
        }
    }

    private bool DaGhiChungTu(string ma)
    {
        bool Trung(string? maChungTu) => string.Equals(ma, maChungTu, StringComparison.OrdinalIgnoreCase);
        return _hoaDons.Any(hoaDon => hoaDon.LucXacNhan.HasValue && Trung(hoaDon.MaHoaDon))
            || _phieuTra.Values.Any(phieu => phieu.XacNhan && Trung(phieu.Ma))
            || Kho.PhieuNhaps().SelectMany(phieu => Kho.LichSuTra(phieu.Ma)).Any(tra => Trung(tra.Ma) || Trung(tra.MaPhieu));
    }
}
