# MoreCustoms（Slay the Spire 2 Mod）

当前版本：`v2.2.0`

为《杀戮尖塔2》自定义模式提供更多可选 Modifier（含正面 Buff 与负面 Debuff），并支持通过配置文件调整关键数值。

## 给玩家

### 功能简介

本 Mod 会在「自定义模式」中加入额外选项，偏向高难玩法，同时提供少量补偿型 Buff。

**注意：** 当前无尽模式仍在开发测试中，所以可能会有bug，请谨慎开启！

### 当前已实现

#### 正面 Buff

- `黄金赐福`（`GOLD_GAIN_BUFF`）：所有玩家获得额外金币
- `锻造大师`（`MULTI_SMITH_BUFF`）：休息处可一次升级多张牌

#### 负面 Debuff

- `梦魇首领`（`BOSS_HP_DOUBLE_DEBUFF`）：Boss 更耐打（可配置倍率）
- `减伤屏障`（`SCALING_PLATING_DEBUFF`）：敌人开局获得随进度成长的护甲
- `勇者无畏`（`FEARLESS_HERO_BUFF`）：地图房间强制变更为特殊规则分布
- `封禁行为`（`NO_POTION_DEBUFF`）：禁用药水获取与使用（并替换奖励逻辑）
- `克隆局`（`CLONE_RUN_DEBUFF`）：第二幕古神与克隆逻辑改写，附带生命代价
- `无限轮回`（`INFINITY_ENDLESS_MODE_DEBUFF`）：击败最终 Boss 后进入循环地图

### 安装

将以下文件放入：

`<你的Steam路径>/steamapps/common/Slay the Spire 2/mods/MoreCustoms/`

关键文件：

- `MoreCustoms.dll`
- `MoreCustoms.pck`
- `MoreCustoms.json`
- `config.json`（首次运行后可自动生成/更新）

### 常见问题

- 自定义模式里看不到新选项：请先删除旧版 `mods/MoreCustoms/` 后完整覆盖新包（不要只替换 dll）。
- 出现 `BaseLib.pck did not supply a mod manifest`：请删除 `mods` 目录中的所有 `BaseLib.pck`。
- 新选项加载时报 `LocException ... modifiers.xxx.title`：说明当前 `pck` 与 `dll` 版本不一致，请重新 `publish` 并完整替换。

---

## 给开发者

### 环境路径配置（多人协作）

为避免在 `MoreCustoms.csproj` 里硬编码个人机器路径，项目改为从根目录 `.env` 读取以下变量：

- `SteamLibraryPath`
- `GodotPath`

首次拉取后请执行：

1. 复制 `.env.example` 为 `.env`
2. 按自己机器实际路径填写上述两个变量

示例：

```env
SteamLibraryPath=G:/steam/steamapps
GodotPath=D:/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64.exe
```

说明：

- `.env` 已加入 `.gitignore`，不会提交到仓库
- `.env.example` 作为团队模板应保持在仓库中
- `build/publish` 命令保持不变，仅路径来源变为 `.env`

### 本地开发流程

1. 关闭游戏
2. 构建 dll

```powershell
dotnet build MoreCustoms.csproj
```

3. 打包并发布 pck

```powershell
dotnet publish MoreCustoms.csproj
```

4. 启动游戏验证日志

### 项目结构

- 入口：`MainFile.cs`
- Modifier 实现：`Modifiers/`
- 注入与玩法补丁：`Patches/`
- 本地化：
  - `MoreCustoms/localization/eng/modifiers.json`
  - `MoreCustoms/localization/zhs/modifiers.json`
- 图标资源：`images/packed/modifiers/`

### 新增一个 Modifier（最小步骤）

1. 在 `Modifiers/` 新建类，继承 `ModifierModel`
2. 在注入补丁中加入到 Good/Bad 列表
3. 为 `modifiers.<ID>.title/description` 补齐中英文文本
4. 添加对应图标：`images/packed/modifiers/<id_lowercase>.png`
5. `dotnet build` + `dotnet publish` 后进游戏验证

### 配置文件

路径：

`<你的Steam路径>/steamapps/common/Slay the Spire 2/mods/MoreCustoms/config.json`

示例：

```json
{
  "BossHpMultiplier": 2,
  "PlatingBasePerAct": 5,
  "GoldGainMultiplier": 2,
  "RestSiteSmithCount": 2,
  "EnableEndlessDebugLogs": true
}
```

---

## TODO（路线图）

说明：`[x]` 为已完成，`[ ]` 为计划中。

### 正面 Buff

- [x] 黄金赐福：所有玩家获得额外金币
- [x] 锻造大师：在休息处可一次升级多张牌
- [ ] 复仇战士：每当一名角色死亡，其余所有友方单位获得 20% 最大血量并清除负面效果

### 负面 Debuff

- [x] 梦魇敌首：Boss 会变得更加强大（仓库实现名：`梦魇首领`）
- [x] 减伤屏障：所有敌人开局时获得护甲，随层数升高
- [x] 勇者无畏：所有房间都被 `?` 替换，且 `?` 可出现休息处
- [x] 封禁行为：你无法使用药水
- [x] 克隆局：第二幕古神固定为佩尔，克隆会损失当前生命值 30%（向下取整）
- [ ] 短视症：除了当前房间下一层，无法看到其他房间信息
- [ ] 小心炸弹：敌人有概率携带炸弹，死亡时爆炸
- [ ] 双重压力：每当你对敌人使用攻击牌，自己受到 1 点伤害
- [ ] 熔岩爆发：战斗第 8 回合后熔岩喷发，对所有单位造成半血伤害
- [ ] 丧尸危机：每个战斗房间额外生成 3 个僵尸（2 小 1 大）
- [ ] 拿钱说话：每使用一张卡牌扣除 1 点金币
- [ ] Boss 战：每经历 12 个房间，下一场战斗改为随机 Boss
- [ ] 生命汲取：敌人获得其伤害一半的生命回复（格挡不触发）
- [ ] 刃刀入肉：承受的未格挡伤害翻倍
- [ ] 虚空召唤：某些房间遭遇额外召唤类敌人，击败后获得升级奖励
- [ ] 暗无天日：战斗中部分手牌信息被遮蔽
- [ ] 来去无踪：除敌人意图外，无法查看更多敌人信息
- [ ] 顽强作战：敌人死亡后会额外行动一回合
- [ ] 套娃麻烦：敌人死亡后生成半数值单位，最多重复 3 次
- [ ] 遇强则强：你的升级卡越多，敌人越强
- [ ] 隐秘行动：无法查看敌人意图
- [ ] 全力猛攻：敌人的所有意图都转为进攻
- [ ] 纳税日：获得金币减少 20%
- [ ] 饥不择食：卡牌奖励改为随机获得
- [ ] 虚空侵蚀：战斗开始时半数卡牌获得虚无
- [ ] 饥荒：每进一个房间必须支付 5 金币购买食物，否则死亡
- [ ] 硬件故障：能力牌不再出现在本局
- [ ] 黑死病：每回合损失 5 点生命，药水替换为抗生素
- [ ] 今日歇业：商人不出现，但战斗后有概率遇到神秘商人
- [ ] 灵魂连接：对单体敌人伤害按比例分摊给所有敌人
- [ ] 石头局：开局获得 10 张巨石与两瓶青蛙形状药水
