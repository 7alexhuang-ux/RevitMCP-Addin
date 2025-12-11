@echo off
rem ============================================
rem run_mcp_server.bat
rem ============================================

echo ==============================
echo MCP Server Startup
echo ==============================
echo.

rem Set Node.js path
set "NODE_HOME=%~dp0node-v24.11.1-win-x64\node-v24.11.1-win-x64"
set "PATH=%NODE_HOME%;%PATH%"

rem Verify Node.js
echo Verifying Node.js...
node -v
npm -v
echo.

rem Install dependencies
echo ==============================
echo Step 1/3: npm install
echo ==============================
npm install
if errorlevel 1 (
    echo ERROR: npm install failed
    pause
    exit /b 1
)
echo.

rem Build TypeScript
echo ==============================
echo Step 2/3: npm run build
echo ==============================
npm run build
if errorlevel 1 (
    echo ERROR: npm run build failed
    pause
    exit /b 1
)
echo.

rem Start MCP Server
echo ==============================
echo Step 3/3: npm start
echo ==============================
echo MCP Server will run on ws://localhost:8765
echo Press Ctrl+C to stop
echo.
npm start
