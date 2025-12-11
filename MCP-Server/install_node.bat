@echo off
rem ------------------------------------------------------------
rem install_node.bat – 安裝 Node.js (ZIP 版) 並加入 PATH
rem ------------------------------------------------------------

set "ZIPFILE=C:\Users\01102088\Desktop\DEV\MCP-Server\node-v18.xx.x-win-x64.zip"
set "TARGETDIR=C:\Program Files\nodejs"

rem ---- 檢查 ZIP 是否存在 ----
if not exist "%ZIPFILE%" (
    echo [ERROR] 找不到 Node.js ZIP 檔案: %ZIPFILE%
    echo 請確認已將下載好的 zip 放到上述路徑，然後重新執行此批次檔。
    pause
    exit /b 1
)

rem ---- 建立目標資料夾 ----
if not exist "%TARGETDIR%" (
    mkdir "%TARGETDIR%"
)

rem ---- 解壓縮 ----
echo 正在解壓縮 %ZIPFILE% 到 %TARGETDIR% ...
powershell -NoProfile -Command "Expand-Archive -Path '%ZIPFILE%' -DestinationPath '%TARGETDIR%' -Force"
if errorlevel 1 (
    echo [ERROR] 解壓縮失敗，請檢查是否有權限或磁碟空間不足。
    pause
    exit /b 1
)

rem ---- 把 Node.js 加入系統 PATH (機器層級) ----
for /f "skip=2 tokens=2*" %%A in ('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v Path') do set "OLDPATH=%%B"

echo %OLDPATH% | find /I "%TARGETDIR%" >nul
if %errorlevel%==0 (
    echo Node.js 目錄已在 PATH 中，略過此步驟。
) else (
    echo 正在將 %TARGETDIR% 加入機器層級 PATH ...
    set "NEWPATH=%OLDPATH%;%TARGETDIR%"
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v Path /t REG_EXPAND_SZ /d "%NEWPATH%" /f
    if errorlevel 1 (
        echo [ERROR] 無法寫入機器層級 PATH，請以系統管理員身分重新執行此批次檔。
        pause
        exit /b 1
    )
)

rem ---- 讓當前 cmd 視窗立即看到更新後的 PATH ----
set "PATH=%PATH%;%TARGETDIR%"

rem ---- 驗證安裝 ----
node -v
npm -v
if errorlevel 1 (
    echo [ERROR] node 或 npm 無法正確執行，請檢查 PATH 是否正確設定。
    pause
    exit /b 1
)

echo ==============================
echo ✅ Node.js 已成功安裝並加入 PATH
echo ==============================
pause
exit /b 0
