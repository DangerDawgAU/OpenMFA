@echo off
echo ====================================
echo Smart Card Provider Unregistration
echo ====================================
echo.

REM Check for administrator rights
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

echo Unregistering SmartCardProvider.dll...
regsvr32 /s /u SmartCardProvider.dll

if %errorlevel% equ 0 (
    echo.
    echo SUCCESS! Provider unregistered.
    echo.
) else (
    echo.
    echo Warning: Unregistration returned error code: %errorlevel%
    echo This may be normal if the provider was not registered.
    echo.
)

pause
