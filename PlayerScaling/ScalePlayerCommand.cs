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
    ZRoutedRpc.instance.Register<ZDOID, Vector3, float>("ScalePlayer", SetScale);
    // Somewhat duplicated code but works for now.
    ZRoutedRpc.instance.Register<ZDOID, float>("OffsetPlayer", SetOffSet);
  }
  static void SetScale(long uid, ZDOID id, Vector3 scale, float offset)
  {
    // Special case for local player to allow Cron Job set the scale instantly when the player joins.
    // At that pont, ZDOMAN is not ready yet, so we can't find the player there.
    var localId = Player.m_localPlayer ? Player.m_localPlayer.GetZDOID() : ZDOID.None;
    if (localId == id && Player.m_localPlayer)
    {
      SetScale(Player.m_localPlayer, scale);
      PlayerScale.SetOffset(Player.m_localPlayer, offset);
      SaveScale(scale, offset);
    }
    else
    {
      if (!ZDOMan.instance.m_objectsByID.TryGetValue(id, out var zdo)) return;
      if (!ZNetScene.instance.m_instances.TryGetValue(zdo, out var view)) return;
      if (!view.TryGetComponent<Player>(out var player)) return;
      SetScale(player, scale);
      PlayerScale.SetOffset(player, offset);
    }
  }
  static void SetOffSet(long uid, ZDOID id, float offset)
  {
    // Special case for local player to allow Cron Job set the scale instantly when the player joins.
    // At that pont, ZDOMAN is not ready yet, so we can't find the player there.
    var localId = Player.m_localPlayer ? Player.m_localPlayer.GetZDOID() : ZDOID.None;
    if (localId == id && Player.m_localPlayer)
    {
      var scale = Player.m_localPlayer.transform.localScale;
      PlayerScale.SetOffset(Player.m_localPlayer, offset);
      SaveScale(scale, offset);
    }
    else
    {
      if (!ZDOMan.instance.m_objectsByID.TryGetValue(id, out var zdo)) return;
      if (!ZNetScene.instance.m_instances.TryGetValue(zdo, out var view)) return;
      if (!view.TryGetComponent<Player>(out var player)) return;
      PlayerScale.SetOffset(player, offset);
    }
  }

  public static void SetScale(Player player, Vector3 scale)
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
  public static void SaveScale(Vector3 scale, float offset)
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
    Helper.Command("scale_self", "[scale or x,y,z] [offset from ground] - Sets own scale.", (args) =>
    {
      if (!ZNet.instance || !Player.m_localPlayer || !Player.m_debugMode) throw new InvalidOperationException("Unauthorized to use this command.");
      if (args.Length < 2) throw new InvalidOperationException("Missing the scale");
      var split = args[1].Split(',');
      var scale = split.Length > 2 ? Helper.Scale(split) : Helper.Float(split[0], 1f) * Vector3.one;
      var offset = args.Args.Length > 2 ? Helper.Float(args.Args, 2) : split.Length > 2 ? Helper.Float(split, 3) : Helper.Float(split, 1);
      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ScalePlayer", Player.m_localPlayer.GetZDOID(), scale, offset);
    });
    Helper.Command("scale_player", "[player] [scale or x,y,z] [offset from ground] - Sets player scale.", (args) =>
    {
      if (!ZNet.instance || (!PlayerScaling.ConfigSync.IsAdmin && !ZNet.instance.IsServer())) throw new InvalidOperationException("Unauthorized to use this command.");
      if (args.Length < 2) throw new InvalidOperationException("Missing player name.");
      if (args.Length < 3) throw new InvalidOperationException("Missing the scale");
      var info = Helper.FindPlayer(args[1]);
      var split = args[2].Split(',');
      var scale = split.Length > 2 ? Helper.Scale(split) : Helper.Float(split[0], 1f) * Vector3.one;
      var offset = args.Args.Length > 3 ? Helper.Float(args.Args, 3) : split.Length > 2 ? Helper.Float(split, 3) : Helper.Float(split, 1);
      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ScalePlayer", info.m_characterID, scale, offset);
    }, PlayerNames);
    Helper.Command("offset_self", "[offset from ground] - Sets own offset.", (args) =>
    {
      if (!ZNet.instance || !Player.m_localPlayer || !Player.m_debugMode) throw new InvalidOperationException("Unauthorized to use this command.");
      if (args.Length < 2) throw new InvalidOperationException("Missing the offset");
      var offset = Helper.Float(args.Args, 1);
      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "OffsetPlayer", Player.m_localPlayer.GetZDOID(), offset);
    });
    Helper.Command("offset_player", "[player] [offset from ground] - Sets player offset.", (args) =>
    {
      if (!ZNet.instance || (!PlayerScaling.ConfigSync.IsAdmin && !ZNet.instance.IsServer())) throw new InvalidOperationException("Unauthorized to use this command.");
      if (args.Length < 2) throw new InvalidOperationException("Missing player name.");
      if (args.Length < 3) throw new InvalidOperationException("Missing the offset");
      var info = Helper.FindPlayer(args[1]);
      var offset = Helper.Float(args.Args, 2);
      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "OffsetPlayer", info.m_characterID, offset);
    }, PlayerNames);
  }
}
