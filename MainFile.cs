using System;
using System.IO;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using ModTemplate.Config;

namespace ModTemplate;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
	private const string ModId = "MoreCustoms"; //At the moment, this is used only for the Logger and harmony names.

	public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

	public static void Initialize()
	{
		Logger.Info("[MoreCustoms] Initialize begin.");
		LogBaseLibStatus();
		MoreCustomsConfig.LoadOrCreate(Logger);

		Harmony harmony = new(ModId);

		harmony.PatchAll();
		Logger.Info("[MoreCustoms] Harmony PatchAll complete.");
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


}
