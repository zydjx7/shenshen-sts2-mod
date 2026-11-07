# 杏桃晨金卡图与百科默认解锁（2026-11-08）

## 确认与接入范围

用户确认本轮已有卡图的统一改绘，并在反馈变身魔法的少女头像偏右、双面背景透明后确认修正版，要求实装；随后要求深深的百科卡牌默认全解锁，便于新玩家查阅。用户自行测试，本轮没有运行自动回归或启动游戏。

本轮收录 **96 张已确认面图**：初始 8、原白卡批次 21、原蓝卡批次 40、新增蓝卡素材 2、原金卡批次 25。这是美术制作批次，不能当作当前稀有度或奖励池数量。其中 95 个面图入口对应当前已有的 93 种卡牌模型；「命运之镜」仍只有设计，本轮只收录图片，不新增玩法模型或奖励入口。「屏息」使用已确认的统一改绘图。5 张退池兼容模型的旧档肖像也保留并更新；不将它们重新放回获取池。

原清单中其余 18 张无原图的金卡／先古牌没有在本轮补画。临别一击继续撤下。并行开发中的其他新图与玩法不作为本轮卡图接入的内容。

## 素材与引用

- 以 [深深形象定稿](shenshen-appearance-baseline.md) 的 `mystery-roundface-soft-lips-v1` 与 [杏桃晨金配色](art-style-apricot-dawn-gold.md) 为统一基准，保留各牌动作、构图与法术主题。使用内置 ImageGen 改绘，确认 PNG 逐字节复制，不重新编码。
- 文件位于 `Shenshen/images/card_portraits/basic/`、`common/`、`uncommon/`、`rare/`。96 张素材的路径、SHA256、尺寸及完整提示／修订提示见 [生成与来源记录](art-prompts-apricot-2026-11-08.json)。
- `ShenshenCard.CustomPortraitPath` 增加 23 条已有模型的独立卡图映射，沿用其当前稀有度、参数与升级。「日轮的另一面」接入自己的少女／猫插画，两个路径均参加预加载。
- 「变身魔法」保留原有 `transformation_girl.tres`、`transformation_cat.tres` 入口，改为引用独立、不透明的 `basic/transformation_human.png`、`basic/transformation_cat.png`，不再引用角色透明图集。
- 少女图采用用户确认的居中修订 r3，1486×1059；取景 `Rect2(46, 0, 1393, 1059)`。猫图为 1439×1093，取景 `Rect2(0, 0, 1438, 1093)`。两面均为完全不透明 RGB，去掉旧 `margin`，帽檐和猫耳保留在卡图区域中。C# 的目标形态判断与变形效果不变。

## 百科默认解锁

新增 `ShenshenCode/Mechanics/ShenshenCardLibrary.cs`，补丁作用于原生 `NCardLibraryGrid.RefreshVisibility`：

1. 原生刷新前，将允许显示在百科中的深深卡牌标记为当前档案已见过。原生搜索与点击详情也依赖该集合，因此同时生效。
2. 原生刷新后，将这些模型加入卡牌网格自己的解锁集合，直接显示卡名、卡图、说明并支持升级预览。

每次打开百科均自动处理，新档和以后新增的深深卡牌都适用。范围限于 `ShenshenCard` 且 `ShouldShowInCardLibrary=true` 的模型；原生隐藏规则、筛选按钮及其他角色的进度沿用原逻辑。多人专属牌通过原生「多人卡牌」开关查看。正常奖励／商店／先古获取规则没有通过该显示补丁修改。助手没有直接编辑玩家存档文件。

本机 API 已从当前安装的 v0.111.0 游戏程序集只读核对：`RefreshVisibility()`、`_seenCards`、`_unlockedCards` 与 `ProgressState.MarkCardAsSeen`。百科以当前原生界面为入口，不新增独立界面。

## 编译、打包与部署

- Windows Release 编译成功，0 错误。NuGet 在线漏洞审计源不可达，产生 1 条 NU1900 警告；沿用锁定的现有依赖，没有升级包。
- 卡图使用项目锁定的 MegaDot，在独立资源工程导入并导出，再以当时已安装的资源包为底合入已确认卡图依赖。最终 PCK 为 320 项资源，新增 52 项，原 268 项中 133 项内容相同。原有非卡图资源及文本保留，未带入角色动画与透明头像的工作区草稿。
- 百科更新只重编译 DLL，不重打或改动已交付的卡图 PCK。编译保留刚安装版本的 209 个源码输入，并通过忽略目录的构建覆盖排除同时进行的 3 份未交付源码变更；未恢复或覆盖这些工作区文件。
- 按用户“已退出，请继续覆盖”，完成旧版备份及最终安装。**最终 7 个游戏安装文件与整合 ZIP／dist 一致**。本轮仅编译、资源打包和文件一致性核对；未运行自动回归、未由助手启动游戏或进行游戏界面实测。macOS、真实多人及实际显示由后续实测验收。

最终整合包：`dist/Shenshen-apricot-art-library-unlocked-20261108.zip`。

| 产物 | SHA256 |
| --- | --- |
| `Shenshen.dll` | `86D0BF2375B214DF09FC90AC2C42482301B61AE28CEBE82C96BEE5D9BCAF2DB1` |
| `Shenshen.pck` | `D1BA3C577FD52EE415D339CDD8470AEFA7B4011CEF832E1316292B37457A282C` |
| 最终 ZIP | `4F6C29F3260D4008B8B723784C8F90EF4E67E9853CEE3610E76D49D9D9E1EAD3` |

本机交付记录：`work/transformation-art-delivery-20261108/`、`work/card-art-apricot-integration-20261108/`、`work/compendium-unlock-20261108/`。最终覆盖前备份为 `local/mod-backups/20261108-020834-874-library-unlocked/`。以上工作区、工具与产物均不进入 Git。
