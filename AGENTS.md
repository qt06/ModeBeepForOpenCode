# AGENTS.md

## 项目概述

ModeBeep：Windows 系统托盘应用，当 opencode 终端在 agent 模式（plan/build）间切换（Tab 键）时播放提示音。纯 Win32 P/Invoke + UI Automation，无第三方 NuGet 依赖。

## 构建 / 验证

```powershell
dotnet build        # 编译（必须通过，0 警告 0 错误）
dotnet run          # 运行
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

修改代码后必须执行 `dotnet build` 确认通过。本项目无自动化测试。

## 技术栈与约定

- 语言：C#，目标框架 `net10.0-windows`（WinExe），启用 `<ImplicitUsings>` 和 `<Nullable>`，不使用 `TopLevelStatements`
- 样式：花括号独占一行（Allman），4 空格缩进；方法不写注释前缀除非必须，但公开成员/复杂逻辑保留简洁 XML 文档注释
- 所有 Win32 API 经 `Program.cs` 中的 `Win32` 静态类 `[DllImport]` 声明，或各文件内部私有声明
- 配置通过 `Config.Load()` 读取 `config.json`（source-generated `JsonSerializerContext`，属性名大小写不敏感）
- 不支持中文的 API 交互统一用 `W` 后缀 Unicode 版本（如 `RegisterClassW`、`CreateWindowExW`）

## 关键机制

- **模式检测**（`ModeDetector.cs`）：前台窗口进程名命中 `processNames` 且窗口标题包含 `windowTitleFilters` 任一项 → 用 UIA `TextPattern` 读取屏幕文本 → 正则匹配徽标行（agent 名紧邻 ● 或 ·）返回 agent
- **切换逻辑**（`Program.cs`）：`OnTabPressed` → 延迟 `delayMs` 后用定时器触发 `TrySwitchMode`，agent 变化才播放声音（`_lastAgent` 去重）
- **键盘钩子**（`KeyboardHook.cs`）：低层钩子 `WH_KEYBOARD_LL`，回调在同一线程（消息循环）执行
- **单实例**：`Main` 中命名 Mutex `Local\ModeBeepSingleInstance`

## 日志

运行期间默认不写日志文件。`DLog`（Program.cs）与 `Log`（AppSound.cs）的方法体已注释
（原写入 `modebeep.log`）。调试排查可取消注释恢复，但不要默认开启。

## 命令行参数

- `--probe`：诊断，结果写入 `%TEMP%\modebeep-probe.txt` 并在附加的控制台输出
- `--test-sound`：依次播放所有配置 agent 的声音

## 修改注意

- 改配置字段时同步更新 `config.json` 与 `Config.cs` 的默认值/注释
- 新增 WAV 提示音放在 `sounds/`，并在 `config.json` 的 `sounds` 中登记路径
- `config.json` 与 `sounds/*.wav` 已配置 `CopyToOutputDirectory=PreserveNewest`
- 托盘图标是普通应用图标（`LoadImageW(IDI_APPLICATION)`），如需自定义需改动 `SetupTrayIcon`
