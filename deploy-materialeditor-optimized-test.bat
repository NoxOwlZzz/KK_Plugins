@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "ME_GAME_ROOT=D:\Games\Koikatsu"
set "ME_BEPINEX_ROOT=%ME_GAME_ROOT%\BepInEx"
set "ME_PLUGIN_ROOT=%ME_BEPINEX_ROOT%\plugins"
set "ME_PLUGIN_DIR=%ME_PLUGIN_ROOT%\KK_Plugins"
set "ME_TARGET_DLL=%ME_PLUGIN_DIR%\KK_MaterialEditor.dll"
set "ME_TARGET_NATIVE=%ME_PLUGIN_DIR%\libwebp.lib"

set "ME_BACKUP_DIR=%ME_BEPINEX_ROOT%\PluginBackups\MaterialEditor\20260808-0800-before-critical-optimization"
set "ME_BACKUP_INFO=%ME_BACKUP_DIR%\BACKUP_INFO.txt"
set "ME_BACKUP_DLL=%ME_BACKUP_DIR%\KK_MaterialEditor.dll"
set "ME_BACKUP_NATIVE=%ME_BACKUP_DIR%\libwebp.lib"
set "ME_BACKUP_CONFIG=%ME_BACKUP_DIR%\com.deathweasel.bepinex.materialeditor.cfg"
set "ME_BACKUP_INFO_SHA=747AC8C0DF40A175BE490E878B885F907856A25F0CF0B6969AC16E19703A240D"
set "ME_BACKUP_DLL_SHA=11CD1B91661E44B3620DA0FFE2D6FE20C2998F31791F6368A24E6E8F6CEEA550"
set "ME_BACKUP_NATIVE_SHA=8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966"
set "ME_BACKUP_CONFIG_SHA=CCE2D1A271BD796186B6FB74F519A44B935073107169FC1FC680C32343464C11"
set "ME_EXPECTED_ASSEMBLY_NAME=KK_MaterialEditor"
set "ME_EXPECTED_ASSEMBLY_VERSION=4.0.3.0"

set "ME_REPO_ROOT=%~dp0"
set "ME_BUILD_DIR=%ME_REPO_ROOT%bin\build\KK.MaterialEditor"
set "ME_BUILD_DLL=%ME_BUILD_DIR%\KK_MaterialEditor.dll"
set "ME_BUILD_NATIVE=%ME_BUILD_DIR%\libwebp.lib"
set "ME_VERIFY_ONLY=0"
set "ME_DEPLOY_CONFIRMED=0"

:parse_arguments
if "%~1"=="" goto :arguments_parsed
if /I "%~1"=="--verify-only" (
    set "ME_VERIFY_ONLY=1"
    shift
    goto :parse_arguments
)
if /I "%~1"=="--deploy-confirmed" (
    set "ME_DEPLOY_CONFIRMED=1"
    shift
    goto :parse_arguments
)
echo ERROR: Unknown argument: %~1
echo Usage: %~nx0 [--verify-only] [--deploy-confirmed]
exit /b 2

:arguments_parsed
if "%ME_VERIFY_ONLY%"=="1" if "%ME_DEPLOY_CONFIRMED%"=="1" (
    echo ERROR: --verify-only and --deploy-confirmed cannot be used together.
    exit /b 2
)

echo ================================================================
echo Do not install this optimized fork together with the official Material Editor DLL.
echo ================================================================
echo Material Editor optimized test deployment
echo Game:   %ME_GAME_ROOT%
echo Target: %ME_TARGET_DLL%
echo Backup: %ME_BACKUP_DIR%
echo.

if not exist "%ME_GAME_ROOT%\Koikatu.exe" (
    echo ERROR: The exact Koikatsu installation was not found.
    goto :fail
)
if not exist "%ME_PLUGIN_DIR%" (
    echo ERROR: The exact KK_Plugins directory was not found.
    goto :fail
)

echo [1/9] Checking that Koikatu, Koikatsu, Studio, and VR processes are closed...
call :guard_game_processes
if errorlevel 1 (
    echo ERROR: Close every Koikatsu game, Studio, and VR process before deployment.
    goto :fail
)

echo [2/9] Verifying the immutable functional backup before any copy...
call :verify_backup
if errorlevel 1 goto :fail

echo [3/9] Checking the loadable plugin tree for duplicate Material Editor DLLs...
call :check_duplicates
if errorlevel 1 goto :fail

echo [4/9] Inspecting the currently installed target...
if exist "%ME_TARGET_DLL%" (
    call :verify_assembly "%ME_TARGET_DLL%" "currently installed DLL"
    if errorlevel 1 (
        echo ERROR: Restore the verified functional backup before replacing an unknown assembly.
        goto :fail
    )
    call :get_hash "%ME_TARGET_DLL%" ME_INSTALLED_DLL_SHA
    if errorlevel 1 goto :fail
    call echo   Installed DLL SHA-256: %%ME_INSTALLED_DLL_SHA%%
) else (
    echo   The target DLL is absent; deployment would create it at the exact target path.
)
if exist "%ME_TARGET_NATIVE%" (
    call :get_hash "%ME_TARGET_NATIVE%" ME_INSTALLED_NATIVE_SHA
    if errorlevel 1 goto :fail
    call echo   Installed native SHA-256: %%ME_INSTALLED_NATIVE_SHA%%
) else (
    echo   The target libwebp.lib is absent; deployment would create it.
)

if "%ME_VERIFY_ONLY%"=="1" (
    echo [5/9] VERIFY-ONLY: optimized build skipped; no restore, clean, build, test, or copy will run.
) else (
    echo [5/9] Building and testing the optimized fork from a clean Release state...
    if not exist "%ME_REPO_ROOT%build-materialeditor-optimized.bat" (
        echo ERROR: Missing optimized build script:
        echo        %ME_REPO_ROOT%build-materialeditor-optimized.bat
        goto :fail
    )
    call "%ME_REPO_ROOT%build-materialeditor-optimized.bat" --clean
    if errorlevel 1 (
        echo ERROR: The optimized build or one of its mandatory gates failed.
        echo Nothing was copied to Koikatsu.
        goto :fail
    )
)

echo [6/9] Verifying optimized build artifacts...
if not exist "%ME_BUILD_DLL%" (
    echo ERROR: Missing build DLL. Run build-materialeditor-optimized.bat first.
    goto :fail
)
if not exist "%ME_BUILD_NATIVE%" (
    echo ERROR: Missing build libwebp.lib. Run build-materialeditor-optimized.bat first.
    goto :fail
)
call :verify_assembly "%ME_BUILD_DLL%" "optimized build DLL"
if errorlevel 1 goto :fail
call :verify_hash "%ME_BUILD_NATIVE%" "%ME_BACKUP_NATIVE_SHA%" "optimized build libwebp.lib"
if errorlevel 1 goto :fail
call :get_hash "%ME_BUILD_DLL%" ME_BUILD_DLL_SHA
if errorlevel 1 goto :fail
call :get_hash "%ME_BUILD_NATIVE%" ME_BUILD_NATIVE_SHA
if errorlevel 1 goto :fail
echo   Build DLL SHA-256:    %ME_BUILD_DLL_SHA%
echo   Build native SHA-256: %ME_BUILD_NATIVE_SHA%

if "%ME_VERIFY_ONLY%"=="1" (
    echo.
    echo VERIFICATION SUCCEEDED. No files were changed.
    echo Run this script without --verify-only to request deployment confirmation.
    exit /b 0
)

echo [7/9] Confirming the exact two-file replacement...
echo Only these two files will be copied:
echo   %ME_BUILD_DLL%
echo     to %ME_TARGET_DLL%
echo   %ME_BUILD_NATIVE%
echo     to %ME_TARGET_NATIVE%
echo No config, card, coordinate, scene, shared dependency, BepInEx file,
echo KKAPI file, Unity file, or unrelated plugin will be copied or deleted.
if "%ME_DEPLOY_CONFIRMED%"=="1" (
    echo Explicit --deploy-confirmed authorization received.
    goto :confirmed
)
set "ME_CONFIRM="
set /p "ME_CONFIRM=Type DEPLOY to continue: "
if /I not "%ME_CONFIRM%"=="DEPLOY" (
    echo Deployment cancelled. No files were changed.
    exit /b 2
)

:confirmed
echo [8/9] Rechecking game processes and copying only the optimized DLL and libwebp.lib...
call :guard_game_processes
if errorlevel 1 (
    echo ERROR: A Koikatsu game, Studio, or VR process started during the build.
    echo Nothing was copied to Koikatsu.
    goto :fail
)
copy /b /y "%ME_BUILD_DLL%" "%ME_TARGET_DLL%" >nul
if errorlevel 1 goto :rollback
copy /b /y "%ME_BUILD_NATIVE%" "%ME_TARGET_NATIVE%" >nul
if errorlevel 1 goto :rollback

echo [9/9] Verifying deployed hashes, assembly identity, and duplicate-DLL state...
call :verify_hash "%ME_TARGET_DLL%" "%ME_BUILD_DLL_SHA%" "deployed KK_MaterialEditor.dll"
if errorlevel 1 goto :rollback
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_BUILD_NATIVE_SHA%" "deployed libwebp.lib"
if errorlevel 1 goto :rollback
call :verify_assembly "%ME_TARGET_DLL%" "deployed DLL"
if errorlevel 1 goto :rollback
call :check_duplicates
if errorlevel 1 goto :rollback

echo.
echo DEPLOYMENT SUCCEEDED.
echo Deployed DLL SHA-256:    %ME_BUILD_DLL_SHA%
echo Deployed native SHA-256: %ME_BUILD_NATIVE_SHA%
echo The active Material Editor config was not changed.
echo Do not install this optimized fork together with the official Material Editor DLL.
exit /b 0

:guard_game_processes
powershell -NoProfile -ExecutionPolicy Bypass -Command "$names=@('Koikatu','KoikatuVR','Koikatsu','KoikatsuVR','CharaStudio','Studio','StudioVR','StudioNEO','StudioNEOV2'); $running=@(Get-Process -ErrorAction SilentlyContinue ^| Where-Object { $names -contains $_.ProcessName }); if($running.Count -gt 0) { foreach($p in $running) { Write-Host ('  RUNNING: ' + $p.ProcessName + '.exe PID ' + $p.Id) }; exit 1 }"
exit /b %errorlevel%

:verify_backup
if not exist "%ME_BACKUP_INFO%" (
    echo ERROR: Missing backup record: %ME_BACKUP_INFO%
    exit /b 1
)
call :verify_hash "%ME_BACKUP_INFO%" "%ME_BACKUP_INFO_SHA%" "functional backup BACKUP_INFO.txt"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_BACKUP_DLL%" "%ME_BACKUP_DLL_SHA%" "functional backup KK_MaterialEditor.dll"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_BACKUP_NATIVE%" "%ME_BACKUP_NATIVE_SHA%" "functional backup libwebp.lib"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_BACKUP_CONFIG%" "%ME_BACKUP_CONFIG_SHA%" "config safety snapshot - read-only and never restored"
if errorlevel 1 exit /b 1
call :verify_assembly "%ME_BACKUP_DLL%" "functional backup DLL"
exit /b %errorlevel%

:check_duplicates
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $expected=[IO.Path]::GetFullPath($env:ME_TARGET_DLL); $duplicates=New-Object Collections.Generic.List[string]; foreach($f in @(Get-ChildItem -LiteralPath $env:ME_PLUGIN_ROOT -Recurse -File -Filter '*.dll')) { $full=[IO.Path]::GetFullPath($f.FullName); if([String]::Equals($full,$expected,[StringComparison]::OrdinalIgnoreCase)) { continue }; $isDuplicate=$f.Name -in @('KK_MaterialEditor.dll','MaterialEditor.dll'); if(-not $isDuplicate) { try { $identity=[Reflection.AssemblyName]::GetAssemblyName($full); $isDuplicate=[String]::Equals($identity.Name,$env:ME_EXPECTED_ASSEMBLY_NAME,[StringComparison]::OrdinalIgnoreCase) } catch { $isDuplicate=$false } }; if($isDuplicate) { $duplicates.Add($full) } }; if($duplicates.Count -gt 0) { Write-Host 'ERROR: Additional loadable Material Editor DLL(s) detected:'; foreach($path in $duplicates) { Write-Host ('  ' + $path) }; exit 1 }; Write-Host ('  No duplicate ' + $env:ME_EXPECTED_ASSEMBLY_NAME + ' assembly was found.')"
exit /b %errorlevel%

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

:rollback
echo.
echo ERROR: Deployment verification failed after copying began.
echo Restoring only the DLL and libwebp.lib from the prevalidated functional backup...
copy /b /y "%ME_BACKUP_DLL%" "%ME_TARGET_DLL%" >nul
if errorlevel 1 goto :rollback_failed
copy /b /y "%ME_BACKUP_NATIVE%" "%ME_TARGET_NATIVE%" >nul
if errorlevel 1 goto :rollback_failed
call :verify_hash "%ME_TARGET_DLL%" "%ME_BACKUP_DLL_SHA%" "rolled-back KK_MaterialEditor.dll"
if errorlevel 1 goto :rollback_failed
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_BACKUP_NATIVE_SHA%" "rolled-back libwebp.lib"
if errorlevel 1 goto :rollback_failed
call :verify_assembly "%ME_TARGET_DLL%" "rolled-back functional DLL"
if errorlevel 1 goto :rollback_failed
echo Rollback succeeded. The verified functional files are installed.
echo The active config was not changed.
exit /b 1

:rollback_failed
echo CRITICAL: Automatic rollback could not be verified.
echo Close every game process and run restore-materialeditor-backup.bat immediately.
exit /b 1

:fail
echo.
echo DEPLOYMENT ABORTED. No deployment was completed.
exit /b 1
