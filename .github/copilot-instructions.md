## 项目概述

**MoreCustoms** 是《杀戮尖塔 2》(Slay the Spire 2) 的 C# Mod，基于 Harmony + 游戏原生 Hook 系统实现。
目标：在自定义模式中加入更多正面 Buff 与负面 Debuff，并支持无尽模式扩展。

- 游戏引擎：Godot 4（C# 脚本层）
- Mod 框架：游戏内建 ModInitializer + Harmony（用于 Hook 无法覆盖的 UI/逻辑层）
- 语言：C# (.NET)
- 构建：`dotnet build` / `dotnet publish`

---

## 目录结构

```
MoreCustoms/
├── MainFile.cs                     # 入口：Initialize() → Harmony.PatchAll()
├── Config/
│   └── MoreCustomsConfig.cs        # 读写 config.json，所有可配置数值都在这里
├── Models/
│   └── EndlessRunData.cs           # 无尽模式专用持久化数据
├── Modifiers/                      # 每个 Modifier 一个文件，继承 ModifierModel
│   ├── BossHpDoubleDebuff.cs       # 示例：梦魇首领
│   ├── ScalingPlatingDebuff.cs     # 示例：减伤屏障
│   └── ...
├── Patches/                        # Harmony Patch，只用于 Hook 无法覆盖的情况
│   └── ...
├── MoreCustoms/
│   └── localization/
│       ├── eng/modifiers.json      # 英文文本
│       └── zhs/modifiers.json      # 中文文本
└── Resources/images/               # Modifier 图标 (PNG)
```

---

## 核心概念：两种开发方式

### 方式 A：原生 Hook（优先使用）

游戏在 `MegaCrit.Sts2.Core.Hooks.Hook` 里暴露了完整的事件管线。
任何继承了 `AbstractModel` 的类（包括 `ModifierModel`）都可以 override 这些方法。

**原则：能用 Hook 就不用 Harmony。Hook 更稳定，游戏更新后不易崩。**

### 方式 B：Harmony Patch（仅用于 UI 层或 Hook 无法拦截的逻辑）

典型场景：修改意图显示、拦截 MonsterMoveStateMachine 的行动决策。

---

## Hook 速查表（最常用）

### 战斗事件类（async Task）

| Hook 方法 | 触发时机 | 典型用途 |
|---|---|---|
| `AfterCardPlayed(choiceContext, cardPlay)` | 玩家打出一张牌后 | 双重压力、寄生增幅 |
| `BeforeCardPlayed(cardPlay)` | 打牌前 | 拦截/修改打牌行为 |
| `AfterDamageReceived(choiceContext, target, result, ...)` | 角色受到伤害后 | 刃刀入肉（后处理） |
| `BeforeDamageReceived(choiceContext, target, amount, ...)` | 受到伤害前 | 拦截伤害 |
| `AfterDeath(ctx, creature, wasRemovalPrevented, ...)` | 角色/怪物死亡后 | 顽强作战、套娃麻烦 |
| `BeforeDeath(runState, combatState, creature)` | 死亡前 | 死亡预防逻辑 |
| `AfterTurnEnd(ctx, side)` | 回合结束后 | 战场失序、熔岩爆发计数 |
| `BeforeTurnEnd(ctx, side)` | 回合结束前 | 回合末触发效果 |
| `BeforeSideTurnStart(ctx, side, combatState)` | 某一方回合开始前 | 熔岩爆发计数、回合压力 |
| `AfterPlayerTurnStart(ctx, player)` | 玩家回合开始后 | 每回合 buff/debuff |
| `BeforeCombatStart()` | 战斗开始前 | 给怪物附加初始 buff |
| `AfterCombatEnd(room)` | 战斗结束后 | 重置战斗内计数器 |
| `AfterCombatVictory(room)` | 战斗胜利后 | 累加跨战斗数据 |
| `AfterBlockGained(creature, amount, ...)` | 获得格挡后 | 格挡相关效果 |
| `AfterCurrentHpChanged(creature, delta)` | HP 变化后（delta 正/负） | 血量追踪 |
| `AfterGoldGained(player)` | 金币变化后（减少时也触发） | 绝境利滚利 |
| `AfterActEntered()` | 进入新一幕后 | 诅咒蔓延 |
| `AfterMapGenerated(map, actIndex)` | 地图生成后 | 勇者无畏（房间替换） |
| `AfterRestSiteSmith(player)` | 休息处锻造后 | 锻造代价 |
| `AfterRestSiteHeal(player, isMimicked)` | 休息处恢复后 | |
| `AfterItemPurchased(player, entry, goldSpent)` | 商店购买后 | 贪婪商人追踪 |
| `AfterRewardTaken(player, reward)` | 取走奖励后 | 奖励追踪 |
| `BeforeRoomEntered(room)` | 进入房间前 | 房间事件 |
| `AfterRoomEntered(room)` | 进入房间后 | |

### 数值修改类（同步，返回值）

| Hook 方法 | 返回值 | 用途 |
|---|---|---|
| `ModifyDamageAdditive(target, damage, props, dealer, cardSource)` | `decimal`（加法增量） | 给伤害加固定值 |
| `ModifyDamageMultiplicative(target, damage, props, dealer, cardSource)` | `decimal`（乘法系数，默认 1.0） | 伤害翻倍/减半 |
| `ModifyDamageCap(target, props, dealer, cardSource)` | `decimal`（最大值上限） | 限制最大伤害 |
| `ModifyBlockAdditive(target, block, props, cardSource, cardPlay)` | `decimal`（加法增量） | 修改格挡量 |
| `ModifyBlockMultiplicative(target, block, ...)` | `decimal`（乘法系数） | 格挡翻倍 |
| `ModifyCardPlayCount(card, target, playCount)` | `int` | 改变牌的播放次数 |
| `ModifyAttackHitCount(attackCommand, originalHitCount)` | `decimal` | 改变攻击打击次数 |

### Should 类（bool，用于拦截/阻止行为）

| Hook 方法 | 返回 false 的效果 |
|---|---|
| `ShouldProcurePotion(potion, player)` | 阻止获得药水（封禁行为） |
| `ShouldPlay(card, autoPlayType)` | 阻止打出某张牌 |
| `ShouldDraw(player, fromHandDraw)` | 阻止摸牌 |
| `ShouldDie(creature)` | 阻止死亡（返回 false = 存活） |
| `ShouldFlush(player)` | 阻止弃牌 |
| `ShouldGainGold(amount, player)` | 阻止获得金币 |
| `ShouldGenerateTreasure(player)` | 阻止生成宝箱 |
| `ShouldStopCombatFromEnding()` | 阻止战斗结束 |

### 奖励/牌池修改类

| Hook 方法 | 用途 |
|---|---|
| `TryModifyCardRewardOptions(player, options, creationOptions)` | 修改战斗奖励牌列表（贫瘠战利、饥不择食、硬件故障） |
| `ModifyCardRewardCreationOptions(player, options)` | 修改牌的生成规则 |
| `TryModifyCardBeingAddedToDeck(card, out newCard)` | 拦截加入牌组的牌 |

---

## 如何新增一个 Modifier（标准五步）

### 第一步：创建 Modifier 类

在 `Modifiers/` 目录下新建 `XxxModifier.cs`：

```csharp
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
// 按需引入其他命名空间

namespace ModTemplate.Modifiers;

public class XxxModifier : ModifierModel
{
    // Modifier 的唯一 ID（与本地化 key 对应）
    public override string Id => "MODIFIER_ID";

    // 是否是负面 Debuff（影响在 UI 中的显示位置）
    public override bool IsDebuff => true;

    // 在这里 override 需要的 Hook 方法
    // 参考下方"双重压力完整实现"作为模板
}
```

### 第二步：注入到 Modifier 列表

在 `Patches/` 目录下找到负责注入 Modifier 列表的 Patch 文件，把新 Modifier 加入 Bad（或 Good）列表。

### 第三步：添加本地化文本

`MoreCustoms/localization/zhs/modifiers.json`：
```json
{
  "modifiers.MODIFIER_ID.title": "中文名称",
  "modifiers.MODIFIER_ID.description": "效果描述，支持 {VALUE} 占位符"
}
```

`MoreCustoms/localization/eng/modifiers.json`：
```json
{
  "modifiers.MODIFIER_ID.title": "English Name",
  "modifiers.MODIFIER_ID.description": "Effect description."
}
```

### 第四步：添加图标

把 128×128 PNG 图标放到 `Resources/images/modifiers/modifier_id.png`（小写）。

### 第五步：构建验证

```bash
dotnet build MoreCustoms.csproj
dotnet publish MoreCustoms.csproj
# 启动游戏，在自定义模式中验证新 Modifier 出现
```

---

## 配置系统

所有可调数值通过 `Config/MoreCustomsConfig.cs` 管理，对应游戏目录下的 `config.json`。

新增配置项步骤：
1. 在 `MoreCustomsConfig` 类里加一个 `public int/decimal MyValue { get; set; } = 默认值;`
2. 在 Modifier 里通过 `MoreCustomsConfig.Instance.MyValue` 读取
3. `config.json` 会在首次运行后自动生成/更新

---

## 重要规范

### 关于 Harmony vs Hook

- **优先 Hook**：凡是 `Hook.cs` 里有对应方法的，一律用 override，不用 Harmony。
- **Harmony 仅用于**：UI 渲染层（意图显示、手牌显示）、MonsterMoveStateMachine 行动决策。
- Harmony Patch 放在 `Patches/` 目录，类名以 `Patch` 结尾。

### 关于伤害管线

游戏有内建的伤害修改管线，按以下顺序执行：
1. `ModifyDamageAdditive`（所有加法修改求和）
2. `ModifyDamageMultiplicative`（所有乘法修改连乘）
3. `ModifyDamageCap`（取最小上限值）

**不要用 Harmony 拦截伤害计算，用上面三个 override 方法。**

### 关于跨战斗状态持久化

Modifier 的字段在局内持久存在，但需要注意：
- 战斗内计数器（如回合数）在 `AfterCombatEnd` 里重置
- 跨战斗累计数据（如击杀数）在 `AfterCombatVictory` 里更新

### 关于多人模式

游戏支持多人，部分 Hook（如 `AfterDeath`）通过 `LocalContext.NetId` 判断本地玩家。
AI 开发时如遇到 `netId.HasValue` 检查，保持这个模式不要删除。

---

## 已实现的 Modifier 参考

| ID | 类名 | 核心 Hook |
|---|---|---|
| `BOSS_HP_DOUBLE_DEBUFF` | `BossHpDoubleDebuff` | BeforeCombatStart |
| `SCALING_PLATING_DEBUFF` | `ScalingPlatingDebuff` | BeforeCombatStart |
| `FEARLESS_HERO_BUFF` | `FearlessHeroModifier` | AfterMapGenerated |
| `NO_POTION_DEBUFF` | `NoPotionDebuff` | ShouldProcurePotion |
| `CLONE_RUN_DEBUFF` | `CloneRunDebuff` | 多个 Hook |
| `GOLD_GAIN_BUFF` | `GoldGainBuff` | AfterGoldGained |
| `MULTI_SMITH_BUFF` | `MultiSmithBuff` | AfterRestSiteSmith |
| `INFINITY_ENDLESS_MODE_DEBUFF` | `InfinityEndlessModeDebuff` | AfterCombatVictory |

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
2. 只进行 commit,因为需要进一步测试，等用户说明测试完成后再进行push。
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

- build：编译 MoreCustoms.dll，并把 dll 与 MoreCustoms.json 复制到游戏的 mods/MoreCustoms 目录；若目标目录还没有 config.json，也会复制默认配置；同时复制 BaseLib.dll。
- publish：在 build 基础上继续调用 Godot 导出流程，生成 MoreCustoms.pck 到游戏的 mods/MoreCustoms 目录。

交付检查：

- 确认 mods/MoreCustoms 内至少包含 MoreCustoms.dll、MoreCustoms.pck、MoreCustoms.json。
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