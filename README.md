# Caps Lock Indicator

Windows 常駐小工具：按下 **Caps Lock** 時，在螢幕右下角、工作列正上方浮出灰底白字的 `Caps Lock ON` / `Caps Lock OFF` 提示，預設文字大小 **36pt**，可在設定中調整。

## 功能

- 全域鍵盤勾點（low-level keyboard hook）偵測 Caps Lock，任何程式在前景都會顯示
- 另以 250ms 輪詢補捉非實體按鍵造成的狀態變化（螢幕小鍵盤、遠端桌面等）
- 提示視窗不搶焦點、可點擊穿透、不出現在 Alt+Tab，數秒後淡出
- Windows 11 自動圓角
- 系統匣圖示同步顯示目前狀態（實心 `A` = ON，空心 `a` = OFF）
- 單一執行個體

## 設定

在系統匣圖示上按右鍵 →「設定 Settings...」（或雙擊圖示）：

| 項目 | 預設值 | 範圍 |
|---|---|---|
| 文字大小 (pt) | 36 | 8 – 200 |
| 字型 / 粗體 | Segoe UI / 粗體 | 系統已安裝字型 |
| 背景顏色 | `#505050`（灰） | 任意 |
| 文字顏色 | `#FFFFFF`（白） | 任意 |
| 不透明度 (%) | 90 | 20 – 100 |
| 顯示時間 (ms) | 1500 | 300 – 10000 |
| 與邊緣距離 (px) | 16 | 0 – 500 |
| 開啟 / 關閉時文字 | `Caps Lock ON` / `Caps Lock OFF` | 任意 |
| 開機自動啟動 | 關 | 寫入 `HKCU\...\CurrentVersion\Run` |

「預覽」可即時看到效果；按「確定」才會存檔。設定存在 `%AppData%\CapsLockIndicator\settings.json`，也可直接編輯該檔後重啟程式。

## 建置

需求：Windows 10/11、[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
# Debug run
dotnet run --project src/CapsLockIndicator

# Single self-contained exe (no .NET runtime needed on the target PC)
dotnet publish src/CapsLockIndicator/CapsLockIndicator.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

產出：`publish/CapsLockIndicator.exe`。

也可以直接從 GitHub Actions 的 **Build** workflow 下載已編好的 `CapsLockIndicator-win-x64` artifact。

## 專案結構

```
src/CapsLockIndicator/
├── Program.cs                 進入點、單一執行個體
├── TrayApplicationContext.cs  系統匣圖示、選單、狀態偵測
├── KeyboardHook.cs            全域 Caps Lock 按鍵偵測
├── OsdForm.cs                 右下角浮出提示視窗
├── SettingsForm.cs            設定視窗
├── AppSettings.cs             設定讀寫、開機啟動
├── TrayIconFactory.cs         執行時繪製系統匣圖示
└── NativeMethods.cs           Win32 P/Invoke
```

## 備註

- 提示固定顯示在**主螢幕**的工作區右下角；`WorkingArea` 已扣除工作列，所以工作列在底部時會剛好位於其上方。
- 以系統管理員權限執行的視窗在前景時，一般權限的本程式可能收不到按鍵事件，此時由輪詢機制補上（最多延遲約 0.25 秒）。
