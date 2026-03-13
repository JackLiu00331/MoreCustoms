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

      bool normalizedChanged = Normalize();
      bool shouldRewrite = loaded == null || !hasBossHpMultiplier || !hasPlatingBasePerAct || !hasGoldGainMultiplier || !hasRestSiteSmithCount || normalizedChanged;

      if (shouldRewrite)
      {
        SaveCurrent();
      }

      logger.Info($"[MoreCustoms] Config loaded: bossHpMultiplier={Current.BossHpMultiplier}, platingBasePerAct={Current.PlatingBasePerAct}, goldGainMultiplier={Current.GoldGainMultiplier}, restSiteSmithCount={Current.RestSiteSmithCount}");
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
  }
}
