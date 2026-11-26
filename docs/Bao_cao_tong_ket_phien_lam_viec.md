# Báo cáo tổng kết phiên làm việc — Bài tập lớn POS bán lẻ (MI4090)

**Ngày tổng kết:** 27/11/2026  
**Học phần:** Lập trình hướng đối tượng (MI4090) · Nhóm 02 sinh viên  
**Đề tài:** Hệ thống quản lý cửa hàng bán lẻ POS (phạm vi 1 cửa hàng, không quản lý khách hàng thành viên và kênh bán ngoài)  
**Định hướng bố cục báo cáo đã thống nhất:**  
1. Khảo sát hiện trạng → 2. Phân tích hệ thống *(BFD, ERD, Quy tắc nghiệp vụ & RBAC — giảng viên đã miễn DFD và đặc tả UC)* → 3. Thiết kế kiến trúc tổng thể → 4. Thiết kế chi tiết *(UML, Sequence/State, CSDL vật lý, API/Màn hình)* → 5. Cài đặt & Kiểm thử (C#).

---

## I. Các phần việc đã hoàn thiện trong phiên làm việc

### 1. Rà soát chuẩn chấm & tài liệu hướng dẫn môn học
- Đọc và đối chiếu đầy đủ **3 tài liệu chuẩn**:
  - `Checklist_BTL_HTTTQL_v2026.xlsx` (sheet **Nhóm 02 người** và **Rubric A-F & Vấn đáp**, nắm rõ 2 cổng chặn `G_Product`, `G_Epistemic`, 5 khóa chặn chống đối phó và hệ số cá nhân TV1/TV2).
  - `Checklist_BTL_HTTTQL_2026.md` (bản tóm tắt của nhóm).
  - `HuongdanXDHTTTQLv1_1 (1).pdf` (GV. Trần Ngọc Thăng — quy trình 5 bước, 4 nhóm output khảo sát, chuẩn BFD 3 nhánh chung + nhánh riêng, Master–Detail danh mục vs Form nghiệp vụ, Solution/Naming Convention).
- Chốt phân công dọc hai phân hệ:
  - **TV1 (Phân hệ Quầy):** Bán hàng (`F3`), Nhận trả & hoàn tiền (`F4`), Quản lý ca & sổ quỹ (`F7`), báo cáo bán/ca (`F8.2`, `F8.4`) và phối hợp `F8.1`.
  - **TV2 (Phân hệ Kho & Hệ thống):** Quản trị & RBAC (`F1`), Danh mục (`F2`), Nhập hàng & trả NCC (`F5`), Kiểm soát tồn kho & Thẻ kho (`F6`, `BienDongKho`), báo cáo kho/NCC (`F8.3`, `F8.5`) và tổng hợp `F8.1` (phối hợp TV1).

### 2. Hoàn tất Phần 1 — Khảo sát hiện trạng
- Đã có sẵn và đối chiếu nhất quán bộ hồ sơ khảo sát KiotViet tại `Khảo sát KiotViet/Khao_sat_he_thong_ban_le_KiotViet.md` (đủ 4W+1H, 4 nhóm đầu ra: Biểu mẫu, Báo cáo, Dữ liệu, Quy trình, ví dụ đối soát ca) cùng 2 bộ phụ lục ảnh (`KiotViet_Bieu_mau_Giao_dien_POS.zip`, `KiotViet_Bao_cao_thong_ke.zip`).

### 3. Hoàn tất 100% Phần 2 — Phân tích hệ thống
- **Biểu đồ phân cấp chức năng (BFD) bản chốt — `8 nhóm nghiệp vụ / 31 chức năng cấp 3`:**
  - Tinh gọn từ bản 39 chức năng: gộp tạo/cập nhật tài khoản (`F1.2`), gộp giá bán vào mặt hàng (`F2.2`), bỏ Khách hàng thành viên, khôi phục nhánh `F4 · Nhận trả và hoàn tiền` độc lập, bổ sung Hủy hóa đơn (`F3.4`) và Hủy phiếu nhập (`F5.4`), cắt gọn báo cáo về đúng **5 báo cáo cốt lõi** (`F8.1–F8.5`, bỏ Kênh bán hàng, Khách hàng & công nợ, Kết quả tài chính).
  - Sản phẩm đã xuất: `BFD_POS_TinhChinh.html`, `BFD_POS_TinhChinh.png` (2192×2075 px) và `Khảo sát KiotViet/BFD-POS-31.png`.
- **Đặc tả 31 chức năng bản chốt (`Khảo sát KiotViet/Dac-ta-chuc-nang-POS-31.docx`):**
  - Đồng bộ 100% mã và tên `F1.1–F8.5` theo BFD mới, cập nhật toàn bộ `QĐ01–QĐ06` (`F1.2`, `F1.3`, `F3.4`, `F4.1–F4.3`, `F5`, `F5.3–F5.4`, `F7.2–F7.5`).
  - Đã sửa xong 2 lỗi câu chữ nhỏ cuối phiên (`F7.2` bỏ từ lặp `"chọn chọn"` và `F5.4` đổi `"quy tắc đề xuất"` → `"quy tắc đã chốt"`).
- **Mô hình ERD 21 thực thể bản chốt (`Bao-cao-ERD-POS.docx`):**
  - Tinh gọn từ 25 thực thể về **21 thực thể 3NF** (tỷ lệ bảng danh mục đơn ~19% ≤ 40%, vượt ngưỡng ≥11 bảng): gộp hồ sơ nhân viên vào `TaiKhoan`, bỏ `QuyenTaiKhoan`, gộp thông tin hủy/bàn giao vào chứng từ/ca cha.
  - Bổ sung sổ thẻ kho bất biến **`BienDongKho`** do Kho (TV2) sở hữu, Quầy (TV1) giao tiếp qua hợp đồng `IInventoryService` (kèm `khoaSuKien` chống ghi trùng idempotency và khóa dòng `MatHang` theo thứ tự mã hàng chống bán đồng thời).
  - Bổ sung cột **`trangThai`** tường minh cho 5 đối tượng (`HoaDon`, `PhieuNhap`, `PhieuTra`, `PhieuKiem`, `CaBan`) và `hanThanhToan?` trên `PhieuNhap` phục vụ dữ liệu mẫu trạng thái quá hạn.
- **Bảng Quy tắc nghiệp vụ & Ma trận phân quyền RBAC bản chốt (`Quy-tac-nghiep-vu-va-RBAC-POS-da-chot.docx`):**
  - **12 quy tắc nghiệp vụ (`QT01–QT12`):** 6 quy tắc TV1 chủ trì (`QT01–QT06`), 6 quy tắc TV2 chủ trì (`QT07–QT12`), **6 quy tắc liên phân hệ** (`QT02, QT03, QT04, QT06, QT09, QT12`) — vượt định mức mục `3.3` (≥8 QT, ≥4/TV, ≥2 liên phân hệ).
  - **Ma trận RBAC 3 vai trò (*Thu ngân, Nhân viên kho, Quản lý*):** Phủ **31/31 chức năng BFD**, tách thành **41 hàng thao tác** (40 mã quyền cho `QuyenVaiTro` + bước xác thực `F1.1`), tách rõ quyền người dùng khỏi lời gọi nội bộ (`IInventoryService`, `IShiftCashService`).
  - **5 tình huống kiểm chứng dự kiến (`TC-IT-01` → `TC-IT-05`):** Phủ đủ giao dịch hợp lệ Quầy↔Kho, đủ quyền nhưng sai trạng thái/luật, thiếu quyền gọi trực tiếp Service bỏ qua UI, lỗi giữa giao dịch rollback sạch, và gửi lại yêu cầu idempotency. *(Đều ở trạng thái thiết kế dự kiến, chưa chạy).*
  - **Đã chốt dứt điểm 6 quyết định (`QĐ01–QĐ06`):** Không còn điểm mở/đề xuất `ĐX` nào ở giai đoạn Phân tích.
- **Dọn dẹp thư mục:** Đã xóa thư mục xuất tạm `.pos-edit-1795711839697/`.

---

## II. Những phần còn đang dở / chưa triển khai

### 1. Việc vặt cần dọn tay (1 phút)
- [ ] **Đóng Word và xóa bản dự thảo cũ `Quy-tac-nghiep-vu-va-RBAC-POS.docx`:** File này đang bị khóa mở bởi Microsoft Word nên chưa tự xóa được; sau khi đóng Word, xóa bản dự thảo cũ và chỉ giữ lại bản chốt `Quy-tac-nghiep-vu-va-RBAC-POS-da-chot.docx` (hoặc đổi tên lại nếu muốn).
- [ ] *(Tùy chọn thẩm mỹ)* Trong hình ERD tổng quan của `Bao-cao-ERD-POS.docx`, đường nối `PhieuKiem — DongKiem` (góc dưới trái) thiếu dấu chấm tròn chỉ FK phía `DongKiem` (hình cụm 4 đã có đủ).

### 2. Phần 3 — Thiết kế kiến trúc tổng thể C# *(Bước tiếp theo ngay)*
- [ ] **Chốt Tech Stack cụ thể:** Chọn giao diện C# (**WinForms** theo slide Thầy Thăng hay **Web ASP.NET Core**) và hệ quản trị CSDL (**SQL Server** hay **PostgreSQL** hỗ trợ `CHECK`, partial unique index cho 1 ca mở/thu ngân và row-lock `MatHang`).
- [ ] **Thiết kế cấu trúc Solution & Coding Convention (`mục 2.4`, Slide 24–25):** Quy định thư mục chuẩn (`Database/`, `Resource/`, các project theo tầng/module) và quy ước đặt tên thống nhất giữa TV1 & TV2.
- [ ] **Vẽ Package Diagram ≥ 3 tầng (`mục 4.3`):** Phân tách `Presentation/UI` → `Application/Service` → `Domain` ← `Infrastructure/Data`, tách sạch `Module Quầy (TV1)`, `Module Kho & Hệ thống (TV2)` và `Shared Contracts/Kernel` (0 phụ thuộc vòng, 0 SQL/UI trong lõi).
- [ ] **Chốt bộ Hợp đồng Interface/Event TV1 ↔ TV2 (`mục 4.5`):** Khai báo chữ ký chính thức cho `IInventoryService`, `IShiftCashService`, `ISupplierPaymentQueryService`, `ISaleReportQueryService`, `IAuthorizationService`... và cơ chế Constructor Injection + Composition Root.
- [ ] **Chốt ≥ 3 mẫu GoF (`mục 4.6`) & ≥ 2 nhánh Cross-Module LSP (`mục 4.4`):** Chọn ≥3 mẫu GoF thuộc ≥2–3 nhóm (mỗi TV ≥1 mẫu + ≥1 mẫu cầu nối TV1↔TV2, không tính Repository/MVC), thiết kế sẵn điểm mở rộng cho **Live OCP** (thêm lớp mới không sửa code cũ trong 2–3 phút tắt AI).

### 3. Phần 4 — Thiết kế chi tiết
- [ ] **01 UML Class Diagram tổng (`mục 3.2`, `4.1`):** ≥20 lớp thực chất (≥13 lớp Entity/Domain có hành vi bảo vệ bất biến, chống Anemic Model), đủ 6 loại quan hệ UML, khớp 100% code C#.
- [ ] **≥ 4 sơ đồ Sequence / State (`mục 4.2`):** ≥2 sơ đồ/TV, trong đó ≥1 sơ đồ xuyên phân hệ Quầy ↔ Kho khớp Call Stack khi Debug và khớp vòng đời trạng thái 5 đối tượng.
- [ ] **Thiết kế CSDL vật lý & Seed Data (`mục 1.3`, `4.1`, Slide 16–17):** Script SQL tạo đủ 21 bảng 3NF (kiểu dữ liệu, PK/FK, `CHECK`, `UNIQUE`, `INDEX`) + 1 script Seed tạo ≥120 bản ghi liên thông trải 2–3 kỳ/tháng (đủ 3 vai trò, hoàn thành, hủy, công nợ, quá hạn theo `hanThanhToan`) + **bảng tính tay đối chiếu 5 báo cáo**.
- [ ] **Thiết kế Giao diện & Biểu mẫu (`mục 1.1`, `1.2`, `2.1`, Slide 18–20):** Thiết kế ≥9 màn hình nghiệp vụ trên 1 Main Shell chung (≥4/TV, ≤40% danh mục đơn, ≥4 Master–Detail), bộ lọc đa tiêu chí, mẫu in chứng từ (Hóa đơn, Phiếu trả, Phiếu nhập, Phiếu kiểm, Phiếu bàn giao ca) và 5 mẫu báo cáo xuất Excel/PDF sạch font tiếng Việt.

### 4. Phần 5 — Cài đặt, Kiểm thử, Git & Đóng gói bảo vệ
- [ ] **Khởi tạo Git repo & Bảng phân công theo tuần (`mục 1.4`, Slide 27):** Cần bắt đầu sớm để bảo đảm **≥24 commits thực chất rải ≥3 tuần**, **≥2 PR đã merge**, đóng góp **40–60% mã logic/TV** (không dồn >60% trong 48 giờ cuối).
- [ ] **Cài đặt mã nguồn C# (`mục 2.1–2.4`):** Validation 2 lớp (UI + Domain/Service), hệ `CustomException` dùng chung (0 `catch {}` rỗng), hash mật khẩu, SQL Parameterized 100%, ACID Transaction rollback xuyên bảng TV1–TV2, 1 tệp cấu hình ngoài chung (+ tệp mẫu), xuất Excel/PDF, tránh N+1 query.
- [ ] **Xây dựng & chạy bộ ≥ 26 tests PASS 100% (`mục 5.1–5.3`):**
  - ≥18 Unit Tests chuẩn AAA (≥9/TV, mỗi TV ≥2 ca Mock/Stub Interface phân hệ bạn, chạy độc lập không cần CSDL).
  - ≥8 Integration Tests chạy trên Seed ≥120 dòng (≥2 luồng liên phân hệ, ≥2 Rollback tiêm lỗi xuyên phân hệ, ≥2 báo cáo khớp tính tay, ≥2 RBAC 3 vai trò).
  - Hiện thực hóa 5 kịch bản `TC-IT-01`–`TC-IT-05` đã thiết kế ở bước phân tích cùng phần còn lại của ma trận test; vượt Mutation Check / Live Fault-Injection.
- [ ] **Đóng gói Clean-Room & Bảo vệ (`mục 1.4`, `5.3`):** Build/run trên máy sạch ≤3 phút, README, gói thực thi, Slide, Video demo 3–5 phút, luyện tập đặt Breakpoint đọc Call Stack xuyên phân hệ, Live OCP tắt AI và vấn đáp chéo TV1 ↔ TV2.

---

## III. Danh mục tệp chính thức hiện hành trong thư mục dự án

| Tệp | Vai trò | Trạng thái |
|---|---|---|
| `docs/references/Checklist_BTL_HTTTQL_v2026.xlsx` & `Checklist_BTL_HTTTQL_2026.md` | Phiếu đánh giá & rubric MI4090 | Tài liệu tham khảo |
| `docs/survey/Khao_sat_he_thong_ban_le_KiotViet.md` | Báo cáo khảo sát hiện trạng 4W+1H & phạm vi | Đã hoàn thiện |
| `docs/survey/KiotViet_Bieu_mau_Giao_dien_POS.zip` & `KiotViet_Bao_cao_thong_ke.zip` | Phụ lục minh chứng khảo sát | Đã hoàn thiện |
| `docs/analysis/BFD_POS_TinhChinh.png` / `.html` / `BFD-POS-31.png` | Biểu đồ phân cấp chức năng (8 nhóm / 31 chức năng) | Đã chốt |
| `docs/analysis/Dac-ta-chuc-nang-POS-31.docx` | Đặc tả 31 chức năng đồng bộ `QĐ01–QĐ06` | Đã chốt & sửa lỗi nhỏ |
| `docs/analysis/Bao-cao-ERD-POS.docx` & `ERD-POS-21*.png` | Báo cáo ERD 21 thực thể, thẻ kho & vòng đời | Đã chốt |
| `docs/analysis/Quy-tac-nghiep-vu-va-RBAC-POS-da-chot.docx` | 12 quy tắc nghiệp vụ, Ma trận RBAC 41 hàng & 5 kịch bản test dự kiến | Đã chốt (bản chính thức) |
| `docs/Bao_cao_tong_ket_phien_lam_viec.md` | Báo cáo tổng kết phiên làm việc & lộ trình tiếp theo | Đã cập nhật |
