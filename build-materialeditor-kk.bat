@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "ME_REPO_ROOT=%~dp0"
set "ME_API_PROJECT=src\MaterialEditor.API\API.MaterialEditor.csproj"
set "ME_TEST_PROJECT=tests\MaterialEditor.MetadataTests\MaterialEditor.MetadataTests.csproj"
set "ME_KK_PROJECT=src\MaterialEditor.KK\KK.MaterialEditor.csproj"
set "ME_OUTPUT_DIR=bin\build\KK.MaterialEditor"
set "ME_OUTPUT_DLL=%ME_OUTPUT_DIR%\KK_MaterialEditor.dll"
set "ME_OUTPUT_NATIVE=%ME_OUTPUT_DIR%\libwebp.lib"

pushd "%ME_REPO_ROOT%" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Could not enter the repository directory:
    echo        %ME_REPO_ROOT%
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet was not found on PATH.
    goto :fail
)

if not exist "%ME_API_PROJECT%" (
    echo ERROR: Missing project: %ME_API_PROJECT%
    goto :fail
)
if not exist "%ME_TEST_PROJECT%" (
    echo ERROR: Missing project: %ME_TEST_PROJECT%
    goto :fail
)
if not exist "%ME_KK_PROJECT%" (
    echo ERROR: Missing project: %ME_KK_PROJECT%
    goto :fail
)

echo Material Editor KK Phase 1 build
echo Repository: %ME_REPO_ROOT%
echo This script builds and tests only. It does not deploy to Koikatsu.
echo.

if /I "%~1"=="--clean" (
    echo [1/6] Cleaning generated outputs...
    dotnet clean "%ME_API_PROJECT%" -c Release --nologo
    if errorlevel 1 goto :fail
    dotnet clean "%ME_TEST_PROJECT%" -c Release --nologo
    if errorlevel 1 goto :fail
    dotnet clean "%ME_KK_PROJECT%" -c Release --nologo
    if errorlevel 1 goto :fail
) else (
    echo [1/6] Clean skipped. Pass --clean for a clean build.
)

echo [2/6] Building the API and running PublicApiAnalyzers...
dotnet build "%ME_API_PROJECT%" -c Release --nologo
if errorlevel 1 goto :fail

echo [3/6] Running Material Editor metadata tests...
dotnet run --project "%ME_TEST_PROJECT%" -c Release
if errorlevel 1 goto :fail

echo [4/6] Building KK_MaterialEditor.dll...
dotnet build "%ME_KK_PROJECT%" -c Release --nologo
if errorlevel 1 goto :fail

echo [5/6] Verifying normal KK deployment artifacts...
if not exist "%ME_OUTPUT_DLL%" (
    echo ERROR: Build completed without the expected DLL:
    echo        %ME_REPO_ROOT%%ME_OUTPUT_DLL%
    goto :fail
)
if not exist "%ME_OUTPUT_NATIVE%" (
    echo ERROR: Build completed without the normal native library:
    echo        %ME_REPO_ROOT%%ME_OUTPUT_NATIVE%
    goto :fail
)

powershell -NoProfile -Command ^
    "$p=$env:ME_REPO_ROOT + $env:ME_OUTPUT_DLL; try { $a=[Reflection.AssemblyName]::GetAssemblyName($p); if ($a.Name -ne 'KK_MaterialEditor') { Write-Error ('Unexpected assembly name: ' + $a.Name); exit 1 }; Write-Host ('Assembly: ' + $a.FullName) } catch { Write-Error $_; exit 1 }"
if errorlevel 1 goto :fail

echo [6/6] SHA-256 hashes...
call :print_hash "%ME_REPO_ROOT%%ME_OUTPUT_DLL%" "KK_MaterialEditor.dll"
if errorlevel 1 goto :fail
call :print_hash "%ME_REPO_ROOT%%ME_OUTPUT_NATIVE%" "libwebp.lib"
if errorlevel 1 goto :fail

echo.
echo BUILD SUCCEEDED.
echo Deployment inputs are limited to:
echo   %ME_REPO_ROOT%%ME_OUTPUT_DLL%
echo   %ME_REPO_ROOT%%ME_OUTPUT_NATIVE%
echo No game, BepInEx, Unity, KKAPI, or other plugin dependency is copied.
popd >nul
exit /b 0

:print_hash
set "ME_HASH_FILE=%~1"
set "ME_HASH_VALUE="
for /f "usebackq delims=" %%H in (`powershell -NoProfile -Command "try { (Get-FileHash -Algorithm SHA256 -LiteralPath $env:ME_HASH_FILE).Hash } catch { exit 1 }"`) do set "ME_HASH_VALUE=%%H"
if not defined ME_HASH_VALUE (
    echo ERROR: Could not hash %~2.
    exit /b 1
)
echo   %~2
echo     %ME_HASH_VALUE%
exit /b 0

:fail
echo.
echo BUILD FAILED. Nothing was deployed to Koikatsu.
popd >nul
exit /b 1
