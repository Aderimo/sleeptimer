# SleepTimer — proje kuralları

Windows kapatma zamanlayıcısı. C# + WinForms, hedef .NET Framework 4.8 (Windows'ta hazır). Tek exe, kurulumsuz.

## Komutlar
- `.\build.ps1` → proje klasöründe `SleepTimer.exe` (+ `obj\SHA256SUMS.txt`); `-Shortcut` masaüstü kısayolu yapar. Program açıksa üzerine yazamaz.
- `.\build.ps1 -Test` → derler, `tests\Tests.cs` çalıştırır, ekran görüntülerini `obj\test\` altına yazar.

## Yapı
- `src\` uygulama (Program, MainForm, WarningForm, Ui, Duration, Power, Settings, Native)
- `tools\MakeIcon.cs` simgeyi (`assets\icon.ico`) üretir; ico silinirse build yeniden üretir.
- `tests\Tests.cs` sürüm ayrıştırma + arayüz akışı testleri (InternalsVisibleTo ile).

## Kurallar
- Derleyici Windows'un `csc.exe`'si → **C# 5**: string interpolation, `?.`, `=>` üyeler, `nameof`, `out var` YOK.
- Kaynaklar UTF-8, derleme `/codepage:65001`. `build.ps1` sadece ASCII (PowerShell 5.1).
- Kapatma `shutdown.exe /s /t 0` (zorlamasız). Testlerde `Power.TestHook` kullan; testler bilgisayarı asla kapatmaz.
- Arka planda gereksiz iş yok: zamanlayıcı sadece geri sayım varken çalışır (görünürken 250 ms, tepside 1 sn).
- Tasarım "C": koyu kompakt şerit, renkler `Ui.cs` içinde.
- Arayüz iki dilli: her metin `Lang.T("türkçe", "english")`; Windows dili tr ise Türkçe.
- Herkese açık depo: github.com/Aderimo/sleeptimer. README.md (EN) + README.tr.md, sürüm notları `docs/release-notes/`, ekran görüntüleri `.\build.ps1 -Test` çıktısından `docs/screenshots/{en,tr}`.
