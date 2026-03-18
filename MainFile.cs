using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Modding;
using ModTemplate.Config;
using ModTemplate.Patches;

namespace ModTemplate;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
	private const string ModId = "MoreCustoms"; //At the moment, this is used only for the Logger and harmony names.
	private const string UnknownVersion = "unknown";

	public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);
	public static string ModVersion { get; private set; } = UnknownVersion;

	public static void Initialize()
	{
		Logger.Info("[MoreCustoms] Initialize begin.");
		LogBaseLibStatus();
		MoreCustomsConfig.LoadOrCreate(Logger);
		LoadModVersion();

		Harmony harmony = new(ModId);

		harmony.PatchAll();
		Logger.Info("[MoreCustoms] Harmony PatchAll complete.");
		EnsureUpdateNoticePatchInstalled(harmony);
	}

	private static void LogBaseLibStatus()
	{
		try
		{
			var thisAssembly = typeof(MainFile).Assembly;
			var referencedAssemblies = thisAssembly.GetReferencedAssemblies();
			bool hasCompileReference = referencedAssemblies.Any(a => a.Name != null && a.Name.Contains("BaseLib", StringComparison.OrdinalIgnoreCase));
			Logger.Info($"[MoreCustoms] BaseLib compile reference detected: {hasCompileReference}. Referenced assemblies: {referencedAssemblies.Length}");

			var loadedBaseLibAssemblies = AppDomain.CurrentDomain
				.GetAssemblies()
				.Where(a => a.GetName().Name != null && a.GetName().Name!.Contains("BaseLib", StringComparison.OrdinalIgnoreCase))
				.ToList();

			Logger.Info($"[MoreCustoms] BaseLib runtime assemblies loaded: {loadedBaseLibAssemblies.Count}");
			foreach (var assembly in loadedBaseLibAssemblies)
			{
				string location;
				try
				{
					location = string.IsNullOrWhiteSpace(assembly.Location) ? "<dynamic>" : assembly.Location;
				}
				catch
				{
					location = "<unavailable>";
				}

				Logger.Info($"[MoreCustoms] BaseLib assembly: {assembly.GetName().Name} {assembly.GetName().Version} @ {location}");
			}

			string gameDir = Path.GetDirectoryName(OS.GetExecutablePath()) ?? ".";
			string modsDir = Path.Combine(gameDir, "mods");
			string rootDll = Path.Combine(modsDir, "BaseLib.dll");
			string rootPck = Path.Combine(modsDir, "BaseLib.pck");
			string localDll = Path.Combine(modsDir, ModId, "BaseLib.dll");
			string localPck = Path.Combine(modsDir, ModId, "BaseLib.pck");

			Logger.Info($"[MoreCustoms] BaseLib file exists (root dll): {File.Exists(rootDll)} path={rootDll}");
			Logger.Info($"[MoreCustoms] BaseLib file exists (root pck): {File.Exists(rootPck)} path={rootPck}");
			Logger.Info($"[MoreCustoms] BaseLib file exists (mod dll): {File.Exists(localDll)} path={localDll}");
			Logger.Info($"[MoreCustoms] BaseLib file exists (mod pck): {File.Exists(localPck)} path={localPck}");
		}
		catch (Exception ex)
		{
			Logger.Error($"[MoreCustoms] Failed to log BaseLib status: {ex}");
		}
	}

	private static void LoadModVersion()
	{
		try
		{
			string gameDir = Path.GetDirectoryName(OS.GetExecutablePath()) ?? ".";
			string manifestPath = Path.Combine(gameDir, "mods", ModId, $"{ModId}.json");
			if (!File.Exists(manifestPath))
			{
				Logger.Warn($"[MoreCustoms] Manifest not found at {manifestPath}, using fallback version '{UnknownVersion}'.");
				ModVersion = UnknownVersion;
				return;
			}

			using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifestPath));
			if (document.RootElement.TryGetProperty("version", out JsonElement versionElement))
			{
				string? version = versionElement.GetString();
				ModVersion = string.IsNullOrWhiteSpace(version) ? UnknownVersion : version;
			}
			else
			{
				ModVersion = UnknownVersion;
			}

			Logger.Info($"[MoreCustoms] Loaded mod version: {ModVersion}");
		}
		catch (Exception ex)
		{
			ModVersion = UnknownVersion;
			Logger.Error($"[MoreCustoms] Failed to load mod version: {ex.Message}");
		}
	}

	private static void EnsureUpdateNoticePatchInstalled(Harmony harmony)
	{
		try
		{
			MethodInfo? runReadyMethod = AccessTools.Method(typeof(NRun), nameof(NRun._Ready));
			MethodInfo? gameReadyMethod = AccessTools.Method(typeof(NGame), nameof(NGame._Ready));
			MethodInfo? mainMenuReadyMethod = AccessTools.Method(typeof(NMainMenu), nameof(NMainMenu._Ready));

			MethodInfo? runPatchMethod = AccessTools.Method(typeof(UpdateNoticePopupPatch), "ShowUpdateNoticeAfterRunReady");

			if (runReadyMethod == null || runPatchMethod == null)
			{
				Logger.Warn($"[UpdateNotice] Patch verification skipped. runReadyMethodNull={runReadyMethod == null}, runPatchMethodNull={runPatchMethod == null}");
				return;
			}

			var runPatchInfo = Harmony.GetPatchInfo(runReadyMethod);
			bool runPatchedByThisMod = runPatchInfo?.Postfixes.Any(p => string.Equals(p.owner, ModId, StringComparison.OrdinalIgnoreCase)) == true;

			Logger.Info($"[UpdateNotice] Patch check for NRun._Ready: alreadyPatchedByThisMod={runPatchedByThisMod}, postfixCount={runPatchInfo?.Postfixes.Count ?? 0}");

			if (gameReadyMethod != null)
			{
				var gamePatchInfo = Harmony.GetPatchInfo(gameReadyMethod);
				bool gamePatchedByThisMod = gamePatchInfo?.Postfixes.Any(p => string.Equals(p.owner, ModId, StringComparison.OrdinalIgnoreCase)) == true;
				Logger.Info($"[UpdateNotice] Patch check for NGame._Ready: alreadyPatchedByThisMod={gamePatchedByThisMod}, postfixCount={gamePatchInfo?.Postfixes.Count ?? 0}");
			}

			if (mainMenuReadyMethod != null)
			{
				var menuPatchInfo = Harmony.GetPatchInfo(mainMenuReadyMethod);
				bool menuPatchedByThisMod = menuPatchInfo?.Postfixes.Any(p => string.Equals(p.owner, ModId, StringComparison.OrdinalIgnoreCase)) == true;
				Logger.Info($"[UpdateNotice] Patch check for NMainMenu._Ready: alreadyPatchedByThisMod={menuPatchedByThisMod}, postfixCount={menuPatchInfo?.Postfixes.Count ?? 0}");
			}

			if (!runPatchedByThisMod)
			{
				harmony.Patch(runReadyMethod, postfix: new HarmonyMethod(runPatchMethod));
				Logger.Warn("[UpdateNotice] Applied manual fallback patch to NRun._Ready.");
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"[UpdateNotice] Patch verification failed: {ex}");
		}
	}


}
