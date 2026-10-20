# LabAPI--command

SCP: Secret Laboratory 的 LabAPI 中文聊天插件。代码从
[`shadow096-1/SL-Translate-CN`](https://github.com/shadow096-1/SL-Translate-CN/tree/3d711586584d7bfd11a5cc4b7afc54c28d4dc48a)
迁入此仓库，保留 `SLTranslateCN.dll`、插件名称及 `sl_translate_cn.yml` 配置文件名，方便已有服务器更新。

## 下载

当前插件版本：**1.0.3**。下载 [`SLTranslateCN.dll`](dist/SLTranslateCN.dll) 后点击 GitHub 文件页的下载按钮保存二进制文件。
这是服务器端 LabAPI 插件，不是 LabAPI 框架本体。

## 命令

- `.bc <内容>`：公屏聊天，发送给已就绪玩家。
- `.c <内容>`：阵营聊天，仅发送给同阵营的已就绪玩家。
- `.ac <内容>`：举报，仅发送给已就绪且有 Remote Admin 访问权限的玩家，并记录到服务器日志；无人在线时也记录日志。

消息包含发送者阵营、存活状态及角色。`.bc`、`.c` 仅显示左上方小字；`.ac` 保留顶部大字，同时显示左上方小字，仅管理员接收。三种消息默认均为 10 秒，不做永久显示；可配置客户端控制台纯文本副本。

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
hint_font_size: 28
send_console_copy: true
```

旧配置不必删除，新增字段使用默认值。`hint_vertical_offset` 为相对原提示基线向上的偏移（0–700），`hint_font_size` 为字号（16–40）；不同分辨率可适当微调。原有 `*_broadcast_duration` 配置名保留兼容，公屏和阵营的值现在控制小字时长。

## 本次修正

- 顶部大字仅用于 `.ac`；小字改为左对齐并上移，举报保持默认 10 秒。

- 补齐游戏程序集引用及缺失依赖提示，增加 .NET Framework 4.8 参考程序集包。
- 插件未加载、配置未加载或已禁用时拒绝聊天命令。
- 在检查空消息前统一清理输入；替换富文本标签符号和控制字符，日志中的玩家名与消息也进行清理。
- 公屏命令回执使用实际配置时长。

## 验证

```sh
dotnet run --project tests/ChatRegression/ChatRegression.csproj -c Release
```

GitHub Actions 自动执行这组回归检查，覆盖启停、输入、消息路由、举报日志和配置时长。
回归测试链接实际聊天源码，使用游戏/LabAPI 的测试替身。
此外，`dist/SLTranslateCN.dll` 已通过上传的实际游戏程序集编译；目标框架为 .NET Framework 4.8，平台为 AnyCPU。
尚未启动真实游戏服务器测试，请在服务器中核对 `.bc`、`.c`、`.ac` 的显示和接收范围。

不参与本插件的旧 Exiled 自动回血代码已从当前分支清理；如需恢复，可查看提交 `52562da718f4c8553c7d7960397131972a603e42`。
