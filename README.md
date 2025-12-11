# Revit MCP Connect (Gemini 版)

本專案包含一個 Revit Add-in (外掛) 與一個 Node.js MCP Server，利用 Google Gemini API 實現 Revit 的 AI 自動化功能。

## 專案結構
- **MCP**: C# Revit Add-in 原始碼。
- **MCP-Server**: Node.js Server 原始碼。

## 快速開始

### 1. MCP Server 設定
1. 進入 `MCP-Server` 資料夾。
# Revit MCP Connect (Gemini 版)

本專案包含一個 Revit Add-in (外掛) 與一個 Node.js MCP Server，利用 Google Gemini API 實現 Revit 的 AI 自動化功能。

## 專案結構
- **MCP**: C# Revit Add-in 原始碼。
- **MCP-Server**: Node.js Server 原始碼。

## 快速開始

### 1. MCP Server 設定
1. 進入 `MCP-Server` 資料夾。
2. 將 `.env` 檔案重新命名為 `.env.local` (或直接編輯 `.env`)，並填入您的 `GEMINI_API_KEY`。
3. 執行 `npm start` 啟動伺服器。

### 2. Revit Add-in 設定
1. 在 `MCP` 資料夾中編譯專案：`dotnet build -c Release`。
2. 將 `MCP/RevitMCP.addin` 複製到 `%AppData%\Autodesk\Revit\Addins\2024\`。
3. 更新 `RevitMCP.addin` 中的 `<Assembly>` 路徑，使其指向編譯好的- **Revit Error**: 檢查 DLL 是否被 Windows 封鎖 (右鍵 -> 內容 -> 解除封鎖)。

### 6. 常用指令 (Troubleshooting)
- **停止伺服器**: 在終端機按 `Ctrl + C`。
- **強制關閉所有 Node.js (若 Port 被佔用)**:
  ```powershell
  taskkill /F /IM node.exe
  ```

## 功能
- **Ribbon UI**: 在 Revit 上方工具列新增 "MCP Tools" 分頁與 "Connect" 按鈕。
- **AI 整合**: 透過本機 MCP Server 將使用者指令傳送給 Gemini 模型。
