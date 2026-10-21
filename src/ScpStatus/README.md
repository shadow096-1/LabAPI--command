# ScpStatus SCP 状态

适用于 SCP: Secret Laboratory / LabAPI 1.1.7。将 `ScpStatus.dll` 放入服务器 `LabAPI/plugins/<端口>/` 或 `LabAPI/plugins/global/`，只留一份并重启。

- 管理员面板输入 `scphp 173 3500` 设置固定最大血量，`scphp 173 off` 恢复默认，`scphp list` 查询；SCP-079 没有可设置的实体血量。
- 站立不动 5 秒后每秒恢复 5 点，转视角不算移动。
- SCP 队员可在右上角查看队友血量、护盾和 049-2 数量。
- 同时使用等级插件时请安装新版 `PlayerLevels.dll`，三个插件共用一次原生提示显示，不再互相顶掉。

首次运行自动创建 `config.yml`，可以调整回血和右上角文字大小。旧版配置会继续使用。
