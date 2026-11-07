# 白卡、蓝卡实装核对与文本整理（2026-11-07）

## 核对结论

按 `docs/character-design.md` 中的白卡表和 G01–G20／C01–C20 蓝卡表逐张对照当前源码：**白卡 21 张，蓝卡 40 张，共 61 张，未发现实装缺项**。每张均有实际效果入口、升级入口、明确稀有度、独立卡图，以及中英文名称和说明；同时检查了涉及的能力状态及选牌提示。

- 白卡：少女 10 张、猫 10 张，以及仅多人模式出现的少女牌「光之祝福」。单人白卡候选为 20 张。
- 蓝卡：少女 20 张、猫 20 张，均为 `CardRarity.Uncommon`，没有额外的多人模式限制。
- Basic 的 7 种起始牌单独计算，不包含在白卡／蓝卡数量中；永久起始牌组仍是 19 张。金卡评审稿不在本次核对和实装范围内。
- 61 个卡图映射均指向现有 PNG。正常图、升级图及放大图沿用现有 BaseLib 肖像入口；本轮不更换卡图。

角色的 `CardPool` 指向 `ShenshenCardPool`；这些牌继承带有 `[Pool(typeof(ShenshenCardPool))]` 的 `ShenshenCard`。池过滤仅排除 Token，没有排除蓝卡。核对的是源码接入和资源完整性，实际游戏中的奖励出现、各项效果、文本排版及平衡由用户实测。

下表所有条目均已核对效果、升级、稀有度、卡图和中英文本。既有原型选择沿用当前实现，不把本轮文字整理视为新增玩法确认。

## 游戏目录中的版本

对游戏 `mods/Shenshen/Shenshen.dll` 和本次 `dist` DLL 用项目锁定的 ILSpy 只读列出类清单，没有加载或运行游戏代码：

| 位置 | 白卡类 | 蓝卡类 | Basic 类 |
| --- | --- | --- | --- |
| 游戏目录中的旧 DLL | 21 | 3 | 7 |
| 本次交付 DLL | 21 | 40 | 7 |

旧 DLL 的蓝卡只有 `IDontLikeYou`（不喜欢你！）、`LightMirror`（光之镜）和 `FluffedUp`（炸毛），其余 37 张对应类缺失。因此使用该旧 DLL 时，奖励不可能出现这 37 张新蓝卡。当前工程和新交付包完整；实测前需要在退出游戏后，用新包更新游戏中的模组文件。

只读清单保存在本机忽略目录 `work/installed-mod-types-20261107.txt` 与 `work/packaged-mod-types-20261107.txt`。旧游戏 DLL SHA256 为 `69F14D1AF3DA5E9F238AC1E9823CCB652A8E1861D9A866BE071E0E595680D5A9`，旧游戏 PCK 为 `D0E47C92B0A801EBC254354F3C12A822F3CC530FBEBA613DC9C6EF7669F6A8DC`；本轮未自动覆盖它们。

## 玩家文本

这次只修改 `Shenshen/localization/{zhs,eng}/{cards,powers}.json`，不修改卡牌逻辑、数值、费用、升级、奖励规则或美术。保留动态数值、升级分支和变形两面分支，未新增或删除本地化键。

- 保留「将这张牌放回你的手牌」。变形两面继续分别写清弃少女牌抽猫牌／弃猫牌抽少女牌。
- 将「猫弃牌置顶」等简写展开为从哪里选择什么牌、放到哪个牌堆的说明；选牌界面使用同样的表达。
- 将「原攻击牌」「每段伤害增加」「回补」等表达改为「你选择的攻击牌」「每次攻击的伤害增加」「重新获得该次攻击消耗的格挡」。
- 统一「点伤害」「点格挡」「点能量」等单位；固定格挡直接说明不受敏捷影响，临时增益的到期时间写在对应提示中。
- 区分光之镜的全部格挡反弹与碎光镜／翻身回击的有限格挡反弹。修正通用反弹提示只提碎光镜的旧表述，补充对应格挡优先消耗。
- 炸毛仍为生效后的首次敌方攻击失去生命值奖励，升级才增加后续成长；没有改成每回合刷新。
- 清理状态提示中的「少量张牌」「耗能减少少量」「已处理」「已计数」等占位或实现用语。

## 逐张清单

### 白卡（21 张）

| 卡牌 | 形态 | 类型 | 费用 | 源码类型 |
| --- | --- | --- | --- | --- |
| 弓背 | 猫 | 技能 | 1 | `ArchedBack` |
| 回眸 | 猫 | 攻击 | 1 | `BackwardGlance` |
| 咬住破绽 | 猫 | 攻击 | 1 | `BiteTheOpening` |
| 抱住蹬踹 | 猫 | 攻击 | 2 | `BunnyKick` |
| 小憩 | 猫 | 技能 | 1 | `Catnap` |
| 追焰 | 少女 | 攻击 | 1 | `ChasingFlame` |
| 好奇探头 | 猫 | 技能 | 1 | `CuriousPeek` |
| 眩光术 | 少女 | 攻击 | 1 | `DazzlingLight` |
| 蜷成毛球 | 猫 | 技能 | 2 | `FurballCurl` |
| 舔毛 | 猫 | 技能 | 0 | `Grooming` |
| 光晕 | 少女 | 攻击 | 1 | `Halo` |
| 光之祝福（仅多人） | 少女 | 技能 | 1 | `LightBlessing` |
| 光幕 | 少女 | 技能 | 1 | `LightCurtain` |
| 轻盈祝福 | 少女 | 技能 | 0 | `LightfootBlessing` |
| 留光 | 少女 | 技能 | 1 | `LingeringLight` |
| 折光连射 | 少女 | 攻击 | 1 | `RefractedVolley` |
| 碎光镜 | 少女 | 技能 | 1 | `ShatteredMirror` |
| 抖毛 | 猫 | 攻击 | 1 | `ShedFur` |
| 横扫爪 | 猫 | 攻击 | 1 | `SweepingClaws` |
| 荆棘祝祷 | 少女 | 技能 | 1 | `ThornPrayer` |
| 双生火花 | 少女 | 攻击 | 1 | `TwinSparks` |

### 蓝卡（40 张）

| 卡牌 | 形态 | 类型 | 费用 | 源码类型 |
| --- | --- | --- | --- | --- |
| 伸个懒腰 | 猫 | 技能 | 0 | `BigStretch` |
| 躲进纸箱 | 猫 | 技能 | 1 | `CardboardHideout` |
| 捉住光点 | 猫 | 技能 | 1 | `CatchTheLight` |
| 追逐光点 | 猫 | 能力 | 1 | `ChaseTheLight` |
| 净焰 | 少女 | 技能 | 1 | `CleansingFlame` |
| 相伴祈愿 | 少女 | 技能 | 1 | `CompanionWish` |
| 蓬成蒲公英 | 猫 | 技能 | 2 | `DandelionPuff` |
| 耀光结界 | 少女 | 技能 | 2 | `DazzlingBarrier` |
| 萤火漫天 | 少女 | 攻击 | 2 | `FirefliesAbove` |
| 流火雨 | 少女 | 攻击 | 2 | `FireRain` |
| 炸毛 | 猫 | 能力 | 1 | `FluffedUp` |
| 护食本能 | 猫 | 能力 | 1 | `FoodGuardian` |
| 护住肚皮 | 猫 | 技能 | 1 | `GuardTheBelly` |
| 藏起玩具 | 猫 | 技能 | 0 | `HiddenToy` |
| 哈气！ | 猫 | 技能 | 1 | `Hiss` |
| 不喜欢你！ | 少女 | 攻击 | 2 | `IDontLikeYou` |
| 灵光护符 | 少女 | 技能 | 1 | `InsightCharm` |
| 点燃火种 | 少女 | 攻击 | 1 | `KindleTheSpark` |
| 踩奶 | 猫 | 攻击 | 1 | `Kneading` |
| 光之镜 | 少女 | 技能 | 2 | `LightMirror` |
| 光之蓄势 | 少女 | 能力 | 1 | `LightReserve` |
| 光影交接 | 少女 | 技能 | 1 | `LightShadowHandoff` |
| 镜中余辉 | 少女 | 能力 | 1 | `MirrorAfterglow` |
| 镜面法衣 | 少女 | 能力 | 1 | `MirroredVestments` |
| 熔光穿刺 | 少女 | 攻击 | 2 | `MoltenLightPierce` |
| 装睡 | 猫 | 技能 | 0 | `PretendSleep` |
| 护身绒 | 猫 | 攻击 | 1 | `ProtectiveFluff` |
| 净光祷告 | 少女 | 技能 | 1 | `PureLightPrayer` |
| 折射焰矢 | 少女 | 攻击 | 1 | `RefractedFlameArrows` |
| 叼回法杖 | 猫 | 技能 | 0 | `RetrieveTheStaff` |
| 回敬一爪 | 猫 | 攻击 | 1 | `ReturningClaw` |
| 翻身回击 | 猫 | 攻击 | 1 | `RollingCounter` |
| 满屋飞扑 | 猫 | 攻击 | 2 | `RoomwidePounce` |
| 追着光挠 | 猫 | 攻击 | 2 | `ScratchTheLight` |
| 镜碎流火 | 少女 | 技能 | 1 | `ShatteredMirrorFlames` |
| 星愿祝福 | 少女 | 技能 | 1 | `StarlitWish` |
| 逗逗你 | 猫 | 攻击 | 0 | `Teasing` |
| 带刺的呼噜 | 猫 | 能力 | 1 | `ThornyPurr` |
| 明日之愿 | 少女 | 技能 | 1 | `TomorrowWish` |
| 护身祝祷 | 少女 | 能力 | 2 | `WardingPrayer` |

## 编译、打包与验证范围

本轮按用户要求不运行自动回归、不启动游戏、不自动部署到 Steam，也不把静态核对或编译成功称为游戏实测。仅做请求范围内的源码／资源／文本核对，并执行项目构建打包入口。

- 源码／资源核对：21 张 Common、40 张 Uncommon、7 种 Basic；61 个白蓝卡肖像映射均有资源。中英键完整，所有修改保留了原动态参数与条件参数，标记配对完整。
- 编译：`tools/build.ps1 pack --no-restore` 中的 Release publish 成功，退出码 0。
- 打包：独立 MegaDot 工程导入与 PCK 导出成功，日志为本机忽略目录 `work/pack-card-copy-20261107.log`。
- 交付：`dist/Shenshen-card-text-20261107.zip`，121,793,042 字节，包含 `Shenshen/` 与 `BaseLib/`。
- 自动回归与游戏窗口：本轮均未运行；Mac、多人双端、奖励概率及实际排版也未在本轮实测。

本次产物 SHA256：

| 文件 | SHA256 |
| --- | --- |
| `Shenshen.dll` | `9B4DB463B9A944A5E2F7FB90491C8B270C54E6926A6561E2709ED02233776165` |
| `Shenshen.pck` | `43D8E4095E3D7346CDCD72A0BD49F3327B7DAC3E036062862653D3F69D6029AE` |
| `Shenshen-card-text-20261107.zip` | `3D7A4CE1E3720CD122DA6E556646520AB10D62510C4131606C327360503D925F` |
