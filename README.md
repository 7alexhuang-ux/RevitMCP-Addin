# Revit MCP Connect (Gemini + Ollama Edition)

這是一個結合 **Autodesk Revit** 與 **Google Gemini AI** 的智慧自動化專案。
透過本地運行的 MCP (Model Context Protocol) Server，將 Revit 的操作能力賦予大型語言模型，實現透過自然語言對 BIM 模型進行查詢、建立、修改與分析。

本專案支援 **Google Gemini** (雲端高智商模型) 作為主力，並內建 **Ollama** (本地開源模型) 作為備援機制，確保服務不中斷。

---

## 功能特色

### 🤖 雙模 AI 核心
*   **Gemini Integration**: 預設使用 Google Gemini 1.5/2.0 Flash 模型，具備極快的反應速度與強大的工具呼叫能力。
*   **Auto-Fallback**: 當 Gemini API 遭遇連線限制 (Rate Limit) 或錯誤時，系統會自動切換至本地運行的 **Ollama (gpt-oss:20b)** 模型繼續服務。

### 🔌 Revit 深度整合
*   **Ribbon UI**: 在 Revit 工具列新增專屬 "MCP Tools" 分頁。
*   **Smart Connect**: 一鍵連接本地 MCP Server，即時通訊。
*   **Interactive Chat**: 內建 WPF 聊天視窗 (`ChatWindow`)，支援對話歷史記錄與工具執行回饋。

### 🛠️ 豐富的 MCP 工具集
專案內建多種 Revit 專用控制工具，讓 AI 能執行以下操作：
*   **查詢 (Query)**:
    *   專案資訊、樓層、視圖資訊。
    *   選取元素、過濾特定類別元素 (如牆、門、窗)。
    *   查詢房間資訊 (面積、名稱) 與房間內含元件。
    *   讀取元素參數 (Parameters)。
*   **建立 (Creation)**:
    *   繪製牆體 (Wall)。
    *   放置族群元件 (Doors, Windows, Furniture)。
    *   繪製線段與模型線。
    *   建立樓板 (Floor)。
*   **修改 (Modification)**:
    *   刪除元素 (單一或批次)。
    *   修改元素參數值。
    *   重置/清空視圖。
*   **視覺化 (Visualization)**:
    *   **Color Splash**: 根據參數值將元素填色 (例如根據防火等級著色)。
    *   自動標註 (Tagging)。

---

## 專案結構

```
REVIT-MCP-ADDIN/
├── MCP/                    # Revit Add-in (C#)
│   ├── Application.cs           # 主程式進入點 (Ribbon UI)
│   ├── ConnectCommand.cs        # 連線測試命令
│   ├── ChatCommand.cs           # 啟動聊天視窗命令
│   ├── RevitMCP.addin           # Add-in 配置擋 (需安裝至 %AppData%)
│   ├── Tools/                   # 核心工具邏輯
│   │   └── ToolHandler.cs       # 處理 Revit API 操作 (建立/查詢/修改)
│   ├── Views/                   # UI 介面
│   │   ├── ChatWindow.xaml      # 聊天視窗介面
│   │   └── ChatWindow.xaml.cs   # 視窗邏輯
│   ├── Utils/                   # 工具類別
│   │   └── RevitEvent.cs        # 處理外部事件 (External Event)
│   └── Services/                # 服務類別
│       └── ChatService.cs       # 聊天功能服務
├── MCP-Server/             # MCP Server (Node.js)
│   ├── server.js                # MCP Server 主程式 (Gemini/Ollama 整合)
│   ├── chats.json               # 對話歷史記錄
│   ├── .env.example             # 環境變數範例 (API Key)
│   └── package.json             # NPM 套件設定
└── README.md               # 專案說明文件
```

---

## 系統需求

*   **作業系統**: Windows 10 或 Windows 11 (64-bit)。
*   **主程式**: Autodesk Revit **2024** (其他版本可能需要重新編譯)。
*   **Runtime**:
    *   [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) (Revit 2024 內建)。
    *   [Node.js](https://nodejs.org/) (建議安裝 LTS 版本，如 v18 或 v20+)。
*   *(選用)* **Ollama**: 若需啟用本地備援功能，請安裝 [Ollama](https://ollama.com/) 並下載 `gpt-oss:20b` 模型 (或修改程式碼指定其他模型)。

---

## 透過 Git Clone 的首次設定

如果您是第一次下載本專案，請依照以下步驟進行環境準備：

1.  **Clone 專案**:
    ```powershell
    git clone <repository-url>
    cd RevitMCP-Addin
    ```
2.  **檢查目錄**: 確認 `MCP` 與 `MCP-Server` 資料夾皆存在。

---

## 安裝步驟

### Step 1: 設定 MCP Server (Node.js)

1.  開啟終端機 (PowerShell 或 CMD)，進入 Server 目錄：
    ```powershell
    cd MCP-Server
    ```
2.  安裝相依套件：
    ```powershell
    npm install
    ```
3.  設定環境變數：
    *   將 `.env.example` 複製並重新命名為 `.env`。
    *   使用文字編輯器開啟 `.env`，填入您的 **Gemini API Key**：
        ```env
        GEMINI_API_KEY=AIzaSyDxxxxxxxxx...
        ```

### Step 2: 編譯與安裝 Revit Add-in

1.  **編譯**:
    *   使用 Visual Studio 開啟 `MCP/RevitMCP.csproj` (或 `MCP.sln`)。
    *   確認參考 (References) 中的 `RevitAPI.dll` 與 `RevitAPIUI.dll` 路徑正確 (通常在 `C:\Program Files\Autodesk\Revit 2024\` 依您的安裝路徑而定)。
    *   選擇 `Release` 模式進行建置 (Build)。
    *   (或使用 CLI):
        ```powershell
        cd ../MCP
        dotnet build -c Release
        ```
2.  **安裝 Add-in Manifest**:
    *   找到專案中的 `MCP/RevitMCP.addin` 檔案。
    *   使用文字編輯器開啟它，修改 `<Assembly>` 標籤內容，指向您剛剛編譯出的 DLL **絕對路徑**。
        ```xml
        <Assembly>C:\Project\RevitMCP-Addin\MCP\bin\Release\RevitMCP.dll</Assembly>
        ```
    *   將此 `.addin` 檔案複製到 Revit 的 Add-ins 資料夾：
        *   路徑: `%AppData%\Autodesk\Revit\Addins\2024\`

---

## 啟動方式

為了讓系統正常運作，您必須**先啟動 Server，再執行 Revit 指令**。

### 1. 啟動 MCP Server
您可以直接執行批次檔 (如果有的話) 或透過指令：
```powershell
cd MCP-Server
npm start
```
當看到 `MCP Server listening at http://localhost:3005` 即表示伺服器已就緒。

### 2. 啟動 Revit Client
1.  開啟 Autodesk Revit 2024。
2.  進入任何專案 (或新建專案)。
3.  在上方 Ribbon 選單找到 **"MCP Tools"** 分頁。
4.  點選 **"Chat"** 按鈕開啟 AI 對話視窗。
5.  輸入指令 (例如：「幫我在 Level 1 畫四面牆，形成一個 10x10 的房間」) 即可開始使用。

---

## 安全注意事項

1.  **API Key 安全**: 請勿將包含真實 API Key 的 `.env` 檔案 commit 到版本控制系統 (Git) 中。`.gitignore` 應已預設排除此檔案。
2.  **DLL 封鎖**: Windows 可能會為了安全封鎖從網路下載的 DLL 檔案。
    *   若 Revit 啟動時發生錯誤，請至 `MCP\bin\Release` 資料夾，對 `RevitMCP.dll` 點擊右鍵 -> **內容** -> 勾選 **「解除封鎖 (Unblock)」**。
3.  **模型執行**: 請注意使用 `delete_elements` 或 `reset_model` 等破壞性指令時，建議先備份 Revit 專案。
