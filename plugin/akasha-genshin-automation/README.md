# Akasha 原神自动化

AkashaNavigator 的进程外原神自动化插件。设置面板按功能分组，提供两个 Profile 级总开关：

- 自动拾取
- 自动剧情

两个功能默认关闭。自动拾取可配置交互键、内置黑名单和自定义名单；名单使用多行输入，每行一项，对话／机关白名单填写任意有效项目后自动启用。自动剧情可配置选项顺序、推进按键、动作延迟和剧情辅助操作。VAD、自定义剧情关键词、后台输入以及识别算法参数不在用户面板中开放。

保存插件设置后，AkashaNavigator 会重载插件、启动随包分发的 Worker，并把当前 Profile 的完整设置同步给 Worker。切换 Profile、禁用或卸载插件时，Worker 会被停止。

真实输入仅在原神窗口位于前台时发送。如果原神以管理员权限运行，AkashaNavigator 也必须以管理员权限运行，否则 Windows 会拦截输入。

## 开发测试

在 `akasha-automation` 仓库执行：

```powershell
.\scripts\Install-DevPlugin.ps1
```

脚本会构建相邻的 `AkashaNavigator` 仓库，并把插件和 Worker 发布到 Navigator 的 Debug 输出。随后启动脚本打印的 `AkashaNavigator.exe`，在插件中心安装“Akasha 原神自动化”，再把它添加到当前原神 Profile。

如果已经安装旧开发版，请在插件中心执行更新；看不到更新入口时，卸载插件后重新安装。Profile 设置文件独立于插件本体，重装不会改变已有功能配置。

## 正式安装

从本仓库 Release 下载 `akasha-genshin-automation-v*.zip`，不要手动解压：

1. 打开 AkashaNavigator 的“插件中心”。
2. 进入“可用插件”，点击“从 ZIP 安装”。
3. 选择下载的 ZIP，并确认 companion 高风险权限。
4. 转到“已安装插件”，把“Akasha 原神自动化”添加到原神 Profile。
5. 在 Profile 的插件设置中开启自动拾取或自动剧情。

安装更新时再次导入更高版本 ZIP 即可；Profile 配置存放在插件包之外，不会被覆盖。
