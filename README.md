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

### Lưu ý:
- Thay <Tên Server MSSQL của bạn> bằng tên server thực tế (ví dụ: . hoặc localhost hoặc .\SQLEXPRESS).
- Nếu dùng SQL Authentication (tài khoản sa) thay vì Windows Authentication, hãy thay đoạn Trusted_Connection=True bằng User Id=sa;Password=mat_khau_cua_ban;.

## Tạo database và apply migrations

Để tạo cơ sở dữ liệu và các bảng, bạn cần chạy lệnh cập nhật.

### Cách 1: Dùng Visual Studio Mở Package Manager Console và chạy lệnh: 
> Update-Database

### Cách 2: Dùng Terminal (VS Code hoặc CMD) Tại thư mục gốc của dự án (nơi chứa file .csproj), chạy lệnh:
> dotnet ef database update

Lệnh này sẽ thực hiện:
- Tạo mới database tên là BookstoreDB (nếu chưa có).
- Áp dụng toàn bộ migrations để dựng cấu trúc bảng (Users, Roles, Permissions, RolePermissions, v.v.).

## Cập nhật Database khi thay đổi Model
Nếu bạn có chỉnh sửa Code First Model và muốn cập nhật lại database, hãy thực hiện các bước sau:

### 1. Tạo migration mới
> dotnet ef migrations add "Ten_Migration_Mota_ThayDoi"

### 2. Cập nhật vào database
> dotnet ef database update

## Chạy dự án
Sử dụng lệnh sau để khởi chạy ứng dụng:
> dotnet run
- Dự án thường sẽ chạy trên địa chỉ: http://localhost:5177 (hoặc cổng được cấu hình trong launchSettings.json)


## Kiểm tra Database
Sau khi chạy lệnh update database thành công, bạn có thể mở SQL Server Management Studio (SSMS) để kiểm tra. Database BookstoreDB sẽ bao gồm các bảng:
+ Users (AppUser)
+ Roles (AppRole)
+ Identity Tables: UserRoles, UserClaims, UserLogins, UserTokens, RoleClaims (các bảng mặc định của ASP.NET Identity).
+ Permissions
+ RolePermissions
+ Business Tables: Orders, ImportTickets, ExportTickets, v.v.

## Khắc phục sự cố (Troubleshooting)
Nếu gặp lỗi khi apply migration hoặc database bị xung đột, bạn có thể làm mới lại hoàn toàn migration bằng cách:

1. Xóa thư mục Migrations/ trong dự án.
2. Xóa database trong SQL Server.

### Chạy lại các lệnh sau để khởi tạo lại từ đầu:
> dotnet ef migrations add "Init"

> dotnet ef database update

### TRONG THƯ MỤC DỰ ÁN CÓ ĐÍNH KÈM FILE .SQL, CÓ THỂ CHẠY FILE .SQL ĐÍNH KÈM ĐỂ CÓ SẴN DỮ LIỆU MẪU
