@echo off
setlocal EnableExtensions DisableDelayedExpansion

rem This deployment script is intentionally bound to the verified test install
rem and the immutable pre-change backup made for this task.
set "ME_GAME_ROOT=D:\Games\Koikatsu"
set "ME_BEPINEX_ROOT=%ME_GAME_ROOT%\BepInEx"
set "ME_PLUGIN_ROOT=%ME_BEPINEX_ROOT%\plugins"
set "ME_PLUGIN_DIR=%ME_PLUGIN_ROOT%\KK_Plugins"
set "ME_TARGET_DLL=%ME_PLUGIN_DIR%\KK_MaterialEditor.dll"
set "ME_TARGET_NATIVE=%ME_PLUGIN_DIR%\libwebp.lib"
set "ME_CONFIG=%ME_BEPINEX_ROOT%\config\com.deathweasel.bepinex.materialeditor.cfg"

set "ME_BACKUP_DIR=%ME_BEPINEX_ROOT%\PluginBackups\MaterialEditor\20260807-195335"
set "ME_BACKUP_INFO=%ME_BACKUP_DIR%\BACKUP_INFO.txt"
set "ME_BACKUP_DLL=%ME_BACKUP_DIR%\plugins\KK_Plugins\KK_MaterialEditor.dll"
set "ME_BACKUP_NATIVE=%ME_BACKUP_DIR%\plugins\KK_Plugins\libwebp.lib"
set "ME_BACKUP_CONFIG=%ME_BACKUP_DIR%\config\com.deathweasel.bepinex.materialeditor.cfg"
set "ME_BACKUP_DLL_SHA=0B9CC89E51D301F1DD4A8734A3F248D46AE320DCE9C2798D8E53F507A5CAC6C2"
set "ME_BACKUP_NATIVE_SHA=8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966"
set "ME_BACKUP_CONFIG_SHA=C1556C15501664762749B8FAC2DB3A50876952EF2CED61B2037FA20DD7789032"

set "ME_REPO_ROOT=%~dp0"
set "ME_BUILD_DIR=%ME_REPO_ROOT%bin\build\KK.MaterialEditor"
set "ME_BUILD_DLL=%ME_BUILD_DIR%\KK_MaterialEditor.dll"
set "ME_BUILD_NATIVE=%ME_BUILD_DIR%\libwebp.lib"

echo ================================================================
echo WARNING: Do not use this build together with the official Material Editor DLL.
echo ================================================================
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

echo [1/8] Checking that Koikatsu and CharaStudio are closed...
powershell -NoProfile -Command "$running=@(); foreach($n in @('Koikatu','KoikatuVR','Koikatsu','KoikatsuVR','CharaStudio')) { $running += @(Get-Process -Name $n -ErrorAction SilentlyContinue) }; if($running.Count -gt 0) { foreach($p in $running) { Write-Host ('RUNNING: ' + $p.ProcessName + '.exe PID ' + $p.Id) }; exit 1 }"
if errorlevel 1 (
    echo ERROR: Close Koikatsu and CharaStudio before deployment.
    goto :fail
)

echo [2/8] Verifying the immutable pre-change backup...
if not exist "%ME_BACKUP_INFO%" (
    echo ERROR: Missing backup record: %ME_BACKUP_INFO%
    goto :fail
)
call :verify_hash "%ME_BACKUP_DLL%" "%ME_BACKUP_DLL_SHA%" "backup KK_MaterialEditor.dll"
if errorlevel 1 goto :fail
call :verify_hash "%ME_BACKUP_NATIVE%" "%ME_BACKUP_NATIVE_SHA%" "backup libwebp.lib"
if errorlevel 1 goto :fail
call :verify_hash "%ME_BACKUP_CONFIG%" "%ME_BACKUP_CONFIG_SHA%" "backup Material Editor config"
if errorlevel 1 goto :fail

echo [3/8] Verifying the currently installed original...
if not exist "%ME_TARGET_DLL%" (
    echo ERROR: The installed original DLL is missing. Restore it first.
    goto :fail
)
if not exist "%ME_TARGET_NATIVE%" (
    echo ERROR: The installed original libwebp.lib is missing. Restore it first.
    goto :fail
)
call :verify_hash "%ME_TARGET_DLL%" "%ME_BACKUP_DLL_SHA%" "installed original KK_MaterialEditor.dll"
if errorlevel 1 (
    echo ERROR: The installed DLL is not the verified original.
    echo        Run restore-materialeditor-backup.bat before another deployment.
    goto :fail
)
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_BACKUP_NATIVE_SHA%" "installed original libwebp.lib"
if errorlevel 1 (
    echo ERROR: The installed native library is not the verified original.
    echo        Run restore-materialeditor-backup.bat before another deployment.
    goto :fail
)

powershell -NoProfile -Command "try { $a=[Reflection.AssemblyName]::GetAssemblyName($env:ME_TARGET_DLL); Write-Host ('Installed assembly before deploy: ' + $a.FullName) } catch { Write-Error $_; exit 1 }"
if errorlevel 1 goto :fail

echo [4/8] Checking for a second loadable Material Editor DLL...
call :check_duplicates
if errorlevel 1 goto :fail

echo [5/8] Verifying build artifacts...
if not exist "%ME_BUILD_DLL%" (
    echo ERROR: Missing build DLL. Run build-materialeditor-kk.bat first.
    goto :fail
)
if not exist "%ME_BUILD_NATIVE%" (
    echo ERROR: Missing build libwebp.lib. Run build-materialeditor-kk.bat first.
    goto :fail
)
powershell -NoProfile -Command "try { $a=[Reflection.AssemblyName]::GetAssemblyName($env:ME_BUILD_DLL); if($a.Name -ne 'KK_MaterialEditor') { Write-Error ('Unexpected build assembly: ' + $a.Name); exit 1 }; Write-Host ('Build assembly: ' + $a.FullName) } catch { Write-Error $_; exit 1 }"
if errorlevel 1 goto :fail
call :get_hash "%ME_BUILD_DLL%" ME_BUILD_DLL_SHA
if errorlevel 1 goto :fail
call :get_hash "%ME_BUILD_NATIVE%" ME_BUILD_NATIVE_SHA
if errorlevel 1 goto :fail
echo   Build DLL SHA-256:    %ME_BUILD_DLL_SHA%
echo   Build native SHA-256: %ME_BUILD_NATIVE_SHA%

if /I "%~1"=="--verify-only" (
    echo.
    echo VERIFICATION SUCCEEDED. No files were changed.
    echo Run this script without --verify-only to request deployment confirmation.
    exit /b 0
)

echo [6/8] Confirmation...
echo Only these two files will be replaced:
echo   %ME_TARGET_DLL%
echo   %ME_TARGET_NATIVE%
echo The config, cards, coordinates, scenes, BepInEx, KKAPI, Unity and all
echo unrelated plugins will not be copied, deleted, or modified.
if /I "%~1"=="--deploy-confirmed" (
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
echo [7/8] Copying the test build...
copy /b /y "%ME_BUILD_DLL%" "%ME_TARGET_DLL%" >nul
if errorlevel 1 goto :rollback
copy /b /y "%ME_BUILD_NATIVE%" "%ME_TARGET_NATIVE%" >nul
if errorlevel 1 goto :rollback

echo [8/8] Verifying deployed hashes and duplicate-DLL state...
call :verify_hash "%ME_TARGET_DLL%" "%ME_BUILD_DLL_SHA%" "deployed KK_MaterialEditor.dll"
if errorlevel 1 goto :rollback
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_BUILD_NATIVE_SHA%" "deployed libwebp.lib"
if errorlevel 1 goto :rollback
call :check_duplicates
if errorlevel 1 goto :rollback

echo.
echo DEPLOYMENT SUCCEEDED.
echo Deployed DLL SHA-256: %ME_BUILD_DLL_SHA%
echo Deployed native SHA-256: %ME_BUILD_NATIVE_SHA%
echo User configuration was not changed: %ME_CONFIG%
echo.
echo WARNING: Do not use this build together with the official Material Editor DLL.
exit /b 0

:check_duplicates
powershell -NoProfile -Command "$expected=[IO.Path]::GetFullPath($env:ME_TARGET_DLL); $extra=@(); if(Test-Path -LiteralPath $env:ME_PLUGIN_ROOT) { foreach($f in @(Get-ChildItem -LiteralPath $env:ME_PLUGIN_ROOT -Recurse -File -Filter '*MaterialEditor*.dll' -ErrorAction Stop)) { if(-not [String]::Equals([IO.Path]::GetFullPath($f.FullName),$expected,[StringComparison]::OrdinalIgnoreCase)) { $extra += $f.FullName } } }; if($extra.Count -gt 0) { Write-Host 'ERROR: Additional loadable Material Editor DLL(s):'; foreach($p in $extra) { Write-Host ('  ' + $p) }; exit 1 }"
exit /b %errorlevel%

:get_hash
set "ME_HASH_FILE=%~1"
set "ME_HASH_VALUE="
for /f "usebackq delims=" %%H in (`powershell -NoProfile -Command "try { (Get-FileHash -Algorithm SHA256 -LiteralPath $env:ME_HASH_FILE).Hash } catch { exit 1 }"`) do set "ME_HASH_VALUE=%%H"
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
echo Restoring only the DLL and libwebp.lib from the verified backup...
copy /b /y "%ME_BACKUP_DLL%" "%ME_TARGET_DLL%" >nul
if errorlevel 1 goto :rollback_failed
copy /b /y "%ME_BACKUP_NATIVE%" "%ME_TARGET_NATIVE%" >nul
if errorlevel 1 goto :rollback_failed
call :verify_hash "%ME_TARGET_DLL%" "%ME_BACKUP_DLL_SHA%" "rolled-back KK_MaterialEditor.dll"
if errorlevel 1 goto :rollback_failed
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_BACKUP_NATIVE_SHA%" "rolled-back libwebp.lib"
if errorlevel 1 goto :rollback_failed
echo Rollback succeeded. The verified original files are installed.
exit /b 1

:rollback_failed
echo CRITICAL: Automatic rollback could not be verified.
echo Close the game and run restore-materialeditor-backup.bat immediately.
exit /b 1

:fail
echo.
echo DEPLOYMENT ABORTED. No deployment was completed.
exit /b 1
