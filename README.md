# Sales Order Management System

Sales Order Management System adalah aplikasi untuk mengelola data Customer dan Sales Order menggunakan arsitektur microservices berbasis .NET.

1. Prasyarat
Sebelum menjalankan aplikasi, pastikan sudah tersedia:

- .NET 8 SDK
- SQL Server
- Visual Studio 2022
- SQL Server Management Studio (SSMS)
- Git

2. Struktur Project
sales-order-management-system/
??? CustomerService/
??? SalesOrderService/
??? FrontEnd/
??? Database/
?   ??? schema.sql
??? README.md
??? CATATAN-DESAIN.md

3. Setup Database
	1. Buka SQL Server Management Studio.
	2. Buat database dengan nama:
		CREATE DATABASE SalesOrderManagementDb;
	3. Jalankan file:
		Database/schema.sql
	4. Pastikan tabel berikut berhasil dibuat:
		- COM_CUSTOMER
		- SALES_SO
		- SALES_SO_LITEM
	5. Pastikan data contoh Customer tersedia.

4. Konfigurasi Connection String
	Connection string dikonfigurasi pada appsettings.json masing-masing service.
	{
  "ConnectionStrings": {
    "DefaultConnection": "Server=LAPTOP-NAI125NF\\SQLEXPRESS;Database=SalesOrderManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },

5. Menjalankan Service
	Jalankan ketiga project berikut
	1. CustomerService
		Port: https://localhost:7118/
	2. SalesOrderService
		Port: https://localhost:7212
	3. FrontEnd
		Port: https://localhost:7261
	Ketiga project harus berjalan secara bersamaan.

6. Mengakses User Interface
	Setelah seluruh service berjalan, buka browser: https://localhost:7261
	Fitur yang tersedia:
	- Customer Management
	- Sales Order List
	- Search Sales Order
	- Filter berdasarkan tanggal
	= Create Sales Order
	= Edit Sales Order
	= Delete Sales Order
	= Grand Total
	= Export Excel

7. Sales Order API
	SalesOrderService menyediakan endpoint:
	GET    /api/orders
	GET    /api/orders/{id}
	POST   /api/orders
	PUT    /api/orders/{id}
	DELETE /api/orders/{id}
	GET    /api/orders/export
	POST   /api/orders/calculate
	Contoh:
	GET https://localhost:7212/api/orders
	Search: GET https://localhost:7212/api/orders?keyword=SO-2026
	Filter tanggal: GET https://localhost:7212/api/orders?orderDate=2026-09-02
	Export: GET https://localhost:7212/api/orders/export

8. Customer API
	CustomerService menyediakan endpoint CRUD Customer:
	GET/api/Customers
	 Mengambil daftar Customer untuk kebutuhan dropdown pada FrontEnd.

9. Kalkulasi Sales Order
	Kalkulasi dilakukan pada SalesOrderService.
		TOTAL = QUANTITY × PRICE
		GRAND TOTAL = SUM(TOTAL)
	FrontEnd hanya menampilkan hasil kalkulasi dari service dan tidak melakukan kalkulasi bisnis secara langsung.

10. Build
	Build seluruh solution dengan: dotnet build SalesOrderManagement.sln
	Atau build masing-masing project: - dotnet build CustomerService/CustomerService.csproj
									  - dotnet build SalesOrderService/SalesOrderService.csproj
									  - dotnet build FrontEnd/FrontEnd.csproj