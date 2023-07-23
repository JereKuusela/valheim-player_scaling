using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Service;
using UnityEngine;

namespace PlayerScaling;

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
public class PlayerScaleRPC
{
  static void Postfix()
  {
    ZRoutedRpc.instance.Register<string, Vector3, float>("ScalePlayer", SetScale);
  }
  static void SetScale(long uid, string name, Vector3 scale, float offset)
  {
    var info = Helper.FindPlayer(name);
    // Special case for local player to allow Cron Job set the scale instantly when the player joins.
    // At that pont, ZDOMAN is not ready yet, so we can't find the player there.
    var localId = Player.m_localPlayer ? Player.m_localPlayer.GetZDOID() : ZDOID.None;
    if (localId == info.m_characterID && Player.m_localPlayer)
    {
      SetScale(Player.m_localPlayer, scale);
      PlayerScale.SetOffset(Player.m_localPlayer, offset);
      SaveScale(scale, offset);
    }
    else
    {
      if (!ZDOMan.instance.m_objectsByID.TryGetValue(info.m_characterID, out var zdo)) return;
      if (!ZNetScene.instance.m_instances.TryGetValue(zdo, out var view)) return;
      if (!view.TryGetComponent<Player>(out var player)) return;
      SetScale(player, scale);
      PlayerScale.SetOffset(player, offset);
    }
  }
  static void SetScale(Player player, Vector3 scale)
  {
    player.m_nview.SetLocalScale(scale);
    if (player.m_visEquipment)
    {
      player.m_visEquipment.m_currentRightItemHash = 0;
      player.m_visEquipment.m_currentLeftItemHash = 0;
      player.m_visEquipment.m_currentRightBackItemHash = 0;
      player.m_visEquipment.m_currentLeftBackItemHash = 0;
      player.m_visEquipment.m_currentShoulderItemHash = 0;
      player.m_visEquipment.UpdateVisuals();
    }
  }
  static void SaveScale(Vector3 scale, float offset)
  {
    var hash = "scale_" + WorldGenerator.instance.m_world.m_uid + "=";
    var data = Player.m_localPlayer.m_uniques;
    var previous = data.FirstOrDefault(str => str.StartsWith(hash, StringComparison.OrdinalIgnoreCase));
    if (!string.IsNullOrEmpty(previous)) data.Remove(previous);
    var item = hash + Helper.PrintVectorXZY(scale) + "=" + offset;
    Player.m_localPlayer.AddUniqueKey(item);
  }
}
public class ScalePlayerCommand
{
  public static List<string> PlayerNames()
  {
    if (ZNet.instance)
      return ZNet.instance.m_players.Select(player => player.m_name.Replace(" ", "_")).ToList();
    return new();
  }
  public ScalePlayerCommand()
  {
    Helper.Command("scale_player", "[player] [scale or x,y,z] [offset from ground] - Sets player scale.", (args) =>
    {
      if (!ZNet.instance || (!PlayerScaling.ConfigSync.IsAdmin && !ZNet.instance.IsServer())) throw new InvalidOperationException("Unauthorized to use this command.");
      if (args.Length < 2) throw new InvalidOperationException("Missing player name.");
      if (args.Length < 3) throw new InvalidOperationException("Missing the scale");
      Helper.FindPlayer(args[1]);
      var scale = Helper.Scale(args[2].Split(','));
      var offset = Helper.Float(args.Args, 3, 0f);
      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ScalePlayer", args[1], scale, offset);
    }, PlayerNames);
  }
}
