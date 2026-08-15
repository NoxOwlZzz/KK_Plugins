@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "ME_GAME_ROOT=D:\Games\Koikatsu"
set "ME_BEPINEX_ROOT=%ME_GAME_ROOT%\BepInEx"
set "ME_PLUGIN_ROOT=%ME_BEPINEX_ROOT%\plugins"
set "ME_PLUGIN_DIR=%ME_PLUGIN_ROOT%\KK_Plugins"
set "ME_TARGET_DLL=%ME_PLUGIN_DIR%\KK_MaterialEditor.dll"
set "ME_TARGET_NATIVE=%ME_PLUGIN_DIR%\libwebp.lib"
set "ME_TARGET_XML=%ME_PLUGIN_DIR%\KK_MaterialEditor.xml"

set "ME_BACKUP_ROOT=%ME_BEPINEX_ROOT%\PluginBackups\MaterialEditor"
set "ME_BACKUP_POINTER=%ME_BACKUP_ROOT%\LATEST_UI_FIXES_BACKUP.txt"
set "ME_RESTORE_ROLLBACK_ROOT=%ME_BACKUP_ROOT%\RestoreRollback"
set "ME_FALLBACK_BACKUP_DIR=%ME_BEPINEX_ROOT%\PluginBackups\MaterialEditor\20260808-210636-before-optimized-sync"
set "ME_FALLBACK_BACKUP_INFO_SHA=CC213B60D3545E8590BA64100719F5C06B0B5B893FA0B66702AD7D762B027B64"
set "ME_FALLBACK_BACKUP_DLL_SHA=F3C4840AC92A376AC799FC30FAA10B33C20ABB7DB47545AE8FFD170DD3E8710A"
set "ME_FALLBACK_BACKUP_NATIVE_SHA=8931C0A14F6740109E692F46097B42470FF65E2B28088FC8D4CF31EEDC893966"

set "ME_EXPECTED_ASSEMBLY_NAME=KK_MaterialEditor"
set "ME_EXPECTED_ASSEMBLY_VERSION=4.0.3.0"
set "ME_VERIFY_ONLY=0"
set "ME_RESTORE_CONFIRMED=0"
set "ME_REQUESTED_BACKUP_DIR="

:parse_arguments
if "%~1"=="" goto :arguments_parsed
if /I "%~1"=="--verify-only" (
    set "ME_VERIFY_ONLY=1"
    shift
    goto :parse_arguments
)
if /I "%~1"=="--restore-confirmed" (
    set "ME_RESTORE_CONFIRMED=1"
    shift
    goto :parse_arguments
)
if /I "%~1"=="--backup" (
    if "%~2"=="" (
        echo ERROR: --backup requires an absolute backup directory.
        exit /b 2
    )
    set "ME_REQUESTED_BACKUP_DIR=%~2"
    shift
    shift
    goto :parse_arguments
)
echo ERROR: Unknown argument: %~1
echo Usage: %~nx0 [--verify-only] [--restore-confirmed] [--backup "absolute backup directory"]
exit /b 2

:arguments_parsed
if "%ME_VERIFY_ONLY%"=="1" if "%ME_RESTORE_CONFIRMED%"=="1" (
    echo ERROR: --verify-only and --restore-confirmed cannot be used together.
    exit /b 2
)

echo ================================================================
echo Do not install this Material Editor fork together with the official Material Editor DLL.
echo ================================================================
echo Material Editor UI-fixes exact-backup restore
echo Game:   %ME_GAME_ROOT%
echo Target: %ME_TARGET_DLL%
echo.

if not exist "%ME_GAME_ROOT%\Koikatu.exe" (
    echo ERROR: The exact Koikatsu installation was not found.
    goto :fail
)
if not exist "%ME_PLUGIN_DIR%" (
    echo ERROR: The exact KK_Plugins directory was not found.
    goto :fail
)

echo [1/6] Checking that Koikatsu, CharaStudio, Studio, and VR processes are closed...
call :guard_game_processes
if errorlevel 1 (
    echo ERROR: Close every Koikatsu game, Studio, and VR process before restore.
    goto :fail
)

echo [2/6] Selecting and cryptographically verifying one exact non-loadable backup...
call :select_backup
if errorlevel 1 goto :fail
echo   Selected source:      %ME_ACTIVE_BACKUP_SOURCE%
echo   Backup directory:    %ME_ACTIVE_BACKUP_DIR%
echo   Backup manifest SHA: %ME_ACTIVE_BACKUP_INFO_SHA%
echo   Backup DLL SHA-256:  %ME_ACTIVE_BACKUP_DLL_SHA%
echo   Backup native SHA:   %ME_ACTIVE_BACKUP_NATIVE_SHA%
echo   Backup file version: %ME_ACTIVE_BACKUP_FILE_VERSION%
echo   Backup XML copied:   %ME_ACTIVE_BACKUP_XML_COPIED%
if /I "%ME_ACTIVE_BACKUP_XML_COPIED%"=="True" echo   Backup XML SHA-256:  %ME_ACTIVE_BACKUP_XML_SHA%

echo [3/6] Checking the loadable plugin tree for duplicate Material Editor DLLs...
call :check_duplicates
if errorlevel 1 (
    echo ERROR: Restore never deletes another DLL. Remove or relocate the reported
    echo        duplicate outside BepInEx\plugins, then verify again.
    goto :fail
)

echo [4/6] Inspecting the current exact runtime targets without changing them...
set "ME_CURRENT_DLL_PRESENT=False"
set "ME_CURRENT_DLL_SHA=NONE"
if exist "%ME_TARGET_DLL%" (
    call :get_hash "%ME_TARGET_DLL%" ME_CURRENT_DLL_SHA
    if errorlevel 1 goto :fail
    set "ME_CURRENT_DLL_PRESENT=True"
    call echo   Current DLL SHA-256: %%ME_CURRENT_DLL_SHA%%
    call :describe_assembly "%ME_TARGET_DLL%"
) else (
    echo   Current DLL is absent; restore would recreate it.
)
set "ME_CURRENT_NATIVE_PRESENT=False"
set "ME_CURRENT_NATIVE_SHA=NONE"
if exist "%ME_TARGET_NATIVE%" (
    call :get_hash "%ME_TARGET_NATIVE%" ME_CURRENT_NATIVE_SHA
    if errorlevel 1 goto :fail
    set "ME_CURRENT_NATIVE_PRESENT=True"
    call echo   Current native SHA-256: %%ME_CURRENT_NATIVE_SHA%%
) else (
    echo   Current libwebp.lib is absent; restore would recreate it.
)
call :capture_runtime_xml_state
if errorlevel 1 goto :fail
echo   Current documentation XML present: %ME_CURRENT_XML_PRESENT%
if /I "%ME_CURRENT_XML_PRESENT%"=="True" echo   Current XML SHA-256: %ME_CURRENT_XML_SHA%

if "%ME_VERIFY_ONLY%"=="1" (
    echo.
    echo VERIFICATION SUCCEEDED. The selected backup is intact and no files were changed.
    echo Run without --verify-only to request restore confirmation.
    echo To select a specific validated backup, pass --backup "absolute directory".
    exit /b 0
)

echo [5/6] Confirming the exact two-file restore...
echo Only these files will be copied from the selected verified backup:
echo   %ME_ACTIVE_BACKUP_DLL%
echo     to %ME_TARGET_DLL%
echo   %ME_ACTIVE_BACKUP_NATIVE%
echo     to %ME_TARGET_NATIVE%
echo Documentation XML, active config, cards, coordinates, scenes, data,
echo shared dependencies, BepInEx files, and unrelated plugins are untouched.
if "%ME_RESTORE_CONFIRMED%"=="1" (
    echo Explicit --restore-confirmed authorization received.
    goto :confirmed
)
set "ME_CONFIRM="
set /p "ME_CONFIRM=Type RESTORE to continue: "
if /I not "%ME_CONFIRM%"=="RESTORE" (
    echo Restore cancelled. No files were changed.
    exit /b 2
)

:confirmed
call :guard_game_processes
if errorlevel 1 (
    echo ERROR: A Koikatsu game, Studio, or VR process started before restore.
    goto :fail
)
call :check_duplicates
if errorlevel 1 goto :fail
call :verify_current_runtime_unchanged
if errorlevel 1 goto :fail
call :stage_pre_restore_state
if errorlevel 1 goto :fail
call :guard_game_processes
if errorlevel 1 goto :fail
call :verify_current_runtime_unchanged
if errorlevel 1 goto :fail
copy /b /y "%ME_ACTIVE_BACKUP_DLL%" "%ME_TARGET_DLL%" >nul
if errorlevel 1 goto :copy_failed
copy /b /y "%ME_ACTIVE_BACKUP_NATIVE%" "%ME_TARGET_NATIVE%" >nul
if errorlevel 1 goto :copy_failed

echo [6/6] Verifying restored hashes, assembly identity, and duplicate-DLL state...
call :verify_hash "%ME_TARGET_DLL%" "%ME_ACTIVE_BACKUP_DLL_SHA%" "restored KK_MaterialEditor.dll"
if errorlevel 1 goto :verification_failed
call :verify_hash "%ME_TARGET_NATIVE%" "%ME_ACTIVE_BACKUP_NATIVE_SHA%" "restored libwebp.lib"
if errorlevel 1 goto :verification_failed
call :verify_assembly "%ME_TARGET_DLL%" "restored DLL"
if errorlevel 1 goto :verification_failed
call :verify_runtime_xml_unchanged
if errorlevel 1 goto :verification_failed
call :check_duplicates
if errorlevel 1 goto :verification_failed

echo.
echo RESTORE SUCCEEDED.
echo Version:                 %ME_EXPECTED_ASSEMBLY_VERSION%
echo DLL path:                %ME_TARGET_DLL%
echo DLL SHA-256:             %ME_ACTIVE_BACKUP_DLL_SHA%
echo libwebp path:            %ME_TARGET_NATIVE%
echo libwebp SHA-256:         %ME_ACTIVE_BACKUP_NATIVE_SHA%
echo Exact backup restored:   %ME_ACTIVE_BACKUP_DIR%
echo Backup manifest SHA-256: %ME_ACTIVE_BACKUP_INFO_SHA%
echo Pre-restore rollback:     %ME_RESTORE_ROLLBACK_DIR%
echo Rollback manifest SHA:    %ME_RESTORE_ROLLBACK_INFO_SHA%
echo Documentation XML, config, and every user-data file were untouched.
echo Do not install this Material Editor fork together with the official Material Editor DLL.
exit /b 0

:select_backup
if defined ME_REQUESTED_BACKUP_DIR (
    call :load_requested_backup "%ME_REQUESTED_BACKUP_DIR%"
    if errorlevel 1 exit /b 1
    exit /b 0
)
if exist "%ME_BACKUP_POINTER%" (
    call :load_latest_pointer
    if errorlevel 1 exit /b 1
    exit /b 0
)
echo   No UI-fixes latest-backup pointer exists; using the explicit verified pre-UI fallback.
call :load_fallback_backup
if errorlevel 1 exit /b 1
exit /b 0

:load_requested_backup
call :normalize_absolute_path "%~1" ME_NORMALIZED_REQUESTED_BACKUP
if errorlevel 1 exit /b 1
if /I "%ME_NORMALIZED_REQUESTED_BACKUP%"=="%ME_FALLBACK_BACKUP_DIR%" (
    call :load_fallback_backup
    if errorlevel 1 exit /b 1
    exit /b 0
)
call :load_dynamic_backup_from_directory "%ME_NORMALIZED_REQUESTED_BACKUP%"
if errorlevel 1 exit /b 1
exit /b 0

:load_latest_pointer
set "ME_POINTER_FORMAT="
set "ME_POINTER_BACKUP_DIR="
set "ME_POINTER_INFO_SHA="
for /f "usebackq tokens=1,* delims==" %%A in ("%ME_BACKUP_POINTER%") do (
    if /I "%%A"=="Format" set "ME_POINTER_FORMAT=%%B"
    if /I "%%A"=="BackupDirectory" set "ME_POINTER_BACKUP_DIR=%%B"
    if /I "%%A"=="BackupInfoSha256" set "ME_POINTER_INFO_SHA=%%B"
)
if /I not "%ME_POINTER_FORMAT%"=="MaterialEditorUiFixesPointerV1" (
    echo ERROR: Unexpected latest-backup pointer format: %ME_BACKUP_POINTER%
    exit /b 1
)
if not defined ME_POINTER_BACKUP_DIR (
    echo ERROR: Latest-backup pointer has no BackupDirectory.
    exit /b 1
)
if not defined ME_POINTER_INFO_SHA (
    echo ERROR: Latest-backup pointer has no BackupInfoSha256.
    exit /b 1
)
call :validate_sha256 "%ME_POINTER_INFO_SHA%" "latest-backup manifest"
if errorlevel 1 exit /b 1
call :load_dynamic_backup "%ME_POINTER_BACKUP_DIR%" "%ME_POINTER_INFO_SHA%" "latest validated deployment backup"
exit /b %errorlevel%

:load_dynamic_backup_from_directory
set "ME_REQUESTED_INFO_SHA_FILE=%~1\BACKUP_INFO.sha256"
if not exist "%ME_REQUESTED_INFO_SHA_FILE%" (
    echo ERROR: Explicit backup lacks BACKUP_INFO.sha256:
    echo        %ME_REQUESTED_INFO_SHA_FILE%
    exit /b 1
)
set "ME_REQUESTED_INFO_SHA="
for /f "usebackq tokens=1" %%H in ("%ME_REQUESTED_INFO_SHA_FILE%") do if not defined ME_REQUESTED_INFO_SHA set "ME_REQUESTED_INFO_SHA=%%H"
if not defined ME_REQUESTED_INFO_SHA (
    echo ERROR: Explicit backup checksum file is empty.
    exit /b 1
)
call :validate_sha256 "%ME_REQUESTED_INFO_SHA%" "explicit backup manifest"
if errorlevel 1 exit /b 1
call :load_dynamic_backup "%~1" "%ME_REQUESTED_INFO_SHA%" "explicit validated deployment backup"
exit /b %errorlevel%

:load_dynamic_backup
set "ME_ACTIVE_BACKUP_DIR=%~1"
set "ME_ACTIVE_BACKUP_INFO=%~1\BACKUP_INFO.txt"
set "ME_ACTIVE_BACKUP_INFO_SHA=%~2"
set "ME_ACTIVE_BACKUP_DLL=%~1\KK_MaterialEditor.dll"
set "ME_ACTIVE_BACKUP_NATIVE=%~1\libwebp.lib"
set "ME_ACTIVE_BACKUP_XML=%~1\KK_MaterialEditor.xml"
set "ME_ACTIVE_BACKUP_SOURCE=%~3"
call :verify_dynamic_backup_location "%ME_ACTIVE_BACKUP_DIR%"
if errorlevel 1 exit /b 1
if not exist "%ME_ACTIVE_BACKUP_INFO%" (
    echo ERROR: Missing backup manifest: %ME_ACTIVE_BACKUP_INFO%
    exit /b 1
)
if not exist "%ME_ACTIVE_BACKUP_DLL%" (
    echo ERROR: Missing backup DLL: %ME_ACTIVE_BACKUP_DLL%
    exit /b 1
)
if not exist "%ME_ACTIVE_BACKUP_NATIVE%" (
    echo ERROR: Missing backup native library: %ME_ACTIVE_BACKUP_NATIVE%
    exit /b 1
)
call :verify_hash "%ME_ACTIVE_BACKUP_INFO%" "%ME_ACTIVE_BACKUP_INFO_SHA%" "selected backup manifest"
if errorlevel 1 exit /b 1
set "ME_MANIFEST_FORMAT="
set "ME_MANIFEST_BACKUP_DIR="
set "ME_MANIFEST_DLL_SHA="
set "ME_MANIFEST_NATIVE_SHA="
set "ME_MANIFEST_ASSEMBLY_NAME="
set "ME_MANIFEST_ASSEMBLY_VERSION="
set "ME_MANIFEST_FILE_VERSION="
set "ME_MANIFEST_RUNTIME_COUNT="
set "ME_MANIFEST_XML_COPIED="
set "ME_MANIFEST_XML_SHA="
set "ME_MANIFEST_CONFIG_COPIED="
set "ME_MANIFEST_DATA_COPIED="
for /f "usebackq tokens=1,* delims==" %%A in ("%ME_ACTIVE_BACKUP_INFO%") do (
    if /I "%%A"=="Format" set "ME_MANIFEST_FORMAT=%%B"
    if /I "%%A"=="BackupDirectory" set "ME_MANIFEST_BACKUP_DIR=%%B"
    if /I "%%A"=="DllSha256" set "ME_MANIFEST_DLL_SHA=%%B"
    if /I "%%A"=="NativeSha256" set "ME_MANIFEST_NATIVE_SHA=%%B"
    if /I "%%A"=="AssemblyName" set "ME_MANIFEST_ASSEMBLY_NAME=%%B"
    if /I "%%A"=="AssemblyVersion" set "ME_MANIFEST_ASSEMBLY_VERSION=%%B"
    if /I "%%A"=="FileVersion" set "ME_MANIFEST_FILE_VERSION=%%B"
    if /I "%%A"=="RuntimeFileCount" set "ME_MANIFEST_RUNTIME_COUNT=%%B"
    if /I "%%A"=="DocumentationXmlCopied" set "ME_MANIFEST_XML_COPIED=%%B"
    if /I "%%A"=="XmlSha256" set "ME_MANIFEST_XML_SHA=%%B"
    if /I "%%A"=="ConfigCopied" set "ME_MANIFEST_CONFIG_COPIED=%%B"
    if /I "%%A"=="DataCopied" set "ME_MANIFEST_DATA_COPIED=%%B"
)
if /I not "%ME_MANIFEST_FORMAT%"=="MaterialEditorUiFixesBackupV1" if /I not "%ME_MANIFEST_FORMAT%"=="MaterialEditorThreePanelUiBackupV1" (
    echo ERROR: Unexpected backup manifest format.
    exit /b 1
)
if /I not "%ME_MANIFEST_BACKUP_DIR%"=="%ME_ACTIVE_BACKUP_DIR%" (
    echo ERROR: Backup manifest directory does not match the selected directory.
    exit /b 1
)
if /I not "%ME_MANIFEST_ASSEMBLY_NAME%"=="%ME_EXPECTED_ASSEMBLY_NAME%" (
    echo ERROR: Backup manifest assembly name is invalid.
    exit /b 1
)
if /I not "%ME_MANIFEST_ASSEMBLY_VERSION%"=="%ME_EXPECTED_ASSEMBLY_VERSION%" (
    echo ERROR: Backup manifest assembly version is invalid.
    exit /b 1
)
if not defined ME_MANIFEST_FILE_VERSION (
    echo ERROR: Backup manifest has no DLL FileVersion.
    exit /b 1
)
call :validate_sha256 "%ME_MANIFEST_DLL_SHA%" "backup DLL"
if errorlevel 1 exit /b 1
call :validate_sha256 "%ME_MANIFEST_NATIVE_SHA%" "backup native library"
if errorlevel 1 exit /b 1
if not "%ME_MANIFEST_RUNTIME_COUNT%"=="2" (
    echo ERROR: Backup manifest must describe exactly two runtime files.
    exit /b 1
)
call :verify_manifest_documentation_xml
if errorlevel 1 exit /b 1
if /I not "%ME_MANIFEST_CONFIG_COPIED%"=="False" (
    echo ERROR: Backup manifest unexpectedly includes config.
    exit /b 1
)
if /I not "%ME_MANIFEST_DATA_COPIED%"=="False" (
    echo ERROR: Backup manifest unexpectedly includes user data.
    exit /b 1
)
set "ME_ACTIVE_BACKUP_DLL_SHA=%ME_MANIFEST_DLL_SHA%"
set "ME_ACTIVE_BACKUP_NATIVE_SHA=%ME_MANIFEST_NATIVE_SHA%"
set "ME_ACTIVE_BACKUP_FILE_VERSION=%ME_MANIFEST_FILE_VERSION%"
set "ME_ACTIVE_BACKUP_XML_COPIED=%ME_MANIFEST_XML_COPIED%"
set "ME_ACTIVE_BACKUP_XML_SHA=%ME_MANIFEST_XML_SHA%"
call :verify_hash "%ME_ACTIVE_BACKUP_DLL%" "%ME_ACTIVE_BACKUP_DLL_SHA%" "selected backup KK_MaterialEditor.dll"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_ACTIVE_BACKUP_NATIVE%" "%ME_ACTIVE_BACKUP_NATIVE_SHA%" "selected backup libwebp.lib"
if errorlevel 1 exit /b 1
call :verify_file_version "%ME_ACTIVE_BACKUP_DLL%" "%ME_ACTIVE_BACKUP_FILE_VERSION%" "selected backup DLL"
if errorlevel 1 exit /b 1
call :verify_assembly "%ME_ACTIVE_BACKUP_DLL%" "selected backup DLL"
exit /b %errorlevel%

:verify_manifest_documentation_xml
if /I "%ME_MANIFEST_XML_COPIED%"=="True" (
    if not defined ME_MANIFEST_XML_SHA (
        echo ERROR: Backup manifest says XML was copied but has no XmlSha256.
        exit /b 1
    )
    if /I "%ME_MANIFEST_XML_SHA%"=="NONE" (
        echo ERROR: Backup manifest says XML was copied but its hash is NONE.
        exit /b 1
    )
    call :validate_sha256 "%ME_MANIFEST_XML_SHA%" "backup documentation XML"
    if errorlevel 1 exit /b 1
    if not exist "%ME_ACTIVE_BACKUP_XML%" (
        echo ERROR: Backup manifest says XML was copied, but the file is missing.
        exit /b 1
    )
    call :verify_hash "%ME_ACTIVE_BACKUP_XML%" "%ME_MANIFEST_XML_SHA%" "selected backup KK_MaterialEditor.xml"
    if errorlevel 1 exit /b 1
    exit /b 0
)
if /I not "%ME_MANIFEST_XML_COPIED%"=="False" (
    echo ERROR: Backup manifest DocumentationXmlCopied must be True or False.
    exit /b 1
)
if /I not "%ME_MANIFEST_XML_SHA%"=="NONE" (
    echo ERROR: Backup manifest must use XmlSha256=NONE when XML was not copied.
    exit /b 1
)
if exist "%ME_ACTIVE_BACKUP_XML%" (
    echo ERROR: Backup contains an unrecorded KK_MaterialEditor.xml.
    exit /b 1
)
exit /b 0

:load_fallback_backup
set "ME_ACTIVE_BACKUP_DIR=%ME_FALLBACK_BACKUP_DIR%"
set "ME_ACTIVE_BACKUP_INFO=%ME_FALLBACK_BACKUP_DIR%\BACKUP_INFO.txt"
set "ME_ACTIVE_BACKUP_INFO_SHA=%ME_FALLBACK_BACKUP_INFO_SHA%"
set "ME_ACTIVE_BACKUP_DLL=%ME_FALLBACK_BACKUP_DIR%\KK_MaterialEditor.dll"
set "ME_ACTIVE_BACKUP_NATIVE=%ME_FALLBACK_BACKUP_DIR%\libwebp.lib"
set "ME_ACTIVE_BACKUP_DLL_SHA=%ME_FALLBACK_BACKUP_DLL_SHA%"
set "ME_ACTIVE_BACKUP_NATIVE_SHA=%ME_FALLBACK_BACKUP_NATIVE_SHA%"
set "ME_ACTIVE_BACKUP_XML_COPIED=False"
set "ME_ACTIVE_BACKUP_XML_SHA=NONE"
set "ME_ACTIVE_BACKUP_SOURCE=explicit immutable pre-UI fallback"
call :verify_non_plugin_backup_location "%ME_ACTIVE_BACKUP_DIR%"
if errorlevel 1 exit /b 1
call :validate_sha256 "%ME_ACTIVE_BACKUP_INFO_SHA%" "pre-UI fallback manifest"
if errorlevel 1 exit /b 1
call :validate_sha256 "%ME_ACTIVE_BACKUP_DLL_SHA%" "pre-UI fallback DLL"
if errorlevel 1 exit /b 1
call :validate_sha256 "%ME_ACTIVE_BACKUP_NATIVE_SHA%" "pre-UI fallback native library"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_ACTIVE_BACKUP_INFO%" "%ME_ACTIVE_BACKUP_INFO_SHA%" "pre-UI fallback BACKUP_INFO.txt"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_ACTIVE_BACKUP_DLL%" "%ME_ACTIVE_BACKUP_DLL_SHA%" "pre-UI fallback KK_MaterialEditor.dll"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_ACTIVE_BACKUP_NATIVE%" "%ME_ACTIVE_BACKUP_NATIVE_SHA%" "pre-UI fallback libwebp.lib"
if errorlevel 1 exit /b 1
call :get_file_version "%ME_ACTIVE_BACKUP_DLL%" ME_ACTIVE_BACKUP_FILE_VERSION
if errorlevel 1 exit /b 1
call :verify_assembly "%ME_ACTIVE_BACKUP_DLL%" "pre-UI fallback DLL"
exit /b %errorlevel%

:normalize_absolute_path
set "ME_PATH_TO_NORMALIZE=%~1"
set "ME_NORMALIZED_PATH="
for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; if(-not [IO.Path]::IsPathRooted($env:ME_PATH_TO_NORMALIZE)) { throw 'The backup path must be absolute.' }; [IO.Path]::GetFullPath($env:ME_PATH_TO_NORMALIZE).TrimEnd('\')"`) do set "ME_NORMALIZED_PATH=%%P"
if not defined ME_NORMALIZED_PATH (
    echo ERROR: Could not normalize the requested absolute backup path.
    exit /b 1
)
set "%~2=%ME_NORMALIZED_PATH%"
exit /b 0

:verify_dynamic_backup_location
set "ME_BACKUP_PATH_TO_VALIDATE=%~1"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $path=[IO.Path]::GetFullPath($env:ME_BACKUP_PATH_TO_VALIDATE).TrimEnd('\'); $root=[IO.Path]::GetFullPath($env:ME_BACKUP_ROOT).TrimEnd('\') + '\'; $plugins=[IO.Path]::GetFullPath($env:ME_PLUGIN_ROOT).TrimEnd('\') + '\'; if(-not $path.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) { throw ('Dynamic backup is outside the approved root: ' + $path) }; if($path.StartsWith($plugins,[StringComparison]::OrdinalIgnoreCase)) { throw ('Backup is inside the loadable plugin tree: ' + $path) }; Write-Host ('  Verified non-loadable dynamic backup path: ' + $path)"
exit /b %errorlevel%

:verify_non_plugin_backup_location
set "ME_BACKUP_PATH_TO_VALIDATE=%~1"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $path=[IO.Path]::GetFullPath($env:ME_BACKUP_PATH_TO_VALIDATE).TrimEnd('\'); $plugins=[IO.Path]::GetFullPath($env:ME_PLUGIN_ROOT).TrimEnd('\'); if($path -eq $plugins -or $path.StartsWith(($plugins + '\'),[StringComparison]::OrdinalIgnoreCase)) { throw ('Backup is inside the loadable plugin tree: ' + $path) }; Write-Host ('  Verified non-loadable backup path: ' + $path)"
exit /b %errorlevel%

:verify_current_runtime_unchanged
if /I "%ME_CURRENT_DLL_PRESENT%"=="True" (
    if not exist "%ME_TARGET_DLL%" (
        echo ERROR: The current DLL disappeared before restore staging.
        exit /b 1
    )
    call :verify_hash "%ME_TARGET_DLL%" "%ME_CURRENT_DLL_SHA%" "unchanged current KK_MaterialEditor.dll"
    if errorlevel 1 exit /b 1
) else (
    if /I not "%ME_CURRENT_DLL_PRESENT%"=="False" (
        echo ERROR: Invalid captured current DLL presence state.
        exit /b 1
    )
    if exist "%ME_TARGET_DLL%" (
        echo ERROR: KK_MaterialEditor.dll appeared before restore staging.
        exit /b 1
    )
)
if /I "%ME_CURRENT_NATIVE_PRESENT%"=="True" (
    if not exist "%ME_TARGET_NATIVE%" (
        echo ERROR: The current native library disappeared before restore staging.
        exit /b 1
    )
    call :verify_hash "%ME_TARGET_NATIVE%" "%ME_CURRENT_NATIVE_SHA%" "unchanged current libwebp.lib"
    if errorlevel 1 exit /b 1
) else (
    if /I not "%ME_CURRENT_NATIVE_PRESENT%"=="False" (
        echo ERROR: Invalid captured current native presence state.
        exit /b 1
    )
    if exist "%ME_TARGET_NATIVE%" (
        echo ERROR: libwebp.lib appeared before restore staging.
        exit /b 1
    )
)
call :verify_runtime_xml_unchanged
exit /b %errorlevel%

:stage_pre_restore_state
set "ME_RESTORE_ROLLBACK_TIMESTAMP="
for /f "usebackq delims=" %%T in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-Date -Format 'yyyyMMdd-HHmmss-fff'"`) do set "ME_RESTORE_ROLLBACK_TIMESTAMP=%%T"
if not defined ME_RESTORE_ROLLBACK_TIMESTAMP (
    echo ERROR: Could not generate a pre-restore rollback timestamp.
    exit /b 1
)
set "ME_RESTORE_ROLLBACK_DIR=%ME_RESTORE_ROLLBACK_ROOT%\%ME_RESTORE_ROLLBACK_TIMESTAMP%-before-restore"
set "ME_RESTORE_ROLLBACK_DLL=%ME_RESTORE_ROLLBACK_DIR%\KK_MaterialEditor.dll"
set "ME_RESTORE_ROLLBACK_NATIVE=%ME_RESTORE_ROLLBACK_DIR%\libwebp.lib"
set "ME_RESTORE_ROLLBACK_INFO=%ME_RESTORE_ROLLBACK_DIR%\RESTORE_ROLLBACK_INFO.txt"
set "ME_RESTORE_ROLLBACK_INFO_SHA_FILE=%ME_RESTORE_ROLLBACK_DIR%\RESTORE_ROLLBACK_INFO.sha256"
call :verify_dynamic_backup_location "%ME_RESTORE_ROLLBACK_DIR%"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; New-Item -ItemType Directory -Path $env:ME_RESTORE_ROLLBACK_ROOT -Force | Out-Null; if(Test-Path -LiteralPath $env:ME_RESTORE_ROLLBACK_DIR) { throw ('Restore rollback directory already exists: ' + $env:ME_RESTORE_ROLLBACK_DIR) }; New-Item -ItemType Directory -Path $env:ME_RESTORE_ROLLBACK_DIR | Out-Null"
if errorlevel 1 exit /b 1
if /I "%ME_CURRENT_DLL_PRESENT%"=="True" (
    copy /b /y "%ME_TARGET_DLL%" "%ME_RESTORE_ROLLBACK_DLL%" >nul
    if errorlevel 1 exit /b 1
    call :verify_hash "%ME_RESTORE_ROLLBACK_DLL%" "%ME_CURRENT_DLL_SHA%" "pre-restore rollback KK_MaterialEditor.dll"
    if errorlevel 1 exit /b 1
)
if /I "%ME_CURRENT_NATIVE_PRESENT%"=="True" (
    copy /b /y "%ME_TARGET_NATIVE%" "%ME_RESTORE_ROLLBACK_NATIVE%" >nul
    if errorlevel 1 exit /b 1
    call :verify_hash "%ME_RESTORE_ROLLBACK_NATIVE%" "%ME_CURRENT_NATIVE_SHA%" "pre-restore rollback libwebp.lib"
    if errorlevel 1 exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $lines=@('Format=MaterialEditorRestoreRollbackV1',('BackupDirectory=' + $env:ME_RESTORE_ROLLBACK_DIR),('CreatedUtc=' + [DateTime]::UtcNow.ToString('o')),('TargetDll=' + $env:ME_TARGET_DLL),('TargetNative=' + $env:ME_TARGET_NATIVE),('DllPresent=' + $env:ME_CURRENT_DLL_PRESENT),('DllSha256=' + $env:ME_CURRENT_DLL_SHA),('NativePresent=' + $env:ME_CURRENT_NATIVE_PRESENT),('NativeSha256=' + $env:ME_CURRENT_NATIVE_SHA),('SelectedBackupDirectory=' + $env:ME_ACTIVE_BACKUP_DIR),('SelectedBackupInfoSha256=' + $env:ME_ACTIVE_BACKUP_INFO_SHA),('DocumentationXmlPresent=' + $env:ME_CURRENT_XML_PRESENT),('DocumentationXmlSha256=' + $env:ME_CURRENT_XML_SHA),'RuntimeFileCount=2','DocumentationXmlCopied=False','ConfigCopied=False','DataCopied=False'); [IO.File]::WriteAllLines($env:ME_RESTORE_ROLLBACK_INFO,$lines,[Text.Encoding]::ASCII)"
if errorlevel 1 exit /b 1
call :get_hash "%ME_RESTORE_ROLLBACK_INFO%" ME_RESTORE_ROLLBACK_INFO_SHA
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; if($env:ME_RESTORE_ROLLBACK_INFO_SHA -notmatch '^[0-9A-Fa-f]{64}$') { throw 'Invalid restore rollback manifest SHA-256.' }; [IO.File]::WriteAllText($env:ME_RESTORE_ROLLBACK_INFO_SHA_FILE,($env:ME_RESTORE_ROLLBACK_INFO_SHA + [Environment]::NewLine),[Text.Encoding]::ASCII)"
if errorlevel 1 exit /b 1
call :verify_hash "%ME_RESTORE_ROLLBACK_INFO%" "%ME_RESTORE_ROLLBACK_INFO_SHA%" "pre-restore rollback manifest"
if errorlevel 1 exit /b 1
call :verify_current_runtime_unchanged
if errorlevel 1 exit /b 1
echo   Staged exact pre-restore state: %ME_RESTORE_ROLLBACK_DIR%
echo   Rollback manifest SHA-256:     %ME_RESTORE_ROLLBACK_INFO_SHA%
exit /b 0

:rollback_pre_restore_state
echo Restoring the exact pre-restore presence and bytes of both runtime targets...
set "ME_ROLLBACK_STATUS=0"
call :restore_one_runtime_target "%ME_RESTORE_ROLLBACK_DLL%" "%ME_TARGET_DLL%" "%ME_CURRENT_DLL_PRESENT%" "%ME_CURRENT_DLL_SHA%" "KK_MaterialEditor.dll"
if errorlevel 1 set "ME_ROLLBACK_STATUS=1"
call :restore_one_runtime_target "%ME_RESTORE_ROLLBACK_NATIVE%" "%ME_TARGET_NATIVE%" "%ME_CURRENT_NATIVE_PRESENT%" "%ME_CURRENT_NATIVE_SHA%" "libwebp.lib"
if errorlevel 1 set "ME_ROLLBACK_STATUS=1"
call :verify_runtime_xml_unchanged
if errorlevel 1 set "ME_ROLLBACK_STATUS=1"
call :check_duplicates
if errorlevel 1 set "ME_ROLLBACK_STATUS=1"
if not "%ME_ROLLBACK_STATUS%"=="0" exit /b 1
exit /b 0

:restore_one_runtime_target
if /I "%~3"=="True" (
    if not exist "%~1" (
        echo ERROR: Missing staged rollback file for %~5.
        exit /b 1
    )
    copy /b /y "%~1" "%~2" >nul
    if errorlevel 1 exit /b 1
    call :verify_hash "%~2" "%~4" "rolled-back %~5"
    if errorlevel 1 exit /b 1
    exit /b 0
)
if /I not "%~3"=="False" (
    echo ERROR: Invalid staged presence state for %~5.
    exit /b 1
)
if exist "%~2" (
    del /f /q "%~2" >nul
    if errorlevel 1 exit /b 1
)
if exist "%~2" (
    echo ERROR: Could not restore the original absence of %~5.
    exit /b 1
)
echo   Restored original absence of %~5.
exit /b 0

:guard_game_processes
powershell -NoProfile -ExecutionPolicy Bypass -Command "$names=@('Koikatu','KoikatuVR','Koikatsu','KoikatsuVR','CharaStudio','Studio','StudioVR','StudioNEO','StudioNEOV2'); $running=@(Get-Process -ErrorAction SilentlyContinue | Where-Object { $names -contains $_.ProcessName }); if($running.Count -gt 0) { foreach($p in $running) { Write-Host ('  RUNNING: ' + $p.ProcessName + '.exe PID ' + $p.Id) }; exit 1 }"
exit /b %errorlevel%

:capture_runtime_xml_state
set "ME_CURRENT_XML_PRESENT=False"
set "ME_CURRENT_XML_SHA=NONE"
if not exist "%ME_TARGET_XML%" exit /b 0
call :get_hash "%ME_TARGET_XML%" ME_CURRENT_XML_SHA
if errorlevel 1 exit /b 1
set "ME_CURRENT_XML_PRESENT=True"
exit /b 0

:verify_runtime_xml_unchanged
if /I "%ME_CURRENT_XML_PRESENT%"=="True" (
    if not exist "%ME_TARGET_XML%" (
        echo ERROR: The installed documentation XML was removed unexpectedly.
        exit /b 1
    )
    call :verify_hash "%ME_TARGET_XML%" "%ME_CURRENT_XML_SHA%" "untouched installed documentation XML"
    if errorlevel 1 exit /b 1
    exit /b 0
)
if exist "%ME_TARGET_XML%" (
    echo ERROR: Documentation XML appeared unexpectedly in the runtime directory.
    exit /b 1
)
exit /b 0

:check_duplicates
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $expected=[IO.Path]::GetFullPath($env:ME_TARGET_DLL); $duplicates=New-Object Collections.Generic.List[string]; foreach($f in @(Get-ChildItem -LiteralPath $env:ME_PLUGIN_ROOT -Recurse -File -Filter '*.dll')) { $full=[IO.Path]::GetFullPath($f.FullName); if([String]::Equals($full,$expected,[StringComparison]::OrdinalIgnoreCase)) { continue }; $isDuplicate=$f.Name -in @('KK_MaterialEditor.dll','MaterialEditor.dll'); if(-not $isDuplicate) { try { $identity=[Reflection.AssemblyName]::GetAssemblyName($full); $isDuplicate=[String]::Equals($identity.Name,$env:ME_EXPECTED_ASSEMBLY_NAME,[StringComparison]::OrdinalIgnoreCase) } catch { $isDuplicate=$false } }; if($isDuplicate) { $duplicates.Add($full) } }; if($duplicates.Count -gt 0) { Write-Host 'ERROR: Additional loadable Material Editor DLL(s) detected:'; foreach($path in $duplicates) { Write-Host ('  ' + $path) }; exit 1 }; Write-Host ('  No duplicate ' + $env:ME_EXPECTED_ASSEMBLY_NAME + ' assembly was found.')"
exit /b %errorlevel%

:describe_assembly
set "ME_ASSEMBLY_PATH=%~1"
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $a=[Reflection.AssemblyName]::GetAssemblyName($env:ME_ASSEMBLY_PATH); Write-Host ('  Current assembly: ' + $a.FullName) } catch { Write-Host ('  Current DLL has no readable managed assembly identity: ' + $_.Exception.Message) }"
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

:validate_sha256
set "ME_SHA_TO_VALIDATE=%~1"
set "ME_SHA_LABEL=%~2"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; if($env:ME_SHA_TO_VALIDATE -notmatch '^[0-9A-Fa-f]{64}$') { throw ('Invalid SHA-256 for ' + $env:ME_SHA_LABEL) }"
exit /b %errorlevel%

:copy_failed
echo.
echo RESTORE FAILED while copying the two verified files.
goto :rollback_after_restore_failure

:verification_failed
echo.
echo CRITICAL: The post-copy restore verification failed.
goto :rollback_after_restore_failure

:rollback_after_restore_failure
call :rollback_pre_restore_state
if errorlevel 1 goto :rollback_failed
echo Automatic rollback succeeded; both runtime targets exactly match their pre-restore state.
echo The selected backup remains intact at:
echo   %ME_ACTIVE_BACKUP_DIR%
echo Pre-restore rollback snapshot retained at:
echo   %ME_RESTORE_ROLLBACK_DIR%
echo Documentation XML, config, and every unrelated file were untouched.
exit /b 1

:rollback_failed
echo CRITICAL: Automatic rollback could not be verified.
echo The selected backup remains intact at:
echo   %ME_ACTIVE_BACKUP_DIR%
echo Pre-restore rollback snapshot retained at:
echo   %ME_RESTORE_ROLLBACK_DIR%
echo Keep all Koikatsu processes closed and recover only the two exact runtime targets.
exit /b 1

:fail
echo.
echo RESTORE ABORTED. No restore was completed.
exit /b 1
