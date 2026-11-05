# Windows 接续开发

本说明用于从 Mac 切换到 Windows 后继续「光之魔法少女深深」项目。源码私有仓库为 [zydjx7/shenshen-sts2-mod](https://github.com/zydjx7/shenshen-sts2-mod)，接续分支为 `main`。

## Windows 接续更新（2026-11-05）

已从 `9b303ab` 在 Windows 创建本地工程 `F:\shenshen-sts2-mod`，完成 bootstrap、Release 编译和资源打包。实际安装游戏为 **public-beta / v0.111.0 / Build 24724944**，已据该机程序集实现独立卡池与双形态原型；.NET、MegaDot、BaseLib 和 NuGet 版本保持原锁定值。API 目标版本记录已更新，Mac 需更新到同版本后重新验证。本轮临时规则、源码/测试进度和剩余工作见 [Windows 验证记录](windows-validation.md)。

下文保留首次换机的准备方法和交接基线；其中“仍为模板”“Windows 未验证”描述的是 `9b303ab`，不能代替当前状态。

## 在 Windows 取得工程

准备 Git，以及通过 Steam 安装的《杀戮尖塔 2》。仓库是私有的，克隆时使用拥有访问权限的 GitHub 账号。选择游戏安装目录之外的本地开发目录，例如 `D:\Code`；不需要复制 Mac 的整份目录或工具缓存。

在选定的父目录打开 PowerShell，逐条运行：

```powershell
git clone https://github.com/zydjx7/shenshen-sts2-mod.git SpireCharacterMod
cd SpireCharacterMod
git status --short --branch
./tools/bootstrap.ps1
./tools/build.ps1 build
./tools/build.ps1 pack
```

如果已经克隆过，进入该目录，先检查 `git status`，工作区干净时运行 `git pull --ff-only`，无需再次克隆。

如果游戏装在自定义 Steam 库，首次 bootstrap 用实际路径，例如：

```powershell
./tools/bootstrap.ps1 -Sts2Path 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
```

上面的路径只是例子。若 `local.props` 已存在，bootstrap 会保留它，调整路径应编辑该文件。bootstrap 自动准备项目内 .NET 与 MegaDot；无须再次生成角色模板。

Windows 脚本尚未在 Windows 真机运行。本次换机的第一个验证目标是安装、编译与打包成功；遇到错误时保留完整报错，由当前电脑上的开发会话修复。若 PowerShell 限制脚本执行，记录错误和当前策略，按系统允许的流程处理，不默认修改系统级执行策略。

## Mac 交接基线与设计依据（9b303ab）

Mac 已通过模板工程编译、资源打包及独立资源包检查，尚未启动游戏验证。Windows 构建、两平台的游戏内角色入口、双形态、原创牌、保存及联机都尚未验证。

运行代码仍为模板角色：5 张战士打击、5 张防御、燃烧之血及原版卡池；`ShenshenForm` 仅是枚举。已确认设计与实际代码不能混为一谈。完整规则见 [角色设计](character-design.md)，主要包括：

- 70 HP，3 能量，正常抽牌 5 张；少女与猫互斥，战斗结束恢复少女。
- 变形魔法 1 费，额外起手、保留、结算后回手；变形交换当前形态专属手牌，两套已拥有牌组分别循环。诅咒、无色牌和状态牌变形时留手，资源与增益/负面保留。
- 少女初始 5 小火球（1 费 6 伤）与 5 光之护盾（1 费 5 格挡）；猫初始 5 爪击（1 费 3×2）与 5 咪闪（1 费 6 格挡）。
- 猫形每回合自动躲掉遇到的第一次多段攻击的第一段，单段不闪；下次自身回合开始刷新，变身不刷新。闪避段不触发荆棘或格挡反弹。
- 光之祝福为少女 1 费普通多人牌，只祝福其他队友；目标下回合正常抽牌前，每层自己选 1 张额外入手，可叠加。
- 不喜欢你！为少女 2 费攻击，沿用与我一战！的基础及升级数值；光之镜为少女 2 费技能，15/20 格挡与本回合实际格挡伤害反弹。
- 炸毛为猫 1 费能力，效果跨形态：生效后本场首次被敌方攻击实际扣血获得 3 荆棘，后续每次 1；敌方成功施加每种负面另获 1，不占首次奖励，同种负面一次多层只计一次，伤害与负面可叠加。敌方塞状态牌暂不算，自伤和中毒扣血不走扣血触发。

已确认的首次计数与负面触发规则无需再询问。初始遗物、部分升级/稀有度、多张炸毛叠加、光之镜重复施放、共享牌循环及边界处理仍有未定细节，见设计文档的待确定部分。

## 下一步工作

1. 核对 Windows 安装的游戏版本与 `toolchain.lock.json`。当前参考为 `0.107.1 / Steam Build 23811903`；若已更新，先核对 API 与 BaseLib 兼容性，再决定调整依赖，不把旧版构建结果当成新版兼容证明。
2. 配置并验证 Windows 编译、打包。检查 `dist/Shenshen/` 与 `dist/BaseLib/`；默认不自动部署到游戏。
3. 验证角色入口，再从独立卡池、基础牌和最小双形态换牌开始实现。保留实际卡牌实例，确认资源与状态跨形态保持一致；同步考虑保存与联机。
4. 加入猫形闪避、炸毛与光之镜，再验证多人祝福；按阶段记录实测结果。初始遗物和其他内容可继续设计，不把占位内容当成正式设定。

在 Windows 的开发会话可直接使用这段接续说明：

> 继续开发这个《杀戮尖塔 2》角色模组。请先读取 AGENTS.md、README.md、docs/windows-handoff.md 和 docs/character-design.md，承接已确认设计，不再重复询问炸毛的首次计数等规则。先检查本机游戏版本和 Git 状态，配置 Windows 工具链并完成编译、打包验证，再推进独立卡池、基础牌与最小双形态原型。当前代码仍是模板，请明确区分设计、实现和实测结果。

项目根目录的 `AGENTS.md` 用于保存开发约定；其作用参考 [OpenAI Docs：项目指令](https://learn.chatgpt.com/docs/agent-configuration/agents-md)。源码、设计与这份接续说明通过 Git 同步；两台电脑各自的工具链、路径及存档留在本机。日常换机流程见 [开发说明](development.md)。
