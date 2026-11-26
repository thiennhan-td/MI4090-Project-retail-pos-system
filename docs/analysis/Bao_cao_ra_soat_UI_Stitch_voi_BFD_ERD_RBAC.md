# Báo cáo rà soát giao diện Front-end (Stitch) đối chiếu với BFD, ERD 21 thực thể và RBAC

**Ngày lập:** 27/11/2026  
**Đối tượng rà soát:** 02 gói mã giao diện Front-end do Thành viên 2 (TV2) xuất từ Stitch:
- `stitch_thi_t_k_form_ch_c_n_ng.zip` — 16 màn hình (`F1.1` → `F7.5`) + `retail_core_pos/DESIGN.md`
- `stitch_thi_t_k_form_ch_c_n_ng_(1).zip` — 03 màn hình báo cáo (`F8.1–F8.2`, `F8.3`, `F8.4–F8.5`) + `retail_core_pos/DESIGN.md`

**Tài liệu chuẩn làm gốc đối chiếu (đã chốt):**
1. **BFD 31 chức năng:** `docs/analysis/BFD_POS_TinhChinh.png` & `docs/analysis/Dac-ta-chuc-nang-POS-31.docx`
2. **ERD 21 thực thể:** `docs/analysis/Bao-cao-ERD-POS.docx`
3. **12 Quy tắc nghiệp vụ & Ma trận RBAC:** `docs/analysis/Quy-tac-nghiep-vu-va-RBAC-POS-da-chot.docx`
4. **Checklist đánh giá MI4090 (Nhóm 02 người):** `docs/references/Checklist_BTL_HTTTQL_v2026.xlsx`

---

## 1. Kết luận tổng quan

> **Nguyên tắc vàng:** **KHÔNG điều chỉnh ERD 21 thực thể chạy theo các trường do AI (Stitch) tự sinh ra trên giao diện.** Thay vào đó, cần **gọt và căn chỉnh lại bộ giao diện** để khớp 100% với BFD 31 chức năng, ERD 21 thực thể và 6 quyết định `QĐ01–QĐ06` đã chốt.

- **Điểm mạnh rất đáng giữ:**
  - Bộ giao diện có thẩm mỹ cao, hệ thống thiết kế (`Retail Core POS` — Navy `#1E3A8A` + Royal Blue `#2563EB`, `tabular-nums`, tiền `#,##0 đ`) hiện đại và nhất quán về màu sắc, phông chữ.
  - Phủ đủ **19 màn hình** (1 Đăng nhập + **18 màn hình nghiệp vụ**, vượt xa định mức ≥9 Form của mục `1.1`), tỷ lệ màn hình danh mục đơn chỉ **3/18 ≈ 16,7% ≤ 40%** (an toàn trước Khóa 2), và thể hiện đúng nhiều kịch bản lõi của đề tài (thanh toán hỗn hợp `F3`, đối chiếu `Đã mua / Đã trả / Còn được trả` ở `F4`, kiểm kho phân biệt `0` vs chưa đếm ở `F6`, đối chiếu lệch két `-20.000 đ` ở `F7`).
- **Lý do chưa thể dùng nguyên trạng để thiết kế DB & kiến trúc:**
  - Công cụ Stitch đã tự mở rộng bối cảnh thành **Enterprise ERP đa chi nhánh** (thêm chi nhánh, đa kho, đa quầy, khách hàng VIP, VAT 8%, chiết khấu, giá vốn/lãi gộp, lô SX/HSD, RFID/2FA, 4 vai trò...).
  - Nếu chạy DB theo UI sẽ phá vỡ hoàn toàn ERD 21 bảng vừa chốt (phình lên ~35–40 bảng, quá tải cho nhóm 2 SV). Ngược lại, nếu giữ DB 21 bảng mà để nguyên các cột/nút thừa trên UI thì sẽ **trượt kiểm chứng truy vết ngược `B → A` (mục `3.1`)** khi giảng viên chỉ vào một cột bất kỳ trên màn hình và hỏi lưu ở bảng nào.

---

## 2. Khung chuẩn BFD và ERD hiện tại làm căn cứ đối chiếu

### 2.1. Biểu đồ phân cấp chức năng (BFD — 8 nhóm / 31 chức năng)
- **Phạm vi khóa cứng:** 01 cửa hàng bán lẻ (quầy thu ngân + kho), 03 vai trò (`Thu ngân`, `Nhân viên kho`, `Quản lý`), **loại trừ** khách hàng thành viên và kênh bán ngoài.
- **Phân công dọc:**
  - **TV1 (Quầy):** `F3` (Bán hàng & thu tiền: `F3.1–F3.4`), `F4` (Nhận trả & hoàn tiền: `F4.1–F4.3`), `F7` (Quản lý ca & sổ quỹ: `F7.1–F7.5`), báo cáo bán/ca (`F8.2`, `F8.4`) và phối hợp `F8.1`.
  - **TV2 (Kho & Hệ thống):** `F1` (Quản trị hệ thống: `F1.1–F1.3`), `F2` (Quản lý danh mục: `F2.1–F2.3`), `F5` (Nhập hàng & trả tiền: `F5.1–F5.4`), `F6` (Kiểm soát tồn kho: `F6.1–F6.4`), báo cáo kho/NCC (`F8.3`, `F8.5`) và chủ trì `F8.1`.

### 2.2. Cấu trúc ERD hiện tại (21 thực thể 3NF — 4 cụm)

| Cụm nghiệp vụ | Thực thể (21 bảng) | Thuộc tính trọng tâm & Ràng buộc đã chốt |
|---|---|---|
| **1. Quản trị & Ca bán (6)** | `TaiKhoan`, `VaiTro`, `QuyenVaiTro`, `Quyen`, `CaBan`, `PhieuThuChi` | Hồ sơ nhân viên gộp trực tiếp trong `TaiKhoan` (`QĐ06`); mỗi tài khoản đúng **1 `VaiTro`**; 3 vai trò & 40 mã quyền trong `QuyenVaiTro`; 1 thu ngân tối đa 1 `CaBan` `DangMo` (`QĐ05`, không có bảng `Quay`); `CaBan` chứa luôn thông tin đóng ca (`maNguoiChot?`, `lucDong?`, `tienDuKienKhiChot?`, `tienThucDem?`). |
| **2. Bán hàng & Nhận trả (6)** | `HoaDon`, `DongBan`, `ThanhToanBan`, `PhieuTra`, `DongTra`, `KhoanHoan` | `HoaDon` có `trangThai` (`Nhap \| DaXacNhan \| DaHuy`) và trường hủy gộp (`maNguoiHuy?`, `lucHuy?`, `lyDoHuy?`, `maCaDao?`); `DongBan` lưu `donGiaThucBan` (giá lịch sử); `ThanhToanBan` & `KhoanHoan` hỗ trợ đa phương thức; `DongTra` trỏ `DongBan`, tách riêng `soLuongTra` và `soLuongNhapLai` (`0 ≤ soLuongNhapLai ≤ soLuongTra`); không có bảng `KhachHang`, không có VAT/Chiết khấu. |
| **3. Nhập hàng & Danh mục (6)** | `NhomHang`, `MatHang`, `NhaCungCap`, `PhieuNhap`, `DongNhap`, `TraNhaCungCap` | `NhomHang` phẳng `(maNhom, tenNhom, trangThai)`; `MatHang` gồm `(maHang, maNhom, tenHang, donViTinh, giaBanHienHanh, trangThai)` — **không có `giaVon`, `loSX`, `hanDung`, `dinhMucTon`**; `NhaCungCap` gồm `(maNhaCungCap, tenNhaCungCap, thongTinLienHe)`; `PhieuNhap` có `trangThai` (`Nhap \| DaXacNhan \| DaHuy`), `hanThanhToan?`; `TraNhaCungCap` hỗ trợ trả nhiều lần (`nguonChi`, `maCaChi?`). |
| **4. Kiểm kho & Thẻ kho (3)** | `PhieuKiem`, `DongKiem`, `BienDongKho` | `PhieuKiem` có vòng đời 2 bước (`DangKiem → ChoDuyet → HoanTat` hoặc `DaHuy`); `DongKiem` có `PK(maPhieuKiem, maHang)`, `tonSoTaiMoc`, `soLuongDem?` (`NULL` = chưa đếm, `0` = hết hàng), `giaiTrinh?`; `BienDongKho` là sổ cái bất biến do Kho sở hữu (gọi từ Quầy qua `IInventoryService`), tồn hiện tại tính từ tổng biến động. |

---

## 3. Bảng ánh xạ tổng thể 19 màn hình Stitch ↔ BFD ↔ ERD

| # | Thư mục màn hình Stitch | Mã F tương ứng | Thực thể ERD tương ứng | Đánh giá khớp |
|---|---|---|---|---|
| 1 | `f1.1_ng_nh_p_h_th_ng_pos` | `F1.1` | `TaiKhoan`, `VaiTro`, `QuyenVaiTro` | **Lệch trung bình** (thừa chi nhánh, quầy, RFID, PIN, QR, chọn ca trước khi đăng nhập) |
| 2 | `f1.2_qu_n_l_t_i_kho_n_nh_n_s` | `F1.2` | `TaiKhoan`, `VaiTro` | **Lệch nhẹ** (thừa chi nhánh, SĐT, mã PIN, phân công ca cố định) |
| 3 | `f1.3_c_u_h_nh_ph_n_quy_n_ch_c_n_ng` | `F1.3` | `VaiTro`, `Quyen`, `QuyenVaiTro` | **Lệch nặng** (4 vai trò, tạo vai trò tùy chỉnh, 2FA, đóng ca cưỡng bức, P&L, không khớp 40 mã quyền) |
| 4 | `f2.1_qu_n_l_nh_m_h_ng` | `F2.1` | `NhomHang` | **Lệch trung bình** (cây nhóm cha–con, thứ tự POS, mô tả, nhập Excel) |
| 5 | `f2.2_qu_n_l_h_ng_v_gi_b_n` | `F2.2` | `MatHang`, `NhomHang`, `BienDongKho` (tồn) | **Lệch nặng** (thừa giá vốn, VAT, HSD, định mức tồn, thiết lập bảng giá, in mã vạch, VIP) |
| 6 | `f2.3_qu_n_l_nh_cung_c_p` | `F2.3` | `NhaCungCap`, `PhieuNhap`, `TraNhaCungCap` | **Lệch trung bình** (thừa MST, email, địa chỉ chi tiết, STK ngân hàng, hạn mức công nợ) |
| 7 | `f3.1_f3.3_m_n_h_nh_b_n_h_ng_pos_c_m_ng` | `F3.1–F3.3` | `HoaDon`, `DongBan`, `ThanhToanBan`, `MatHang`, `CaBan`, `BienDongKho` | **Lệch trung bình** (khác Shell, thừa Khách lẻ [F4], Chiết khấu, Thẻ POS, Quầy 01, nút Mở két phần cứng) |
| 8 | `f3.4_tra_c_u_in_h_y_h_a_n` | `F3.4` | `HoaDon`, `DongBan`, `ThanhToanBan`, `PhieuTra` | **Lệch trung bình** (mất Sidebar chung, lọc theo Quầy 01/02, xác thực PIN thay vì RBAC phiên Quản lý) |
| 9 | `f4.1_f4.2_l_p_phi_u_tr_h_ng_ho_n_ti_n` | `F4.1–F4.2` | `PhieuTra`, `DongTra`, `KhoanHoan`, `HoaDon`, `DongBan`, `BienDongKho` | **Lệch nặng** (mất Sidebar, dùng checkbox thay vì ô số `soLuongNhapLai`, chỉ 1 phương thức hoàn, thừa VAT 8%/Lô SX/Phí đổi trả, thiếu xóa nháp) |
| 10 | `f4.3_tra_c_u_phi_u_tr` | `F4.3` | `PhieuTra`, `DongTra`, `KhoanHoan` | **Lệch trung bình** (mất Sidebar, lọc Quầy 01/02, phương thức `Voucher/Điểm thưởng`, tỷ lệ hoàn) |
| 11 | `f5.1_f5.2_l_p_phi_u_x_c_nh_n_nh_p_kho` | `F5.1–F5.2` | `PhieuNhap`, `DongNhap`, `NhaCungCap`, `MatHang`, `BienDongKho` | **Lệch nặng** (thừa VAT 8%, chiết khấu, số HĐ GTGT, Lô SX, Hạn dùng, SL chứng từ vs thực nhận, hạn mức nợ, sai định dạng ngày `05/15/2025`) |
| 12 | `f5.3_f5.4_s_phi_u_nh_p_tr_ti_n_ncc` | `F5.3–F5.4` | `PhieuNhap`, `DongNhap`, `TraNhaCungCap`, `CaBan` | **Lệch trung bình** (thừa nút `Tạo phiếu trả hàng NCC`; đang đăng nhập Thủ kho nhưng hiện nút `Trả tiền` và `Hủy phiếu` vốn chỉ Quản lý mới có) |
| 13 | `f6.1_s_chi_ti_t_h_ng_h_a_th_kho` | `F6.1` | `BienDongKho`, `MatHang`, `NhomHang` | **Khớp tốt (lệch nhẹ)** (chỉ thừa chọn Chi nhánh/Kho lưu trữ, Quy cách, Barcode) |
| 14 | `f6.2_f6.4_phi_u_ki_m_k_c_n_ch_nh_t_n` | `F6.2–F6.4` | `PhieuKiem`, `DongKiem`, `MatHang`, `BienDongKho` | **Lệch nặng** (thừa Đơn giá vốn & Giá trị lệch `-752.000 đ`, HSD/Lô/Kệ; sai quyền Thủ kho bấm Duyệt cân chỉnh; lỗi cộng `2+2+1=5` nhưng ghi `4 SKU`) |
| 15 | `f7.1_f7.4_f7.5_qu_n_l_ca_b_n_giao_k_t` | `F7.1`, `F7.4`, `F7.5` | `CaBan`, `HoaDon`, `PhieuTra`, `PhieuThuChi`, `TraNhaCungCap` | **Lệch trung bình** (thiếu nguồn thu/chi riêng, chi NCC từ két & đảo hủy trong công thức két; thừa Người nhận bàn giao + PIN) |
| 16 | `f7.2_f7.3_s_qu_thu_chi_ti_n_m_t` | `F7.2–F7.3` | `PhieuThuChi`, `CaBan`, các nguồn sổ quỹ | **Lệch trung bình** (thừa Tài khoản ngân hàng `42.850.000 đ`, Đối chiếu sổ phụ ngân hàng, chọn Quầy 01) |
| 17 | `f8.1_f8.2_b_o_c_o_cu_i_ng_y_th_ng_k_b_n_tr` | `F8.1`, `F8.2` | Tổng hợp Quầy ↔ Kho | **Lệch trung bình** (thừa cột Khách hàng VIP, Visa, Gửi email; hiển thị Thu ngân đang xem báo cáo của Quản lý) |
| 18 | `f8.3_th_ng_k_xu_t_nh_p_t_n_xnt` | `F8.3` | `BienDongKho`, `MatHang`, `NhomHang` | **Lệch trung bình** (thừa Kho đệm tầng 2, cột Thành tiền / Giá trị tồn theo giá vốn, Cảnh báo dưới định mức an toàn; hiển thị Thu ngân xem XNT) |
| 19 | `f8.4_f8.5_t_ng_h_p_thanh_to_n_l_ch_ca_c_ng_n_ncc` | `F8.4`, `F8.5` | `CaBan`, `ThanhToanBan`, `KhoanHoan`, `PhieuNhap`, `TraNhaCungCap` | **Lệch trung bình** (thừa cột Quầy `POS-01/02`, Hạn mức két `15tr`, Rút nộp quỹ phụ; hiển thị Thu ngân xem báo cáo Quản lý) |

---

## 4. Phân tích chi tiết các vấn đề và điểm mâu thuẫn

### Nhóm I — Mâu thuẫn phạm vi & Trường dữ liệu ngoài ERD 21 thực thể

1. **Bối cảnh đa chi nhánh, đa kho, đa quầy (`19/19` màn hình đều dính):**
   - Toàn bộ thanh top-bar có bộ chọn *Chi nhánh hoạt động*, `F8.3` có *Kho đệm lưu trữ tầng 2*, nhiều bảng có cột *Quầy 01 / Quầy 02 / POS-01 / POS-02*.
   - **Mâu thuẫn:** Toàn bộ BFD, ERD và `QĐ05` đã chốt phạm vi **1 cửa hàng, 1 kho, giới hạn ca theo thu ngân (`maNguoiMo`), không có bảng `ChiNhanh`, `Kho`, `Quay`**.
2. **Dữ liệu Khách hàng thành viên, VIP và Voucher (`F3.1`, `F4.3`, `F7.2`, `F8.1`):**
   - `F3.1` gắn phím tắt `F4 Khách hàng` và nhãn `Khách lẻ [F4]`; `F8.1` hiển thị cột `Khách hàng` với tên *"Nguyễn Văn B (VIP)"*, *"Trần Thị Hương"*; `F4.3` có bộ lọc *"Voucher hoàn / Điểm thưởng"*.
   - **Mâu thuẫn:** Cả BFD 31 chức năng, Đặc tả và ERD đều ghi rõ **loại trừ quản lý khách hàng thành viên** (không có bảng `KhachHang` hay `Voucher`).
3. **Thuế VAT, Chiết khấu, Giá vốn, Lãi gộp, Lô SX & Hạn dùng (`F1.3`, `F2.2`, `F3.1`, `F4.1`, `F5.1`, `F6.2`, `F8.3`):**
   - `F2.2` hiển thị cột *Giá vốn (VNĐ)*, nút *Thiết lập bảng giá*, *In mã vạch*, *Định mức tồn tối thiểu*.
   - `F5.1` tính *VAT 8%*, *Chiết khấu thương mại*, *Số hóa đơn GTGT*, *Lô SX*, *Hạn dùng*, *SL chứng từ vs SL thực nhận*, *Hạn mức công nợ*.
   - `F6.2` hiển thị *Đơn giá vốn* và *Giá trị lệch (-752.000 đ)*; `F8.3` có cột *Thành tiền* và *Giá trị tồn*.
   - **Mâu thuẫn:** Trong `Bao-cao-ERD-POS.docx`, bảng `MatHang` chỉ lưu `giaBanHienHanh` (không có `giaVon`, `loSX`, `hanDung`, `dinhMucTon`), `HoaDon`/`PhieuNhap` không có thuế VAT/chiết khấu, và phần báo cáo `F8.3` chốt chỉ tính **số lượng xuất–nhập–tồn** từ `BienDongKho` (không tính giá vốn bình quân gia quyền hay lãi gộp P&L).
4. **Danh mục và hồ sơ mở rộng vượt cấu trúc bảng (`F1.1`, `F1.2`, `F2.1`, `F2.3`):**
   - `F1.1` đưa thêm *Thẻ từ RFID*, *Mã PIN 6 số*, *Quét QR nhân viên*, *Két tiền tự động*, *Chọn ca làm việc trước khi đăng nhập*.
   - `F2.1` dựng cây **Nhóm hàng 2 cấp (Nhóm chính → Nhóm con)** và *Thứ tự POS*, trong khi `NhomHang` trong ERD là bảng phẳng `(maNhom, tenNhom, trangThai)` không có `maNhomCha`.
   - `F2.3` có form nhập *MST*, *Email*, *Địa chỉ chi tiết*, *Số tài khoản ngân hàng*, *Hạn mức công nợ*, trong khi `NhaCungCap` trong ERD chỉ có `(maNhaCungCap, tenNhaCungCap, thongTinLienHe)`.
5. **Chức năng phát sinh ngoài BFD 31 chức năng (`F5.3–F5.4`, `F7.2–F7.3`, `F8.1`):**
   - `F5.3–F5.4`: có nút `Tạo phiếu trả hàng NCC` (không có trong BFD/ERD).
   - `F7.2–F7.3`: có thẻ `Tài khoản Ngân hàng (42.850.000 đ)` và nút `Đối chiếu sổ phụ ngân hàng` (không có thực thể tài khoản ngân hàng trong ERD).
   - `F8.1`: có nút `Gửi email quản lý`.

---

### Nhóm II — Mâu thuẫn trực tiếp với Quy tắc nghiệp vụ (`QT01–QT12`) & RBAC (`QĐ01–QĐ06`)

6. **`F1.3 · Cấu hình phân quyền` sai mô hình RBAC ở 4 điểm cốt lõi:**
   - Hiển thị **4 vai trò** (thêm `Kế toán quỹ`) và cho `Tạo vai trò tùy chỉnh` — trong khi nhóm chốt **3 vai trò** (*Thu ngân, Nhân viên kho, Quản lý*).
   - Cột ma trận là `Xem / Thêm / Sửa / Xóa / Đặc quyền` kèm `2FA`, `Hạn mức hoàn trả`, `CK tối đa 100%`, `Xem P&L`, **`Đóng ca cưỡng bức`** — mâu thuẫn trực tiếp với `QĐ02` (**cấm Quản lý đóng ca thay**) và không phản ánh đúng bảng `QuyenVaiTro` (40 mã quyền cụ thể gắn `F1.2–F8.5`).
   - Còn tích quyền cho `F1.1 Đăng nhập` — trong khi mục 3.1 tài liệu RBAC đã chốt `F1.1` là bước **Xác thực** diễn ra **trước khi nạp quyền**.
7. **`F4.1–F4.2 · Lập phiếu trả & Hoàn tiền` sai cấu trúc dữ liệu `DongTra` và `KhoanHoan` (`QT04`, `QĐ04`, `QĐ05`):**
   - UI hiện tại chỉ có 1 dropdown tình trạng + 1 checkbox `Hoàn kho` cho cả dòng hàng → **không thể nhập được trường hợp khách trả 3 hộp nhưng chỉ 2 hộp đủ điều kiện nhập lại kho** (`QĐ05`, `0 ≤ soLuongNhapLai ≤ soLuongTra`).
   - Phần hoàn tiền dùng **radio chọn duy nhất 1 phương thức**, không cho phân bổ nhiều dòng `KhoanHoan` (VD: hoàn 1 phần tiền mặt + 1 phần chuyển khoản như ERD `PhieuTra 1 — 0..n KhoanHoan`).
   - Thiếu nút **Xóa phiếu trả nháp** (`QĐ04`: Thu ngân được xóa `PhieuTra` còn `Nhap` của mình trong ca mở).
8. **`F7.1 / F7.4 / F7.5 · Quản lý ca & Bàn giao két` lệch công thức `QT06` và `QĐ02`:**
   - Thẻ `Dự kiến trong két` chỉ liệt kê `Đầu ca + Thu bán tiền mặt − Hoàn tiền mặt`, **thiếu 3 thành phần bắt buộc trong `QT06`**: `Thu riêng tiền mặt`, `Chi riêng tiền mặt` (`PhieuThuChi`), `Chi trả NCC từ két` (`TraNhaCungCap`), và `Đảo tiền mặt hủy hóa đơn`.
   - Phần chốt ca bắt buộc chọn `Người nhận bàn giao (Ca chiều tiếp theo)` và nhập `Mã PIN xác thực bàn giao` — trong khi ERD `CaBan` chỉ lưu `maNguoiMo` và `maNguoiChot` (chính người mở ca tự chốt theo `QĐ02`).
9. **Hiển thị sai quyền vai trò trên UI & Lỗi logic dữ liệu mẫu (`QT07`, `QT11`, `QĐ01–QĐ03`):**
   - `F6.2–F6.4`: Tài khoản đang đăng nhập là `Trần Văn Kho — Thủ kho / Kiểm kho`, nhưng lại hiển thị sáng nút `Xác nhận cân chỉnh kho & Chốt kiểm kê (F9)` và ô `Ý kiến chỉ đạo của Quản lý`. Theo `QT11` & `QĐ02`, Thủ kho chỉ có nút **Gửi duyệt (`DangKiem → ChoDuyet`)**, còn chỉ **Quản lý** mới thấy nút **Duyệt điều chỉnh tồn (`ChoDuyet → HoanTat`)**.
   - `F5.3–F5.4`: Tài khoản đang đăng nhập là `Trần Văn Kho`, nhưng vẫn hiển thị nút `Trả hết nợ`, `Xác nhận thanh toán`, `Hủy phiếu` đã xác nhận — theo `QĐ01`, cả ghi trả NCC (`F5.3`) lẫn hủy phiếu nhập đã xác nhận (`F5.4`) **chỉ cấp cho Quản lý**.
   - `F8.1–F8.5`: Tài khoản đang xem các báo cáo quản trị lại là `Lê Thị Mai — Thu ngân / Quản lý ca` — sai với ma trận RBAC (Thu ngân không có quyền `F8.1–F8.5`).
   - **Lỗi tính toán mẫu trên `F6.2`:** 4 dòng kiểm gồm 1 dòng khớp (`0`), 2 dòng thiếu (`-2`, `-15`), 1 dòng thừa (`+1`), nhưng thẻ KPI trên cùng lại ghi `Khớp 100%: 2 SKU` (`2 + 2 + 1 = 5 ≠ 4`).

---

### Nhóm III — Vấn đề kiến trúc UI & Chuẩn trình bày theo Checklist MI4090

10. **Chưa thống nhất 1 Main Shell duy nhất (Checklist mục `2.1`):**
    - 15 màn hình (`F1.2–F2.3`, `F5.1–F8.5`) dùng **Sidebar trái 8 nhóm** (`<aside>`), trong khi `F3.1–F3.4` và `F4.1–F4.3` chuyển sang **Top-bar ngang riêng biệt**. Khi bảo vệ, mục `2.1` kiểm tra tính hợp nhất 1 Main Shell giữa 2 phân hệ TV1 & TV2; nếu TV1 (Quầy) dùng Top-bar còn TV2 (Kho) dùng Sidebar sẽ rất dễ bị đánh giá *"giống 2 app ghép lại"*.
11. **Lỗi định dạng ngày & nhất quán phương thức thanh toán (Checklist mục `2.1`):**
    - Trên `F5.1`, ô `Ngày ghi nhận chứng từ` hiển thị `05/15/2025` (`MM/dd/yyyy`) — **vi phạm mục `2.1` yêu cầu chuẩn ngày `dd/MM/yyyy`**.
    - Enum phương thức thanh toán trên UI đang liệt kê không đồng nhất giữa các màn (`Tiền mặt`, `Chuyển khoản VietQR`, `Thẻ POS`, `Visa`, `Ví điện tử`, `Voucher`). Cần chốt cố định 2 phương thức chuẩn trong tài liệu (`TienMat`, `ChuyenKhoan`) hoặc chốt rõ 3 phương thức (`TienMat`, `ChuyenKhoan`, `ThePos` — đều không cộng vào két tiền mặt) dùng đồng nhất trên `F3`, `F4`, `F5`, `F7`, `F8`.
12. **Tổ chức mã nguồn UI đang là HTML tĩnh rời rạc:**
    - Cả 2 file zip đều xuất trùng thư mục gốc `stitch_thi_t_k_form_ch_c_n_ng/`, mỗi màn hình là 1 file `code.html` độc lập lặp lại toàn bộ Sidebar, Header, CSS CDN Tailwind và dữ liệu cứng. Khi ghép vào dự án C# thật cần tách thành **Layout chung (1 Main Shell)** + các View/Component tái sử dụng (DRY — mục `2.4`).

---

## 5. Đề xuất cách sửa và Kỳ vọng mong muốn

### 5.1. Lộ trình sửa theo mức độ ưu tiên

| Mức độ | Hạng mục cần sửa | Kỳ vọng đầu ra (Definition of Done) |
|---|---|---|
| **P0 — Bắt buộc (trước khi thiết kế API & Code)** | **1. Gọt sạch toàn bộ trường ngoài ERD 21 bảng:** Xóa chọn Chi nhánh, Kho đệm, Quầy 01/02, Khách hàng VIP/Voucher, VAT 8%, Giá vốn/Lãi gộp/P&L, Lô SX/HSD, RFID/PIN/2FA, nút Trả hàng NCC, Sổ phụ ngân hàng, Gửi email, Thiết lập bảng giá, In mã vạch. | **100% cột trên mọi bảng và ô nhập trên mọi form ánh xạ 1–1 về đúng cột của 21 bảng ERD** (hoặc giá trị tính dẫn xuất hợp lệ như `Thành tiền = SL × Đơn giá`, `Lệch = Thực đếm − Tồn sổ`). |
| **P0 — Bắt buộc** | **2. Sửa 3 màn hình lệch logic nặng (`F1.3`, `F4.1–F4.2`, `F6.2–F6.4`):**<br>• `F1.3`: Đúng 3 vai trò, gán 1 vai trò/tài khoản, tích chọn đúng 40 mã quyền (`F1.2–F8.5`), bỏ `F1.1` khỏi bảng quyền, bỏ `Đóng ca cưỡng bức`.<br>• `F4.1–F4.2`: Thay checkbox `Hoàn kho` bằng ô số `SL nhập lại (0..SL trả)`; cho thêm nhiều dòng `KhoanHoan`; thêm nút `Xóa phiếu nháp`.<br>• `F6.2–F6.4`: Tách rõ nút `Gửi duyệt` (NV kho ở `DangKiem`) vs `Duyệt & Điều chỉnh tồn` (Quản lý ở `ChoDuyet`); bỏ cột Đơn giá vốn/Giá trị lệch; sửa số liệu mẫu khớp 100%. | Khớp 100% với `QT04`, `QT07`, `QT11` và 6 quyết định `QĐ01–QĐ06`; mỗi màn hình phản ánh đúng vai trò thực tế được cấp quyền. |
| **P0 — Bắt buộc** | **3. Cập nhật công thức két trên `F7.1/F7.4` & quyền trên `F5.3–F5.4`, `F8.1–F8.5`:**<br>• Hiển thị đủ 6 dòng cộng/trừ tiền mặt trong `QT06` (Đầu ca, Thu bán mặt, Hoàn mặt, Thu/Chi riêng mặt, Chi NCC từ két, Đảo hủy mặt).<br>• Bỏ ô `Người nhận bàn giao` & `Mã PIN` trên `F7.4`.<br>• Chỉ hiển thị nút Trả tiền NCC / Hủy phiếu đã xác nhận / Báo cáo `F8` cho đúng vai trò theo ma trận RBAC. | Công thức hiển thị trên `F7.4` khớp 100% `QT06` và bảng đối chiếu tính tay của báo cáo `F8.4`. |
| **P1 — Nên sửa (để đạt trọn điểm `2.1` & `2.4`)** | **4. Thống nhất 1 Main Shell duy nhất cho cả 18 màn hình:** Giữ Sidebar trái 8 nhóm (`F1–F8`) dùng chung làm khung chính; riêng màn bán hàng `F3.1–F3.3` có thể cho nút thu gọn Sidebar (collapse) để rộng không gian bán hàng nhưng vẫn nằm trong cùng 1 Shell. | TV1 (Quầy) và TV2 (Kho) nhìn đồng bộ như 1 tác giả duy nhất; menu tự ẩn/hiện theo `QuyenVaiTro` của phiên đăng nhập; 100% ngày định dạng `dd/MM/yyyy`, tiền `#,##0 đ`. |
| **P2 — Giữ lại làm điểm cộng UX** | **5. Giữ lại các thiết kế rất hay không làm phình DB:**<br>• Bảng đếm theo mệnh giá tờ tiền (`500k, 200k, ..., 1k`) trên `F7.4` → chỉ dùng tính tổng `tienThucDem` trên client, lưu duy nhất `tienThucDem` xuống `CaBan`.<br>• Bảng đối chiếu `Đã mua / Đã trả trước / Còn được trả` trên `F4.1`.<br>• Thẻ lọc `Tất cả / Có chênh lệch / Khớp 100% / Chưa đếm` trên `F6.2`.<br>• Biểu đồ xu hướng trên `F8.1`, `F8.4` (đạt yêu cầu ≥1 biểu đồ của mục `1.2`). | Giao diện vừa giữ nguyên độ đẹp & chuyên nghiệp của Stitch, vừa nhẹ và sạch về CSDL. |

---

## 6. Thông tin bổ sung cần chốt trước khi bước sang Thiết kế Kiến trúc & DB

### 6.1. Chốt hướng công nghệ triển khai C# (liên quan trực tiếp cách dùng 2 file zip)
- **Phương án 1 (Khuyến nghị mạnh — tận dụng trực tiếp bộ HTML/CSS của TV2): Web POS với ASP.NET Core (C#)**
  - Tách 1 `Layout` chung (`_Layout.cshtml` hoặc SPA Shell) từ Design System `Retail Core POS` (`DESIGN.md`), dùng **ASP.NET Core MVC / Razor Pages** hoặc **ASP.NET Core Web API + HTML/JS thuần** gọi xuống tầng Service C#.
  - Tổ chức Solution C# chuẩn Clean/3-Layer (**0 phụ thuộc vòng**, đáp ứng `mục 2.4`, `4.3`, `4.5`):
    ```text
    RetailPos.sln
    ├── Database/                 # Script SQL 21 bảng 3NF + Script Seed ≥120 dòng + Bảng tính tay đối chiếu
    ├── Resource/                 # Biểu mẫu in, tài liệu phân tích, DESIGN.md, ảnh màn hình
    ├── src/
    │   ├── Pos.Contracts/        # Shared Interfaces & DTOs: IInventoryService, IShiftCashService,
    │   │                         # ISupplierPaymentQueryService, ISaleReportQueryService, IAuthorizationService
    │   ├── Pos.Counter/          # Module TV1 (Quầy): Domain + Services + Repositories (F3, F4, F7)
    │   ├── Pos.Inventory/        # Module TV2 (Kho & Hệ thống): Domain + Services + Repositories (F1, F2, F5, F6, F8)
    │   └── Pos.Web/              # Main Shell + Views/Controllers mỏng + Composition Root (DI) + appsettings.json
    └── tests/
        └── Pos.Tests/            # xUnit: ≥18 Unit Tests AAA (Mock chéo TV1↔TV2) + ≥8 Integration Tests (Seed ≥120)
    ```
- **Phương án 2 (Nếu làm C# WinForms theo slide cũ):** Giữ 19 ảnh `screen.png` (sau khi sửa theo mục 5) làm bản thiết kế giao diện (Wireframe) trong báo cáo, rồi thay `Pos.Web` bằng `Pos.WinForms` (`frm000_dang_nhap`, `frm_dm_*`, `frm_nv_*`) gọi đúng 3 project lõi `Pos.Contracts`, `Pos.Counter`, `Pos.Inventory`.

### 6.2. Chốt sớm 2 điểm nhỏ ảnh hưởng đến ERD & mẫu GoF (`mục 4.4–4.6`)
1. **Enum `phuongThuc` thanh toán & hoàn tiền:** Chốt rõ dùng **2 giá trị** (`TienMat`, `ChuyenKhoan`) hay **3 giá trị** (`TienMat`, `ChuyenKhoan`, `ThePos` — trong đó chỉ `TienMat` cộng/trừ két) để thống nhất từ DB `CHECK`, Domain Enum đến các nút trên `F3`, `F4`, `F5`, `F7`, `F8`.
2. **Gợi ý gắn ≥ 3 mẫu GoF + Live OCP mà KHÔNG cần sửa ERD 21 bảng:**
   - **Mẫu 1 — State Pattern *(Behavioral — dùng chung TV1 & TV2)*:** Quản lý vòng đời & State Guard cho `HoaDon` (`Nhap → DaXacNhan → DaHuy`), `PhieuNhap`, `PhieuKiem` (`DangKiem → ChoDuyet → HoanTat / DaHuy`). Chặn chuyển trạng thái cấm ngay tại Domain (`mục 4.2`).
   - **Mẫu 2 — Strategy Pattern *(Behavioral — TV1 hoặc dùng chung xuất báo cáo)*:**
     - *Gợi ý cho TV2 (Xuất báo cáo `F8.1–F8.5`):* `IReportExporter` (`ExcelReportExporter`, `PdfReportExporter`) → khi bảo vệ **Live OCP** tắt AI, TV2 thêm ngay `CsvReportExporter` hoặc `HtmlReportExporter` trong 2 phút mà **0 sửa code cũ**!
     - *Gợi ý cho TV1 (Tính chiết khấu / Chính sách tính tiền `F3.1` hoặc Bộ tính két/thanh toán):* Nếu TV1 muốn làm Strategy cho **Chính sách giảm giá (`IPricingStrategy`: Giá niêm yết / Giảm % theo đơn / Giảm cố định)** để Live OCP thêm *"Giảm 15% ngày lễ"*, chỉ cần chốt thêm **1 cột duy nhất** `tienGiamGia` (mặc định `0`) vào `HoaDon` trước khi viết SQL; nếu không sửa ERD thì TV1 có thể dùng **Strategy cho kiểm tra & phân bổ phương thức thanh toán (`IPaymentMethodHandler`: `CashPaymentHandler`, `BankTransferPaymentHandler`)** → Live OCP thêm `EWalletPaymentHandler` không sửa `SaleService`!
   - **Mẫu 3 — Observer / Domain Event hoặc Facade *(Cầu nối TV1 ↔ TV2 — nhóm Behavioral/Structural)*:** Ví dụ **Facade** (`ICheckoutFacade` / `IDailyReportFacade`) hoặc **Observer** (phát sự kiện khi xác nhận bán/trả/hủy/nhập để ghi nhận cập nhật hoặc nhật ký đối soát) + **Factory Method** *(Creational — đủ cả 3 nhóm GoF!)* khởi tạo bộ chứng từ / bộ xuất báo cáo.
   - **Cross-Module LSP (`mục 4.4`):** Xây dựng lớp cha trừu tượng chung trong `Pos.Contracts` (VD: `ChungTuKhoBase` hoặc `GiaoDichDongTienBase` — nơi lớp con của TV2 như `TraNhaCungCapCashEntry` chạy mượt trong hàm tổng hợp sổ quỹ nhận kiểu cha của TV1 mà **không dùng `is`/`as`/ép kiểu**!).

### 6.3. Checklist tự kiểm tra trước khi chốt bộ UI đã gọt
- [ ] Không còn chữ *Chi nhánh*, *Kho đệm*, *Quầy 01/02*, *VIP*, *Voucher*, *VAT*, *Giá vốn*, *Lãi gộp*, *Lô SX*, *RFID*, *2FA*, *Kế toán quỹ*, *Trả hàng NCC* trên bất kỳ màn hình nào.
- [ ] `F1.3` chỉ còn đúng 3 vai trò và 40 mã quyền khớp bảng `QuyenVaiTro`.
- [ ] `F4.1–F4.2` nhập được `soLuongNhapLai` tách khỏi `soLuongTra`, nhiều dòng `KhoanHoan`, có nút xóa phiếu `Nhap`.
- [ ] `F6.2–F6.4` tách đúng quyền NV kho (`Gửi duyệt`) vs Quản lý (`Duyệt hoàn tất`).
- [ ] `F7.4` hiển thị đủ công thức két theo `QT06` và bỏ `Người nhận bàn giao`.
- [ ] Toàn bộ 18 màn hình nghiệp vụ dùng chung 1 Main Shell, ngày `dd/MM/yyyy`, tiền `#,##0 đ`.
