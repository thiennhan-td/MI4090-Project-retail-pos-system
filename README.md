# Hệ thống Quản lý Cửa hàng Bán lẻ POS (MI4090)

Bài tập lớn học phần **Lập trình hướng đối tượng (MI4090)** — Nhóm 02 sinh viên.

## 1. Phạm vi hệ thống
- Hệ thống POS cho **01 cửa hàng bán lẻ** gồm quầy thu ngân và kho hàng.
- **03 vai trò RBAC:** `Thu ngân`, `Nhân viên kho`, `Quản lý` (truy quyền qua `TaiKhoan → VaiTro → QuyenVaiTro → Quyen`).
- **Phạm vi loại trừ:** Không quản lý khách hàng thành viên, không bán đa kênh ngoài cửa hàng.
- **Giảng viên xác nhận miễn:** DFD và đặc tả Use Case.

## 2. Phân công dọc 02 phân hệ (TV1 ↔ TV2)
| Thành viên | Phân hệ phụ trách | Nhánh BFD chính | Thực thể ERD chính |
|---|---|---|---|
| **TV1** | **Quầy** *(Bán hàng, Trả hàng, Ca & Sổ quỹ)* | `F3`, `F4`, `F7`, `F8.2`, `F8.4`, phối hợp `F8.1` | `HoaDon`, `DongBan`, `ThanhToanBan`, `PhieuTra`, `DongTra`, `KhoanHoan`, `CaBan`, `PhieuThuChi` |
| **TV2** | **Kho & Hệ thống** *(Quản trị, Danh mục, Nhập, Kiểm kho, Thẻ kho)* | `F1`, `F2`, `F5`, `F6`, `F8.3`, `F8.5`, chủ trì `F8.1` | `TaiKhoan`, `VaiTro`, `QuyenVaiTro`, `Quyen`, `NhomHang`, `MatHang`, `NhaCungCap`, `PhieuNhap`, `DongNhap`, `TraNhaCungCap`, `PhieuKiem`, `DongKiem`, `BienDongKho` |

## 3. Danh mục tài liệu hiện có

### Khảo sát hiện trạng & Checklist môn học
- `Khảo sát KiotViet/Khao_sat_he_thong_ban_le_KiotViet.md` — Báo cáo khảo sát thực tế theo 4W+1H, 4 nhóm đầu ra và phạm vi đề xuất.
- `Khảo sát KiotViet/KiotViet_Bieu_mau_Giao_dien_POS.zip` — Phụ lục minh chứng biểu mẫu và giao diện POS.
- `Khảo sát KiotViet/KiotViet_Bao_cao_thong_ke.zip` — Phụ lục minh chứng báo cáo thống kê.
- `Checklist_BTL_HTTTQL_v2026.xlsx` & `Checklist_BTL_HTTTQL_2026.md` — Phiếu đánh giá và tóm tắt tiêu chí MI4090.

### Phân tích hệ thống (Đã chốt 100%)
- `BFD_POS_TinhChinh.png` / `BFD_POS_TinhChinh.html` / `Khảo sát KiotViet/BFD-POS-31.png` — Sơ đồ phân cấp chức năng bản chốt (**8 nhóm nghiệp vụ · 31 chức năng**).
- `Khảo sát KiotViet/Dac-ta-chuc-nang-POS-31.docx` — Đặc tả chi tiết 31 chức năng (`F1.1–F8.5`), đã đồng bộ `QĐ01–QĐ06`.
- `Bao-cao-ERD-POS.docx`, `ERD-POS-21.png`, `ERD-POS-21-phan-tang.png` — Mô hình ERD **21 thực thể 3NF**, sổ thẻ kho bất biến `BienDongKho` và vòng đời trạng thái 5 chứng từ/ca.
- `Quy-tac-nghiep-vu-va-RBAC-POS-da-chot.docx` — **12 quy tắc nghiệp vụ** (`QT01–QT12`: 6 TV1, 6 TV2, 6 liên phân hệ), **Ma trận RBAC** (3 vai trò × 31/31 chức năng = 41 hàng thao tác) và **5 kịch bản kiểm chứng dự kiến**.
- `Bao_cao_tong_ket_phien_lam_viec.md` — Tổng kết tiến độ và danh mục đầu việc còn lại cho giai đoạn Thiết kế, Cài đặt C# & Kiểm thử.

## 4. Lộ trình tiếp theo
1. **Thiết kế kiến trúc tổng thể (C#):** Chốt Tech Stack, cấu trúc Solution, Package Diagram ≥3 tầng, Hợp đồng Interface Quầy ↔ Kho và ≥3 mẫu GoF (có điểm mở rộng Live OCP).
2. **Thiết kế chi tiết:** UML Class Diagram tổng (≥20 lớp), ≥4 Sequence/State, Script SQL 21 bảng + Seed ≥120 bản ghi + Bảng tính tay đối chiếu 5 báo cáo, Wireframe & Biểu mẫu.
3. **Cài đặt & Kiểm thử:** Triển khai C#, đạt ≥26 tests (≥18 Unit Test Mock chéo + ≥8 Integration Test) và đóng gói Clean-Room.
