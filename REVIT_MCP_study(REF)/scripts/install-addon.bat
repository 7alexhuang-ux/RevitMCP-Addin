@echo off
REM ============================================================================
REM Revit MCP Add-in 自動安裝程式 (安全版本)
REM ============================================================================
REM 此指令稿會自動：
REM 1. 偵測您的 Revit 版本
REM 2. 從本機複製 RevitMCP.dll 和 RevitMCP.addin
REM 3. 複製到正確的資料夾
REM ============================================================================
REM 安全注意事項：
REM - 此指令稿只從本機複製檔案，不會從網路下載
REM - 不需要系統管理員權限（Add-in 目錄在使用者資料夾）
REM - 所有路徑都使用引號包覆，防止路徑注入攻擊
REM ============================================================================

chcp 65001 > nul
setlocal enabledelayedexpansion

echo.
echo ============================================================================
echo   Revit MCP Add-in 自動安裝程式 (安全版本)
echo ============================================================================
echo.

REM ============================================================================
REM 安全檢查 1：驗證執行環境
REM ============================================================================

REM 取得指令稿所在目錄（使用安全的方式）
set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

REM 檢查是否在正確的專案目錄中執行
set "PROJECT_ROOT=%SCRIPT_DIR%\.."
pushd "%PROJECT_ROOT%" 2>nul
if errorlevel 1 (
    echo ❌ 錯誤：無法確定專案目錄
    echo 請確認您在正確的位置執行此程式
    pause
    exit /b 1
)
set "PROJECT_ROOT=%CD%"
popd

REM 驗證專案結構（確認這是正確的專案）
if not exist "%PROJECT_ROOT%\MCP" (
    echo ❌ 錯誤：找不到 MCP 資料夾
    echo 請確認您在 REVIT_MCP_study 專案目錄中執行此程式
    pause
    exit /b 1
)

if not exist "%PROJECT_ROOT%\MCP-Server" (
    echo ❌ 錯誤：找不到 MCP-Server 資料夾
    echo 這可能不是正確的專案目錄
    pause
    exit /b 1
)

echo ✓ 專案目錄驗證通過：%PROJECT_ROOT%
echo.

REM ============================================================================
REM 安全檢查 2：驗證 APPDATA 環境變數
REM ============================================================================

if "%APPDATA%"=="" (
    echo ❌ 錯誤：APPDATA 環境變數未設定
    echo 這可能是系統設定問題，請聯繫技術支援
    pause
    exit /b 1
)

REM 檢查 APPDATA 是否指向有效路徑
if not exist "%APPDATA%" (
    echo ❌ 錯誤：APPDATA 路徑不存在：%APPDATA%
    pause
    exit /b 1
)

echo ✓ 環境變數驗證通過
echo.

REM ============================================================================
REM 偵測 Revit 版本
REM ============================================================================

echo 正在偵測已安裝的 Revit 版本...
echo.

set "REVIT_VERSION="
set "ADDON_PATH="
set "FOUND_VERSIONS="

REM 檢查支援的 Revit 版本（2022-2024）
for %%V in (2024 2023 2022) do (
    if exist "%APPDATA%\Autodesk\Revit\Addins\%%V" (
        echo ✓ 找到 Revit %%V
        if "!REVIT_VERSION!"=="" (
            set "REVIT_VERSION=%%V"
            set "ADDON_PATH=%APPDATA%\Autodesk\Revit\Addins\%%V"
        )
        set "FOUND_VERSIONS=!FOUND_VERSIONS! %%V"
    )
)

echo.

if "%REVIT_VERSION%"=="" (
    echo ❌ 錯誤：沒有找到已安裝的 Revit
    echo.
    echo 可能的原因：
    echo - 您的電腦沒有安裝 Revit
    echo - 支援的版本：2022、2023、2024
    echo.
    echo 檢查的路徑：%APPDATA%\Autodesk\Revit\Addins\
    pause
    exit /b 1
)

REM 如果找到多個版本，讓使用者選擇
set "VERSION_COUNT=0"
for %%V in (%FOUND_VERSIONS%) do set /a VERSION_COUNT+=1

if %VERSION_COUNT% gtr 1 (
    echo 找到多個 Revit 版本：%FOUND_VERSIONS%
    echo.
    set /p "USER_VERSION=請輸入要安裝的版本號 (例如 2022): "
    
    REM 驗證使用者輸入（只允許 2022、2023、2024）
    set "VALID_INPUT="
    for %%V in (2022 2023 2024) do (
        if "!USER_VERSION!"=="%%V" set "VALID_INPUT=1"
    )
    
    if "!VALID_INPUT!"=="" (
        echo ❌ 錯誤：無效的版本號
        pause
        exit /b 1
    )
    
    if not exist "%APPDATA%\Autodesk\Revit\Addins\!USER_VERSION!" (
        echo ❌ 錯誤：該版本未安裝
        pause
        exit /b 1
    )
    
    set "REVIT_VERSION=!USER_VERSION!"
    set "ADDON_PATH=%APPDATA%\Autodesk\Revit\Addins\!USER_VERSION!"
)

echo.
echo ✓ 將安裝到 Revit %REVIT_VERSION%
echo ✓ Add-in 路徑：%ADDON_PATH%
echo.

REM ============================================================================
REM 安全檢查 3：驗證來源檔案
REM ============================================================================

echo 正在驗證來源檔案...
echo.

REM 定義來源檔案路徑
set "SOURCE_DLL=%PROJECT_ROOT%\MCP\MCP\bin\Release\RevitMCP.dll"
set "SOURCE_ADDIN=%PROJECT_ROOT%\MCP\MCP\RevitMCP.addin"

REM 檢查 DLL 是否存在
if not exist "%SOURCE_DLL%" (
    REM 嘗試其他可能的位置
    set "SOURCE_DLL=%PROJECT_ROOT%\MCP\MCP\bin\Debug\RevitMCP.dll"
)

if not exist "%SOURCE_DLL%" (
    echo ❌ 錯誤：找不到 RevitMCP.dll
    echo.
    echo 請先製作程式：
    echo 1. 打開命令提示字元
    echo 2. cd "%PROJECT_ROOT%\MCP\MCP"
    echo 3. dotnet build -c Release
    echo.
    echo 或者下載現成版本放到：
    echo %PROJECT_ROOT%\MCP\MCP\bin\Release\
    pause
    exit /b 1
)

if not exist "%SOURCE_ADDIN%" (
    echo ❌ 錯誤：找不到 RevitMCP.addin
    echo 路徑：%SOURCE_ADDIN%
    pause
    exit /b 1
)

echo ✓ 找到 RevitMCP.dll
echo ✓ 找到 RevitMCP.addin
echo.

REM ============================================================================
REM 安全檢查 4：顯示檔案資訊供使用者確認
REM ============================================================================

echo 即將複製以下檔案：
echo.
echo 來源：
echo   - %SOURCE_DLL%
echo   - %SOURCE_ADDIN%
echo.
echo 目標：
echo   - %ADDON_PATH%
echo.

set /p "CONFIRM=確認安裝？(Y/N): "
if /i not "%CONFIRM%"=="Y" (
    echo 安裝已取消
    pause
    exit /b 0
)

echo.

REM ============================================================================
REM 執行安裝（不需要管理員權限）
REM ============================================================================

echo 正在複製檔案...
echo.

REM 檢查目標資料夾是否存在
if not exist "%ADDON_PATH%" (
    echo 正在建立目標資料夾...
    mkdir "%ADDON_PATH%" 2>nul
    if errorlevel 1 (
        echo ❌ 錯誤：無法建立目標資料夾
        echo 路徑：%ADDON_PATH%
        pause
        exit /b 1
    )
)

REM 複製 DLL
copy /Y "%SOURCE_DLL%" "%ADDON_PATH%\RevitMCP.dll" >nul 2>&1
if errorlevel 1 (
    echo ❌ 錯誤：無法複製 RevitMCP.dll
    echo.
    echo 可能的原因：
    echo - Revit 正在執行中（請關閉 Revit 後重試）
    echo - 目標資料夾沒有寫入權限
    pause
    exit /b 1
)
echo ✓ 已複製 RevitMCP.dll

REM 複製 ADDIN
copy /Y "%SOURCE_ADDIN%" "%ADDON_PATH%\RevitMCP.addin" >nul 2>&1
if errorlevel 1 (
    echo ❌ 錯誤：無法複製 RevitMCP.addin
    pause
    exit /b 1
)
echo ✓ 已複製 RevitMCP.addin

REM 複製相依套件（如果存在）
set "SOURCE_JSON=%PROJECT_ROOT%\MCP\MCP\bin\Release\Newtonsoft.Json.dll"
if exist "%SOURCE_JSON%" (
    copy /Y "%SOURCE_JSON%" "%ADDON_PATH%\Newtonsoft.Json.dll" >nul 2>&1
    if not errorlevel 1 (
        echo ✓ 已複製 Newtonsoft.Json.dll
    )
)

echo.

REM ============================================================================
REM 安裝完成
REM ============================================================================

echo ============================================================================
echo   ✓ 安裝完成！
echo ============================================================================
echo.
echo 安裝摘要：
echo   - Revit 版本：%REVIT_VERSION%
echo   - 安裝路徑：%ADDON_PATH%
echo.
echo 接下來的步驟：
echo   1. 完全關閉 Revit（如果正在執行）
echo   2. 重新開啟 Revit
echo   3. 應該會看到「MCP Tools」面板
echo   4. 點擊「MCP 服務 (開/關)」啟動服務
echo.
echo 如有問題，請參考 README.md 的「常見問題」章節
echo.
pause
