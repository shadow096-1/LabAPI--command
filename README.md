# LabAPI--command

SCP: Secret Laboratory 的 LabAPI 中文聊天插件。代码从
[`shadow096-1/SL-Translate-CN`](https://github.com/shadow096-1/SL-Translate-CN/tree/3d711586584d7bfd11a5cc4b7afc54c28d4dc48a)
迁入此仓库，保留 `SLTranslateCN.dll`、插件名称及 `sl_translate_cn.yml` 配置文件名，方便已有服务器更新。

## 命令

- `.bc <内容>`：公屏聊天，发送给已就绪玩家。
- `.c <内容>`：阵营聊天，仅发送给同阵营的已就绪玩家。
- `.ac <内容>`：举报，仅发送给已就绪且有 Remote Admin 访问权限的玩家，并记录到服务器日志；无人在线时也记录日志。

消息包含发送者阵营、存活状态及角色。画面广播和提示同时显示，默认 10 秒；可配置客户端控制台纯文本副本。

## 构建

需要 .NET SDK 8 或更高版本，以及与你的目标服务器匹配的 SCP:SL 游戏程序集。
工程使用 `Northwood.LabAPI` 1.1.5；尚未验证其他游戏/LabAPI 版本。

将 `SL_REFERENCES` 指向服务器的 `SCPSL_Data/Managed` 目录，其中需要：

- `Assembly-CSharp.dll`
- `Assembly-CSharp-firstpass.dll`
- `CommandSystem.Core.dll`
- `Mirror.dll`
- `UnityEngine.CoreModule.dll`

Windows PowerShell：

```powershell
$env:SL_REFERENCES = 'C:\SCPServer\SCPSL_Data\Managed'
dotnet build SL-Translate-CN.sln -c Release
```

Linux：

```sh
SL_REFERENCES=/path/to/server/SCPSL_Data/Managed dotnet build SL-Translate-CN.sln -c Release
```

缺少引用时，构建会给出明确错误。程序集来自服务器，不存入本仓库。
参考：[LabAPI 官方命令示例的工程引用](https://github.com/northwood-studios/LabAPI/blob/1.1.5/LabApi.Examples/Commands/CommandsPlugin/CommandsPlugin.csproj)。

## 安装和配置

将 `src/SLTranslateCN/bin/Release/net48/SLTranslateCN.dll` 放入服务器的
`LabAPI/plugins/<端口>` 或 `LabAPI/plugins/global`。更新时替换旧 DLL，不要同时加载两份插件。
插件配置目录内的 `sl_translate_cn.yml` 支持：

```yaml
is_enabled: true
public_broadcast_duration: 10
faction_broadcast_duration: 10
admin_report_broadcast_duration: 10
send_console_copy: true
```

## 本次修正

- 补齐游戏程序集引用及缺失依赖提示，增加 .NET Framework 4.8 参考程序集包。
- 插件未加载、配置未加载或已禁用时拒绝聊天命令。
- 在检查空消息前统一清理输入；替换富文本标签符号和控制字符，日志中的玩家名与消息也进行清理。
- 公屏命令回执使用实际配置时长。

## 验证

```sh
dotnet run --project tests/ChatRegression/ChatRegression.csproj -c Release
```

GitHub Actions 自动执行这组回归检查，覆盖启停、输入、消息路由、举报日志和配置时长。
测试链接实际聊天源码，使用游戏/LabAPI 的测试替身；通过测试不代表通过真实服务器程序集的编译或游戏内验证。
部署前仍需使用上述实际 DLL 构建，并在测试服务器核对三个命令。

`legacy/ScpAutoHeal.cs` 是原仓库中独立的 Exiled 自动回血代码，原样留存，不参与 LabAPI 聊天插件编译，也未移植到 LabAPI。
