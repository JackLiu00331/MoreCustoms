using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

// 允许 act X 指令在无尽模式下跳到任意幕数（不限于当前幕列表上限）
// 若目标幕超出现有列表，则自动追加随机幕直到满足后再跳转。
[HarmonyPatch(typeof(ActConsoleCmd), nameof(ActConsoleCmd.Process))]
public static class EndlessActConsoleCmdPatch
{
  private static readonly PropertyInfo StateProperty = AccessTools.Property(typeof(RunManager), "State");

  [HarmonyPrefix]
  private static bool AllowUnboundedActJump(Player? issuingPlayer, string[] args, ref CmdResult __result)
  {
    if (args.Length != 1) return true;
    if (issuingPlayer?.RunState == null) return true;
    if (!int.TryParse(args[0], out int targetAct)) return true;
    if (!InfinityEndlessModeDebuff.IsActive(issuingPlayer.RunState)) return true;

    if (issuingPlayer.RunState.Players.Count > 1)
    {
      __result = new CmdResult(success: false,
        msg: "[Endless] `act X` long jump is disabled in multiplayer to prevent map vote desync. Use normal progression or singleplayer debug.");
      MainFile.Logger.Warn($"[Endless Debug] Blocked multiplayer act jump command: act {targetAct}.");
      return false;
    }

    int count = issuingPlayer.RunState.Acts.Count;

    // 目标在当前已有幕内（包括下限错误）交给原始命令处理
    if (targetAct < 1 || targetAct <= count) return true;

    RunState runState = (RunState)issuingPlayer.RunState;

    // 追加幕直到数量满足
    int appended = 0;
    while (runState.Acts.Count < targetAct)
    {
      // 取现有 loopCount = 距离第一个无尽幕的偏移，用于双Boss节奏判断
      int loopCount = runState.Acts.Count - (count - 1);
      EndlessEnterNextActPatch.AppendLoopAct(runState, loopCount);
      appended++;
    }

    int actIndex = targetAct - 1;
    Task task = JumpToAct(actIndex);
    MainFile.Logger.Info($"[Endless Debug] act {targetAct}: appended {appended} act(s), jumping to actIndex={actIndex}.");
    __result = new CmdResult(task, success: true, $"[Endless] Jumped to act {targetAct} (appended {appended} act(s)).");
    return false;
  }

  private static async Task JumpToAct(int actIndex)
  {
    NMapScreen.Instance?.SetTravelEnabled(enabled: true);
    await RunManager.Instance.EnterAct(actIndex);
  }
}
