@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "ME_GAME_ROOT=D:\Games\Koikatsu"
set "ME_BEPINEX_ROOT=%ME_GAME_ROOT%\BepInEx"
set "ME_PLUGIN_ROOT=%ME_BEPINEX_ROOT%\plugins"
set "ME_PLUGIN_DIR=%ME_PLUGIN_ROOT%\KK_Plugins"
set "ME_TARGET_DLL=%ME_PLUGIN_DIR%\KK_MaterialEditor.dll"
set "ME_TARGET_NATIVE=%ME_PLUGIN_DIR%\libwebp.lib"
set "ME_TARGET_XML=%ME_PLUGIN_DIR%\KK_MaterialEditor.xml"

set "ME_BACKUP_ROOT=%ME_BEPINEX_ROOT%\PluginBackups\MaterialEditor\three-panel-ui"
set "ME_BACKUP_POINTER=%ME_BACKUP_ROOT%\LATEST_BACKUP.txt"

set "ME_REPO_ROOT=%~dp0"
set "ME_BUILD_SCRIPT=%ME_REPO_ROOT%build-materialeditor-three-panel-ui.bat"
set "ME_BUILD_DIR=%ME_REPO_ROOT%bin\build\KK.MaterialEditor"
set "ME_BUILD_DLL=%ME_BUILD_DIR%\KK_MaterialEditor.dll"
set "ME_BUILD_XML=%ME_BUILD_DIR%\KK_MaterialEditor.xml"
set "ME_BUILD_NATIVE=%ME_BUILD_DIR%\libwebp.lib"
set "ME_BUILD_ZIP=%ME_REPO_ROOT%bin\out\KK_MaterialEditor_v4.0.3.zip"
set "ME_EXPECTED_ASSEMBLY_NAME=KK_MaterialEditor"
set "ME_EXPECTED_ASSEMBLY_VERSION=4.0.3.0"
set "ME_EXPECTED_NATIVE_SHA=8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966"
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
echo Do not install this Material Editor fork together with the official Material Editor DLL.
echo ================================================================
echo Material Editor three-panel UI test deployment
echo Game:       %ME_GAME_ROOT%
echo Target DLL: %ME_TARGET_DLL%
echo Backup root outside plugins:
echo   %ME_BACKUP_ROOT%
echo.

if not exist "%ME_GAME_ROOT%\Koikatu.exe" (
    echo ERROR: The exact Koikatsu installation was not found.
    goto :fail
)
if not exist "%ME_PLUGIN_DIR%" (
    echo ERROR: The exact KK_Plugins directory was not found.
    goto :fail
)
if not exist "%ME_BUILD_SCRIPT%" (
    echo ERROR: Missing three-panel build script:
    echo        %ME_BUILD_SCRIPT%
    goto :fail
)
call :verify_backup_location "%ME_BACKUP_ROOT%"
if errorlevel 1 goto :fail

echo [1/10] Checking that Koikatsu, CharaStudio, Studio, and VR processes are closed...
call :guard_game_processes
if errorlevel 1 (
    echo ERROR: Close every Koikatsu game, Studio, and VR process before deployment.
    goto :fail
)

echo [2/10] Checking the loadable plugin tree for duplicate Material Editor DLLs...
call :check_duplicates
if errorlevel 1 goto :fail

echo [3/10] Verifying the exact currently installed build that will be backed up...
if not exist "%ME_TARGET_DLL%" (
    echo ERROR: The installed KK_MaterialEditor.dll is missing; there is no exact prior build to back up.
    goto :fail
)
if not exist "%ME_TARGET_NATIVE%" (
    echo ERROR: The installed libwebp.lib is missing; there is no exact prior runtime state to back up.
    goto :fail
)
call :verify_assembly "%ME_TARGET_DLL%" "currently installed DLL"
if errorlevel 1 goto :fail
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_EXPECTED_NATIVE_SHA%" "currently installed libwebp.lib"
if errorlevel 1 goto :fail
call :get_hash "%ME_TARGET_DLL%" ME_INSTALLED_DLL_SHA
if errorlevel 1 goto :fail
call :get_hash "%ME_TARGET_NATIVE%" ME_INSTALLED_NATIVE_SHA
if errorlevel 1 goto :fail
call :get_file_version "%ME_TARGET_DLL%" ME_INSTALLED_FILE_VERSION
if errorlevel 1 goto :fail
echo   Installed version:           %ME_EXPECTED_ASSEMBLY_VERSION%
echo   Installed file version:      %ME_INSTALLED_FILE_VERSION%
echo   Installed DLL SHA-256:       %ME_INSTALLED_DLL_SHA%
echo   Installed libwebp SHA-256:   %ME_INSTALLED_NATIVE_SHA%
if exist "%ME_TARGET_XML%" (
    echo   Installed XML is present but is documentation-only and will not be changed.
) else (
    echo   Installed XML is absent. Deployment will not add documentation to the runtime folder.
)

if "%ME_VERIFY_ONLY%"=="1" (
    echo [4/10] VERIFY-ONLY: clean build, tests, harness, backup creation, and deployment are skipped.
) else (
    echo [4/10] Running the mandatory clean Release build and every gate...
    call "%ME_BUILD_SCRIPT%"
    if errorlevel 1 (
        echo ERROR: The clean build or one of its mandatory gates failed.
        echo Nothing was copied to Koikatsu.
        goto :fail
    )
)

echo [5/10] Verifying Release outputs and the exact DLL/XML/libwebp package...
call "%ME_BUILD_SCRIPT%" --verify-only
if errorlevel 1 (
    echo ERROR: Release artifact verification failed.
    goto :fail
)
call :verify_assembly "%ME_BUILD_DLL%" "three-panel build DLL"
if errorlevel 1 goto :fail
call :verify_hash "%ME_BUILD_NATIVE%" "%ME_EXPECTED_NATIVE_SHA%" "three-panel build libwebp.lib"
if errorlevel 1 goto :fail
call :get_hash "%ME_BUILD_DLL%" ME_BUILD_DLL_SHA
if errorlevel 1 goto :fail
call :get_hash "%ME_BUILD_NATIVE%" ME_BUILD_NATIVE_SHA
if errorlevel 1 goto :fail
call :get_hash "%ME_BUILD_XML%" ME_BUILD_XML_SHA
if errorlevel 1 goto :fail
call :get_hash "%ME_BUILD_ZIP%" ME_BUILD_ZIP_SHA
if errorlevel 1 goto :fail
echo   Build version:               %ME_EXPECTED_ASSEMBLY_VERSION%
echo   Build DLL SHA-256:           %ME_BUILD_DLL_SHA%
echo   Package XML SHA-256:         %ME_BUILD_XML_SHA%
echo   Build libwebp SHA-256:       %ME_BUILD_NATIVE_SHA%
echo   Release ZIP SHA-256:         %ME_BUILD_ZIP_SHA%

echo [6/10] Inspecting the last validated deployment backup pointer...
call :describe_latest_backup
if errorlevel 1 goto :fail

if "%ME_VERIFY_ONLY%"=="1" (
    echo.
    echo VERIFICATION SUCCEEDED. No build, backup, copy, delete, config, or data change occurred.
    echo A full deployment will create a new timestamped backup immediately before copying.
    echo Run this script without --verify-only to request deployment confirmation.
    exit /b 0
)

echo [7/10] Confirming the exact two-file runtime replacement...
echo Only these runtime files will be overwritten:
echo   %ME_BUILD_DLL%
echo     to %ME_TARGET_DLL%
echo   %ME_BUILD_NATIVE%
echo     to %ME_TARGET_NATIVE%
echo The verified XML remains inside the release ZIP and is not required at runtime.
echo No config, card, coordinate, scene, shared dependency, BepInEx file,
echo KKAPI file, Unity file, data file, or unrelated plugin will be copied or deleted.
if "%ME_DEPLOY_CONFIRMED%"=="1" (
    echo Explicit --deploy-confirmed authorization received.
    goto :confirmed
)
set "ME_CONFIRM="
set /p "ME_CONFIRM=Type DEPLOY to continue: "
if /I not "%ME_CONFIRM%"=="DEPLOY" (
    echo Deployment cancelled. No game files or backups were changed.
    exit /b 2
)

:confirmed
echo [8/10] Rechecking processes, duplicate DLLs, and the unchanged installed source...
call :guard_game_processes
if errorlevel 1 (
    echo ERROR: A Koikatsu game, Studio, or VR process started during the build.
    echo Nothing was copied to Koikatsu.
    goto :fail
)
call :check_duplicates
if errorlevel 1 goto :fail
call :verify_hash "%ME_TARGET_DLL%" "%ME_INSTALLED_DLL_SHA%" "unchanged installed DLL before backup"
if errorlevel 1 goto :fail
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_INSTALLED_NATIVE_SHA%" "unchanged installed libwebp before backup"
if errorlevel 1 goto :fail

echo [9/10] Creating and verifying an additional timestamped backup outside plugins...
call :create_deployment_backup
if errorlevel 1 goto :fail

echo [10/10] Replacing only the prior DLL/libwebp build and verifying the deployed state...
copy /b /y "%ME_BUILD_DLL%" "%ME_TARGET_DLL%" >nul
if errorlevel 1 goto :rollback
copy /b /y "%ME_BUILD_NATIVE%" "%ME_TARGET_NATIVE%" >nul
if errorlevel 1 goto :rollback
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
echo Version:                    %ME_EXPECTED_ASSEMBLY_VERSION%
echo DLL path:                   %ME_TARGET_DLL%
echo DLL SHA-256:                %ME_BUILD_DLL_SHA%
echo libwebp path:               %ME_TARGET_NATIVE%
echo libwebp SHA-256:            %ME_BUILD_NATIVE_SHA%
echo Release ZIP:                %ME_BUILD_ZIP%
echo Release ZIP SHA-256:        %ME_BUILD_ZIP_SHA%
echo Immediate pre-copy backup:  %ME_DEPLOY_BACKUP_DIR%
echo Backup manifest SHA-256:    %ME_DEPLOY_BACKUP_INFO_SHA%
echo The active Material Editor config and every user-data file were untouched.
echo Do not install this Material Editor fork together with the official Material Editor DLL.
exit /b 0

:create_deployment_backup
set "ME_BACKUP_TIMESTAMP="
for /f "usebackq delims=" %%T in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-Date -Format 'yyyyMMdd-HHmmss'"`) do set "ME_BACKUP_TIMESTAMP=%%T"
if not defined ME_BACKUP_TIMESTAMP (
    echo ERROR: Could not generate a backup timestamp.
    exit /b 1
)
set "ME_DEPLOY_BACKUP_DIR=%ME_BACKUP_ROOT%\%ME_BACKUP_TIMESTAMP%-before-three-panel-ui"
set "ME_DEPLOY_BACKUP_DLL=%ME_DEPLOY_BACKUP_DIR%\KK_MaterialEditor.dll"
set "ME_DEPLOY_BACKUP_NATIVE=%ME_DEPLOY_BACKUP_DIR%\libwebp.lib"
set "ME_DEPLOY_BACKUP_XML=%ME_DEPLOY_BACKUP_DIR%\KK_MaterialEditor.xml"
set "ME_DEPLOY_BACKUP_INFO=%ME_DEPLOY_BACKUP_DIR%\BACKUP_INFO.txt"
set "ME_DEPLOY_BACKUP_INFO_SHA_FILE=%ME_DEPLOY_BACKUP_DIR%\BACKUP_INFO.sha256"
call :verify_backup_location "%ME_DEPLOY_BACKUP_DIR%"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; New-Item -ItemType Directory -Path $env:ME_BACKUP_ROOT -Force | Out-Null; if(Test-Path -LiteralPath $env:ME_DEPLOY_BACKUP_DIR) { throw ('Backup directory already exists: ' + $env:ME_DEPLOY_BACKUP_DIR) }; New-Item -ItemType Directory -Path $env:ME_DEPLOY_BACKUP_DIR | Out-Null"
if errorlevel 1 exit /b 1
copy /b /y "%ME_TARGET_DLL%" "%ME_DEPLOY_BACKUP_DLL%" >nul
if errorlevel 1 exit /b 1
copy /b /y "%ME_TARGET_NATIVE%" "%ME_DEPLOY_BACKUP_NATIVE%" >nul
if errorlevel 1 exit /b 1
call :verify_hash "%ME_DEPLOY_BACKUP_DLL%" "%ME_INSTALLED_DLL_SHA%" "backup KK_MaterialEditor.dll"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_DEPLOY_BACKUP_NATIVE%" "%ME_INSTALLED_NATIVE_SHA%" "backup libwebp.lib"
if errorlevel 1 exit /b 1
call :verify_assembly "%ME_DEPLOY_BACKUP_DLL%" "backup DLL"
if errorlevel 1 exit /b 1
call :verify_file_version "%ME_DEPLOY_BACKUP_DLL%" "%ME_INSTALLED_FILE_VERSION%" "backup DLL"
if errorlevel 1 exit /b 1
call :backup_optional_documentation_xml
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $lines=@('Format=MaterialEditorThreePanelUiBackupV1',('BackupDirectory=' + $env:ME_DEPLOY_BACKUP_DIR),('CreatedUtc=' + [DateTime]::UtcNow.ToString('o')),('SourceDll=' + $env:ME_TARGET_DLL),('SourceNative=' + $env:ME_TARGET_NATIVE),('DllSha256=' + $env:ME_INSTALLED_DLL_SHA),('NativeSha256=' + $env:ME_INSTALLED_NATIVE_SHA),('AssemblyName=' + $env:ME_EXPECTED_ASSEMBLY_NAME),('AssemblyVersion=' + $env:ME_EXPECTED_ASSEMBLY_VERSION),('FileVersion=' + $env:ME_INSTALLED_FILE_VERSION),'RuntimeFileCount=2',('DocumentationXmlCopied=' + $env:ME_BACKUP_XML_COPIED),('XmlSha256=' + $env:ME_BACKUP_XML_SHA),'ConfigCopied=False','DataCopied=False'); [IO.File]::WriteAllLines($env:ME_DEPLOY_BACKUP_INFO,$lines,[Text.Encoding]::ASCII)"
if errorlevel 1 exit /b 1
call :get_hash "%ME_DEPLOY_BACKUP_INFO%" ME_DEPLOY_BACKUP_INFO_SHA
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; [IO.File]::WriteAllText($env:ME_DEPLOY_BACKUP_INFO_SHA_FILE,($env:ME_DEPLOY_BACKUP_INFO_SHA + [Environment]::NewLine),[Text.Encoding]::ASCII); $pointerLines=@('Format=MaterialEditorThreePanelUiPointerV1',('BackupDirectory=' + $env:ME_DEPLOY_BACKUP_DIR),('BackupInfoSha256=' + $env:ME_DEPLOY_BACKUP_INFO_SHA)); $temp=$env:ME_BACKUP_POINTER + '.tmp'; [IO.File]::WriteAllLines($temp,$pointerLines,[Text.Encoding]::ASCII); Move-Item -LiteralPath $temp -Destination $env:ME_BACKUP_POINTER -Force"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_DEPLOY_BACKUP_INFO%" "%ME_DEPLOY_BACKUP_INFO_SHA%" "backup manifest"
if errorlevel 1 exit /b 1
echo   Created immediate backup: %ME_DEPLOY_BACKUP_DIR%
echo   Backup manifest SHA-256:  %ME_DEPLOY_BACKUP_INFO_SHA%
echo   Backup DLL file version:  %ME_INSTALLED_FILE_VERSION%
echo   Documentation XML copied: %ME_BACKUP_XML_COPIED%
if /I "%ME_BACKUP_XML_COPIED%"=="True" echo   Documentation XML SHA:    %ME_BACKUP_XML_SHA%
exit /b 0

:backup_optional_documentation_xml
set "ME_BACKUP_XML_COPIED=False"
set "ME_BACKUP_XML_SHA=NONE"
if not exist "%ME_TARGET_XML%" exit /b 0
call :get_hash "%ME_TARGET_XML%" ME_INSTALLED_XML_SHA
if errorlevel 1 exit /b 1
copy /b /y "%ME_TARGET_XML%" "%ME_DEPLOY_BACKUP_XML%" >nul
if errorlevel 1 exit /b 1
call :verify_hash "%ME_DEPLOY_BACKUP_XML%" "%ME_INSTALLED_XML_SHA%" "backup KK_MaterialEditor.xml"
if errorlevel 1 exit /b 1
set "ME_BACKUP_XML_SHA=%ME_INSTALLED_XML_SHA%"
set "ME_BACKUP_XML_COPIED=True"
exit /b 0

:describe_latest_backup
if not exist "%ME_BACKUP_POINTER%" (
    echo   No three-panel deployment pointer exists yet.
    echo   A full deployment will create it only after confirmation and final guards.
    exit /b 0
)
set "ME_POINTER_PATH=%ME_BACKUP_POINTER%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $data=@{}; foreach($line in Get-Content -LiteralPath $env:ME_POINTER_PATH) { $i=$line.IndexOf('='); if($i -gt 0) { $data[$line.Substring(0,$i)]=$line.Substring($i+1) } }; if($data.Format -ne 'MaterialEditorThreePanelUiPointerV1') { throw 'Unexpected latest-backup pointer format.' }; $dir=[IO.Path]::GetFullPath($data.BackupDirectory); $root=[IO.Path]::GetFullPath($env:ME_BACKUP_ROOT).TrimEnd('\') + '\'; $plugins=[IO.Path]::GetFullPath($env:ME_PLUGIN_ROOT).TrimEnd('\') + '\'; if(-not $dir.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) { throw 'Latest backup is outside the approved backup root.' }; if($dir.StartsWith($plugins,[StringComparison]::OrdinalIgnoreCase)) { throw 'Latest backup is inside the loadable plugin tree.' }; $info=Join-Path $dir 'BACKUP_INFO.txt'; if(-not (Test-Path -LiteralPath $info)) { throw ('Missing latest backup manifest: ' + $info) }; $actual=(Get-FileHash -Algorithm SHA256 -LiteralPath $info).Hash; if($actual -ne $data.BackupInfoSha256) { throw 'Latest backup manifest hash mismatch.' }; Write-Host ('  Latest validated pointer: ' + $dir); Write-Host ('  Manifest SHA-256:       ' + $actual)"
exit /b %errorlevel%

:verify_backup_location
set "ME_BACKUP_PATH_TO_VALIDATE=%~1"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $path=[IO.Path]::GetFullPath($env:ME_BACKUP_PATH_TO_VALIDATE).TrimEnd('\'); $root=[IO.Path]::GetFullPath($env:ME_BACKUP_ROOT).TrimEnd('\'); $plugins=[IO.Path]::GetFullPath($env:ME_PLUGIN_ROOT).TrimEnd('\'); if($path -ne $root -and -not $path.StartsWith(($root + '\'),[StringComparison]::OrdinalIgnoreCase)) { throw ('Backup path is outside the approved root: ' + $path) }; if($path -eq $plugins -or $path.StartsWith(($plugins + '\'),[StringComparison]::OrdinalIgnoreCase)) { throw ('Backup path is inside the loadable plugin tree: ' + $path) }; Write-Host ('  Safe non-loadable backup path: ' + $path)"
exit /b %errorlevel%

:guard_game_processes
powershell -NoProfile -ExecutionPolicy Bypass -Command "$names=@('Koikatu','KoikatuVR','Koikatsu','KoikatsuVR','CharaStudio','Studio','StudioVR','StudioNEO','StudioNEOV2'); $running=@(Get-Process -ErrorAction SilentlyContinue | Where-Object { $names -contains $_.ProcessName }); if($running.Count -gt 0) { foreach($p in $running) { Write-Host ('  RUNNING: ' + $p.ProcessName + '.exe PID ' + $p.Id) }; exit 1 }"
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

:get_file_version
set "ME_VERSION_FILE=%~1"
set "ME_FILE_VERSION_VALUE="
for /f "usebackq delims=" %%V in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $v=[Diagnostics.FileVersionInfo]::GetVersionInfo($env:ME_VERSION_FILE).FileVersion; if([String]::IsNullOrWhiteSpace($v)) { throw ('Missing FileVersion: ' + $env:ME_VERSION_FILE) }; $v"`) do set "ME_FILE_VERSION_VALUE=%%V"
if not defined ME_FILE_VERSION_VALUE (
    echo ERROR: Could not read FileVersion from %~1
    exit /b 1
)
set "%~2=%ME_FILE_VERSION_VALUE%"
exit /b 0

:verify_file_version
call :get_file_version "%~1" ME_ACTUAL_FILE_VERSION
if errorlevel 1 exit /b 1
if /I not "%ME_ACTUAL_FILE_VERSION%"=="%~2" (
    echo ERROR: FileVersion mismatch for %~3
    echo   Expected: %~2
    echo   Actual:   %ME_ACTUAL_FILE_VERSION%
    exit /b 1
)
echo   Verified %~3 FileVersion: %ME_ACTUAL_FILE_VERSION%
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
echo Restoring only the DLL and libwebp.lib from the immediate pre-copy backup...
copy /b /y "%ME_DEPLOY_BACKUP_DLL%" "%ME_TARGET_DLL%" >nul
if errorlevel 1 goto :rollback_failed
copy /b /y "%ME_DEPLOY_BACKUP_NATIVE%" "%ME_TARGET_NATIVE%" >nul
if errorlevel 1 goto :rollback_failed
call :verify_hash "%ME_TARGET_DLL%" "%ME_INSTALLED_DLL_SHA%" "rolled-back KK_MaterialEditor.dll"
if errorlevel 1 goto :rollback_failed
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_INSTALLED_NATIVE_SHA%" "rolled-back libwebp.lib"
if errorlevel 1 goto :rollback_failed
call :verify_assembly "%ME_TARGET_DLL%" "rolled-back DLL"
if errorlevel 1 goto :rollback_failed
call :check_duplicates
if errorlevel 1 goto :rollback_failed
echo Rollback succeeded. The exact immediate pre-copy runtime files are installed.
echo Backup retained at: %ME_DEPLOY_BACKUP_DIR%
echo Config and user data were untouched.
exit /b 1

:rollback_failed
echo CRITICAL: Automatic rollback could not be verified.
echo Keep every game process closed and run restore-materialeditor-backup.bat --verify-only.
echo Immediate backup: %ME_DEPLOY_BACKUP_DIR%
exit /b 1

:fail
echo.
echo DEPLOYMENT ABORTED. No deployment was completed.
exit /b 1
