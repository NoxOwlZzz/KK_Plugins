@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "ME_REPO_ROOT=%~dp0"
set "ME_API_PROJECT=src\MaterialEditor.API\API.MaterialEditor.csproj"
set "ME_METADATA_PROJECT=tests\MaterialEditor.MetadataTests\MaterialEditor.MetadataTests.csproj"
set "ME_PERF_PROJECT=tests\MaterialEditor.PerformanceHarness\MaterialEditor.PerformanceHarness.csproj"
set "ME_PERF_BASELINE=tests\MaterialEditor.PerformanceHarness\baselines\pre-optimization.json"
set "ME_PERF_RESULT=bin\build\materialeditor-performance-final.json"
set "ME_AI_PROJECT=src\MaterialEditor.AI\AI.MaterialEditor.csproj"
set "ME_EC_PROJECT=src\MaterialEditor.EC\EC.MaterialEditor.csproj"
set "ME_HS2_PROJECT=src\MaterialEditor.HS2\HS2.MaterialEditor.csproj"
set "ME_KK_PROJECT=src\MaterialEditor.KK\KK.MaterialEditor.csproj"
set "ME_KKS_PROJECT=src\MaterialEditor.KKS\KKS.MaterialEditor.csproj"
set "ME_PH_PROJECT=src\MaterialEditor.PH\PH.MaterialEditor.csproj"
set "ME_OUTPUT_DIR=bin\build\KK.MaterialEditor"
set "ME_OUTPUT_DLL=%ME_OUTPUT_DIR%\KK_MaterialEditor.dll"
set "ME_OUTPUT_NATIVE=%ME_OUTPUT_DIR%\libwebp.lib"
set "ME_EXPECTED_ASSEMBLY_NAME=KK_MaterialEditor"
set "ME_EXPECTED_ASSEMBLY_VERSION=4.0.3.0"
set "ME_EXPECTED_NATIVE_SHA=8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966"
set "ME_CLEAN=0"
set "ME_VERIFY_ONLY=0"
set "ME_NUGET_CONFIG=%ME_ROOT%nuget.config"

:parse_arguments
if "%~1"=="" goto :arguments_parsed
if /I "%~1"=="--clean" (
    set "ME_CLEAN=1"
    shift
    goto :parse_arguments
)
if /I "%~1"=="--verify-only" (
    set "ME_VERIFY_ONLY=1"
    shift
    goto :parse_arguments
)
echo ERROR: Unknown argument: %~1
echo Usage: %~nx0 [--clean] [--verify-only]
exit /b 2

:arguments_parsed
if "%ME_CLEAN%"=="1" if "%ME_VERIFY_ONLY%"=="1" (
    echo ERROR: --clean and --verify-only cannot be used together.
    exit /b 2
)

pushd "%ME_REPO_ROOT%" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Could not enter the repository directory:
    echo        %ME_REPO_ROOT%
    exit /b 1
)

echo ================================================================
echo Do not install this optimized fork together with the official Material Editor DLL.
echo ================================================================
echo Material Editor optimized build
echo Repository: %ME_REPO_ROOT%
echo This script never deploys files to Koikatsu.
echo.

if "%ME_VERIFY_ONLY%"=="1" (
    echo VERIFY-ONLY: no restore, clean, build, test, or deployment will run.
    call :verify_output_artifacts
    if errorlevel 1 goto :fail
    echo.
    echo VERIFICATION SUCCEEDED. No files were changed.
    popd >nul
    exit /b 0
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet was not found on PATH.
    goto :fail
)

call :require_project "%ME_API_PROJECT%"
if errorlevel 1 goto :fail
call :require_project "%ME_METADATA_PROJECT%"
if errorlevel 1 goto :fail
call :require_project "%ME_PERF_PROJECT%"
if errorlevel 1 (
    echo ERROR: The performance harness is a mandatory optimized-build gate.
    goto :fail
)
call :require_file "%ME_PERF_BASELINE%"
if errorlevel 1 (
    echo ERROR: The pre-optimization performance baseline is a mandatory optimized-build gate.
    goto :fail
)
call :require_project "%ME_AI_PROJECT%"
if errorlevel 1 goto :fail
call :require_project "%ME_EC_PROJECT%"
if errorlevel 1 goto :fail
call :require_project "%ME_HS2_PROJECT%"
if errorlevel 1 goto :fail
call :require_project "%ME_KK_PROJECT%"
if errorlevel 1 goto :fail
call :require_project "%ME_KKS_PROJECT%"
if errorlevel 1 goto :fail
call :require_project "%ME_PH_PROJECT%"
if errorlevel 1 goto :fail

if not exist "%ME_NUGET_CONFIG%" (
    echo ERROR: The repository NuGet configuration was not found:
    echo        %ME_NUGET_CONFIG%
    echo This versioned configuration is required for reproducible restores from
    echo nuget.org, IllusionMods, and BepInEx.
    goto :fail
)

echo [1/7] Restoring exact project dependencies with the repository NuGet configuration...
call :restore_project "%ME_API_PROJECT%"
if errorlevel 1 goto :fail
call :restore_project "%ME_METADATA_PROJECT%"
if errorlevel 1 goto :fail
call :restore_project "%ME_PERF_PROJECT%"
if errorlevel 1 goto :fail
call :restore_project "%ME_AI_PROJECT%"
if errorlevel 1 goto :fail
call :restore_project "%ME_EC_PROJECT%"
if errorlevel 1 goto :fail
call :restore_project "%ME_HS2_PROJECT%"
if errorlevel 1 goto :fail
call :restore_project "%ME_KK_PROJECT%"
if errorlevel 1 goto :fail
call :restore_project "%ME_KKS_PROJECT%"
if errorlevel 1 goto :fail
call :restore_project "%ME_PH_PROJECT%"
if errorlevel 1 goto :fail

if "%ME_CLEAN%"=="1" (
    echo [2/7] Cleaning Material Editor outputs...
    call :clean_project "%ME_API_PROJECT%"
    if errorlevel 1 goto :fail
    call :clean_project "%ME_METADATA_PROJECT%"
    if errorlevel 1 goto :fail
    call :clean_project "%ME_PERF_PROJECT%"
    if errorlevel 1 goto :fail
    call :clean_project "%ME_AI_PROJECT%"
    if errorlevel 1 goto :fail
    call :clean_project "%ME_EC_PROJECT%"
    if errorlevel 1 goto :fail
    call :clean_project "%ME_HS2_PROJECT%"
    if errorlevel 1 goto :fail
    call :clean_project "%ME_KK_PROJECT%"
    if errorlevel 1 goto :fail
    call :clean_project "%ME_KKS_PROJECT%"
    if errorlevel 1 goto :fail
    call :clean_project "%ME_PH_PROJECT%"
    if errorlevel 1 goto :fail
) else (
    echo [2/7] Clean skipped. Pass --clean for a clean build.
)

echo [3/7] Building the public API and running PublicApiAnalyzers...
dotnet build "%ME_API_PROJECT%" -c Release --no-restore --nologo
if errorlevel 1 goto :fail

echo [4/7] Running metadata and regression tests...
dotnet run --project "%ME_METADATA_PROJECT%" -c Release --no-restore
if errorlevel 1 goto :fail

echo [5/7] Running the deterministic performance harness and invariants...
if exist "%ME_PERF_RESULT%" del /q "%ME_PERF_RESULT%"
if errorlevel 1 (
    echo ERROR: Could not remove the previous final performance report:
    echo        %ME_REPO_ROOT%%ME_PERF_RESULT%
    goto :fail
)
dotnet run --project "%ME_PERF_PROJECT%" -c Release --no-restore -- --compare-baseline "%ME_PERF_BASELINE%" --json "%ME_PERF_RESULT%"
if errorlevel 1 goto :fail
call :require_file "%ME_PERF_RESULT%"
if errorlevel 1 (
    echo ERROR: The performance harness did not create its final JSON report.
    goto :fail
)
echo   Final performance report: %ME_REPO_ROOT%%ME_PERF_RESULT%

echo [6/7] Building every Material Editor target from the shared optimized sources...
call :build_project "%ME_AI_PROJECT%"
if errorlevel 1 goto :fail
call :build_project "%ME_EC_PROJECT%"
if errorlevel 1 goto :fail
call :build_project "%ME_HS2_PROJECT%"
if errorlevel 1 goto :fail
call :build_project "%ME_KK_PROJECT%"
if errorlevel 1 goto :fail
call :build_project "%ME_KKS_PROJECT%"
if errorlevel 1 goto :fail
call :build_project "%ME_PH_PROJECT%"
if errorlevel 1 goto :fail

echo [7/7] Verifying the only KK deployment inputs...
call :verify_output_artifacts
if errorlevel 1 goto :fail

echo.
echo BUILD SUCCEEDED.
echo Deployment inputs are limited to:
echo   %ME_REPO_ROOT%%ME_OUTPUT_DLL%
echo   %ME_REPO_ROOT%%ME_OUTPUT_NATIVE%
echo No shared dependency, game file, config, card, coordinate, or scene is copied.
popd >nul
exit /b 0

:require_project
if not exist "%~1" (
    echo ERROR: Missing required project: %~1
    exit /b 1
)
exit /b 0

:require_file
if not exist "%~1" (
    echo ERROR: Missing required file: %~1
    exit /b 1
)
exit /b 0

:restore_project
dotnet restore "%~1" --configfile "%ME_NUGET_CONFIG%" -p:Configuration=Release --nologo
exit /b %errorlevel%

:clean_project
dotnet clean "%~1" -c Release --nologo
exit /b %errorlevel%

:build_project
dotnet build "%~1" -c Release --no-restore --nologo
exit /b %errorlevel%

:verify_output_artifacts
if not exist "%ME_OUTPUT_DLL%" (
    echo ERROR: Missing optimized build DLL:
    echo        %ME_REPO_ROOT%%ME_OUTPUT_DLL%
    exit /b 1
)
if not exist "%ME_OUTPUT_NATIVE%" (
    echo ERROR: Missing native deployment artifact:
    echo        %ME_REPO_ROOT%%ME_OUTPUT_NATIVE%
    exit /b 1
)
call :verify_assembly "%ME_OUTPUT_DLL%" "optimized build DLL"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_OUTPUT_NATIVE%" "%ME_EXPECTED_NATIVE_SHA%" "optimized build libwebp.lib"
if errorlevel 1 exit /b 1
call :get_hash "%ME_OUTPUT_DLL%" ME_OUTPUT_DLL_SHA
if errorlevel 1 exit /b 1
echo   Optimized DLL SHA-256: %ME_OUTPUT_DLL_SHA%
echo   Native SHA-256:       %ME_EXPECTED_NATIVE_SHA%
exit /b 0

:verify_assembly
set "ME_ASSEMBLY_PATH=%~1"
set "ME_ASSEMBLY_LABEL=%~2"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $a=[Reflection.AssemblyName]::GetAssemblyName($env:ME_ASSEMBLY_PATH); if($a.Name -ne $env:ME_EXPECTED_ASSEMBLY_NAME -or $a.Version.ToString() -ne $env:ME_EXPECTED_ASSEMBLY_VERSION) { throw ('Unexpected ' + $env:ME_ASSEMBLY_LABEL + ' identity: ' + $a.FullName) }; Write-Host ('  Verified ' + $env:ME_ASSEMBLY_LABEL + ': ' + $a.FullName)"
exit /b %errorlevel%

:get_hash
set "ME_HASH_FILE=%~1"
set "ME_HASH_VALUE="
for /f "usebackq delims=" %%H in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; (Get-FileHash -Algorithm SHA256 -LiteralPath $env:ME_HASH_FILE).Hash"`) do set "ME_HASH_VALUE=%%H"
if not defined ME_HASH_VALUE (
    echo ERROR: Could not hash %~1
    exit /b 1
)
set "%~2=%ME_HASH_VALUE%"
exit /b 0

:verify_hash
call :get_hash "%~1" ME_ACTUAL_HASH
if errorlevel 1 exit /b 1
if /I not "%ME_ACTUAL_HASH%"=="%~2" (
    echo ERROR: SHA-256 mismatch for %~3
    echo   Expected: %~2
    echo   Actual:   %ME_ACTUAL_HASH%
    exit /b 1
)
echo   Verified %~3: %ME_ACTUAL_HASH%
exit /b 0

:fail
echo.
echo BUILD FAILED. Nothing was deployed to Koikatsu.
popd >nul
exit /b 1
