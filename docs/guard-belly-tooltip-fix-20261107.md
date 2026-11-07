# 护住肚皮：悬停提示数值修复（2026-11-07）

用户反馈卡面显示最多回补 6 点格挡，而卡牌旁的效果提示显示最多 0 点。

## 原因与修改

`GuardTheBelly.ExtraHoverTips` 创建效果提示时，没有向 `HoverTipFactory.FromPower` 传入数值。本机原生 `PowerModel.GetDumbHoverTip` 因此把模板效果的默认 `Amount=0` 写入提示。

现直接传入该卡的 `Recovery` 动态变量：基础 6 点、升级 8 点。卡面、升级预览与悬停提示使用同一个来源；不修改回补结算、费用或升级数值。实际打出卡牌时，原有代码已经把该变量传给 `GuardTheBellyPower`。

## 结算示例

以用户截图的 10 点格挡、最多回补 6 点为例，假设没有闪避或其他影响：敌人攻击 11×2，第一段消耗 10 点格挡并损失 1 点生命，然后获得 6 点格挡。第二段消耗这 6 点格挡并损失 5 点生命，合计损失 6 点生命。回补仅在第一段之后触发一次，不恢复已经损失的生命。

卡牌基础格挡为 9，升级为 12；截图中的 10 是当时显示的实际格挡，示例按截图计算。

## 构建、交付与验证边界

- Windows Release：`tools/build.ps1 build --no-restore` 成功，0 警告、0 错误。日志为本机忽略目录 `work/guard-belly-tooltip-fix/build.log`。
- 交付包：`dist/Shenshen-guard-belly-tooltip-20261107.zip`，133,373,098 字节，包含 Shenshen 与 BaseLib。此次仅更新程序集，沿用已安装版本的正式 PCK；未将工作区内其他尚在修改的美术资源重新打包。
- 构建前，核对 dist 与游戏已安装的 7 个交付文件哈希一致。构建后，核对除 DLL／PDB 外的文件保持原哈希。
- 首次覆盖检查发现游戏仍在运行，脚本在任何写入之前停止。用户正常退出并明确要求覆盖后，已完整备份现有 Shenshen／BaseLib 至本机忽略目录 `local/mod-backups/20261107-191035-guard-belly-tooltip/`，核对备份后，仅覆盖 `Shenshen.dll` 与 `Shenshen.pdb`。安装后的 7 个交付文件 SHA256 全部与 dist 一致；部署清单位于 `work/guard-belly-tooltip-fix/deployment.json`。
- 遵照用户要求，没有运行自动测试或启动游戏。编译成功与文件哈希核对不代替用户的实际界面验收。

| 产物 | SHA256 |
| --- | --- |
| Shenshen.dll | `A66FD03D7E6769FA972AC9604E1D2F8EB573D924B56A5E743F77F1E05AA348B0` |
| Shenshen.pck（沿用） | `0BCCCB463A5B773D7D1F70D3D81843E7BE023FF6A8D33356F1EE48F333F0F8D2` |
| 整合 ZIP | `DA0B88E2F9AF36CA15080E0BAFCBD34175B1E1814A4BBF41965376384D17B7E6` |
