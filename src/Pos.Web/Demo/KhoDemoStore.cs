using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace Pos.Web.Demo;

public sealed class KhoDemoSessionProvider(IMemoryCache cache)
{
    private readonly object _sync = new();

    public DanhTinhKhoDemo? DanhTinhHienTai(HttpContext context) =>
        context.Session.GetString("KhoDemo") is null ? null : TrongPhien(context, store => store.DanhTinh);

    public T TrongPhien<T>(HttpContext context, Func<KhoDemoStore, T> operation)
    {
        var session = context.Session;
        if (session.GetString("KhoDemo") is null)
            session.SetString("KhoDemo", "1");

        KhoDemoStore store;
        lock (_sync)
        {
            store = cache.GetOrCreate($"kho-demo:{session.Id}", entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(4);
                return new KhoDemoStore();
            })!;
        }

        lock (store)
            return operation(store);
    }
}

public sealed class ThaoTacKhoDemoException(string message) : Exception(message);
public sealed class QuyenKhoDemoException(string quyen) : Exception(quyen);

public sealed partial class KhoDemoStore
{
    public const string QuanLy = "QuanLy";
    public const string NhanVienKho = "NhanVienKho";
    public const string Nhap = "Nhap";
    public const string DaXacNhan = "DaXacNhan";
    public const string DaHuy = "DaHuy";
    public const string DangKiem = "DangKiem";
    public const string ChoDuyet = "ChoDuyet";
    public const string HoanTat = "HoanTat";

    private readonly Dictionary<string, TaiKhoanDemo> _taiKhoans = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _quyenTheoVaiTro = new(StringComparer.Ordinal);
    private readonly Dictionary<string, NhomHangDemo> _nhomHangs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MatHangKhoDemo> _matHangs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NhaCungCapDemo> _nhaCungCaps = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PhieuNhapDemo> _phieuNhaps = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PhieuKiemDemo> _phieuKiems = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CaChiDemo> _caChis = [];
    private readonly List<TraNhaCungCapDemo> _khoanTras = [];
    private readonly List<BienDongKhoDemo> _bienDongs = [];
    private string? _maDangNhap;
    private readonly string _bamMatKhauMau = BamMatKhau(MatKhauMau);

    public KhoDemoStore() => NapDuLieuMau();

    public DanhTinhKhoDemo? DanhTinh
    {
        get
        {
            var account = TaiKhoanHienTai();
            return account is null ? null : new(account.Ma, account.HoTen, account.VaiTro,
                _quyenTheoVaiTro[account.VaiTro].ToHashSet(StringComparer.Ordinal));
        }
    }

    public bool CoQuyen(string quyen, string? vaiTro = null)
    {
        var account = TaiKhoanHienTai();
        return account is not null && (vaiTro is null || account.VaiTro == vaiTro)
            && _quyenTheoVaiTro[account.VaiTro].Contains(quyen);
    }

    private void KiemTraQuyen(string quyen, string? vaiTro = null)
    {
        if (!CoQuyen(quyen, vaiTro)) throw new QuyenKhoDemoException(quyen);
    }

    public bool DangNhap(string ten, string matKhau)
    {
        var account = _taiKhoans.Values.FirstOrDefault(x => x.HoatDong && Khop(x.TenDangNhap, ten.Trim()));
        if (account is null || !KiemTraMatKhau(account.BamMatKhau, matKhau))
            return false;

        _maDangNhap = account.Ma;
        return true;
    }

    public void DangXuat() => _maDangNhap = null;

    public TaiKhoanKhoDemo[] TaiKhoans() => _taiKhoans.Values.OrderBy(x => x.TenDangNhap)
        .Select(x => new TaiKhoanKhoDemo(x.Ma, x.HoTen, x.TenDangNhap, x.VaiTro, x.HoatDong)).ToArray();

    public IReadOnlySet<string> QuyenCuaVaiTro(string vaiTro) =>
        _quyenTheoVaiTro.GetValueOrDefault(vaiTro)?.ToHashSet(StringComparer.Ordinal)
        ?? throw new ThaoTacKhoDemoException("Vai trò không hợp lệ.");

    public void CapNhatTaiKhoan(string? ma, string hoTen, string tenDangNhap, string vaiTro, string? matKhau, bool hoatDong)
    {
        KiemTraQuyen("HT_QL_TAI_KHOAN");
        var cu = ma is null ? null : Tim(_taiKhoans, ma, "Không tìm thấy tài khoản.");
        if (!_quyenTheoVaiTro.ContainsKey(vaiTro))
            throw new ThaoTacKhoDemoException("Vai trò không hợp lệ.");
        if (cu is null || cu.VaiTro != vaiTro)
            KiemTraQuyen("HT_PHAN_QUYEN");
        if (cu?.HoatDong == true && cu.VaiTro == QuanLy && (!hoatDong || vaiTro != QuanLy)
            && !_taiKhoans.Values.Any(x => x.Ma != cu.Ma && x.HoatDong && x.VaiTro == QuanLy))
            throw new ThaoTacKhoDemoException("Cần giữ ít nhất một tài khoản Quản lý hoạt động.");

        tenDangNhap = tenDangNhap.Trim();
        if (_taiKhoans.Values.Any(x => x.Ma != ma && Khop(x.TenDangNhap, tenDangNhap)))
            throw new ThaoTacKhoDemoException("Tên đăng nhập đã được sử dụng.");

        var moi = new TaiKhoanDemo(ma ?? TaoMa("TK"), hoTen.Trim(), tenDangNhap, vaiTro,
            string.IsNullOrWhiteSpace(matKhau) ? cu?.BamMatKhau ?? _bamMatKhauMau : BamMatKhau(matKhau), hoatDong);
        _taiKhoans[moi.Ma] = moi;
    }

    public void CapNhatQuyen(string vaiTro, IEnumerable<string> quyen)
    {
        KiemTraQuyen("HT_PHAN_QUYEN");
        if (!_quyenTheoVaiTro.ContainsKey(vaiTro))
            throw new ThaoTacKhoDemoException("Vai trò không hợp lệ.");

        var tapQuyen = quyen.ToHashSet(StringComparer.Ordinal);
        if (tapQuyen.Except(DanhSachQuyen.Select(x => x.Ma)).Any())
            throw new ThaoTacKhoDemoException("Mã quyền không hợp lệ.");
        if (vaiTro == QuanLy && (!tapQuyen.Contains("HT_PHAN_QUYEN") || !tapQuyen.Contains("HT_QL_TAI_KHOAN")))
            throw new ThaoTacKhoDemoException("Vai trò Quản lý cần giữ quyền quản lý tài khoản và phân quyền.");

        _quyenTheoVaiTro[vaiTro] = tapQuyen;
    }

    public NhomHangDemo[] NhomHangs() => _nhomHangs.Values.OrderBy(x => x.Ten).ToArray();
    public MatHangKhoDemo[] MatHangs() => _matHangs.Values.OrderBy(x => x.Ten).ToArray();
    public MatHangKhoDemo[] MatHangDangDung() => MatHangs().Where(x => DangDung(x.Ma)).ToArray();
    public NhaCungCapDemo[] NhaCungCaps() => _nhaCungCaps.Values.OrderBy(x => x.Ten).ToArray();
    public CaChiDemo[] CaDangMo() => _caChis.Where(x => x.DangMo).ToArray();

    public string TenHang(string ma) => _matHangs[ma].Ten;
    public string TenNcc(string ma) => _nhaCungCaps[ma].Ten;

    public void LuuNhom(string? ma, string ten)
    {
        KiemTraQuyen("DM_NHOM_QL");
        var cu = ma is null ? null : Tim(_nhomHangs, ma, "Không tìm thấy nhóm hàng.");
        ten = ten.Trim();
        KiemTraTrungTen(_nhomHangs.Values.Any(x => x.Ma != ma && Khop(x.Ten, ten)));
        var moi = new NhomHangDemo(cu?.Ma ?? TaoMa("NH"), ten, cu?.HoatDong ?? true);
        _nhomHangs[moi.Ma] = moi;
    }

    public void NgungNhom(string ma)
    {
        KiemTraQuyen("DM_NHOM_QL");
        var nhom = Tim(_nhomHangs, ma, "Không tìm thấy nhóm hàng.");
        if (_matHangs.Values.Any(x => x.MaNhom == nhom.Ma && x.HoatDong))
            throw new ThaoTacKhoDemoException("Chuyển nhóm hoặc ngừng dùng các mặt hàng đang hoạt động trước.");
        _nhomHangs[nhom.Ma] = nhom with { HoatDong = false };
    }

    public void LuuHang(string? ma, string maNhom, string ten, string donVi, decimal gia)
    {
        KiemTraQuyen("DM_HANG_QL");
        var cu = ma is null ? null : Tim(_matHangs, ma, "Không tìm thấy mặt hàng.");
        var nhom = Tim(_nhomHangs, maNhom, "Không tìm thấy nhóm hàng.");
        if (!nhom.HoatDong)
            throw new ThaoTacKhoDemoException("Chọn một nhóm hàng đang hoạt động.");

        ten = ten.Trim();
        KiemTraTrungTen(_matHangs.Values.Any(x => x.Ma != ma && Khop(x.Ten, ten)));
        KiemTraTien(gia);
        var moi = new MatHangKhoDemo(cu?.Ma ?? TaoMa("MH"), nhom.Ma, ten, donVi.Trim(), gia, cu?.HoatDong ?? true);
        _matHangs[moi.Ma] = moi;
    }

    public void NgungHang(string ma)
    {
        KiemTraQuyen("DM_HANG_QL");
        var hang = Tim(_matHangs, ma, "Không tìm thấy mặt hàng.");
        _matHangs[hang.Ma] = hang with { HoatDong = false };
    }

    public void LuuNcc(string? ma, string ten, string? lienHe)
    {
        KiemTraQuyen("DM_NCC_QL");
        var cu = ma is null ? null : Tim(_nhaCungCaps, ma, "Không tìm thấy nhà cung cấp.");
        ten = ten.Trim();
        KiemTraTrungTen(_nhaCungCaps.Values.Any(x => x.Ma != ma && Khop(x.Ten, ten)));
        var moi = new NhaCungCapDemo(cu?.Ma ?? TaoMa("NCC"), ten, lienHe?.Trim());
        _nhaCungCaps[moi.Ma] = moi;
    }

    private TaiKhoanDemo? TaiKhoanHienTai() =>
        _maDangNhap is not null && _taiKhoans.TryGetValue(_maDangNhap, out var account) && account.HoatDong
            ? account : null;

    private bool DangDung(string ma) => _matHangs.TryGetValue(ma, out var hang) && hang.HoatDong
        && _nhomHangs[hang.MaNhom].HoatDong;

    private static T Tim<T>(Dictionary<string, T> entries, string? ma, string thongBao) =>
        ma is not null && entries.TryGetValue(ma, out var result) ? result : throw new ThaoTacKhoDemoException(thongBao);

    private static bool Khop(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static void KiemTraTrungTen(bool trung)
    {
        if (trung) throw new ThaoTacKhoDemoException("Tên này đã được sử dụng.");
    }

    private static void KiemTraTien(decimal tien)
    {
        if (tien < 0 || tien != decimal.Truncate(tien))
            throw new ThaoTacKhoDemoException("Số tiền phải là số nguyên không âm (đồng).");
    }

    private static string TaoMa(string tienTo) => $"{tienTo}-{Guid.NewGuid():N}"[..(tienTo.Length + 13)].ToUpperInvariant();

    private static string BamMatKhau(string matKhau)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    private static bool KiemTraMatKhau(string chuoiBam, string matKhau)
    {
        var parts = chuoiBam.Split(':');
        var hash = Rfc2898DeriveBytes.Pbkdf2(matKhau, Convert.FromHexString(parts[0]), 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, Convert.FromHexString(parts[1]));
    }
}

public sealed record DanhTinhKhoDemo(string Ma, string HoTen, string VaiTro, IReadOnlySet<string> Quyen)
{
    public bool CoQuyen(string quyen, string? vaiTro = null) => Quyen.Contains(quyen) && (vaiTro is null || VaiTro == vaiTro);
}

public sealed record QuyenDemo(string Ma, string Ten);
internal sealed record TaiKhoanDemo(string Ma, string HoTen, string TenDangNhap, string VaiTro, string BamMatKhau, bool HoatDong);
public sealed record TaiKhoanKhoDemo(string Ma, string HoTen, string TenDangNhap, string VaiTro, bool HoatDong);
public sealed record NhomHangDemo(string Ma, string Ten, bool HoatDong);
public sealed record MatHangKhoDemo(string Ma, string MaNhom, string Ten, string DonVi, decimal GiaBan, bool HoatDong);
public sealed record NhaCungCapDemo(string Ma, string Ten, string? LienHe);
public sealed record CaChiDemo(string Ma, decimal TienMatDauCa, bool DangMo);
