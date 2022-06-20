<img src="./Xunkong.SimpleLauncher/Icon.png" style="float: left;" />

# 原神 简单启动器

> 寻空的右键快速启动在 Win11 上不能用了

正如其名，这是一个简单启动器，直接运行时会打开原神游戏本体，以管理员权限运行时会打开原神启动器。

如果设置了米游社Cookie，启动成功后自动检查洞天宝钱和参量质变仪的状态，可收取或可用则会发送系统通知，否则无事发生。

压缩包内有一个设置文件 `Config.txt`，以下设置均**可不填写**：

| 名称 | 可填内容 | 备注 |
| --- | --- | --- |
| GenshinGamePath | `string` | 原神本体位置，不要有引号 |
| GenshinLauncherPath | `string` | 原神启动器位置，不要有引号 |
| IsPopupWindow | `bool` | 是否以无边框窗口启动 |
| IsFullScreen | `bool` | 是否以全屏模式启动 |
| Width | `uint` | 启动时窗口宽，以全屏模式启动时忽略此项 |
| Height |`uint` | 启动时窗口高，以全屏模式启动时忽略此项 |
| Cookie | `string` | 米游社Cookie，不要换行 |
| HomeCoinNotifyRatio | `double` |  洞天宝钱提醒阈值，0到1之间 |

> 顶级语句非常适合写面向过程