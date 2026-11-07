# 六张初始牌卡图接入（2026-11-07）

## 确认与接入范围

用户已确认六张预览，并明确要求直接实装、打包及覆盖游戏 `mods`，不运行测试。本轮将这些确认原图接入初始牌的正常、升级及放大肖像入口，沿用同一张卡牌的现有效果与升级，不改变初始牌组组成、数值、费用或存档。

此前 `ShenshenCard.CustomPortraitPath` 仅映射白卡与蓝卡，小火球等六种起始牌落入通用 `card.png` 分支。这是用户重进游戏后初始牌仍显示占位图的直接原因；上一版覆盖后的 DLL／PCK 文件一致性与用户启动日志已经核对，文件覆盖本身成功。

## 素材与路径

六张均使用内置 `image_gen` 工具，基于已确认的白蓝卡作为美术参考生成，随后获用户确认。保留粉白金太阳少女、帽影遮脸、奶油灰长毛幼猫，以及暗紫石室、暖金光效与手绘笔触。完整提示与相对参考路径见 [生成提示记录](art-prompts-starter-2026-11-07.json)。

确认原图均为 **1438×1093 PNG**，逐字节复制，不裁切、不缩放、不重绘或重新编码。正式文件位于 `Shenshen/images/card_portraits/basic/`，并在已有肖像 switch 中增加六条映射。

| 中文名 | 源码类型 | 图像文件 |
| --- | --- | --- |
| 小火球 | `LittleFireball` | `little_fireball.png` |
| 光之护盾 | `LightShield` | `light_shield.png` |
| 光之印记 | `LightMark` | `light_mark.png` |
| 爪击 | `ClawStrike` | `claw_strike.png` |
| 咪挡 | `MiDodge` | `mi_dodge.png` |
| 猫之甩尾 | `CatTailSweep` | `cat_tail_sweep.png` |

加上已接入的 21 张白卡与 40 张蓝卡，当前共有 67 张独立 PNG 卡图映射；第 68 种卡牌「变形魔法」继续使用已有的两个面图，不落入通用肖像分支。

## 编译、打包与部署

- 编译：`tools/build.ps1 pack --no-restore` 的 Windows Release publish 成功，退出码 0。
- 资源包：独立 MegaDot 工程完成纹理导入和 PCK 导出；导出日志包含六张 `basic/` 卡图的导入映射及对应纹理。日志保存在本机忽略目录 `work/pack-starter-art-20261107.log`。
- 整合包：`dist/Shenshen-starter-art-20261107.zip`，131,533,273 字节，包含 `Shenshen/` 与 `BaseLib/`，也包含既有白卡和蓝卡内容。
- 游戏部署：确认游戏已退出后，先完整备份现有两个模组目录，再覆盖 7 个交付文件；安装后的 7 个文件均与 dist 的 SHA256 一致。旧版备份和部署清单位于本机忽略目录 `local/mod-backups/20261107-174502-starter-art/`。

| 产物 | SHA256 |
| --- | --- |
| `Shenshen.dll` | `A7A022B72771B114102D4332DCDC6C5A2B74CE9B945182CCE712CB372EF8BC07` |
| `Shenshen.pck` | `2D694FDC9AACB739F833E8C982B4CCA586F4F3DCF1BF75E7FE580E2BBA51B87F` |
| `Shenshen-starter-art-20261107.zip` | `AD3BCF51DA7454991ECECCE7B9AEB1E1967D2FA4CF075807C0E8778F703D025A` |

依用户要求，本轮不运行自动回归或游戏测试，也不启动游戏。文件复制后的哈希核对仅确认部署文件一致，不能替代实际游戏中的显示、效果、Mac 或多人验证；这些由用户实测。
