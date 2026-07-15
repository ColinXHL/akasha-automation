# AutoPick DevHost 实机测试

`AkashaAutomation.DevHost` 是开发专用的独立控制台程序。它不连接 AkashaNavigator、不使用 companion 管道，直接运行真实窗口发现、Windows Graphics Capture、模板匹配、PaddleOCR、AutoPick 规则和 Input Arbiter。

DevHost 永久为 observe-only：项目中只注册 `ObserveOnlyInputService`，不会发送键盘或鼠标输入，也没有启用真实输入的参数。

## 启动

1. 启动原神，推荐使用窗口化或无边框窗口模式。
2. 如果原神以管理员身份运行，也用管理员 PowerShell 启动 DevHost。
3. 在 `akasha-automation` 仓库根目录执行：

```powershell
dotnet run `
  --project .\src\AkashaAutomation.DevHost\AkashaAutomation.DevHost.csproj `
  --configuration Release `
  -- --pick-key F
```

首次启动会编译项目。之后也可以直接运行生成的 EXE：

```powershell
.\src\AkashaAutomation.DevHost\bin\Release\net8.0-windows10.0.19041.0\win-x64\AkashaAutomation.DevHost.exe --pick-key F
```

按 `Ctrl+C` 会先触发 Input Arbiter 急停，再释放截图和 OCR 资源。

## 观察输出

正常输出示例：

```text
[21:10:01.120] waiting reason=game_window_not_found
[21:10:08.430] frame=1 text=<none> reason=interaction_key_not_found wouldPress=false arbiter=no_intent
[21:10:12.840] frame=24 text="甜甜花" reason=pick wouldPress=true arbiter=executed
[21:10:18.210] frame=57 text=<none> reason=chat_icon wouldPress=false arbiter=no_intent
```

`wouldPress=true` 只表示正式 Worker 在该帧会提交按键意图；DevHost 实际发送的输入永远是 0 组。默认只在结果变化时打印，使用 `--show-all` 可打印每一帧。

建议依次靠近以下目标：

| 场景 | 预期 reason | wouldPress |
|---|---|---:|
| 甜甜花、薄荷等普通拾取物 | `pick` | `true` |
| NPC 对话 | `chat_icon` | `false` |
| 机关或解谜交互 | `settings_icon` | `false` |
| 烹饪等默认黑名单条目 | `blacklist_exact` | `false` |
| 没有交互提示 | `interaction_key_not_found` | `false` |

## 参数

```text
--pick-key E|F|G
--interval-ms 25..2000
--no-blacklist
--exact-blacklist TEXT
--fuzzy-blacklist TEXT
--whitelist TEXT
--show-all
--help
```

名单参数可以重复。例如：

```powershell
dotnet run `
  --project .\src\AkashaAutomation.DevHost\AkashaAutomation.DevHost.csproj `
  --configuration Release `
  -- --whitelist "与凯瑟琳对话" --fuzzy-blacklist "测试" --show-all
```
