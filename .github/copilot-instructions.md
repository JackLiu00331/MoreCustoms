# MoreCustoms Agent Instructions

本文件用于给后续 AI agent 提供稳定、可复用的仓库上下文，避免每次重复说明项目结构、环境和协作流程。

## 适用范围

- 默认工作仓库是 MoreCustoms。
- STS2Decode 仅作为游戏反编译参考目录，默认只读，不要主动在其中修改代码，除非用户明确要求。
- 除非用户明确要求输出绝对路径，否则优先只使用目录名或文件名描述上下文。

## 目录与名称约定

- Git 仓库目录名：MoreCustoms
- 游戏 mods 内的安装目录名：MoreCustoms
- 游戏本体目录名：Slay the Spire 2
- 游戏程序集数据目录名：data_sts2_windows_x86_64
- 反编译参考目录名：STS2Decode
- 仓库链接：https://github.com/JackLiu00331/MoreCustoms.git
- 主要开发分支：main
注意：仓库根目录名是 MoreCustoms，仓库内部还有一个同名目录 MoreCustoms。后者是 Godot 运行时资源目录，不是 git 根目录。每次请确保实在MoreCustoms仓库根目录下工作，而不是 MoreCustoms/MoreCustoms 目录下。不要创建新的git init或子模块。

## 关键目录职责

- Config：配置加载与默认值逻辑。
- Modifiers：每个自定义 Modifier 的定义实现。
- Patches：Harmony 注入、玩法逻辑改写、UI 注入和无尽模式补丁。
- Models：额外数据模型与事件模型；当前重点是 Events。
- MoreCustoms：Godot 运行时资源目录，主要放本地化和随包内容。
- images：项目使用的图片资源原件与打包资源。
- Resources：额外资源目录。
- packages：NuGet 包与构建依赖缓存，通常不手改。
- STS2Decode：游戏本体反编译代码，用于查找原版实现和签名。

## 关键文件职责

- MoreCustoms.csproj：核心构建入口。负责读取 .env、解析 SteamLibraryPath 和 GodotPath、引用游戏程序集、在 build 后复制 dll 和 manifest 到 mods 目录、在 publish 后导出 MoreCustoms.pck。
- MainFile.cs：Mod 入口。负责初始化日志、加载配置、执行 Harmony PatchAll。
- MoreCustomsConfig.cs：配置中心。负责创建/读取 config.json、补齐缺失字段、做默认值归一化。
- mod_manifest.json：Mod 清单元数据，定义名称、作者、描述和版本。
- project.godot：Godot 工程定义，供导出 pck 使用。
- export_presets.cfg：Godot 导出预设，publish 时依赖其中的导出配置。
- config.json：默认配置模板，首次运行或缺失时会被生成/补齐。
- README.md：玩家说明、开发流程说明、环境说明和路线图。
- CONFIG_GUIDE.txt：配置项说明补充文档。
- MoreCustoms.sln：解决方案入口，便于 IDE 打开和多人协作。
- nuget.config：NuGet 源配置。
- .env.example：本地环境变量模板，开发者首次拉取后复制为 .env。
- MoreCustoms/localization/eng/modifiers.json：英文文案。
- MoreCustoms/localization/zhs/modifiers.json：中文文案。

## 当前实现关注点

- 新增 Modifier 时，通常同时涉及 Modifiers、Patches、本地化文本和图标资源。
- 无尽模式相关逻辑主要集中在带 Endless 前缀的补丁和 Events 模型中。
- 与游戏原始逻辑对照时，优先去 STS2Decode 查原类、原字段、原方法签名。

## 开发环境要求

- 需要 .NET SDK 9.0。
- 需要 Godot 4.5.1 Mono 版本，版本不要高于游戏当前使用版本，否则 pck 可能无法加载。
- 需要本机已安装 Slay the Spire 2。
- 首次开发前，先把 .env.example 复制为 .env。
- .env 至少要配置两个变量：SteamLibraryPath、GodotPath。
- 本项目默认在 Windows 下开发，但 csproj 也包含 Linux 和 macOS 条件分支；若不是 Windows，先确认对应路径解析是否满足当前机器。

## 标准工作流

每次接到新的需求时，先做以下事情：

1. 在 MoreCustoms 仓库内检查当前工作树状态。
2. 基于当前主线创建一个新的需求分支，不要直接在主分支上工作。
3. 分支名应能表达需求意图，推荐使用 feature/、fix/、refactor/ 之类前缀。

完成需求后，执行以下约束：

1. 只提交当前需求相关改动。
2. 只进行 commit 和 push。
3. 不要自动 merge。
4. 不要自动删除分支。
5. 不要改写历史，除非用户明确要求。

如果仓库里已经有与当前需求无关的脏改动：

- 不要回退它们。
- 只在当前需求涉及的文件内谨慎工作。
- 如果现有改动与当前需求直接冲突，再向用户确认。

## Build 与 Publish 规则

常用命令：

```powershell
dotnet build MoreCustoms.csproj
dotnet publish MoreCustoms.csproj
```

执行语义：

- build：编译 MoreCustoms.dll，并把 dll 与 mod_manifest.json 复制到游戏的 mods/MoreCustoms 目录；若目标目录还没有 config.json，也会复制默认配置；同时复制 BaseLib.dll。
- publish：在 build 基础上继续调用 Godot 导出流程，生成 MoreCustoms.pck 到游戏的 mods/MoreCustoms 目录。

交付检查：

- 确认 mods/MoreCustoms 内至少包含 MoreCustoms.dll、MoreCustoms.pck、mod_manifest.json。
- config.json 若不存在会在首次构建或运行时生成。

## Agent 执行偏好

- 若需求是功能开发，优先改 MoreCustoms，不要默认改 STS2Decode。
- 若需求涉及原版行为定位，先在 STS2Decode 查证，再回到 MoreCustoms 实现。
- 若需求涉及 Modifier 文案或显示，记得同步检查中英文本地化。
- 若需求涉及自定义模式入口是否出现新选项，优先检查 Modifier 注入补丁。
- 若需求涉及无尽模式，优先检查 Endless 相关补丁、事件模型和配置项。
- 若需求涉及构建失败或无法发布，优先检查 .env、GodotPath、SteamLibraryPath、游戏版本与 Godot 版本是否匹配。

## 默认回答约束

- 解释改动时，优先说明改动落在哪类文件，以及为什么在那里改。
- 如果用户没有要求，不要自动 merge 分支。
- 如果用户没有要求，不要修改 README 或其他文档以外的无关文件。