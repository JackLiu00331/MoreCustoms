using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace ModTemplate.Config;

public static class MoreCustomsConfig
{
  private const string ConfigFileName = "config.json";

  public static Settings Current { get; private set; } = new();

  public static string ConfigPath { get; private set; } = string.Empty;

  public static void LoadOrCreate(MegaCrit.Sts2.Core.Logging.Logger logger)
  {
    string gameDir = Path.GetDirectoryName(OS.GetExecutablePath()) ?? ".";
    string modDir = Path.Combine(gameDir, "mods", "MoreCustoms");
    ConfigPath = Path.Combine(modDir, ConfigFileName);

    Directory.CreateDirectory(modDir);

    if (!File.Exists(ConfigPath))
    {
      Current = new Settings();
      SaveCurrent();
      logger.Info($"[MoreCustoms] Config created at: {ConfigPath}");
      return;
    }

    try
    {
      string raw = File.ReadAllText(ConfigPath);
      Settings? loaded = JsonSerializer.Deserialize<Settings>(raw);
      Current = loaded ?? new Settings();
      bool hasBossHpMultiplier = raw.Contains("\"BossHpMultiplier\"", StringComparison.OrdinalIgnoreCase);
      bool hasPlatingBasePerAct = raw.Contains("\"PlatingBasePerAct\"", StringComparison.OrdinalIgnoreCase);
      bool hasGoldGainMultiplier = raw.Contains("\"GoldGainMultiplier\"", StringComparison.OrdinalIgnoreCase);
      bool hasRestSiteSmithCount = raw.Contains("\"RestSiteSmithCount\"", StringComparison.OrdinalIgnoreCase);
      bool hasEnableEndlessDebugLogs = raw.Contains("\"EnableEndlessDebugLogs\"", StringComparison.OrdinalIgnoreCase);
      bool hasEndlessEnemyHpPerActPercent = raw.Contains("\"EndlessEnemyHpPerActPercent\"", StringComparison.OrdinalIgnoreCase);
      bool hasEndlessBossExtraHpPerActPercent = raw.Contains("\"EndlessBossExtraHpPerActPercent\"", StringComparison.OrdinalIgnoreCase);
      bool hasEndlessDoubleBossExtraHpPercent = raw.Contains("\"EndlessDoubleBossExtraHpPercent\"", StringComparison.OrdinalIgnoreCase);
      bool hasEndlessEnemyStrengthEveryActs = raw.Contains("\"EndlessEnemyStrengthEveryActs\"", StringComparison.OrdinalIgnoreCase);
      bool hasEndlessEnemyStrengthPerStep = raw.Contains("\"EndlessEnemyStrengthPerStep\"", StringComparison.OrdinalIgnoreCase);

      bool normalizedChanged = Normalize();
      bool shouldRewrite = loaded == null || !hasBossHpMultiplier || !hasPlatingBasePerAct || !hasGoldGainMultiplier || !hasRestSiteSmithCount || !hasEnableEndlessDebugLogs || !hasEndlessEnemyHpPerActPercent || !hasEndlessBossExtraHpPerActPercent || !hasEndlessDoubleBossExtraHpPercent || !hasEndlessEnemyStrengthEveryActs || !hasEndlessEnemyStrengthPerStep || normalizedChanged;

      if (shouldRewrite)
      {
        SaveCurrent();
      }

      logger.Info($"[MoreCustoms] Config loaded: bossHpMultiplier={Current.BossHpMultiplier}, platingBasePerAct={Current.PlatingBasePerAct}, goldGainMultiplier={Current.GoldGainMultiplier}, restSiteSmithCount={Current.RestSiteSmithCount}, endlessDebug={Current.EnableEndlessDebugLogs}, endlessHpPerActPct={Current.EndlessEnemyHpPerActPercent}, endlessBossExtraHpPerActPct={Current.EndlessBossExtraHpPerActPercent}, endlessDoubleBossExtraHpPct={Current.EndlessDoubleBossExtraHpPercent}, endlessStrengthEveryActs={Current.EndlessEnemyStrengthEveryActs}, endlessStrengthPerStep={Current.EndlessEnemyStrengthPerStep}");
    }
    catch (Exception ex)
    {
      Current = new Settings();
      SaveCurrent();
      logger.Error($"[MoreCustoms] Failed to load config, reset to defaults. Error: {ex.Message}");
    }
  }

  private static bool Normalize()
  {
    bool changed = false;

    if (Current.BossHpMultiplier <= 0m)
    {
      Current.BossHpMultiplier = 1m;
      changed = true;
    }

    if (Current.PlatingBasePerAct < 0)
    {
      Current.PlatingBasePerAct = 0;
      changed = true;
    }

    if (Current.GoldGainMultiplier <= 0m)
    {
      Current.GoldGainMultiplier = 1m;
      changed = true;
    }

    if (Current.RestSiteSmithCount < 1)
    {
      Current.RestSiteSmithCount = 1;
      changed = true;
    }

    if (!Current.EnableEndlessDebugLogs)
    {
      Current.EnableEndlessDebugLogs = false;
    }

    if (Current.EndlessEnemyHpPerActPercent < 0m)
    {
      Current.EndlessEnemyHpPerActPercent = 0m;
      changed = true;
    }

    if (Current.EndlessBossExtraHpPerActPercent < 0m)
    {
      Current.EndlessBossExtraHpPerActPercent = 0m;
      changed = true;
    }

    if (Current.EndlessDoubleBossExtraHpPercent < 0m)
    {
      Current.EndlessDoubleBossExtraHpPercent = 0m;
      changed = true;
    }

    if (Current.EndlessEnemyStrengthEveryActs < 1)
    {
      Current.EndlessEnemyStrengthEveryActs = 1;
      changed = true;
    }

    if (Current.EndlessEnemyStrengthPerStep < 0)
    {
      Current.EndlessEnemyStrengthPerStep = 0;
      changed = true;
    }

    return changed;
  }

  private static void SaveCurrent()
  {
    string content = JsonSerializer.Serialize(Current, new JsonSerializerOptions
    {
      WriteIndented = true
    });
    File.WriteAllText(ConfigPath, content);
  }

  public class Settings
  {
    public decimal BossHpMultiplier { get; set; } = 2m;

    public int PlatingBasePerAct { get; set; } = 5;

    public decimal GoldGainMultiplier { get; set; } = 2m;

    public int RestSiteSmithCount { get; set; } = 2;

    public bool EnableEndlessDebugLogs { get; set; } = true;

    public decimal EndlessEnemyHpPerActPercent { get; set; } = 10m;

    public decimal EndlessBossExtraHpPerActPercent { get; set; } = 10m;

    public decimal EndlessDoubleBossExtraHpPercent { get; set; } = 15m;

    public int EndlessEnemyStrengthEveryActs { get; set; } = 1;

    public int EndlessEnemyStrengthPerStep { get; set; } = 1;
  }
}
