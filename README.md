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

## 3. Cấu trúc thư mục tài liệu (`docs/`)

```text
docs/
├── references/                          # Tài liệu tham khảo & Checklist môn học
│   ├── Checklist_BTL_HTTTQL_v2026.xlsx  # Phiếu đánh giá gốc của giảng viên (MI4090)
│   └── Checklist_BTL_HTTTQL_2026.md     # Bản tóm tắt tiêu chí checklist
├── survey/                              # Hồ sơ khảo sát hiện trạng (KiotViet)
│   ├── Khao_sat_he_thong_ban_le_KiotViet.md
│   ├── BFD-POS-KiotViet.html
│   ├── KiotViet_Bieu_mau_Giao_dien_POS.zip
│   └── KiotViet_Bao_cao_thong_ke.zip
├── analysis/                            # Hồ sơ phân tích hệ thống (Đã chốt 100%)
│   ├── BFD_POS_TinhChinh.png            # Sơ đồ BFD bản chốt (8 nhóm / 31 chức năng)
│   ├── BFD_POS_TinhChinh.html
│   ├── BFD-POS-31.png
│   ├── Dac-ta-chuc-nang-POS-31.docx     # Đặc tả 31 chức năng đồng bộ QĐ01–QĐ06
│   ├── Bao-cao-ERD-POS.docx             # Báo cáo ERD 21 thực thể, thẻ kho & vòng đời
│   ├── ERD-POS-21.png
│   ├── ERD-POS-21-phan-tang.png
│   └── Quy-tac-nghiep-vu-va-RBAC-POS-da-chot.docx  # 12 QT, Ma trận RBAC 41 hàng, 5 kịch bản test dự kiến
└── Bao_cao_tong_ket_phien_lam_viec.md   # Tổng kết tiến độ & danh mục đầu việc tiếp theo
```

## 4. Lộ trình tiếp theo
1. **Thiết kế kiến trúc tổng thể (C#):** Chốt Tech Stack, cấu trúc Solution (`src/`, `tests/`, `Database/`, `Resource/`), Package Diagram ≥3 tầng, Hợp đồng Interface Quầy ↔ Kho và ≥3 mẫu GoF (có điểm mở rộng Live OCP).
2. **Thiết kế chi tiết:** UML Class Diagram tổng (≥20 lớp), ≥4 Sequence/State, Script SQL 21 bảng + Seed ≥120 bản ghi + Bảng tính tay đối chiếu 5 báo cáo, Wireframe & Biểu mẫu.
3. **Cài đặt & Kiểm thử:** Triển khai C#, đạt ≥26 tests (≥18 Unit Test Mock chéo + ≥8 Integration Test) và đóng gói Clean-Room.
