# 天堂輔助程式 (LineageHelper)

## 專案說明
- 用途：學習遊戲輔助程式開發技術
- 語言：C# (.NET Framework 4.7+)
- 目標：Windows 桌面應用程式

## 技術架構
```
LineageHelper/
├── Core/              # 核心邏輯
│   ├── BotEngine.cs  # 機器人引擎（狀態機）
│   └── GameHelper.cs # 遊戲助手主類
├── Memory/           # 記憶體操作
│   ├── MemoryReader.cs    # 記憶體讀取
│   ├── MemoryWriter.cs    # 記憶體寫入
│   └── ProcessHelper.cs   # 進程處理
├── Network/          # 網路通訊
│   ├── SocketManager.cs   # Socket 管理
│   └── UpdateChecker.cs   # 更新檢查
├── UI/               # 視窗介面
│   └── MainWindow.xaml    # 主視窗
└── Utils/            # 工具類
    ├── HotKeyManager.cs  # 熱鍵管理
    └── Logger.cs         # 日誌記錄
```

## 開發階段
1. 基礎：找到遊戲進程 + 讀取記憶體
2. 中級：解析遊戲數據 (HP/MP/座標)
3. 進階：自動操作 (鍵盤/滑鼠模擬)
4. 高級：遠端遙控 + 保護機制

## 熱鍵設計
| 鍵位 | 功能 |
|------|------|
| F1 | 隱藏遊戲 |
| E | 戰鬥 |
| Q | 老闆鍵 (快速隱藏) |
| W | 暫停 |

## 數據偏移量 (待研究)
- HP 地址偏移
- MP 地址偏移
- 角色座標偏移
- 怪物列表偏移
