## MoreCustoms（Slay the Spire 2 Mod）

一个基于 ModTemplate + BaseLib 的 STS2 自定义 Mod。

## 快速开始（开发/测试）

每次改完代码、Patch、本地化或图片后，建议按下面顺序：

1. 关闭游戏
2. 构建 DLL

```powershell
dotnet build ModTemplate.csproj
```

3. 重新打包资源（`.pck`）

```powershell
dotnet publish ModTemplate.csproj
```

4. 启动游戏验证

默认部署目录：

`X:/steam/steamapps/common/Slay the Spire 2/mods/MoreCustoms/`

关键文件：
- `MoreCustoms.dll`
- `MoreCustoms.pck`
- `mod_manifest.json`
- `config.json`

## 当前已实现 Modifier

- `梦魇首领`（`BOSS_HP_DOUBLE_DEBUFF`）
- `减伤屏障`（`SCALING_PLATING_DEBUFF`）
- `黄金赐福`（`GOLD_GAIN_BUFF`）
- `锻造大师`（`MULTI_SMITH_BUFF`）
- `勇者无畏`（`FEARLESS_HERO_BUFF`）

## TODO 路线图（按你图片内容整理）

说明：
- 以当前仓库实现为准打勾（`[x]`）。

- [ ] 短视症：除了当前房间的下一层的房间情况，你将无法看到其他的任何房间
- [ ] 小心炸弹：每一位敌人都有概率获得一枚炸弹，在死亡时发生爆炸
- [ ] 双重压力：每当你对敌人使用一张攻击卡牌都会收到 1 点伤害
- [ ] 熔岩爆发：战斗房间的 8 个回合之后熔岩将会喷发，对所有单位造成一半生命值的伤害
- [ ] 丧尸危机：每个战斗房间会额外生成 3 个僵尸（2 个小僵尸一个大僵尸）
- [ ] 复仇战士：每当一名角色死亡，其余所有友方单位获得 20% 最大血量并且清除自身所有负面效果
- [ ] 拿钱说话：每使用一张卡牌都会扣除 1 点金币
- [ ] Boss 战：每经历 12 个房间，下一场战斗将迎战随机 boss
- [ ] 生命汲取：所有的敌人获得造成伤害量的一半生命回复（格挡无法回复）
- [x] 减伤屏障：所有敌人开局时获得护甲，随层数升高
- [ ] 刃刀入肉：承受的未格挡伤害翻倍
- [ ] 虚空召唤：地图中某些房间会遭遇额外一名召唤类敌人，击败后获得升级奖励
- [x] 勇者无畏：所有房间都被 ? 替换，? 现在会出现休息处
- [ ] 暗无天日：战斗回合中，某些手牌你无法看清
- [ ] 来去无踪：除了敌人每回合的意图，你无法查看任何有关敌人的信息
- [ ] 封禁行为：你无法使用药水
- [ ] 顽强作战：所有敌人死亡后会额外行动一回合
- [ ] 套娃麻烦：所有敌人死亡后会生成一个只有一半数值的套娃，重复 3 次
- [ ] 遇强则强：你的升级卡牌越多，敌人会变得越强
- [x] 梦魇敌首：boss 会变得更加强大（仓库实现名：`梦魇首领`）
- [ ] 隐秘行动：无法查看敌人的意图
- [ ] 全力猛攻：敌人的所有意图全部是进攻
- [ ] 纳税日：获得的金币减少 20%
- [ ] 饥不择食：获得的卡牌奖励变为随机获得
- [ ] 虚空侵蚀：战斗开始时，你的半数卡牌获得虚无
- [ ] 饥荒：进入每个房间时你必须支付 5 点金币购买食物，否则就会饿死！
- [ ] 硬件故障：所有的能力卡牌不会出现在本局游戏
- [ ] 黑死病：你感染了黑死病，每过一个回合损失 5 点生命值，所有的药水被替换为抗生素（为你恢复血量并且暂时免疫黑死病）
- [ ] 今日歇业：商人不会在本局游戏出现，但是战斗结束后有几率遭遇神秘商人
- [ ] 灵魂连接：对单个敌人造成的伤害会均等的分摊给所有敌人

### 额外说明（当前仓库已实现但不在图片清单）

- [x] 黄金赐福：所有玩家获得额外金币
- [x] 锻造大师：在休息处可一次升级多张牌

## Topbar 图标规则（简版）

Modifier 图标按 ID 自动读取：

`res://images/packed/modifiers/<id_lowercase>.png`

例如：
- `FEARLESS_HERO_BUFF` → `images/packed/modifiers/fearless_hero_buff.png`

新增图标流程：
1. 准备 png，文件名与 modifier id 小写下划线一致
2. 放入 `images/packed/modifiers/`
3. 执行 `dotnet publish ModTemplate.csproj`
4. 重启游戏验证

## 配置文件

配置路径：

`X:/steam/steamapps/common/Slay the Spire 2/mods/MoreCustoms/config.json`

默认字段：

```json
{
  "BossHpMultiplier": 2,
  "PlatingBasePerAct": 5,
  "GoldGainMultiplier": 2,
  "RestSiteSmithCount": 2
}
```

含义：
- `BossHpMultiplier`：Boss 血量倍率
- `PlatingBasePerAct`：敌方护甲基础值（随幕数生效）
- `GoldGainMultiplier`：金币倍率
- `RestSiteSmithCount`：休息处每次可升级张数

## 开发入口（最常用）

- 入口：`MainFile.cs`
- Modifier 实现目录：`Modifiers/`
- 注入与玩法 Patch：`Patches/`
- 本地化：
  - `MoreCustoms/localization/eng/modifiers.json`
  - `MoreCustoms/localization/zhs/modifiers.json`

新增一个 Modifier 的最小步骤：
1. 在 `Modifiers/` 新建类（继承 `ModifierModel`）
2. 在 `Patches/` 注入到 Good/Bad 列表
3. 补齐中英文本地化
4. `dotnet build` + `dotnet publish`
