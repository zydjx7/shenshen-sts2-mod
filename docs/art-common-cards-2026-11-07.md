# 二十一张白卡卡图：确认稿与接入记录

## 用户确认与范围

2026-11-07，用户先要求设计白卡卡面并展示，认可后再实装；审看本轮 21 张预览后，已确认这套卡图并授权接入、编译打包。用户明确由自己测试，本轮不运行自动测试或启动游戏。

本次覆盖 21 张 Common：少女单人白卡 10 张、猫白卡 10 张，以及少女多人专用的光之祝福 1 张。仅接入卡图及对应路径映射，不修改费用、伤害、格挡、升级、形态、牌堆、存档或联机机制。起始牌（Basic，含双面变形魔法）和蓝卡（Uncommon）不在本次卡图接入范围。

## 统一美术与角色基准

使用内置 `image_gen.imagegen` 逐张生成独立插画。统一方向参考《杀戮尖塔 2》的手绘卡图语言：可见的粗笔触、哑光水粉质感、块面化轮廓、少量深梅紫描边，以及暗靛紫石质背景与金白、柔粉光效的冷暖对照。每张围绕一个清楚的动作构图，便于缩小后识别；卡名、费用、说明和边框由游戏提供，不画入插画。

少女沿用 [B5-3 日轮圣像／柔和饱满确认稿](../Shenshen/images/character_forms/human/solar-b5/approved-design.png)：粉白金斗篷、太阳帽和法杖、奶油白金短发、白皙肤色、帽影遮脸只露下巴；画到脚时保持裸足和自然脚踝。猫沿用 [指定幼猫原稿](../Shenshen/images/character_forms/cat/kitten-faithful/approved-design.png)：奶油灰银长毛、琥珀眼、聪明神态、自然舒展的躯干、小圆爪与蓬尾。两者是同一角色的互斥形态；留光、回眸使用抽象光印提示另一形态，不画成少女与猫伙伴同时登场。

先生成双生火花作为整套画风样本，其余卡图参考其笔触、配色和光影，分别依据各自机制安排镜头和动作。弓背表现当前的格挡与临时荆棘，不采用尚未确认的下回合临时力量候选；光之祝福的光流向另一名队友。

共生成少女图 11 张、猫图 11 张；最终确认并接入少女 11 张和猫 10 张。回眸首稿未清楚表现回头，定向强化背身与相反头部朝向后重新生成；退稿不收录正式资源，也不跟踪退稿文件。最终完整提示和仓库相对参考路径见 [生成提示记录](art-prompts-common-2026-11-07.json)。

## 素材与映射

获确认的原图均为 **1438×1093 PNG**，原封不动放入 `Shenshen/images/card_portraits/common/`。不裁切、不缩放、不重绘，也不重新编码；普通与放大卡面均映射对应原图，由游戏完成显示缩放。以下文件名均相对于该目录。

| 形态 | 类名 | 中文名 | 文件名 |
| --- | --- | --- | --- |
| 少女 | `ShatteredMirror` | 碎光镜 | `shattered_mirror.png` |
| 少女 | `ThornPrayer` | 荆棘祝祷 | `thorn_prayer.png` |
| 少女 | `LightfootBlessing` | 轻盈祝福 | `lightfoot_blessing.png` |
| 少女 | `LightCurtain` | 光幕 | `light_curtain.png` |
| 少女 | `LingeringLight` | 留光 | `lingering_light.png` |
| 少女 | `TwinSparks` | 双生火花 | `twin_sparks.png` |
| 少女 | `ChasingFlame` | 追焰 | `chasing_flame.png` |
| 少女 | `DazzlingLight` | 眩光术 | `dazzling_light.png` |
| 少女 | `Halo` | 光晕 | `halo.png` |
| 少女 | `RefractedVolley` | 折光连射 | `refracted_volley.png` |
| 少女（多人） | `LightBlessing` | 光之祝福 | `light_blessing.png` |
| 猫 | `ArchedBack` | 弓背 | `arched_back.png` |
| 猫 | `FurballCurl` | 蜷成毛球 | `furball_curl.png` |
| 猫 | `CuriousPeek` | 好奇探头 | `curious_peek.png` |
| 猫 | `Catnap` | 小憩 | `catnap.png` |
| 猫 | `Grooming` | 舔毛 | `grooming.png` |
| 猫 | `ShedFur` | 抖毛 | `shed_fur.png` |
| 猫 | `SweepingClaws` | 横扫爪 | `sweeping_claws.png` |
| 猫 | `BunnyKick` | 抱住蹬踹 | `bunny_kick.png` |
| 猫 | `BiteTheOpening` | 咬住破绽 | `bite_the_opening.png` |
| 猫 | `BackwardGlance` | 回眸 | `backward_glance.png` |

本轮只替换上述 Common 的卡图路径。既有起始牌和蓝卡继续使用其原有资源路径，双面变形的图像切换机制不作调整。卡牌规则以 [角色设计](character-design.md) 为依据。

## 分层结果

| 阶段 | 本轮状态与边界 |
| --- | --- |
| 设计确认 | 21 张卡图已获用户确认并授权接入。 |
| 源码与素材 | 21 张正式 PNG 与逐卡路径映射已接入；卡牌机制保持原有实现。 |
| 编译 | Windows Release `build --no-restore` 成功，0 警告、0 错误。 |
| 资源打包 | Windows `tools/build.ps1 pack --no-restore` 成功，退出码 0；21 张图片导入与资源导出完成。最终日志为 `work/common-card-art-pack-final-20261107.log`。 |
| 自动测试 | 按用户要求未运行，不继承此前回归结果。 |
| 游戏界面实测 | 未启动游戏，由用户实测卡面显示。 |
| macOS 与真实多人 | 本轮未实测；不以 Windows 编译打包代替平台或联机验收。 |

前两次打包分别受默认编辑器配置目录与临时目录的写入权限限制，日志保留在 `work/common-card-art-pack-20261107.log` 和 `work/common-card-art-pack-isolated-20261107.log`。最终仅在打包进程中将 `APPDATA`、`LOCALAPPDATA` 指向忽略的 `local/pack-appdata`、`local/pack-localappdata`，将 `TEMP`、`TMP` 指向 `local/pack-temp`，沿用现有构建入口后成功；未修改构建脚本或全局环境变量。

交付目录为 `dist/Shenshen/` 和 `dist/BaseLib/`，另提供包含这两个目录的 `dist/Shenshen-common-cards-20261107.zip`。本轮没有启动游戏；编译和打包成功不代表卡图已在游戏界面验收，由用户自行实测。

最终产物 SHA256：

- `dist/Shenshen/Shenshen.dll`：`9FEE27F9613C70BA0B8CC4B2ED38B088641F1BE8D6B84888BB41099617C100FF`
- `dist/Shenshen/Shenshen.pck`：`B8A8BE0DBBFB90A764472E71CB2CA47CDBEEB5ED9C0DAA837E612DC5CE9E96ED`

预览画廊与本机工作文件留在忽略的 `work/`，不作为游戏加载来源。正式源码、确认素材和制作记录通过 Git 同步，本机生成缓存、工具及存档独立管理。
