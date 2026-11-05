# 光之魔法少女深深

《杀戮尖塔 2》原创角色模组，供个人使用。内部工程、程序集及模组 ID 统一为 `Shenshen`；目录名称可不同，两台电脑使用同一仓库。

角色有人形、猫形两种互斥形态，规则见 [角色设计](docs/character-design.md)。Windows 接续已实现独立卡池、两套实际持有的基础牌与变形换牌。当前是开发原型；各项代码、构建与实测范围见 [Windows 验证记录](docs/windows-validation.md)。

## 当前可用内容

- 可构建的 C# / Godot 角色工程，以及简体中文、英文文本。
- Mac 交接和本轮 Windows 均已安装项目内 .NET SDK、MegaDot 和反编译工具。
- Mac 与 Windows 的安装、构建、资源打包脚本；本机绝对路径放在忽略的 `local.props`。
- 版本锁、NuGet 依赖锁、统一编码和换行、Git 忽略规则。

70 HP、每回合 3 能量、正常抽牌 5 张；玩家永久持有少女 10 张与猫 10 张基础牌。变形只弃掉当前形态专属手牌，从另一形态的实际卡牌中补抽同样数量，保留原实例及其升级、费用等状态。牌堆界面暂显示两套牌总数；共享牌参与当前活跃循环是原型选择。燃烧之血、遗物/药水池、卡图、休息点与商店等场景表现仍为占位。资源打包使用独立临时 Godot 工程。

人形已选择 P1「樱粉圣光」白粉金外形，并按要求调整肤色与自然体态。战斗外观使用真正的 Godot 分件动画，包含待机、眨眼、施法、攻击、受击、倒下和双形态切换；选人背景、头像和地图标记独立替换。配套猫形仍是待定细节的临时美术，动作也可继续打磨；制作来源与验证边界见 [角色美术与动画](docs/character-art.md)。

## 使用

Mac：

```sh
./tools/bootstrap.sh
./tools/build.sh build
./tools/build.sh pack
```

Windows：

```powershell
./tools/bootstrap.ps1
./tools/build.ps1 build
./tools/build.ps1 pack
```

默认只向 `dist/` 输出，不改 Steam 设置、游戏安装目录或存档。`dist/Shenshen/` 包含本模组，`dist/BaseLib/` 包含匹配的运行依赖。游戏退出后，才可按需将这两个目录复制到游戏的 mods 目录进行测试。

Mac 交接版本已通过编译与资源检查；本轮 Windows 已通过编译打包、95 项真实程序集/Godot回归（45 项玩法、35 项动画、15 项 UI 资源），并在前一轮专用测试档实际验证角色入口、双向换牌、费用/格挡保留、变形牌回手与保留、猫形跨回合及保存退出后继续。新美术已完成独立 GPU 动画渲染与录制，但本轮游戏界面验收因用户按 Esc 停止自动操作而未完成，不能沿用旧占位形象的实测结论。当前保存沿用原版战前/房间边界，不恢复战斗中途快照；双端多人和 Mac 新代码仍待验证。完整范围见 [Windows 验证记录](docs/windows-validation.md)，日常协作见 [开发说明](docs/development.md)。

## 锁定版本

| 项目 | 版本 |
| --- | --- |
| 本轮 API 目标游戏 | public-beta / v0.111.0 / Steam Build 24724944 |
| .NET SDK | 9.0.320 |
| MegaDot | 4.5.1-m.12 |
| Godot.NET.Sdk | 4.5.1 |
| 生成工程的模板 | Alchyr.Sts2.Templates 2.5.3 |
| BaseLib | 3.4.7 |
| ModAnalyzers | 0.2.1 |
| ILSpy CLI | 9.1.0.7988 |

`global.json` 精确锁定 SDK，`toolchain.lock.json` 记录配套版本，`packages.lock.json` 锁定 NuGet 依赖，`.config/dotnet-tools.json` 锁定本地工具。升级应单独提交，同时更新版本记录和平台验证。

## 目录

- `ShenshenCode/`：角色、独立卡池、原创牌、形态与战斗机制。
- `Shenshen/`：角色资源和 `localization/eng`、`localization/zhs` 文本。
- `Shenshen/images/character_forms/human/`、`cat/`：两种形态的图集、枢轴定位清单与选定外形参考。
- `tools/`：Mac / Windows 安装和构建入口。
- `pack/`：资源导出专用 Godot 配置。
- `docs/`：设计、协作与第三方来源说明。
- `local/`、`work/`、`.godot/`、`dist/`：本机工具、参考资料、临时文件或构建结果，均不提交。

## 双机同步

源码私有仓库：`https://github.com/zydjx7/shenshen-sts2-mod`。

Mac 的现有工作副本连接 `origin`；Windows 在本机 Steam 游戏之外的普通目录克隆同一仓库，再运行 Windows bootstrap。每次开工先拉取，完成后提交并推送；同时做不同功能时使用独立分支。不要用网盘同步工作目录、工具缓存或游戏存档。

游戏程序集、反编译结果、工具安装包与存档只留在本机；源码、原创素材、项目说明及版本锁通过 Git 同步。来源与模板说明见 [第三方来源](docs/third-party.md)。
