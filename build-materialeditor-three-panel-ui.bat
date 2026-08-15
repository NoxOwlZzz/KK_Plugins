@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "ME_REPO_ROOT=%~dp0"
set "ME_NUGET_CONFIG=%ME_REPO_ROOT%nuget.config"
set "ME_API_PROJECT=src\MaterialEditor.API\API.MaterialEditor.csproj"
set "ME_METADATA_PROJECT=tests\MaterialEditor.MetadataTests\MaterialEditor.MetadataTests.csproj"
set "ME_PERF_PROJECT=tests\MaterialEditor.PerformanceHarness\MaterialEditor.PerformanceHarness.csproj"
set "ME_PERF_BASELINE=tests\MaterialEditor.PerformanceHarness\baselines\pre-three-panel-ui.json"
set "ME_PERF_BASELINE_SHA=E11AB4DB48D256AAF299F2A7F0D900F8761E6313EC8868ABCDD3A9CEACA96B5D"
set "ME_PERF_RESULT=bin\build\materialeditor-performance-three-panel-ui.json"
set "ME_AI_PROJECT=src\MaterialEditor.AI\AI.MaterialEditor.csproj"
set "ME_EC_PROJECT=src\MaterialEditor.EC\EC.MaterialEditor.csproj"
set "ME_HS2_PROJECT=src\MaterialEditor.HS2\HS2.MaterialEditor.csproj"
set "ME_KK_PROJECT=src\MaterialEditor.KK\KK.MaterialEditor.csproj"
set "ME_KKS_PROJECT=src\MaterialEditor.KKS\KKS.MaterialEditor.csproj"
set "ME_PH_PROJECT=src\MaterialEditor.PH\PH.MaterialEditor.csproj"
set "ME_OUTPUT_DIR=bin\build\KK.MaterialEditor"
set "ME_OUTPUT_DLL=%ME_OUTPUT_DIR%\KK_MaterialEditor.dll"
set "ME_OUTPUT_XML=%ME_OUTPUT_DIR%\KK_MaterialEditor.xml"
set "ME_OUTPUT_NATIVE=%ME_OUTPUT_DIR%\libwebp.lib"
set "ME_OUTPUT_ZIP=bin\out\KK_MaterialEditor_v4.0.3.zip"
set "ME_EXPECTED_ASSEMBLY_NAME=KK_MaterialEditor"
set "ME_EXPECTED_ASSEMBLY_VERSION=4.0.3.0"
set "ME_EXPECTED_NATIVE_SHA=8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966"
set "ME_VERIFY_ONLY=0"

:parse_arguments
if "%~1"=="" goto :arguments_parsed
if /I "%~1"=="--verify-only" (
    set "ME_VERIFY_ONLY=1"
    shift
    goto :parse_arguments
)
echo ERROR: Unknown argument: %~1
echo Usage: %~nx0 [--verify-only]
exit /b 2

:arguments_parsed
pushd "%ME_REPO_ROOT%" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Could not enter the repository directory:
    echo        %ME_REPO_ROOT%
    exit /b 1
)

echo ================================================================
echo Do not install this Material Editor fork together with the official Material Editor DLL.
echo ================================================================
echo Material Editor three-panel UI clean Release build
echo Repository: %ME_REPO_ROOT%
echo Baseline:   %ME_REPO_ROOT%%ME_PERF_BASELINE%
echo Package:    %ME_REPO_ROOT%%ME_OUTPUT_ZIP%
echo This script never deploys files to Koikatsu.
echo.

echo [1/9] Checking that Koikatsu, CharaStudio, Studio, and VR processes are closed...
call :guard_game_processes
if errorlevel 1 (
    echo ERROR: Close every Koikatsu game, Studio, and VR process first.
    goto :fail
)

call :require_inputs
if errorlevel 1 goto :fail

if "%ME_VERIFY_ONLY%"=="1" (
    echo [2/9] VERIFY-ONLY: no restore, clean, build, test, harness, package creation, or deployment will run.
    call :verify_all_target_outputs
    if errorlevel 1 goto :fail
    call :verify_output_artifacts
    if errorlevel 1 goto :fail
    call :verify_performance_result
    if errorlevel 1 goto :fail
    echo.
    echo VERIFICATION SUCCEEDED. Existing Release outputs and package are valid.
    echo No files were changed.
    popd >nul
    exit /b 0
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet was not found on PATH.
    goto :fail
)

echo [2/9] Restoring exact dependencies with the repository NuGet configuration...
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

echo [3/9] Cleaning every Material Editor Release target and prior final artifacts...
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
if exist "%ME_PERF_RESULT%" del /f /q "%ME_PERF_RESULT%"
if errorlevel 1 (
    echo ERROR: Could not remove the prior three-panel performance result.
    goto :fail
)
if exist "%ME_OUTPUT_ZIP%" del /f /q "%ME_OUTPUT_ZIP%"
if errorlevel 1 (
    echo ERROR: Could not remove the prior KK release package.
    goto :fail
)

echo [4/9] Building MaterialEditor.API Release and running PublicApiAnalyzers...
dotnet build "%ME_API_PROJECT%" -c Release --no-restore --nologo
if errorlevel 1 goto :fail

echo [5/9] Running all metadata, regression, API, and structural UI tests...
dotnet run --project "%ME_METADATA_PROJECT%" -c Release --no-restore
if errorlevel 1 goto :fail

echo [6/9] Running the performance harness against the optimized pre-three-panel baseline...
dotnet run --project "%ME_PERF_PROJECT%" -c Release --no-restore -- --compare-baseline "%ME_PERF_BASELINE%" --json "%ME_PERF_RESULT%"
if errorlevel 1 goto :fail
call :verify_performance_result
if errorlevel 1 goto :fail

echo [7/9] Building all Material Editor targets from the shared sources...
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

echo [8/9] Verifying every shared-target assembly output...
call :verify_all_target_outputs
if errorlevel 1 goto :fail

echo [9/9] Verifying KK DLL, documentation XML, native library, and exact release ZIP...
call :verify_output_artifacts
if errorlevel 1 goto :fail

echo.
echo BUILD SUCCEEDED.
echo Runtime deployment inputs are limited to:
echo   %ME_REPO_ROOT%%ME_OUTPUT_DLL%
echo   %ME_REPO_ROOT%%ME_OUTPUT_NATIVE%
echo The generated XML is documentation and remains package-only:
echo   %ME_REPO_ROOT%%ME_OUTPUT_XML%
echo Verifiable release package:
echo   %ME_REPO_ROOT%%ME_OUTPUT_ZIP%
echo Performance comparison:
echo   %ME_REPO_ROOT%%ME_PERF_RESULT%
echo No game file, config, card, coordinate, scene, or shared dependency was copied.
popd >nul
exit /b 0

:require_inputs
call :require_file "%ME_NUGET_CONFIG%"
if errorlevel 1 (
    echo ERROR: The repository NuGet configuration is required for reproducible restores.
    exit /b 1
)
call :require_project "%ME_API_PROJECT%"
if errorlevel 1 exit /b 1
call :require_project "%ME_METADATA_PROJECT%"
if errorlevel 1 exit /b 1
call :require_project "%ME_PERF_PROJECT%"
if errorlevel 1 exit /b 1
call :require_project "%ME_AI_PROJECT%"
if errorlevel 1 exit /b 1
call :require_project "%ME_EC_PROJECT%"
if errorlevel 1 exit /b 1
call :require_project "%ME_HS2_PROJECT%"
if errorlevel 1 exit /b 1
call :require_project "%ME_KK_PROJECT%"
if errorlevel 1 exit /b 1
call :require_project "%ME_KKS_PROJECT%"
if errorlevel 1 exit /b 1
call :require_project "%ME_PH_PROJECT%"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_PERF_BASELINE%" "%ME_PERF_BASELINE_SHA%" "optimized pre-three-panel performance baseline"
exit /b %errorlevel%

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

:verify_performance_result
call :require_file "%ME_PERF_RESULT%"
if errorlevel 1 (
    echo ERROR: Missing three-panel performance result. A full clean build must generate it.
    exit /b 1
)
set "ME_PERF_RESULT_PATH=%ME_REPO_ROOT%%ME_PERF_RESULT%"
set "ME_PERF_BASELINE_PATH=%ME_REPO_ROOT%%ME_PERF_BASELINE%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $r=Get-Content -Raw -LiteralPath $env:ME_PERF_RESULT_PATH | ConvertFrom-Json; $b=Get-Content -Raw -LiteralPath $env:ME_PERF_BASELINE_PATH | ConvertFrom-Json; if($r.schemaVersion -ne 1) { throw 'Unexpected performance report schema.' }; if($r.semanticFingerprint -ne $b.semanticFingerprint) { throw 'Performance semantic fingerprint differs from the optimized pre-three-panel baseline.' }; $failed=@($r.invariants | Where-Object { -not $_.passed }); if($failed.Count -gt 0) { throw ('Failed performance invariant(s): ' + (($failed | ForEach-Object { $_.name }) -join ', ')) }; Write-Host ('  Verified three-panel performance report: ' + $env:ME_PERF_RESULT_PATH)"
exit /b %errorlevel%

:verify_all_target_outputs
call :verify_assembly_exact "bin\build\API.MaterialEditor\MaterialEditor.dll" "MaterialEditor" "1.0.0.0" "MaterialEditor API"
if errorlevel 1 exit /b 1
call :verify_assembly_exact "bin\build\AI.MaterialEditor\AI_MaterialEditor.dll" "AI_MaterialEditor" "4.0.3.0" "AI target"
if errorlevel 1 exit /b 1
call :verify_assembly_exact "bin\build\EC.MaterialEditor\EC_MaterialEditor.dll" "EC_MaterialEditor" "4.0.3.0" "EC target"
if errorlevel 1 exit /b 1
call :verify_assembly_exact "bin\build\HS2.MaterialEditor\HS2_MaterialEditor.dll" "HS2_MaterialEditor" "4.0.3.0" "HS2 target"
if errorlevel 1 exit /b 1
call :verify_assembly_exact "%ME_OUTPUT_DLL%" "%ME_EXPECTED_ASSEMBLY_NAME%" "%ME_EXPECTED_ASSEMBLY_VERSION%" "KK target"
if errorlevel 1 exit /b 1
call :verify_assembly_exact "bin\build\KKS.MaterialEditor\KKS_MaterialEditor.dll" "KKS_MaterialEditor" "4.0.3.0" "KKS target"
if errorlevel 1 exit /b 1
call :verify_assembly_exact "bin\build\PH.MaterialEditor\PH_MaterialEditor.dll" "PH_MaterialEditor" "4.0.3.0" "PH target"
exit /b %errorlevel%

:verify_output_artifacts
call :require_file "%ME_OUTPUT_DLL%"
if errorlevel 1 exit /b 1
call :require_file "%ME_OUTPUT_XML%"
if errorlevel 1 exit /b 1
call :require_file "%ME_OUTPUT_NATIVE%"
if errorlevel 1 exit /b 1
call :require_file "%ME_OUTPUT_ZIP%"
if errorlevel 1 exit /b 1
call :verify_assembly_exact "%ME_OUTPUT_DLL%" "%ME_EXPECTED_ASSEMBLY_NAME%" "%ME_EXPECTED_ASSEMBLY_VERSION%" "three-panel KK DLL"
if errorlevel 1 exit /b 1
call :verify_documentation_xml "%ME_OUTPUT_XML%"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_OUTPUT_NATIVE%" "%ME_EXPECTED_NATIVE_SHA%" "libwebp.lib"
if errorlevel 1 exit /b 1
call :verify_release_zip
if errorlevel 1 exit /b 1
call :get_hash "%ME_OUTPUT_DLL%" ME_OUTPUT_DLL_SHA
if errorlevel 1 exit /b 1
call :get_hash "%ME_OUTPUT_XML%" ME_OUTPUT_XML_SHA
if errorlevel 1 exit /b 1
call :get_hash "%ME_OUTPUT_ZIP%" ME_OUTPUT_ZIP_SHA
if errorlevel 1 exit /b 1
echo   Version:              %ME_EXPECTED_ASSEMBLY_VERSION%
echo   KK DLL SHA-256:       %ME_OUTPUT_DLL_SHA%
echo   Documentation SHA-256:%ME_OUTPUT_XML_SHA%
echo   Native SHA-256:       %ME_EXPECTED_NATIVE_SHA%
echo   Release ZIP SHA-256:  %ME_OUTPUT_ZIP_SHA%
exit /b 0

:verify_documentation_xml
set "ME_XML_PATH=%~1"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; [xml]$x=Get-Content -Raw -LiteralPath $env:ME_XML_PATH; if($x.doc.assembly.name -ne $env:ME_EXPECTED_ASSEMBLY_NAME) { throw ('Unexpected documentation assembly name: ' + $x.doc.assembly.name) }; Write-Host ('  Verified documentation XML: ' + $env:ME_XML_PATH)"
exit /b %errorlevel%

:verify_release_zip
set "ME_ZIP_PATH=%ME_REPO_ROOT%%ME_OUTPUT_ZIP%"
set "ME_ZIP_DLL_PATH=%ME_REPO_ROOT%%ME_OUTPUT_DLL%"
set "ME_ZIP_XML_PATH=%ME_REPO_ROOT%%ME_OUTPUT_XML%"
set "ME_ZIP_NATIVE_PATH=%ME_REPO_ROOT%%ME_OUTPUT_NATIVE%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; Add-Type -AssemblyName System.IO.Compression.FileSystem; $expected=[ordered]@{'BepInEx/plugins/KK_Plugins/KK_MaterialEditor.dll'=$env:ME_ZIP_DLL_PATH;'BepInEx/plugins/KK_Plugins/KK_MaterialEditor.xml'=$env:ME_ZIP_XML_PATH;'BepInEx/plugins/KK_Plugins/libwebp.lib'=$env:ME_ZIP_NATIVE_PATH}; $zip=[IO.Compression.ZipFile]::OpenRead($env:ME_ZIP_PATH); try { $files=@($zip.Entries | Where-Object { -not [String]::IsNullOrEmpty($_.Name) }); if($files.Count -ne $expected.Count) { throw ('Release ZIP contains ' + $files.Count + ' files; expected exactly ' + $expected.Count) }; $sha=[Security.Cryptography.SHA256]::Create(); try { foreach($pair in $expected.GetEnumerator()) { $entry=$files | Where-Object { $_.FullName -eq $pair.Key }; if($null -eq $entry) { throw ('Missing ZIP entry: ' + $pair.Key) }; $stream=$entry.Open(); try { $entryHash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','') } finally { $stream.Dispose() }; $fileHash=(Get-FileHash -Algorithm SHA256 -LiteralPath $pair.Value).Hash; if($entryHash -ne $fileHash) { throw ('ZIP entry hash mismatch: ' + $pair.Key) } } } finally { $sha.Dispose() }; Write-Host ('  Verified exact three-file release ZIP: ' + $env:ME_ZIP_PATH) } finally { $zip.Dispose() }"
exit /b %errorlevel%

:verify_assembly_exact
set "ME_ASSEMBLY_PATH=%~1"
set "ME_ASSEMBLY_NAME=%~2"
set "ME_ASSEMBLY_VERSION=%~3"
set "ME_ASSEMBLY_LABEL=%~4"
if not exist "%ME_ASSEMBLY_PATH%" (
    echo ERROR: Missing %ME_ASSEMBLY_LABEL% assembly: %ME_ASSEMBLY_PATH%
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $a=[Reflection.AssemblyName]::GetAssemblyName($env:ME_ASSEMBLY_PATH); if($a.Name -ne $env:ME_ASSEMBLY_NAME -or $a.Version.ToString() -ne $env:ME_ASSEMBLY_VERSION) { throw ('Unexpected ' + $env:ME_ASSEMBLY_LABEL + ' identity: ' + $a.FullName) }; Write-Host ('  Verified ' + $env:ME_ASSEMBLY_LABEL + ': ' + $a.FullName)"
exit /b %errorlevel%

:guard_game_processes
powershell -NoProfile -ExecutionPolicy Bypass -Command "$names=@('Koikatu','KoikatuVR','Koikatsu','KoikatsuVR','CharaStudio','Studio','StudioVR','StudioNEO','StudioNEOV2'); $running=@(Get-Process -ErrorAction SilentlyContinue | Where-Object { $names -contains $_.ProcessName }); if($running.Count -gt 0) { foreach($p in $running) { Write-Host ('  RUNNING: ' + $p.ProcessName + '.exe PID ' + $p.Id) }; exit 1 }"
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
echo BUILD OR VERIFICATION FAILED. Nothing was deployed to Koikatsu.
popd >nul
exit /b 1
