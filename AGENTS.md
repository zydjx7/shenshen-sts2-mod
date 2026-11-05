# 光之魔法少女深深：项目开发约定

使用中文沟通。先阅读 `README.md`、`docs/windows-handoff.md`、`docs/character-design.md`；工具与路径配置见 `docs/development.md`。

## 设计与当前状态

- `docs/character-design.md` 是已确认玩法的依据。用户已反复明确：「炸毛」首次扣血奖励整场战斗只有首次，不是每回合刷新；不要重复询问已确认规则。
- 少女与猫是同一个角色的互斥形态。只使用玩家实际拥有的两套牌及原卡牌实例，保留升级和卡牌状态，不能用随机生成牌冒充换牌。
- 单人、多人、macOS 与 Windows 都是开发目标。多人祝福仅作用于其他队友；两套牌堆和形态需要保存及联机同步。
- Mac 交接提交 `9b303ab` 是模板角色；Windows 接续已加入独立卡池、两套实际持有的基础牌与变形换牌，后续效果和验证范围见 `docs/windows-validation.md`。代码实现、真实程序集回归、编译打包与游戏界面实测必须分别报告。
- 用户目前准备换机继续开发，不再追加本轮设计问题。先完成 Windows 环境与构建核对，再按接续说明推进最小玩法原型。未定内容须标为原型选择，不能声称用户已确认。

## 工具与验证

- 使用 `tools/bootstrap.ps1` / `tools/build.ps1`（Windows），或相应 `.sh`（macOS）。版本以仓库锁文件为准。
- 运行项目内 .NET 工具时，显式设置项目内的 `DOTNET_ROOT`、`DOTNET_CLI_HOME`、`NUGET_PACKAGES` 与进程 PATH，沿用现有构建脚本；避免无意使用全局 CLI 用户目录。
- 每机路径只写忽略的 `local.props`。`local/`、`work/`、构建产物、游戏程序集、反编译内容及存档不进入 Git。
- 构建默认输出 `dist/`。游戏测试使用专用测试档，记录是否实际启动游戏、验证了哪些机制和平台；编译、打包与游戏内验证分别报告。
- 教程的 public-beta 或较新版本 API 不能直接视为本机 API；以当前安装游戏及依赖核对。参考资料见 `docs/tutorial-notes.md`。
- 开始前检查 Git 状态；换机前提交并推送本轮修改。源码和素材用 Git 同步，本机工具及游戏存档各自管理。
