# Khảo sát hệ thống quản lý bán lẻ KiotViet

**Ngày tổng hợp:** 26/11/2026  
**Đối tượng:** KiotViet dành cho ngành bán buôn, bán lẻ.  
**Mục tiêu:** Tìm hiểu cách cửa hàng ghi nhận giao dịch, quản lý hàng và tiền, sau đó xác định yêu cầu cho bài tập lớn xây dựng web POS.

## 1. Bối cảnh và phương pháp khảo sát

Cửa hàng cần trả lời ba câu hỏi sau mỗi ngày kinh doanh: đã bán và nhận trả lại những mặt hàng nào, trong kho còn bao nhiêu hàng, thu ngân phải bàn giao bao nhiêu tiền mặt. Một giao dịch có thể ảnh hưởng đến cả ba kết quả. Chẳng hạn, khi khách trả một phần hóa đơn, cửa hàng cần biết mặt hàng nào được nhận lại, số tiền hoàn là bao nhiêu và khoản hoàn thuộc ca nào.

Khảo sát sử dụng tài liệu hướng dẫn và ảnh do KiotViet công bố, kết hợp năm ảnh biểu mẫu và giao diện đã thu thập. Phạm vi quan sát là một cửa hàng có quầy thu ngân và kho. Các kết quả về KiotViet dựa trên tài liệu công khai. Chưa thực hiện phỏng vấn nhân viên, đo thời gian thao tác hoặc thử nghiệm trên tài khoản cửa hàng thực tế.

### Khảo sát sơ bộ để chọn hệ thống tham khảo

Bảng dưới ghi lại kết quả chấm tham khảo của nhóm khảo sát theo mức độ bao phủ nghiệp vụ thể hiện trong tài liệu công khai. Mỗi tiêu chí dùng thang 10, trọng số 20%. Điểm trung bình là tổng năm điểm chia cho 5.

| Hệ thống | Bán tại quầy | Kho | Ca và tiền mặt | Báo cáo | Đa kênh | Trung bình |
|---|---:|---:|---:|---:|---:|---:|
| KiotViet | 10 | 9 | 10 | 10 | 9 | **9,6** |
| Nhanh.vn | 9 | 10 | 9 | 9 | 10 | 9,4 |
| MISA eShop | 9 | 9 | 10 | 9 | 9 | 9,2 |
| Sapo | 9 | 9 | 8 | 9 | 10 | 9,0 |
| Suno.vn | 9 | 8 | 7 | 8 | 8 | 8,0 |

KiotViet được chọn để khảo sát sâu vì tài liệu mô tả được một chuỗi nghiệp vụ xuyên suốt: thu ngân lập hóa đơn, nhận trả hàng, ghi thu chi, đối chiếu tiền cuối ca và truy lại giao dịch khi có chênh lệch. [KiotViet – Kết ca][ca]

## 2. Kết quả khảo sát theo 4W1H

| Câu hỏi | Kết quả |
|---|---|
| **Who – Ai tham gia?** | Thu ngân bán hàng, hoàn tiền và bàn giao ca. Nhân viên kho nhận hàng và kiểm đếm. Chủ cửa hàng hoặc quản lý cấp quyền, theo dõi chênh lệch và xem báo cáo. Khách hàng và nhà cung cấp là các bên tham gia giao dịch. |
| **What – Thực hiện công việc gì?** | Quản lý danh mục, giá bán và đối tác. Ghi nhận nhập hàng, bán hàng, trả hàng, kiểm kho, thu chi và kết ca. Tổng hợp kết quả theo ngày hoặc kỳ do quản lý chọn. |
| **Where – Ở đâu?** | Thu ngân ghi giao dịch tại quầy, nhân viên kiểm đếm tại nơi lưu hàng. Quản lý xem danh sách giao dịch và báo cáo trên màn hình quản lý hoặc ứng dụng di động. |
| **When – Khi nào?** | Mở ca khi nhận quầy, ghi nhận nghiệp vụ lúc phát sinh, đóng ca khi bàn giao. Kiểm kho theo đợt kiểm đếm của cửa hàng. Báo cáo cuối ngày tổng hợp hoạt động trong ngày, báo cáo theo kỳ phục vụ phân tích dài hơn. |
| **How – Thực hiện như thế nào?** | Nhân viên lập chứng từ cho từng nghiệp vụ. Khi bật kết ca, hệ thống gắn giao dịch vào ca đang mở. Cuối ca, thu ngân so tiền thực đếm với tiền dự kiến, còn khi kiểm kho, nhân viên so số thực đếm với tồn trên hệ thống. |

Nguồn nghiệp vụ: [Quản lý người dùng][quyen] · [Kiểm kho][kiem] · [Kết ca][ca] · [Báo cáo][bc].

## 3. Quy trình hiện tại trên KiotViet

| Nghiệp vụ | Trình tự chính | Đầu ra hoặc thay đổi cần theo dõi |
|---|---|---|
| Nhập hàng | Nhân viên chọn nhà cung cấp, ghi mặt hàng, số lượng, giá nhập và khoản đã thanh toán. Có thể lưu tạm hoặc hoàn thành phiếu. | Phiếu nhập hoàn thành cập nhật tồn. Lịch sử thanh toán hỗ trợ đối chiếu với nhà cung cấp. [Nguồn][nhap] |
| Bán hàng và thanh toán | Thu ngân tìm hoặc quét hàng, nhập số lượng, kiểm tra số phải thu, chọn phương thức và xác nhận thanh toán. Một hóa đơn có thể được trả bằng nhiều phương thức. | Hóa đơn, các dòng hàng, khoản thanh toán và số tiền thừa nếu có. [Bán hàng][ban] · [Thanh toán][tt] |
| Trả hàng theo hóa đơn | Tìm hóa đơn gốc, chọn đúng dòng hàng và số lượng khách trả, kiểm tra giá trị hoàn, chọn phương thức hoàn và xác nhận. | Phiếu trả liên kết với hóa đơn gốc, số lượng hàng nhận lại và khoản hoàn tiền. [Nguồn][tra] |
| Kiểm kho | Tạo phiếu kiểm, chọn hàng, ghi số lượng đếm được và đối chiếu chênh lệch. Khi hoàn thành phiếu, hệ thống cân bằng tồn theo số thực tế. | Phiếu kiểm, số chênh lệch và tồn kho sau cân bằng. [Nguồn][kiem] |
| Thu chi trong ca | Khi phát sinh khoản thu chi riêng, nhân viên chọn quỹ, lập phiếu, ghi đối tượng, lý do và số tiền. | Phiếu thu, phiếu chi và giao dịch trong sổ quỹ. [Nguồn][quy] |
| Mở và kết ca | Thu ngân khai báo tiền đầu ca. Khi bàn giao, thu ngân đếm tiền, nhập số thực tế, kiểm tra chênh lệch và đóng ca. | Phiếu bàn giao, trạng thái ca, tiền dự kiến, tiền bàn giao và chênh lệch. [Nguồn][ca] |
| Xem thống kê | Quản lý chọn nhóm báo cáo, thời gian, chi nhánh và tiêu chí phù hợp với câu hỏi cần trả lời. | Báo cáo cuối ngày hoặc bảng phân tích theo hàng, khách, nhà cung cấp, kênh hay kỳ kinh doanh. [Nguồn][bc] |

### Ví dụ đối soát ca

Giả sử đầu ca có 500.000 đồng. Trong ca thu được 1.200.000 đồng tiền bán hàng bằng tiền mặt và 800.000 đồng bằng chuyển khoản. Thu ngân hoàn 100.000 đồng bằng tiền mặt, các khoản thu chi khác bằng 0.

| Chỉ tiêu | Kết quả |
|---|---:|
| Tổng tiền khách đã thanh toán trước hoàn | 2.000.000 đồng |
| Tiền mặt dự kiến trong két | 500.000 + 1.200.000 − 100.000 = **1.600.000 đồng** |
| Tiền mặt thực đếm khi bàn giao | 1.580.000 đồng |
| Chênh lệch = thực đếm − dự kiến | **−20.000 đồng**, thiếu 20.000 đồng |

Khoản chuyển khoản được ghi trong thống kê thanh toán. Khi tính tiền trong két, chỉ cộng các khoản vào bằng tiền mặt và trừ các khoản ra bằng tiền mặt. Số liệu trên được đặt để minh họa quy tắc. [Cách tính trong hướng dẫn kết ca][ca]

## 4. Bốn nhóm đầu ra chuyển sang bước phân tích

| Đầu ra khảo sát | Minh chứng đã thu thập | Dùng ở bước phân tích |
|---|---|---|
| **Biểu mẫu và giao diện** | Hóa đơn bán hàng, phiếu nhập kho, biên bản kiểm kê, màn hình chọn hóa đơn để trả hàng, màn hình thu chi và bàn giao ca. | Nhận diện trường cần nhập, kết quả cần hiển thị và người thực hiện. |
| **Báo cáo** | Bảy nhóm báo cáo bán lẻ và bảy mẫu chi tiết thuộc nhóm Hàng hóa, liệt kê tại mục 5. | Xác định chỉ tiêu, kỳ tổng hợp và đối tượng phân tích. |
| **Dữ liệu** | Mã hàng, số lượng, đơn giá, nhà cung cấp, mã hóa đơn gốc, phương thức thanh toán, nhân viên, mã ca, số thực đếm và chênh lệch. | Làm căn cứ dựng ERD và từ điển dữ liệu. Ví dụ, một hóa đơn có nhiều dòng hàng và có thể có nhiều khoản thanh toán. |
| **Quy trình** | Các bước, tác nhân, chứng từ và kết quả ở mục 3. | Xây dựng sơ đồ phân cấp chức năng và mô tả use case, gồm cả nhánh xử lý khi chênh lệch kho hoặc tiền cuối ca. |

## 5. Danh mục báo cáo thống kê

| Nhóm | Chỉ tiêu hoặc câu hỏi quản lý |
|---|---|
| Cuối ngày | Số hóa đơn, doanh thu, thu chi phát sinh trong ngày. Có thể xem theo phương thức thanh toán. |
| Bán hàng | Doanh thu, giá trị hàng trả, doanh thu thuần và số đơn bán trong kỳ. |
| Hàng hóa | Hàng bán, lợi nhuận theo sản phẩm, giá trị kho và biến động xuất nhập tồn. |
| Khách hàng | Hàng bán theo khách và khoản công nợ cần theo dõi. |
| Nhà cung cấp | Hàng nhập theo từng nhà cung cấp và khoản còn phải trả. |
| Kênh bán hàng | Doanh thu, lợi nhuận và hàng bán theo kênh. |
| Tài chính | Doanh thu, giá vốn, chi phí và kết quả kinh doanh. |

**Bảy mẫu chi tiết thuộc nhóm Hàng hóa** gồm bán hàng theo mặt hàng, lợi nhuận theo mặt hàng, xuất nhập tồn, xuất nhập tồn chi tiết, nhân viên theo hàng bán, khách theo hàng bán và nhà cung cấp theo hàng nhập. Ảnh chi tiết lấy từ hướng dẫn phiên bản cũ. [Danh mục báo cáo bán lẻ][bc] · [Hướng dẫn chi tiết báo cáo hàng hóa][hh]

## 6. Hạn chế quan sát được và phạm vi hệ thống đề xuất

KiotViet yêu cầu thiết bị có kết nối mạng khi mở hoặc đóng ca. Nếu mất mạng lúc xác nhận đóng ca, ca vẫn mở để thu ngân thử lại. Ca đã đóng không thể mở lại, nên nhân viên cần rà soát số tiền trước khi xác nhận. Đây là hai ràng buộc vận hành có tài liệu đối chiếu. Bộ minh chứng hiện có chưa đủ để kết luận một cửa hàng cụ thể thường xuyên thiếu tiền, sai tồn kho hoặc mất bao lâu để kết ca. [Nguồn][ca]

**Phạm vi đề xuất cho bài tập lớn** là web POS cho một cửa hàng, phục vụ ba vai trò chính: thu ngân, nhân viên kho và quản lý. Các yêu cầu dưới đây là quyết định cho hệ thống của nhóm.

| Chức năng đề xuất | Yêu cầu có thể kiểm tra |
|---|---|
| Danh mục và phân quyền | Quản lý khai báo mặt hàng, nhóm hàng, giá bán, nhà cung cấp, tài khoản và quyền theo vai trò. |
| Bán hàng và thanh toán | Lưu đơn giá tại thời điểm bán và từng khoản thanh toán. Tổng tiền mặt và chuyển khoản khớp số phải thu đối với hóa đơn đã thanh toán đủ. |
| Trả hàng | Tra cứu hóa đơn gốc, xác định số còn được trả theo từng dòng. Nếu đã mua 5 hộp và từng trả 2 hộp thì lần trả sau tối đa 3 hộp. |
| Nhập hàng và kiểm kho | Lưu số lượng nhập, tồn trên sổ, số thực đếm và chênh lệch. Quản lý có thể xem xét chênh lệch trước khi xác nhận điều chỉnh tồn. |
| Ca bán hàng và sổ quỹ | Lưu tiền đầu ca, các giao dịch tiền mặt và tiền bàn giao. Có thể truy từ ca bị lệch về hóa đơn, khoản hoàn hoặc phiếu thu chi liên quan. |
| Báo cáo quản lý | Ưu tiên báo cáo cuối ngày theo phương thức thanh toán, bán hàng và trả hàng, xuất nhập tồn và chênh lệch ca. |

## 7. Phụ lục minh chứng

1. **Biểu mẫu và giao diện POS.** Gồm 5 ảnh do người khảo sát cung cấp, 37 ảnh từ hướng dẫn KiotViet và 7 mẫu trống tự biên soạn có thể điền và in. Trong bộ này, sổ hàng hóa là sổ theo dõi biến động, còn ảnh trả hàng ban đầu chỉ thể hiện bước chọn hóa đơn. [Tải bộ biểu mẫu và giao diện](https://chatgpt.com/library?file_id=libfile_69e3c22aa624819183dad34024f5589a&account_id=personal)
2. **Báo cáo thống kê.** Gồm 14 ảnh với chú thích và nguồn gốc của từng ảnh. Ba ảnh báo cáo tổng hợp cũng xuất hiện trong bộ giao diện. [Tải bộ báo cáo thống kê](https://chatgpt.com/library?file_id=libfile_faf003188ae881919d8388e3f199d06b&account_id=personal)

Giải nén từng bộ và mở `Muc_luc.html` để tra theo tên nghiệp vụ. Khi trích ảnh vào bài, dùng liên kết nguồn KiotViet ghi dưới ảnh. Với các mẫu trống tự biên soạn, ghi nguồn là “Mẫu đề xuất cho hệ thống POS của nhóm”.

[ca]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-ket-ca/ket-ca/
[quyen]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-thiet-lap/quan-ly-nguoi-dung/
[kiem]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-hang-hoa/kiem-kho/
[nhap]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-giao-dich/nhap-hang/
[ban]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-ban-hang/ban-hang/
[tt]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-thanh-toan/thanh-toan/
[tra]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-tra-hang/tra-hang/
[quy]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-so-quy/so-quy/
[bc]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-bao-cao/bao-cao/
[hh]: https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/huong-dan-bao-cao/bao-cao-hang-hoa/
