@echo off
title ORYS Hotel - Yeni Nesil Online Yayin
echo ============================================
echo   ORYS HOTEL - SIFIRDAN ONLINE YAYIN
echo ============================================
echo.

:: 1. Varsa eski sunucu loglarini temizle
if exist api_log.txt del api_log.txt

:: 2. Sunucuyu Tertemiz Baslat
echo [1/2] Yerel sunucu sifirdan baslatiliyor...
cd /d "%~dp0"
start /b cmd /c "dotnet run --project ORYS.WebApi\ORYS.WebApi.csproj > api_log.txt 2>&1"

echo.
echo Sunucu hazirlaniyor, lutfen 8 saniye bekleyin...
timeout /t 8 /nobreak > nul

:: 3. Serveo ile Rastgele ve Benzersiz Link Olustur
echo.
echo [2/2] Internet baglantisi kuruluyor...
echo ----------------------------------------------------
echo ARKADASLARINIZA GONDERECEGINIZ LINK ASAGIDADIR.
echo (Forwarding yazan yerdeki https ile baslayan link)
echo ----------------------------------------------------
echo.
echo NOT: Eger soru sorarsa 'yes' yazip Enter'a basin.
echo.

:: Rastgele bir isim olusturmak icin %RANDOM% kullanalim
ssh -R 80:localhost:5050 serveo.net

pause
