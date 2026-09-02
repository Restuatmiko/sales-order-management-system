CATATAN-DESAIN

1. Alasan Pembagian Service

Sistem dibagi menjadi Customer Service dan Sales Order Service
berdasarkan domain fungsional. Customer Service bertanggung jawab
terhadap data master pelanggan, sedangkan Sales Order Service menangani
transaksi Sales Order beserta detail item, validasi, kalkulasi total,
pencarian, dan ekspor. Front-End digunakan sebagai lapisan presentasi
yang berkomunikasi dengan kedua service melalui HTTP REST API dan tidak
mengakses database secara langsung.

2. Bagian yang Dibantu AI vs Dikerjakan Sendiri

AI digunakan sebagai alat bantu dalam memahami requirement, menyusun
struktur kode, membantu debugging, dan memberikan alternatif
implementasi. Implementasi, pengujian fitur, penyesuaian kode, serta
pengambilan keputusan akhir dilakukan dan diverifikasi sendiri. Setiap
kode yang digunakan dipahami dan diuji agar dapat dijelaskan serta
dimodifikasi kembali.

3. Keputusan Teknis Penting

Menggunakan C# / .NET Web API untuk Customer Service dan Sales Order
Service.

Menggunakan ASP.NET Core MVC sebagai Front-End.

Komunikasi antar komponen menggunakan HTTP REST API.

Menggunakan SQL Server dengan tiga tabel sesuai schema yang
diberikan tanpa mengubah struktur tabel.

Seluruh validasi dan kalkulasi TOTAL serta Grand Total dilakukan
pada Sales Order Service.

Operasi delete Sales Order dan seluruh item terkait menggunakan
database transaction agar atomik.

Pada update Sales Order, seluruh item terkini dikirim dan service
mengganti item lama dengan item yang dikirim.

Export Excel dilakukan oleh Sales Order Service dan mengikuti filter
yang sedang aktif pada Order List.

4. Bagian yang Paling Menantang

Bagian yang paling menantang adalah memastikan komunikasi antar service,
proses update Sales Order beserta seluruh item, serta kalkulasi Grand
Total tetap konsisten. Selain itu, pengelolaan index item pada form
Front-End perlu diperhatikan agar penambahan dan penghapusan item tetap
dapat diterima dengan benar oleh model binding ASP.NET Core.