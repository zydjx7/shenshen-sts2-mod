# Mac 与 Windows 共同开发

两台电脑同步源码、文本、素材与版本锁文件；各自安装 Steam、同一游戏版本及本项目工具链。游戏安装目录、开发工具、NuGet 缓存和本机 `local.props` 不通过 Git 同步。

从 Mac 首次换到 Windows 时，先读 [Windows 接续说明](windows-handoff.md)，其中记录当前设计、代码实际进度和首次克隆命令。

## 初次准备

先将同一个私有仓库克隆到本机。现有工程已经生成，无需再次安装或运行角色模板。项目使用 `global.json` 精确锁定 .NET SDK，`toolchain.lock.json` 记录 MegaDot、Godot SDK、模板、BaseLib、分析器和参考游戏版本。

Mac 在项目根目录运行：

```sh
./tools/bootstrap.sh
./tools/build.sh build
./tools/build.sh pack
```

Windows 在项目根目录用 PowerShell 运行：

```powershell
./tools/bootstrap.ps1
./tools/build.ps1 build
./tools/build.ps1 pack
```

Bootstrap 从 [Microsoft 官方安装脚本](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script)安装锁定的 SDK，从 [MegaDot 官方下载站](https://megadot.megacrit.com/)获取指定版本并校验官方 SHA256，随后通过 `dotnet tool restore` 恢复仓库锁定的本地命令行工具。工具安装到 `local/toolchain/` 或本地工具缓存，命令使用本项目的 SDK、CLI 用户目录及 NuGet 缓存；不会写入系统 PATH。Bootstrap 不执行完整构建，已有相同版本可跳过安装。

Mac 脚本只支持 macOS。Windows 使用 x64 SDK 与 x86_64 MegaDot 编辑器；Windows 安装、构建与打包已在真机验证。若系统限制脚本或应用运行，应按系统提示审核并允许相应文件；脚本不移除隔离标记、不更改执行策略或系统安全设置。

## 本机路径

Bootstrap 只在 `local.props` 不存在时生成文件，保留已有配置。检查这三项：

- `GodotPath`：MegaDot 的可执行文件；Mac 是 `.app/Contents/MacOS/` 内的文件，Windows 优先使用控制台版 `.exe`。
- `Sts2Path`：游戏安装目录，默认采用 Steam 的标准游戏库路径。
- `Sts2Architecture`：Mac 依据运行脚本的架构填写 `arm64` 或 `x86_64`，Windows 默认 `x86_64`；与本机安装的游戏架构保持一致。

Steam 自定义游戏库可在首次生成时指定，也可随后编辑 `local.props`：

```sh
STS2_PATH="/Volumes/Games/SteamLibrary/steamapps/common/Slay the Spire 2" ./tools/bootstrap.sh
```

```powershell
./tools/bootstrap.ps1 -Sts2Path 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
```

不需要自行设置 `Sts2DataDir`，共享工程根据平台、架构和 `Sts2Path` 发现游戏依赖。两台电脑各自通过 Steam 安装游戏；不要将游戏 DLL 或个人存档提交到仓库。

## 构建与打包

`build` 使用 Release 编译 C# 并按需恢复 NuGet 包；`pack` 使用 Release 执行 `dotnet publish`，通过 MegaDot 打包资源。默认配置与现有依赖锁一致；显式 Debug 会增加 GodotSharpEditor，不在当前锁文件范围内。可在动作后追加普通 dotnet 参数，例如 `./tools/build.sh build --no-restore`。

成品目录为根目录的 `dist/Shenshen/`。构建与打包不自动复制到游戏 `mods`；测试时在游戏退出后手动复制所需成品。先用专用测试档验证模组加载，再验证角色入口与双形态机制。

Windows 专用测试可运行 `./tools/play-test.ps1 start`：脚本在游戏退出时备份并部署本项目产物，记录日常存档哈希，以项目内 `local/play-test/appdata` 作为独立进程 APPDATA，并传入本机已核对的 `--force-steam=off`，避免测试档接入 Steam 云存档。首次需在该测试档完成游戏的模组加载提示并重启。退出测试游戏后运行 `./tools/play-test.ps1 verify` 比较日常存档；不要把普通 Steam 启动当成隔离测试。实际路径、版本、运行限制见 [Windows 验证记录](windows-validation.md)。

真实程序集自动回归运行 `./tools/test.ps1`：先打包当前源码，再将 `tools/tests/` 的原创测试工程复制到忽略的 `work/headless-tests/`，引用本机游戏程序集并在 MegaDot headless 中运行。测试的 APPDATA/LOCALAPPDATA 指向 `local/regression-*`，游戏 SaveManager 使用内置 TestMode 的内存存储；不会测试 Steam 双端网络或替代游戏界面验收。

## 日常同步

1. 开工前拉取远端，检查两台电脑的游戏版本和分支与锁文件记录一致。
2. 使用功能分支；同时开发时分配不同文件或功能，减少素材与场景文件冲突。
3. 在当前电脑构建并检查改动后提交、推送；换机后拉取并运行对应平台的构建脚本。
4. 涉及角色机制、资源或平台行为的改动，记录 Mac 与 Windows 的实际游戏内验证结果；一台验证通过不能替代另一台验证。

升级 SDK、MegaDot、BaseLib 或游戏版本应作为独立改动，同时更新版本记录、工程依赖与验证结果。Codex 对话不会代替 Git 同步本地文件。
