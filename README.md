# 光之魔法少女深深

《杀戮尖塔 2》原创角色模组，供个人使用。内部工程、程序集及模组 ID 统一为 `Shenshen`；文件夹保留 `SpireCharacterMod`，便于两台电脑使用同一仓库。

角色已确定有人形、猫形两种形态。当前只预留形态定义与素材目录，切换条件、卡牌、形态效果和正式数值尚未设计或实现。参见 [角色设计](docs/character-design.md)。

## 当前可用内容

- 可构建的 C# / Godot 角色工程，以及简体中文、英文文本。
- 当前 Mac 已安装并验证项目内的 .NET SDK、MegaDot 和按需反编译工具。
- Mac 与 Windows 的安装、构建、资源打包脚本；本机绝对路径放在忽略的 `local.props`。
- 版本锁、NuGet 依赖锁、统一编码和换行、Git 忽略规则。

初始卡组、遗物、70 HP、白色界面、战斗形象等仍为原版或模板占位，不代表原创设计。资源打包使用独立临时 Godot 工程，避免导出时加载游戏程序集。

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

Mac 已通过编译、资源导出与独立资源包检查；尚未启动游戏验证角色，也未在 Windows 真机验证。Windows 接入步骤、每机路径及日常分支协作见 [开发说明](docs/development.md)。

## 锁定版本

| 项目 | 版本 |
| --- | --- |
| 参考游戏 | public / v0.107.1 / Steam Build 23811903 |
| .NET SDK | 9.0.320 |
| MegaDot | 4.5.1-m.12 |
| Godot.NET.Sdk | 4.5.1 |
| 生成工程的模板 | Alchyr.Sts2.Templates 2.5.3 |
| BaseLib | 3.4.7 |
| ModAnalyzers | 0.2.1 |
| ILSpy CLI | 9.1.0.7988 |

`global.json` 精确锁定 SDK，`toolchain.lock.json` 记录配套版本，`packages.lock.json` 锁定 NuGet 依赖，`.config/dotnet-tools.json` 锁定本地工具。升级应单独提交，同时更新版本记录和平台验证。

## 目录

- `ShenshenCode/`：角色入口与代码，包含尚未接入战斗的 `ShenshenForm` 定义。
- `Shenshen/`：角色资源和 `localization/eng`、`localization/zhs` 文本。
- `Shenshen/images/character_forms/human/`、`cat/`：预留的人形和猫形原创美术目录。
- `tools/`：Mac / Windows 安装和构建入口。
- `pack/`：资源导出专用 Godot 配置。
- `docs/`：设计、协作与第三方来源说明。
- `local/`、`work/`、`.godot/`、`dist/`：本机工具、参考资料、临时文件或构建结果，均不提交。

## 双机同步

源码私有仓库：`https://github.com/zydjx7/shenshen-sts2-mod`。

Mac 的现有工作副本连接 `origin`；Windows 在本机 Steam 游戏之外的普通目录克隆同一仓库，再运行 Windows bootstrap。每次开工先拉取，完成后提交并推送；同时做不同功能时使用独立分支。不要用网盘同步工作目录、工具缓存或游戏存档。

游戏程序集、反编译结果、工具安装包与存档只留在本机；源码、原创素材、项目说明及版本锁通过 Git 同步。来源与模板说明见 [第三方来源](docs/third-party.md)。
