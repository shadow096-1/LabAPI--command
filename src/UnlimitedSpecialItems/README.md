# UnlimitedSpecialItems

独立 LabAPI 1.1.7 插件。默认只放宽特殊武器上限至背包总容量 8 格；枪械、药物、SCP 物品的上限直接由游戏服务器配置控制。服装/护甲对类别容量的修正仍由游戏计算。

将 `UnlimitedSpecialItems.dll` 放入服务器 LabAPI 插件目录，替换旧版并重启。自身的 `config.yml` 可用 `is_enabled: false` 关闭功能。只有确实想忽略游戏配置中的 SCP 物品上限时才设 `unlimited_scp_items: true`。

打开 `C:\Users\<Windows用户>\AppData\Roaming\SCP Secret Laboratory\config\<端口>\config_gameplay.txt`（使用自定义配置目录的服务器以实际目录为准），添加或修改：

```yaml
limit_category_firearm: 8
limit_category_medical: 8
limit_category_scpitem: 8
```

这三个设置分别对应枪械、药物、SCP 物品。特殊武器在这个游戏版本中不读取 `limit_category_specialweapon`，所以由本插件处理。背包总共仍只有 8 格。改完游戏配置后重启服务器。

若同一服务器有其他插件也改写这两类的类别容量，插件启停时最后一次同步的设置将生效。
