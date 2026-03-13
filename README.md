A template for an empty Slay the Spire 2 mod with BaseLib as a dependency.

See the [wiki](https://github.com/Alchyr/ModTemplate-StS2/wiki) to get started.

## Rebuild & Test (MoreCustoms)

Every time you add new code, patches, or localization, run these steps:

1. Close the game completely.
2. Build DLL + manifest copy:

```powershell
dotnet build ModTemplate.csproj
```

3. Re-pack `.pck` (required if resources/localization changed, recommended every iteration):

```powershell
dotnet publish ModTemplate.csproj
```

4. Start the game again.

Expected deployed files are in:

`G:/steam/steamapps/common/Slay the Spire 2/mods/MoreCustoms/`

- `MoreCustoms.dll`
- `mod_manifest.json`
- `MoreCustoms.pck`
- `config.json`

If you still have an old folder from previous name (`mods/ModTemplate`), you can delete it.

## Modifier 图标接入指南（本次功能总结）

本次已经为 4 个 modifier 接入了 topbar 图标，核心规则如下：

- 游戏会自动按 `ModifierModel` 的 `Id.Entry` 去找图标。
- 默认图标路径规则是：`res://images/packed/modifiers/<id_lowercase>.png`
- 例如：`BOSS_HP_DOUBLE_DEBUFF` 对应 `images/packed/modifiers/boss_hp_double_debuff.png`

### 下次新增功能时，如何给新 modifier 加图标

1. 先确定你的 modifier ID（通常就是 localization key 的前缀，如 `NEW_COOL_BUFF`）。
2. 准备一张 png，命名为该 ID 的小写蛇形格式：`new_cool_buff.png`。
3. 放到项目目录：`images/packed/modifiers/new_cool_buff.png`。
4. 用 Godot 导入该资源（会自动生成对应 `.import` 与 `.godot/imported/*.ctex` 缓存）：
	 - 最简单方式：执行一次 `dotnet publish ModTemplate.csproj`（会触发 Godot 导出流程）。
5. 重启游戏，在 Custom Run 里启用该 modifier，检查 topbar 图标是否替换 NOPE。

### 打包与发布注意事项

- 发布后在 `mods/MoreCustoms/` 看不到单独 `images` 文件夹是正常的。
- 图片资源会被打进 `MoreCustoms.pck`，运行时从 `.pck` 内加载。
- 所以发布给其他用户时，确保这三个文件是同一次产物即可：
	- `MoreCustoms.dll`
	- `MoreCustoms.pck`
	- `mod_manifest.json`

### 常见问题排查

- 仍显示 NOPE：先检查文件名是否与 ID 小写格式完全一致。
- 改了图片没生效：重新 `dotnet publish` 后重启游戏。
- 只 build 不 publish：代码会更新，但资源 `.pck` 可能还是旧的。

## Config (可自定义数值)

`MoreCustoms` 会在这里读取配置：

`G:/steam/steamapps/common/Slay the Spire 2/mods/MoreCustoms/config.json`

默认内容：

```json
{
	"BossHpMultiplier": 2,
	"PlatingBasePerAct": 5,
	"GoldGainMultiplier": 2,
	"RestSiteSmithCount": 2
}
```

- `BossHpMultiplier`：Boss 血量倍率（包含 TestSubject 二/三阶段）
- `PlatingBasePerAct`：每幕起始敌方护甲基础值（实际值 = 该数值 × 当前幕数）
- `GoldGainMultiplier`：金币获取倍率（默认 `2` 即额外 +100%）
- `RestSiteSmithCount`：休息处每次锻造可升级的牌数量（默认 `2`）

说明：
- `dotnet build` / `dotnet publish` 不会覆盖你已经改过的 `mods/MoreCustoms/config.json`。
- 当版本升级新增配置项时，mod 会在加载时自动补齐缺失字段并保留你已有配置值。
- 若你删除该文件，mod 会在下次加载时自动按默认值重新生成。

## Development Guide (EN)

### 1) Where is the main logic?

- Entry point: [MainFile.cs](MainFile.cs)
	- `Initialize()` creates Harmony instance and calls `PatchAll()`.
- Feature logic (modifier behavior): [Modifiers/BossHpDoubleDebuff.cs](Modifiers/BossHpDoubleDebuff.cs)
	- This is where gameplay effect is implemented.
- Injection logic (show in Custom Run list): [Patches/BadModifierInjectionPatch.cs](Patches/BadModifierInjectionPatch.cs)
	- This patch appends custom modifier into `ModelDb.BadModifiers`.

### 2) Steps to add a new feature (recommended order)

1. Create a new modifier class under `Modifiers/` inheriting `ModifierModel`.
2. Implement effect logic in appropriate hooks (`AfterCreatureAddedToCombat`, `ModifyDamageMultiplicative`, etc.).
3. Add/extend patch under `Patches/` to inject the modifier into `GoodModifiers` or `BadModifiers`.
4. Add localization keys in both:
	 - [MoreCustoms/localization/eng/modifiers.json](MoreCustoms/localization/eng/modifiers.json)
	 - [MoreCustoms/localization/zhs/modifiers.json](MoreCustoms/localization/zhs/modifiers.json)
5. Run build/publish commands.
6. Restart game and validate in Custom Run.

### 3) What should patches do?

- Patches should connect your custom content to game registries or UI lists.
- Keep patches small and focused:
	- list injection
	- compatibility guards
	- avoid rewriting unrelated game logic

### 4) Localization workflow

- Current table path:
	- `res://MoreCustoms/localization/eng/modifiers.json`
	- `res://MoreCustoms/localization/zhs/modifiers.json`
- Key naming convention:
	- `<MODIFIER_ID>.title`
	- `<MODIFIER_ID>.description`
- Example:
	- `BOSS_HP_DOUBLE_DEBUFF.title`
	- `BOSS_HP_DOUBLE_DEBUFF.description`

## 开发说明（中文）

### 1）主逻辑在哪里？

- 入口文件：[MainFile.cs](MainFile.cs)
	- `Initialize()` 里创建 Harmony 并执行 `PatchAll()`。
- 功能逻辑（Debuff 生效逻辑）：[Modifiers/BossHpDoubleDebuff.cs](Modifiers/BossHpDoubleDebuff.cs)
	- 这里写“具体改数值”的代码。
- 注入逻辑（让它出现在 Custom Run 列表）：[Patches/BadModifierInjectionPatch.cs](Patches/BadModifierInjectionPatch.cs)
	- 该补丁把自定义 modifier 加进 `ModelDb.BadModifiers`。

### 2）以后新增功能的开发步骤（推荐顺序）

1. 在 `Modifiers/` 新建一个继承 `ModifierModel` 的类。
2. 选择合适 hook 实现效果（如 `AfterCreatureAddedToCombat`、`ModifyDamageMultiplicative`）。
3. 在 `Patches/` 新增或扩展注入补丁，把它加入 `GoodModifiers` 或 `BadModifiers`。
4. 增加中英文文本：
	 - [MoreCustoms/localization/eng/modifiers.json](MoreCustoms/localization/eng/modifiers.json)
	 - [MoreCustoms/localization/zhs/modifiers.json](MoreCustoms/localization/zhs/modifiers.json)
5. 执行 build/publish。
6. 重启游戏，在 Custom Run 验证。

### 3）Patch 具体负责什么？

- Patch 主要负责“接线”：把你的内容接到游戏现有列表/流程里。
- 建议 Patch 保持小而专一：
	- 列表注入
	- 兼容性判断
	- 不在 Patch 里塞过多业务逻辑

### 4）Localization 在哪，怎么加？

- 路径：
	- `res://MoreCustoms/localization/eng/modifiers.json`
	- `res://MoreCustoms/localization/zhs/modifiers.json`
- 命名约定：
	- `<MODIFIER_ID>.title`
	- `<MODIFIER_ID>.description`
- 例如：
	- `BOSS_HP_DOUBLE_DEBUFF.title`
	- `BOSS_HP_DOUBLE_DEBUFF.description`

## Debug & Boss HP Notes

- DevConsole 的 debug 命令默认需要“调试权限”。
	- 在当前版本中，加载了任意 mod 时通常会开放（你现在就是这种情况）。
	- 不加载 mod 的情况下，很多命令不会出现。
- 如果你想看“未启用你这个 mod 时的 Boss 默认血量”，推荐两种方法：
	1. 反编译查看（最准确）
		 - 先找 Boss 遭遇：`MegaCrit.Sts2.Core.Models.Encounters` 中 `RoomType.Boss`
		 - 再看对应怪物：`MegaCrit.Sts2.Core.Models.Monsters` 中 `MinInitialHp / MaxInitialHp`
	2. 对照测试法
		 - 同一难度、同一 Boss：关闭 MoreCustoms 记录血量 → 开启后再进同 Boss 对比。

## 单人模式 Boss 默认血量清单（A0 / A8+）

说明：
- 这是反编译代码里的“初始血量”清单。
- 单人模式下不触发多人血量缩放。
- A8+ 指 Ascension `ToughEnemies` 生效后的数值。

| Boss Encounter | 开场怪物 | A0 默认血量 | A8+ 血量 |
|---|---|---:|---:|
| CeremonialBeastBoss | CeremonialBeast | 252 | 262 |
| DoormakerBoss | Door（开场） | `155` | 165 |
| KaiserCrabBoss | Crusher + Rocket | 199 + 189 = 388 | 209 + 199 = 408 |
| KnowledgeDemonBoss | KnowledgeDemon | 379 | 399 |
| LagavulinMatriarchBoss | LagavulinMatriarch | 222 | 233 |
| QueenBoss | Queen + TorchHeadAmalgam | 400 + 199 = 599 | 419 + 211 = 630 |
| SoulFyshBoss | SoulFysh | 211 | 221 |
| TestSubjectBoss | TestSubject（一阶段） | 100 | 111 |
| TheInsatiableBoss | TheInsatiable | 321 | 341 |
| TheKinBoss | 2×KinFollower + KinPriest | (58–59)×2 + 190 = 306–308 | (62–63)×2 + 199 = 323–325 |
| VantomBoss | Vantom | 173 | 183 |
| WaterfallGiantBoss | WaterfallGiant | 250 | 260 |

补充（TestSubject 多阶段）：
- 二阶段：A0 `200`，A8+ `212`
- 三阶段：A0 `300`，A8+ `313`

主要来源（反编译）：
- Boss 遭遇定义：`STS2Decode/MegaCrit.Sts2.Core.Models.Encounters/*Boss.cs`
- 怪物血量定义：`STS2Decode/MegaCrit.Sts2.Core.Models.Monsters/*.cs` 中 `MinInitialHp / MaxInitialHp`





#Jerry TODO:
推荐方案（最符合你描述）

核心目标：地图上所有普通节点都显示为问号，并且进入问号时有概率进入休息处。
实现思路：
用 Modifier 的地图 Hook 把地图节点类型统一改为 Unknown（保留 Ancient 起点和 Boss 点不动），这个能力在 AbstractModel.cs 的 ModifyGeneratedMap。
同时对 Unknown 房间的实际落地逻辑做补丁：当前原版 Unknown 掷点不包含 RestSite（可见 UnknownMapPointOdds.cs），需要在 Roll 逻辑里加入 RestSite 概率。
对应入口映射在 RunManager.cs 的 RollRoomTypeFor。
优点：效果和文案“勇者无畏”完全一致，玩家看到和实际行为一致。
风险：要 patch 一处私有流程，但可控。
备选方案 A（更轻量，但不完全符合）

只改地图图标显示为问号，不改实际房间类型。
优点：实现最稳；缺点：本质不是“问号房机制”，只是视觉伪装，不满足你“问号会出现休息处”的机制目标。
备选方案 B（机制近似）

全图 Unknown，但不动 Unknown 掷点；改成在 Event 中注入“休息事件”。
优点：少改底层；缺点：休息处变成事件分支，不是真正 RestSite 房间，手感和奖励逻辑会有偏差。
我建议直接做“推荐方案”。
你只要确认两点我就开工：

RestSite 在问号中的权重你想设多少（例如 10%）？
是否保留 Boss/Ancient 节点原样（通常建议保留）？