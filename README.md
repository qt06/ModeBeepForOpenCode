# ModeBeep

Windows 系统托盘小工具：监听 opencode 终端的 Tab 键切换，当 agent 模式在 `plan` / `build` 之间切换时播放对应的提示音。

## 功能

- 全局低层键盘钩子（`WH_KEYBOARD_LL`）监听 Tab 键
- 通过 **UI Automation** 读取 opencode TUI 界面上的模式徽标（badge），识别当前 agent
- 模式变化时播放对应 WAV 提示音（无配置时可用系统提示音兜底）
- 托盘图标 + 右键菜单（测试声音 / 退出）
- 单实例运行（命名互斥体 `Local\ModeBeepSingleInstance`）

## 环境要求

- Windows 10/11
- .NET 10 SDK（`net10.0-windows`，含 Windows Desktop）

## 构建与运行

```powershell
dotnet build
dotnet run
```

发布单文件可执行文件（可选）：

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

## 配置 `config.json`

配置与可执行文件同目录，首次缺失时使用默认值。示例：

```json
{
  "windowTitleFilters": ["OC |", "Opencode"],
  "processNames": ["WindowsTerminal"],
  "agents": ["plan", "build"],
  "sounds": {
    "plan": "sounds\\plan.wav",
    "build": "sounds\\build.wav"
  },
  "fallbackSound": true,
  "delayMs": 180
}
```

| 字段 | 说明 |
| --- | --- |
| `windowTitleFilters` | opencode 终端窗口标题需包含的任一词（大小写不敏感） |
| `processNames` | 承载 opencode 终端窗口的进程名 |
| `agents` | 视为可切换模式的 agent 名称白名单 |
| `sounds` | 每个 agent 对应的 WAV 文件路径（相对于 config.json） |
| `fallbackSound` | 无对应声音文件时是否播放系统提示音 |
| `delayMs` | 按下 Tab 后延迟多久再读取模式（毫秒） |

## 命令行参数

| 参数 | 说明 |
| --- | --- |
| `--probe` | 诊断模式：输出当前检测到的 agent、原始屏幕文本等信息，并写入 `%TEMP%\modebeep-probe.txt` |
| `--test-sound` | 依次播放所有已配置 agent 的声音 |

## 项目结构

| 文件 | 职责 |
| --- | --- |
| `Program.cs` | 入口、Win32 P/Invoke、托盘图标、消息循环、窗口过程、模式切换逻辑 |
| `Config.cs` | 配置加载（JSON，source-generated 反序列化） |
| `ModeDetector.cs` | 判断前台窗口是否为 opencode 终端，并用 UIA 读取 agent 徽标 |
| `AppSound.cs` | 通过 `winmm.dll` 播放 WAV，支持回退到系统声音 |
| `KeyboardHook.cs` | 全局低层键盘钩子，Tab 按下时回调 |
| `config.json` | 运行时配置 |
| `sounds/` | 各 agent 的 WAV 提示音 |

## 说明

本项目代码由 AI 辅助生成（opencode 编写），仅供个人学习与使用。

## 日志

运行期间默认**不写日志文件**。`Program.DLog` / `AppSound.Log` 保留了注释掉的
`modebeep.log` 写入逻辑，如需开启调试日志，取消对应方法体内的注释后重新编译即可。
