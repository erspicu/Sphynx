@echo off
SETLOCAL
SET PROJECT_NAME=SphynxApp

echo ==========================================
echo   Sphynx AI DevOps Station - Auto Build
echo ==========================================

cd %PROJECT_NAME%

echo [1/3] Cleaning project...
dotnet clean > nul

echo [2/3] Restoring packages...
dotnet restore

echo [3/3] Compiling %PROJECT_NAME%...
dotnet build --configuration Debug

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ==========================================
    echo   BUILD SUCCESSFUL!
    echo ==========================================
    echo.
    echo Tip: You can run the app using 'dotnet run --project %PROJECT_NAME%'
) else (
    echo.
    echo !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    echo   BUILD FAILED! Please check the errors.
    echo !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
)

pause
ENDLOCAL
