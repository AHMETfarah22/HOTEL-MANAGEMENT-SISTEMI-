@echo off
echo ============================================
echo   ORYS Hotel - Online Rezervasyon Sistemi
echo ============================================
echo.
echo [1/2] Web API sunucusu baslatiliyor...
echo       Adres: http://localhost:5050
echo       Web Sitesi: http://localhost:5050
echo.
echo NOT: Bu pencereyi acik tutun!
echo Kapatirsaniz online rezervasyon sistemi durur.
echo.
cd /d "%~dp0"
echo Sunucu baslatiliyor, lutfen bekleyin...
start /b cmd /c "dotnet run --project ORYS.WebApi\ORYS.WebApi.csproj > api_log.txt 2>&1"
echo 4 saniye bekleniyor...
timeout /t 4 /nobreak > nul
echo Tarayici aciliyor...
start "" "http://localhost:5050"
echo.
echo ============================================
echo   Sunucu CALISIYOR - http://localhost:5050
echo   Bu pencereyi KAPATMAYIN!
echo ============================================
echo.
echo Kapatmak icin CTRL+C basin veya pencereyi kapatin.
pause
