# 补充教程的参考方式

来源：[GlitchedReme/SlayTheSpire2ModdingTutorials](https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials)，本次查看的 master 提交为 `c3eeb78bba4b13b125e90e8f3bdbfd8dabd1cb5f`。

本页最初记录 Mac 交接阶段的参考思路，没有复制教程代码、素材。Windows 已按本机 v0.111.0 程序集重新核对 API，BaseLib 仍为 3.4.7，详见 [Windows 验证记录](windows-validation.md)。玩法以角色设计的已确定规则为准。

## 可借鉴的部分

- [BaseLib：添加新人物](https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials/blob/master/BaseLib/14%20-%20%E6%B7%BB%E5%8A%A0%E6%96%B0%E4%BA%BA%E7%89%A9/README.md)：参考独立角色卡池、本地化、角色选择页、战斗视觉场景的组织方式。当前原版卡池仅作工程占位，原创卡牌确定后再建立角色自己的卡池。
- 同一人物章节介绍了静态图片、帧动画与骨骼动画的不同接入方式。人形和猫形原型可以先各用一张占位图验证，正式动画等玩法与美术方案确定后再制作。
- [BaseLib：局内保存](https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials/blob/master/BaseLib/09%20-%20%E5%B1%80%E5%86%85%E4%BF%9D%E5%AD%98/README.md)：参考可保存属性与附加状态的做法。已确认形态跨回合延续、战斗结束恢复少女；教程列出的保存载体不等于允许直接给任意角色对象添加可保存字段，具体载体必须核对本机版本。
- [基础：触发顺序](https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials/blob/master/Basics/12%20-%20%E8%A7%A6%E5%8F%91%E9%A1%BA%E5%BA%8F/README.md)：借鉴其区分抽牌、出牌效果前后、回合结束、战斗结束等时机的设计思路。双形态实现前须决定触发变身的牌按哪种形态结算，以及变身从何时影响后续效果。

## 版本边界

教程首页注明面向 `public-beta`，其中触发顺序章节明确基于游戏 **0.111**，与本项目 **0.107.1** 不同。示例的接口名、签名、注册方式及严格触发顺序均需对照本机程序集和已锁定的 BaseLib 实际验证。

教程同时介绍 BaseLib 与 RitsuLib；参考其组织和设计思路不意味着迁移基础库。库的选择由实际功能需求与兼容性决定。

## 设计确认顺序

1. 深深与猫的关系，以及两种形态各自的角色体验。
2. 期望的变身节奏，再确定触发方式、时机、代价及限制。
3. 生命、能量、格挡、状态与卡池如何共享，战斗边界及读档如何处理。
4. 组成最小玩法闭环的首批卡牌与初始遗物。
5. 首版需要的美术、动画和文字表现。
6. 数值调试及单人、多人和平台验证范围。

以上为早期设计过程记录。当前已经确认的机制见角色设计，Windows 接续不重开问卷；仅将未定细节标注为原型选择。
