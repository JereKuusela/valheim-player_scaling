using System;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Service;

namespace PlayerScaling;
[HarmonyPatch]
[BepInPlugin(GUID, NAME, VERSION)]
public class PlayerScaling : BaseUnityPlugin
{
  const string GUID = "player_scaling";
  const string NAME = "Player Scaling";
  const string VERSION = "1.14";
  public static ServerSync.ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    IsLocked = true,
    ModRequired = true
  };
  public void Awake()
  {
    new Harmony(GUID).PatchAll();
  }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
public class PlayerScalable
{
  static readonly int Hash = "Player".GetStableHashCode();
  [HarmonyPriority(Priority.Last)]
  static void Postfix(ZNetScene __instance)
  {
    if (__instance.m_namedPrefabs.TryGetValue(Hash, out var player))
      player.GetComponent<ZNetView>().m_syncInitialScale = true;
  }
}
[HarmonyPatch(typeof(Player))]
public class PlayerScale
{
  public static void SetOffset(Player obj, float offset)
  {
    var tr = obj.transform.Find("Visual");
    if (tr) tr.localPosition = new(0f, offset, 0f);
  }
  static readonly int OffsetHash = "player_offset".GetStableHashCode();
  [HarmonyPatch(nameof(Player.Awake)), HarmonyPostfix]
  static void LoadOffset(Player __instance)
  {
    if (!__instance.m_nview) return;
    if (!__instance.m_nview.IsValid()) return;
    var offset = __instance.m_nview.GetZDO().GetFloat(OffsetHash, 0f);
    if (offset != 0f) SetOffset(__instance, offset);
  }
  [HarmonyPatch(nameof(Player.OnSpawned)), HarmonyPostfix]
  static void InitScale(Player __instance)
  {
    if (__instance != Player.m_localPlayer) return;
    var hash = "scale_" + WorldGenerator.instance.m_world.m_uid + "=";
    var data = __instance.m_uniques;
    var previous = data.FirstOrDefault(str => str.StartsWith(hash, StringComparison.OrdinalIgnoreCase));
    if (string.IsNullOrEmpty(previous)) return;
    var split = previous.Split('=');
    if (split.Length < 2) return;
    __instance.m_nview.SetLocalScale(Helper.Scale(split[1].Split(',')));
    var offset = Helper.Float(split, 2);
    __instance.m_nview.GetZDO().Set(OffsetHash, offset);
    if (offset != 0f) SetOffset(__instance, offset);
  }
}
[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
public class SetCommands
{
  static void Postfix()
  {
    new ScalePlayerCommand();
  }
}
