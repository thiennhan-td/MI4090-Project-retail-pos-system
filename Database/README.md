# Cơ sở dữ liệu POS

Bộ script dùng **SQL Server 2017 trở lên** và khởi tạo **21 bảng, 124 cột, 38 khóa ngoại** theo [từ điển dữ liệu](../docs/design/Tu_dien_du_lieu_POS.md). Chạy `01_Schema.sql` rồi `02_SeedData.sql` trên cùng một cơ sở dữ liệu trống dành riêng cho POS. Với SSMS, chọn cơ sở dữ liệu đích trước khi chạy. Với SQL Server đặt tên máy chủ hoặc instance khác, thay đối số `-S`.

```powershell
sqlcmd -S localhost -E -d RetailCorePosDemo -b -f 65001 -i Database/01_Schema.sql
if ($LASTEXITCODE -ne 0) { throw "Schema creation failed." }
sqlcmd -S localhost -E -d RetailCorePosDemo -b -f 65001 -i Database/02_SeedData.sql
if ($LASTEXITCODE -ne 0) { throw "Seed loading failed." }
```

Tạo trước database `RetailCorePosDemo` trên máy chủ đã chọn. Mỗi script có giao dịch riêng và hoàn tác khi lỗi. Nếu bảng đã chứa dữ liệu, script seed dừng trước khi ghi. Để nạp lại từ đầu, chạy bộ script trên một database trống khác.

## Nâng cấp kiểm kê F6

Đối với cơ sở dữ liệu POS hiện hữu, chạy [001_DongKiemLucDem.sql](Migrations/001_DongKiemLucDem.sql) trước khi triển khai service mới. Script bổ sung cột `DongKiem.lucDem`, ràng buộc và chỉ mục theo mã hàng. Có thể chạy lại script. Các dòng cũ nhận giá trị NULL.

```powershell
sqlcmd -S localhost -E -d RetailCorePosDemo -b -f 65001 -i Database/Migrations/001_DongKiemLucDem.sql
```

Phiếu cũ đã hoàn tất vẫn xem được và dùng `mocKiem` làm mốc bảo vệ đối với các dòng thiếu `lucDem`. Phiếu đang kiểm có thể đếm lại nếu tồn sổ tại mốc vẫn khớp và hàng chưa phát sinh biến động sau mốc. Nếu hàng đã phát sinh biến động hoặc phiếu cũ đã chờ duyệt, hủy phiếu và lập phiếu mới để đếm lại.

## Quy tắc triển khai

- Mã tự sinh có tiền tố và sequence theo bảng. Mã mẫu bắt đầu bằng `S-`, tách khỏi dãy tự sinh. `datetime2(3)` lưu UTC, `date` giữ ngày nghiệp vụ và `decimal(19,0)` lưu tiền nguyên VND.
- DDL kiểm tra vòng đời trên từng hàng, một ca mở cho mỗi thu ngân, khóa sự kiện duy nhất và quan hệ giữa loại, dấu và nguồn thẻ kho. Trigger kiểm mã hàng của thẻ khớp dòng bán, dòng trả hoặc dòng nhập. Khóa ngoại ghép kiểm dòng kiểm kho.
- Khi triển khai, gán tài khoản CSDL của ứng dụng vào role `pos_app` và cấp quyền cần thiết theo thao tác. Role này cấm sửa và xóa thẻ kho. Seed chứa hồ sơ của cả ba vai trò và ma trận 40 quyền. Giá trị `DEMO_DISABLED` là dấu giữ chỗ. Dùng bộ băm mật khẩu của ứng dụng để cấp thông tin đăng nhập mẫu khi có lớp xác thực.
- Service chịu trách nhiệm kiểm quyền theo ngữ cảnh, quản lý giao dịch và thứ tự khóa, kiểm hạn mức trả hoặc chi, chống ghi trùng yêu cầu và kiểm tồn dưới khóa. Các phép tổng hợp trong báo cáo lấy dữ liệu từ chứng từ có hiệu lực tại thời điểm phát sinh, bao gồm sự kiện đảo tại thời điểm hủy.

## Đối chiếu dữ liệu mẫu

Có **235 bản ghi**, gồm **121 bản ghi nghiệp vụ** ngoài phân quyền, tài khoản và danh mục. Dữ liệu từ tháng 9 đến tháng 11 năm 2026, với ca mở và đã đóng, chứng từ nháp, xác nhận và hủy, hàng nhận trả một phần, kiểm kê và khoản nợ quá hạn.

| Báo cáo | Bộ lọc | Phép tính và kết quả mong đợi |
|---|---|---|
| **F8.1 · Tổng hợp cuối ngày** | Ngày 10/10/2026 theo giờ Việt Nam | Hai hóa đơn, bán **161.000 đ** = 125.000 + 36.000. Thu bán tiền mặt **111.000 đ**, chuyển khoản **50.000 đ**. Không có khoản hoàn trong ngày. Chi NCC từ két **15.000 đ**, thu riêng tiền mặt **10.000 đ**, chi riêng tiền mặt **6.000 đ**, thu riêng chuyển khoản từ quỹ cửa hàng **20.000 đ**. |
| **F8.2 · Bán và trả** | Tháng 10/2026 | Bán trước đảo **161.000 đ**, đảo bán **36.000 đ**, giá trị bán sau đảo **125.000 đ**, trả **50.000 đ**. Bán thuần **75.000 đ**. Hai hộp trả tính theo giá bán gốc 25.000 đ/hộp. |
| **F8.3 · Xuất, nhập, tồn** | Tháng 10/2026 | Sữa `S-SUA` có tồn cuối **134** = 100 + 40 − 5 + 1 − 2. Bánh `S-BANH` có tồn cuối **50** = 50 − 3 + 3. Tồn đầu hình thành từ phiếu nhập tháng 9. |
| **F8.4 · Thanh toán và lệch ca** | Hai ca `S-CA-1010` và `S-CA-1011` | Dự kiến ca 1010 **600.000 đ** = 500.000 + 111.000 − 15.000 + 10.000 − 6.000. Đếm 598.000, lệch **−2.000 đ**. Dự kiến ca 1011 **414.000 đ** = 500.000 − 36.000 − 50.000. Đếm 410.000, lệch **−4.000 đ**. Tổng lệch **−6.000 đ**. |
| **F8.5 · Nhập và công nợ** | NCC `S-NCC-SUA`, tháng 10/2026 | Dư đầu **1.200.000 đ** = 1.500.000 − 300.000. Nhập trong kỳ **600.000 đ**, trả **115.000 đ**. Dư cuối **1.685.000 đ**. |

Khi lọc tháng 10 theo `Asia/Ho_Chi_Minh`, khoảng UTC là `[2026-09-30T17:00:00, 2026-10-31T17:00:00)`.
