<!-- bmad:context -->
<!-- Verified 2026-11-27 against cf40f5490705ef70583feeae5d610ea9b510f8d5. Managed by bmad-project-context. -->

# Quy ước triển khai POS

ASP.NET Core MVC và Razor phục vụ giao diện POS. Đối chiếu luồng nghiệp vụ và hợp đồng hai phân hệ trong `docs/design/Thiet_ke_kien_truc_va_OOP_POS.md`. Đối chiếu schema SQL Server trong `Database/01_Schema.sql`.

## Phân tầng

- `Pos.Web` nhận và kiểm tra dữ liệu form qua ViewModel, gọi service và đăng ký DI tại `Program.cs`. Đặt quy tắc nghiệp vụ trong service hoặc entity, đặt truy vấn SQL trong repository.
- TV1 phụ trách `Pos.Counter`, TV2 phụ trách `Pos.Inventory`. Hai project giao tiếp qua interface và DTO trong `Pos.Contracts`.
- Đặt entity, ngoại lệ nghiệp vụ, interface repository và `IUnitOfWork` trong `Pos.Domain`. Đặt cài đặt repository SQL và `SqlUnitOfWork` trong `Pos.Infrastructure`.

## Convention C#

- Dùng `PascalCase` cho kiểu, phương thức và property, `camelCase` cho tham số và biến cục bộ. Giữ từ vựng nghiệp vụ đã có như `HoaDon`, `maHoaDon`, `XacNhanBanAsync` và tên hợp đồng như `IInventoryService`, `YeuCauXuatBanDto`.
- Dùng `*ViewModel` cho dữ liệu form và `*Dto` cho dữ liệu qua hợp đồng. Ánh xạ từ ViewModel sang dữ liệu service cần. Kiểm tra định dạng ở Web, kiểm tra quyền và điều kiện nghiệp vụ ở service hoặc entity tương ứng.
- Kiểm quyền trong service qua `IAuthorizationService.KiemTraQuyenAsync` với mã quyền của ca nghiệp vụ. Dùng ngoại lệ có kiểu như `UnauthorizedActionException`, `InvalidStateTransitionException` và `InsufficientStockException`. Sau rollback, ném lại ngoại lệ gốc.

## Giao dịch và review chéo

- Service điều phối mở một transaction qua `IUnitOfWork`. Các repository của Quầy và Kho dùng cùng `SqlUnitOfWork` trong giao dịch đó. Commit một lần sau khi cả hai phần thành công. TV2 ghi `BienDongKho` qua `IInventoryService`.
- Nhờ thành viên còn lại review khi đổi interface hoặc DTO dùng chung, schema SQL, mã quyền, thứ tự khóa hoặc ranh giới transaction.

## Kiểm tra và luồng mẫu

- Khi khởi tạo solution, cấu hình `.editorconfig` cho thụt lề, dấu ngoặc, quy tắc hoa thường và tiền tố `I` của interface. Dùng review để kiểm tra thuật ngữ nghiệp vụ, vị trí đặt logic và quyền sở hữu dữ liệu.
- Lấy xác nhận bán làm mẫu. Razor form → Controller → `SaleService.XacNhanBanAsync` → kiểm quyền `BAN_XAC_NHAN` → mở transaction → lưu hóa đơn → gọi `IInventoryService.GhiNhanXuatBanAsync` → commit. Dùng `TC-IT-04` để kiểm tra thiếu tồn khiến cả thay đổi hóa đơn và thẻ kho được hoàn tác.

<!-- /bmad:context -->
