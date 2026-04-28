@echo off
title ORYS Hotel - PROFESYONEL YAYINICI
echo ============================================
echo   ORYS HOTEL - KESIN CALISAN YAYIN SİSTEMİ
echo ============================================
echo.

cd /d "%~dp0"

:: 1. Varsa eski portu temizle ve API'yi baslat
echo [1/3] Sunucu (API) baslatiliyor...
start "ORYS_API_SUNUCUSU" cmd /c "dotnet run --project ORYS.WebApi\ORYS.WebApi.csproj"

:: 2. Sunucunun hazir olmasini bekle (Kontrollu)
echo [2/3] Sunucunun hazir olmasi bekleniyor...
:check_loop
timeout /t 2 /nobreak > nul
curl -s http://localhost:5050 > nul
if %errorlevel% neq 0 (
    echo ...Sunucu henüz hazir degil, bekleniyor...
    goto check_loop
)

echo.
echo [TAMAM] Sunucu artik yerelde (localhost:5050) calisiyor!
echo.

:: 3. Yayini Baslat (Pinggy - En garantisi)
echo [3/3] Internet yayini kuruluyor...
echo ----------------------------------------------------
echo ARKADASLARINIZA GONDERECEGINIZ LINK ASAGIDADIR:
echo.
ssh -p 443 -R 0:localhost:5050 a.pinggy.io

pause
