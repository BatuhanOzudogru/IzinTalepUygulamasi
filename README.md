# 🧾 İzin Talep Uygulaması


## ⚙️ Kurulum

1. Projeyi klonlayın veya indirin:

```bash
git clone https://github.com/BatuhanOzudogru/IzinTalepUygulamasi.git
```

2. `script.sql` dosyasını **SQL Server** üzerinde çalıştırın.

3. `appsettings.json` dosyasındaki **ConnectionStrings → DefaultConnection** kısmında veritabanı adını kendi oluşturduğunuz isimle değiştirin.

Örnek:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=IzinTalepUygulamasiDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "AllowedHosts": "*"
}
```

4. Visual Studio üzerinden projeyi açın ve çalıştırın.
