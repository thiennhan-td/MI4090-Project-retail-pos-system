-- Chạy sau 01_Schema.sql, trên cùng CSDL POS. Có thể chạy lại.
-- Lưu kết quả trả về cùng giao dịch ghi chứng từ để xử lý gửi lại sau khi mất phản hồi.
IF OBJECT_ID(N'dbo.YeuCauGhiQuay', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.YeuCauGhiQuay
    (
        khoa nvarchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        dauVanTay nvarchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        noiDungKetQua nvarchar(max) NOT NULL,
        CONSTRAINT PK_YeuCauGhiQuay PRIMARY KEY (khoa)
    );
END;
