# SleepTimer

Bilgisayarı seçilen süre sonunda Windows'un kendi kapatma komutuyla kapatan küçük, kurulumsuz bir zamanlayıcı.

- Hazır süreler: 5, 10, 15, 20, 30, 45, 60 dakika
- Elle süre: `90`, `1:30`, `2s`, `1,5 sa`, `45dk`, `1s30`
- Canlı geri sayım, tek tıkla iptal, +15 dk erteleme
- Bitime 1 dakika kala uyarı penceresi (kapatılabilir)
- Eylem seçimi: Kapat / Uyku / Hazırda beklet
- Küçültünce sistem tepsisinde çalışmaya devam eder
- İnternet gerekmez, tek `.exe`, yaklaşık 20 MB RAM

## Kullanım

`SleepTimer.exe` dosyasını çalıştır, bir süreye tıkla. Bu kadar.

Komut satırından da başlatılabilir: `SleepTimer.exe 45` (45 dakika) ya da `SleepTimer.exe 1:30`.
Test için `SleepTimer.exe --test` bilgisayarı kapatmaz, sadece bir mesaj gösterir.

Ayarlar `%APPDATA%\SleepTimer\ayarlar.txt` dosyasında tutulur.

## Derleme

Ek bir şey kurmak gerekmez; Windows ile gelen .NET Framework 4.8 derleyicisi kullanılır.

```powershell
.\build.ps1          # dist\SleepTimer.exe
.\build.ps1 -Test    # derle + otomatik testler + ekran görüntüleri (obj\test\)
```
