# DevHost 实机测试

`AkashaAutomation.DevHost` 是开发专用的独立控制台程序。它不连接 AkashaNavigator、不使用 companion 管道，直接运行真实窗口发现、Windows Graphics Capture、模板匹配、PaddleOCR、AutoPick/AutoDialogue 规则和 Input Arbiter。

DevHost 永久为 observe-only：项目中只注册 `ObserveOnlyInputService`，不会发送键盘或鼠标输入，也没有启用真实输入的参数。

## AutoPick 启动

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

## AutoDialogue 启动

```powershell
dotnet run `
  --project .\src\AkashaAutomation.DevHost\AkashaAutomation.DevHost.csproj `
  --configuration Release `
  -- --feature auto-dialogue --option-strategy first
```

常用变体：

```powershell
# 不自动选择普通选项，只观察暂停/识别结果
--feature auto-dialogue --option-strategy none

# 自定义优先选项
--feature auto-dialogue --custom-option "我准备好了"

# 启用游戏进程 loopback + Silero VAD；不可用时自动回退固定延迟
--feature auto-dialogue --voice-wait
```

AutoDialogue 日志字段：

```text
ui=Talk options="选项一 | 选项二" reason=fallback_first wouldAct=true voiceWait=false fallback=false arbiter=executed
```

建议依次验证：

| 场景 | 预期结果 |
|---|---|
| 普通剧情对白 | `ui=Talk`，`reason=advance_dialogue` |
| 两个普通选项 | `options` 列出文字，按策略得到 `fallback_first/last/random` |
| 内置暂停关键词 | `pause_priority` 或 `default_pause_priority`，`wouldAct=false` |
| 橙色选项 | `orange_option`；每日/派遣分别显示专用 reason |
| 黑屏剧情 | `black_screen` |
| 页面、道具或角色介绍弹窗 | `popup_page_close`、`item_popup_triangle` 或 `character_popup` |
| 提交物品 | `submit_select_goods → submit_put_in → submit_delivery` |
| 邀约 | `hangout_option` 或 `hangout_skip` |

这些 `wouldAct=true` 仍只代表“正式输入模式下会提交意图”；DevHost 实际发送始终为 0。

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
--feature auto-pick|auto-dialogue
--option-strategy first|last|random|none
--custom-option TEXT
--advance-key space|interaction
--voice-wait
--hangout-ending TEXT
--help
```

名单参数可以重复。例如：

```powershell
dotnet run `
  --project .\src\AkashaAutomation.DevHost\AkashaAutomation.DevHost.csproj `
  --configuration Release `
  -- --whitelist "与凯瑟琳对话" --fuzzy-blacklist "测试" --show-all
```
