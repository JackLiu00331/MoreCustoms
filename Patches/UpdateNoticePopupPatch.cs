using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ModTemplate.Config;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(NRun))]
public static class UpdateNoticePopupPatch
{
  private static bool _hasShownInSession;

  private static readonly string[] UpdateLines =
  {
    "1. 新增【更新通知】弹窗：进入游戏后会显示本版本更新内容。",
    "2. 适配了游戏V0.99.1的更新。",
    "3. 现在多次锻造buff下，可以看到查看升级效果按钮了。",
    "4. 修复了部分mod添加的buff在无尽模式下不生效的问题。",
    "5. 修复了部分mod添加的buff在无尽模式下不显示在modifier列表中的问题。",
    "6. 添加了部分官方的buff到无尽模式的古代选择池中。",
    "目前已知问题：",
    "1. 到达第16幕后，打完boss后游戏可能会闪烁黑屏导致无法继续游戏，正在调查中。",
    "2. 有玩家反馈在无尽模式中，拿完遗物会无法继续游戏，正在调查中，可以通过控制台输入‘travel’命令来继续游戏。",
  };

  [HarmonyPatch(nameof(NRun._Ready))]
  [HarmonyPostfix]
  private static void ShowUpdateNoticeAfterRunReady(NRun __instance)
  {
    TryShowUpdateNotice(__instance, "NRun._Ready");
  }

  [HarmonyPatch(nameof(NRun.SetCurrentRoom))]
  [HarmonyPostfix]
  private static void ShowUpdateNoticeAfterSetCurrentRoom(NRun __instance)
  {
    TryShowUpdateNotice(__instance, "NRun.SetCurrentRoom");
  }

  internal static void TryShowUpdateNotice(Node sourceNode, string trigger)
  {
    string version = MainFile.ModVersion;
    string lastSeen = MoreCustomsConfig.Current.LastSeenUpdateNoticeVersion;
    MainFile.Logger.Info($"[UpdateNotice] {trigger} triggered. currentVersion={version}, lastSeenVersion={lastSeen}, shownInSession={_hasShownInSession}");

    if (_hasShownInSession)
    {
      MainFile.Logger.Info("[UpdateNotice] Skip show: already shown in current game session.");
      return;
    }

    if (!MoreCustomsConfig.ShouldShowUpdateNotice(version))
    {
      MainFile.Logger.Info($"[UpdateNotice] Skip show: ShouldShowUpdateNotice returned false. currentVersion={version}, lastSeenVersion={lastSeen}");
      return;
    }

    if (sourceNode.GetTree() == null)
    {
      MainFile.Logger.Warn("[UpdateNotice] Skip show: SceneTree is null.");
      return;
    }

    if (sourceNode.GetTree().Root == null)
    {
      MainFile.Logger.Warn("[UpdateNotice] Skip show: SceneTree.Root is null.");
      return;
    }

    if (sourceNode.GetTree().Root.GetNodeOrNull<CanvasLayer>("MoreCustomsUpdateNoticeLayer") != null)
    {
      MainFile.Logger.Info("[UpdateNotice] Skip show: popup layer already exists in scene tree.");
      return;
    }

    _hasShownInSession = true;
    CanvasLayer popupRoot = BuildPopup(version);
    MainFile.Logger.Info($"[UpdateNotice] Popup node built. Name={popupRoot.Name}, Layer={popupRoot.Layer}, ChildCount={popupRoot.GetChildCount()}");
    sourceNode.GetTree().Root.CallDeferred(Node.MethodName.AddChild, popupRoot);
    MainFile.Logger.Info($"[UpdateNotice] AddChild deferred to SceneTree.Root. Showing update popup for version {version}.");
  }

  private static CanvasLayer BuildPopup(string version)
  {
    MainFile.Logger.Info($"[UpdateNotice] BuildPopup begin. version={version}");
    CanvasLayer canvasLayer = new()
    {
      Name = "MoreCustomsUpdateNoticeLayer",
      Layer = 200
    };

    Control overlay = new()
    {
      Name = "Overlay",
      MouseFilter = Control.MouseFilterEnum.Stop
    };
    overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
    canvasLayer.AddChild(overlay);

    ColorRect dimLayer = new()
    {
      Name = "DimLayer",
      Color = new Color(0f, 0f, 0f, 0.72f),
      MouseFilter = Control.MouseFilterEnum.Ignore
    };
    dimLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
    overlay.AddChild(dimLayer);

    PanelContainer panelContainer = new()
    {
      Name = "UpdateNoticePanel",
      CustomMinimumSize = new Vector2(880, 560),
      SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
      SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
      MouseFilter = Control.MouseFilterEnum.Stop
    };
    panelContainer.SetAnchorsPreset(Control.LayoutPreset.Center);
    panelContainer.AnchorLeft = 0.5f;
    panelContainer.AnchorTop = 0.5f;
    panelContainer.AnchorRight = 0.5f;
    panelContainer.AnchorBottom = 0.5f;
    panelContainer.OffsetLeft = -440;
    panelContainer.OffsetTop = -280;
    panelContainer.OffsetRight = 440;
    panelContainer.OffsetBottom = 280;

    StyleBoxFlat panelStyle = new()
    {
      BgColor = new Color(0.08f, 0.1f, 0.14f, 0.96f),
      BorderColor = new Color(0.85f, 0.7f, 0.35f, 1f),
      BorderWidthBottom = 2,
      BorderWidthLeft = 2,
      BorderWidthRight = 2,
      BorderWidthTop = 2,
      CornerRadiusBottomLeft = 14,
      CornerRadiusBottomRight = 14,
      CornerRadiusTopLeft = 14,
      CornerRadiusTopRight = 14,
      ShadowColor = new Color(0f, 0f, 0f, 0.35f),
      ShadowSize = 8
    };
    panelContainer.AddThemeStyleboxOverride("panel", panelStyle);
    overlay.AddChild(panelContainer);

    MarginContainer marginContainer = new()
    {
      Name = "Margin"
    };
    marginContainer.AddThemeConstantOverride("margin_top", 20);
    marginContainer.AddThemeConstantOverride("margin_bottom", 18);
    marginContainer.AddThemeConstantOverride("margin_left", 24);
    marginContainer.AddThemeConstantOverride("margin_right", 24);
    panelContainer.AddChild(marginContainer);

    VBoxContainer vBoxContainer = new()
    {
      Name = "Content"
    };
    vBoxContainer.AddThemeConstantOverride("separation", 14);
    marginContainer.AddChild(vBoxContainer);

    Label titleLabel = new()
    {
      Name = "Title",
      Text = $"更多自定义 {version} 更新内容",
      HorizontalAlignment = HorizontalAlignment.Center,
      AutowrapMode = TextServer.AutowrapMode.WordSmart,
      Modulate = new Color(1f, 0.93f, 0.75f, 1f)
    };
    titleLabel.AddThemeFontSizeOverride("font_size", 30);
    vBoxContainer.AddChild(titleLabel);

    Label subtitleLabel = new()
    {
      Name = "Subtitle",
      Text = "感谢游玩 更多自定义，以下是本次版本的主要更新：",
      HorizontalAlignment = HorizontalAlignment.Center,
      AutowrapMode = TextServer.AutowrapMode.WordSmart,
      Modulate = new Color(0.85f, 0.9f, 0.96f, 0.92f)
    };
    subtitleLabel.AddThemeFontSizeOverride("font_size", 16);
    vBoxContainer.AddChild(subtitleLabel);

    HSeparator separatorTop = new()
    {
      Name = "SeparatorTop"
    };
    vBoxContainer.AddChild(separatorTop);

    RichTextLabel bodyLabel = new()
    {
      Name = "Body",
      BbcodeEnabled = true,
      FitContent = false,
      ScrollActive = true,
      SizeFlagsVertical = Control.SizeFlags.ExpandFill,
      CustomMinimumSize = new Vector2(0, 320),
      Text = BuildBodyText()
    };
    bodyLabel.AddThemeFontSizeOverride("normal_font_size", 18);
    bodyLabel.Modulate = new Color(0.96f, 0.97f, 1f, 1f);
    vBoxContainer.AddChild(bodyLabel);

    HSeparator separatorBottom = new()
    {
      Name = "SeparatorBottom"
    };
    vBoxContainer.AddChild(separatorBottom);

    HBoxContainer footer = new()
    {
      Name = "Footer"
    };
    footer.AddThemeConstantOverride("separation", 10);
    vBoxContainer.AddChild(footer);

    Label footerHint = new()
    {
      Name = "FooterHint",
      Text = "提示：同一版本仅显示一次，更新版本后会再次出现。",
      SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
      VerticalAlignment = VerticalAlignment.Center,
      Modulate = new Color(0.78f, 0.84f, 0.92f, 0.9f)
    };
    footerHint.AddThemeFontSizeOverride("font_size", 14);
    footer.AddChild(footerHint);

    Button okButton = new()
    {
      Name = "ConfirmButton",
      Text = "知道了",
      SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
      CustomMinimumSize = new Vector2(160, 46)
    };
    okButton.AddThemeFontSizeOverride("font_size", 18);

    okButton.Pressed += () =>
    {
      MoreCustomsConfig.MarkUpdateNoticeSeen(version);
      canvasLayer.QueueFree();
      MainFile.Logger.Info($"[UpdateNotice] Version {version} marked as seen.");
    };

    footer.AddChild(okButton);
    MainFile.Logger.Info("[UpdateNotice] BuildPopup complete.");

    return canvasLayer;
  }

  private static string BuildBodyText()
  {
    return string.Join("\n", UpdateLines);
  }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class UpdateNoticeGameReadyPatch
{
  [HarmonyPostfix]
  private static void ShowUpdateNoticeAfterGameReady(NGame __instance)
  {
    UpdateNoticePopupPatch.TryShowUpdateNotice(__instance, "NGame._Ready(startup)");
  }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
public static class UpdateNoticeMainMenuReadyPatch
{
  [HarmonyPostfix]
  private static void ShowUpdateNoticeAfterMainMenuReady(NMainMenu __instance)
  {
    UpdateNoticePopupPatch.TryShowUpdateNotice(__instance, "NMainMenu._Ready(startup)");
  }
}
