# SCP:SL 服务器插件

SCP: Secret Laboratory 的 LabAPI 中文聊天插件。代码从
[`shadow096-1/SL-Translate-CN`](https://github.com/shadow096-1/SL-Translate-CN/tree/3d711586584d7bfd11a5cc4b7afc54c28d4dc48a)
迁入此仓库，保留 `SLTranslateCN.dll`、插件名称及 `sl_translate_cn.yml` 配置文件名，方便已有服务器更新。

## 懒人下载

下载 [四个插件一包（平铺 ZIP）](dist/SCP-SL-Plugins-2026-10-22.zip)，停服后把里面的 DLL 替换到 `LabAPI/plugins/<端口>/` 或 `LabAPI/plugins/global/`，同名插件只放一份，再启动服务器。

| 插件 | 单独下载 | 功能 |
| --- | --- | --- |
| 聊天 1.3.0 | [SLTranslateCN.dll](dist/SLTranslateCN.dll) | `.bc` 公屏、`.c` 阵营、`.ac` 举报；禁言玩家只能使用举报 |
| 等级 1.2.0 | [PlayerLevels.dll](dist/PlayerLevels.dll) | 自动生成可编辑的等级、倍数 TXT；共用提示画面 |
| SCP 状态 1.1.0 | [ScpStatus.dll](dist/ScpStatus.dll) | SCP 静止回血、右上角队友信息 |
| 物品上限 1.1.0 | [UnlimitedSpecialItems.dll](dist/UnlimitedSpecialItems.dll) | 放宽特殊武器单独携带上限 |

**聊天、等级、SCP 状态需要同时换成这次的版本。** 等级插件统一发送画面，三种内容就不会互相顶掉。详细说明：[等级插件](src/PlayerLevels/README.md) · [SCP 状态](src/ScpStatus/README.md) · [物品上限](src/UnlimitedSpecialItems/README.md)。

## 命令

- `.bc <内容>`：公屏聊天，发送给已就绪玩家。
- `.c <内容>`：阵营聊天，仅发送给同阵营的已就绪玩家。
- `.ac <内容>`：举报，仅发送给已就绪且有 Remote Admin 访问权限的玩家，并记录到服务器日志；无人在线时记录日志；配置 QQ 机器人后自动转发到群并艾特指定 QQ。

聊天行只包含频道、昵称前六字（超出省略）和内容，不显示角色身份。`.bc`、`.c` 仅显示左上方小字；`.ac` 保留顶部大字，同时显示左上方小字，仅管理员接收。三种消息至少显示 10 秒，不做永久显示；可配置客户端控制台纯文本副本。

## 构建

需要 .NET SDK 8 或更高版本，以及与你的目标服务器匹配的 SCP:SL 游戏程序集。
工程优先引用 `SL_REFERENCES` 中的 `LabApi.dll`；未提供时使用 `Northwood.LabAPI` 1.1.7。
本次使用上传的真实服务器程序集（LabAPI 1.1.7.0）编译，0 错误、0 警告。

将 `SL_REFERENCES` 指向服务器的 `SCPSL_Data/Managed` 目录，其中需要：

- `LabApi.dll`（建议提供，确保与服务器版本一致）
- `Assembly-CSharp.dll`
- `Assembly-CSharp-firstpass.dll`
- `CommandSystem.Core.dll`
- `Mirror.dll`
- `UnityEngine.CoreModule.dll`

Windows PowerShell：

```powershell
$env:SL_REFERENCES = 'C:\SCPServer\SCPSL_Data\Managed'
dotnet build LabAPI--command.sln -c Release
```

Linux：

```sh
SL_REFERENCES=/path/to/server/SCPSL_Data/Managed dotnet build LabAPI--command.sln -c Release
```

缺少引用时，构建会给出明确错误。程序集来自服务器，不存入本仓库。
参考：[LabAPI 官方命令示例的工程引用](https://github.com/northwood-studios/LabAPI/blob/1.1.5/LabApi.Examples/Commands/CommandsPlugin/CommandsPlugin.csproj)。

## 安装和配置

可直接使用 [`dist/SLTranslateCN.dll`](dist/SLTranslateCN.dll)，或自行构建后取
`src/SLTranslateCN/bin/Release/net48/SLTranslateCN.dll`，放入服务器的
`LabAPI/plugins/<端口>` 或 `LabAPI/plugins/global`。更新时替换旧 DLL，不要同时加载两份插件。
插件配置目录内的 `sl_translate_cn.yml` 支持：

```yaml
is_enabled: true
public_broadcast_duration: 10
faction_broadcast_duration: 10
admin_report_broadcast_duration: 10
hint_vertical_offset: 500
hint_font_size: 14
send_console_copy: true
qq_report_enabled: false
qq_bot_api_url: "http://127.0.0.1:3000"
qq_bot_access_token: ""
qq_report_group_id: 0
qq_report_at_qq: 0
```

旧配置不必删除，新增字段使用默认值。`hint_vertical_offset` 为相对原提示基线向上的偏移（0–700），`hint_font_size` 为字号（实际限制 10–16，旧配置中较大的值会自动压到 16）；不同分辨率可适当微调。原有 `*_broadcast_duration` 配置名保留兼容，公屏和阵营的值现在控制小字时长。

命令发送回执仅显示“发送成功。”或“发送失败。”，不显示接收人数。阵营标签使用“混沌”，不显示“敌对”。

## QQ 机器人转发（可选）

支持 OneBot 11 HTTP 接口（如 NapCat）。有管理员在线时，`.ac` 只在游戏内发送；没有在线管理时，若配置完整，`.ac` 和玩家列表内置的“举报玩家”及“举报作弊”都会异步向机器人 `POST /send_group_msg`，发送群消息并艾特 `qq_report_at_qq`。内置举报消息带上被举报玩家的显示名和举报原因。

在服务器配置文件中设置 `qq_report_enabled: true`，并填入可由游戏服务器访问的机器人 **HTTP API 基础地址**（例如本机的 `http://127.0.0.1:3000`）、`qq_report_group_id`、`qq_report_at_qq`；机器人设置了 access token 才需填写 `qq_bot_access_token`。机器人 QQ 必须在目标群内。不要填写 WebSocket 或反向事件上报地址。重启服务器后生效。

玩家看见“发送成功”表示举报已经交给异步转发流程，不代表 QQ 已确认投递。接口失败或机器人返回失败时，服务器日志会记录错误，但不会泄露 token 和举报正文；举报正文仍有常规举报日志。没有配置机器人时维持原行为，显示“发送失败”。为避免把玩家身份标识公开到群里，机器人只发送游戏内显示名、阵营、状态、角色和举报内容。

## 本次修正

- 顶部大字仅用于 `.ac`；小字改为左对齐并上移，举报保持默认 10 秒。

- 补齐游戏程序集引用及缺失依赖提示，增加 .NET Framework 4.8 参考程序集包。
- 插件未加载、配置未加载或已禁用时拒绝聊天命令。
- 在检查空消息前统一清理输入；替换富文本标签符号和控制字符，日志中的玩家名与消息也进行清理。
- 简化发送回执，删除接收人数和敌对字样。

## 验证

```sh
dotnet run --project tests/ChatRegression/ChatRegression.csproj -c Release
```

GitHub Actions 自动执行这组回归检查，覆盖启停、输入、消息路由、举报日志和配置时长。
回归测试链接实际聊天源码，使用游戏/LabAPI 的测试替身。
此外，`dist/SLTranslateCN.dll` 已通过上传的实际游戏程序集编译；目标框架为 .NET Framework 4.8，平台为 AnyCPU。
尚未启动真实游戏服务器测试，请在服务器中核对 `.bc`、`.c`、`.ac` 的显示和接收范围。

不参与本插件的旧 Exiled 自动回血代码已从当前分支清理；如需恢复，可查看提交 `52562da718f4c8553c7d7960397131972a603e42`。

## 游戏自带物品上限

在 Windows 的 `%APPDATA%\\SCP Secret Laboratory\\config\\<端口>\\config_gameplay.txt` 中填写：

```yaml
limit_category_firearm: 8
limit_category_medical: 8
limit_category_scpitem: 8
```

这三项分别是枪械、药物和 SCP 物品上限，背包合计仍只有 8 格。这个游戏版本的特殊武器上限不读取同类配置项，因此由新版 `UnlimitedSpecialItems.dll` 解除。旧版上限 DLL 同时覆盖 SCP 物品配置，请务必替换。
