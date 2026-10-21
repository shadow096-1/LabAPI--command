# PlayerLevels 玩家等级插件

适用：SCP: Secret Laboratory，LabAPI 1.1.7。插件使用游戏原生提示显示下方两行信息。

## 懒人安装：只下载一个 DLL

1. 下载 [PlayerLevels.dll](../../dist/PlayerLevels.dll)。
2. 停止服务器，把它放进 `LabAPI/plugins/<端口>/` 或 `LabAPI/plugins/global/`，替换旧版；只留一份。
3. 启动服务器。`config.yml`、`players.txt`、`multipliers.txt` 会自动创建，无需手工放配置文件。

已有旧版 `players.json` 会自动转为两份 TXT；旧文件保留作备份。不要覆盖玩家的旧存档。需要修改经验倍数时，可直接编辑 `multipliers.txt` 并在管理员面板输入 `levelreload`。

## 安装

1. 将 `PlayerLevels.dll` 放入 LabAPI 的插件目录，替换旧版 DLL。
2. 启动服务器。插件会在自己的配置目录自动生成 `config.yml`、`players.txt`、`multipliers.txt`。
3. 如果同目录存在旧版 `players.json`，首次启动会将其中的等级和倍数分别迁移到两份 TXT；旧 JSON 不会删除。迁移完成后，以 TXT 为准。

`players.txt` 每行写 `SteamID | 等级 | 当前经验 | 累计积分`，例如：

```text
76561198000000000@steam | 12 | 20 | 130
```

`multipliers.txt` 每行写 `SteamID | 倍数`；未列出的玩家默认为 1 倍，例如：

```text
76561198000000000@steam | 2
```

以 `#` 开头的行是注释。编辑两份 TXT 后，在远程管理面板输入 `levelreload` 即可立即生效，也可以重启服务器。无效行会显示行号并保留内存中的旧数据。

## 积分

| 事件 | 基础积分 |
| --- | ---: |
| 参与一局 | 2 |
| D级人员、科学家成功撤离 | 5 |
| 击杀主力 SCP | 6 |
| 击杀敌对人员 | 2 |
| 击杀 SCP-049-2 | 3 |

同阵营击杀不给击杀积分。参与一局的积分在回合结束时发放，一局一名玩家只领一次；中途断线的参与者也会计入。管理员倍数会乘在上述积分上。

| 当前等级 | 升到下一级所需经验 |
| --- | ---: |
| 1–10 | 10 |
| 11–50 | 50 |
| 51–100 | 100 |
| 101–200 | 200 |
| 201 起 | 300 |

玩家列表只显示 `[Lv.等级]昵称`，始终从玩家原始昵称生成，不拼接旧显示名中的颜色代码。阵营只在底部小字两行中显示，连同欢迎语、角色、状态、等级、当前经验和本次连接的游玩时间；重新连接后计时从零开始。旧版默认配置 `hud_y: 850`、`hud_font_size: 22` 加载时会自动采用新版显示位置与字号。

## 管理员后台命令

在远程管理面板输入：

```text
levelboost 7656119xxxxxxxxxx@steam 2
```

最后的整数是经验倍数；设置为 `1` 即恢复普通积分。省略倍数可查询，例如 `levelboost 7656119xxxxxxxxxx@steam`。默认最大值为 100，可通过 `max_admin_multiplier` 调整。

## 配置

`config.yml` 可以调整奖励、界面纵向位置 `hud_y`、字号 `hud_font_size` 与刷新周期 `hud_refresh_seconds`。升级 DLL 时请保留 `players.txt`、`multipliers.txt` 和 `config.yml`。
