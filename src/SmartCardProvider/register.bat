@echo off
echo ==================================
echo Smart Card Provider Registration
echo ==================================
echo.

REM Check for administrator rights
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

echo Registering SmartCardProvider.dll...
regsvr32 /s SmartCardProvider.dll

if %errorlevel% equ 0 (
    echo.
    echo SUCCESS! Provider registered.
    echo.
    echo Next steps:
    echo 1. Insert smart card
    echo 2. Lock workstation (Windows+L)
    echo 3. Provider should appear on login screen
    echo 4. Enter PIN to authenticate
    echo.
    echo Note: You may need to restart for changes to take effect.
    echo.
) else (
    echo.
    echo FAILED! Error code: %errorlevel%
    echo Make sure SmartCardProvider.dll exists in this directory.
    echo.
)

pause
