# 第三方来源与开发依据

- 工程由 [Alchyr/ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) 的 Character 模板生成，NuGet 包为 `Alchyr.Sts2.Templates` 2.5.3；模板 NuGet 包 SHA-256 为 `944e1c535f46fe9ea3ecd3e478d0733a70edc0bcfdf66449ce0eeb73ff2be9f2`。初始代码及占位图片来自该模板，后续再替换为本角色的原创内容。
- [Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) 3.4.7 是运行依赖（MIT）；通过 NuGet 引用，构建时将所需运行文件复制到忽略的 `dist/BaseLib/`，不把包内二进制提交到源码仓库。
- [ModAnalyzers](https://github.com/Alchyr/ModAnalyzers) 0.2.1 用于编译期校验；NuGet 来源与完整内容哈希以 `packages.lock.json` 为准。
- [MegaDot](https://megadot.megacrit.com/) 4.5.1-m.12 从 Mega Crit 官方站点下载，核对同名 `.sha256`，安装在本机忽略目录。
- .NET SDK 使用 [Microsoft 官方安装脚本](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script)下载固定的 9.0.320。
- [ILSpy](https://github.com/icsharpcode/ILSpy) CLI 9.1.0.7988 通过本地 .NET 工具清单安装。按需查看本机游戏程序集，反编译输出保留在忽略的本机目录。

游戏 `sts2.dll`、Harmony 等程序集均直接引用本机 Steam 安装文件，不复制进源码仓库或模组资源包。本项目不是游戏的完整源码重建。
