# Bookstore Management – Setup Guide

Hướng dẫn cài đặt và khởi chạy dự án Bookstore Management.

## Yêu cầu hệ thống

* **.NET SDK**: Phiên bản 6.0 trở lên.
* **SQL Server**: Có thể dùng bản Express, Developer hoặc LocalDB.
* **Entity Framework Core Tools**: Công cụ để chạy migration. Nếu chưa cài đặt, hãy chạy lệnh sau trong terminal:

> dotnet tool install --global dotnet-ef

## Clone dự án
Mở terminal và chạy lệnh sau để tải mã nguồn về máy:
> git clone [https://github.com/tuan171204/BookstoreManagement.git](https://github.com/tuan171204/BookstoreManagement.git)

## Cấu hình chuỗi kết nối
Mở file appsettings.json trong thư mục dự án, tìm và sửa phần ConnectionStrings:DefaultConnection để phù hợp với SQL Server trên máy bạn:
> "ConnectionStrings": {
  "DefaultConnection": "Server=<Tên Server MSSQL của bạn>;Database=BookstoreDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
