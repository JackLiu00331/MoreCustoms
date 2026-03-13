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
