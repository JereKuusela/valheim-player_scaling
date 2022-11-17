using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Service;

public class Helper
{
  public static void AddMessage(Terminal context, string message, bool priority = true)
  {
    context.AddString(message);
    var hud = MessageHud.instance;
    if (!hud) return;
    if (priority)
    {
      var items = hud.m_msgQeue.ToArray();
      hud.m_msgQeue.Clear();
      Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, message);
      foreach (var item in items)
        hud.m_msgQeue.Enqueue(item);
      hud.m_msgQueueTimer = 10f;
    }
    else
    {
      Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, message);
    }
  }

  public static void Command(string name, string description, Terminal.ConsoleEvent action, Terminal.ConsoleOptionsFetcher? fetcher = null)
  {
    new Terminal.ConsoleCommand(name, description, Helper.Catch(action), optionsFetcher: fetcher);
  }
  public static void AddError(Terminal context, string message, bool priority = true)
  {
    AddMessage(context, $"Error: {message}", priority);
  }
  public static Terminal.ConsoleEvent Catch(Terminal.ConsoleEvent action) =>
    (args) =>
    {
      try
      {
        if (!Player.m_localPlayer) throw new InvalidOperationException("Player not found.");
        action(args);
      }
      catch (InvalidOperationException e)
      {
        Helper.AddError(args.Context, e.Message);
      }
    };
  public static string PrintVectorXZY(Vector3 vector) => vector.x.ToString(CultureInfo.InvariantCulture) + "," + vector.z.ToString(CultureInfo.InvariantCulture) + "," + vector.y.ToString(CultureInfo.InvariantCulture);

  public static ZNet.PlayerInfo FindPlayer(string name)
  {
    var players = ZNet.instance.m_players;
    var player = players.FirstOrDefault(player => player.m_name == name);
    if (!player.m_characterID.IsNone()) return player;
    player = players.FirstOrDefault(player => player.m_name.ToLower().StartsWith(name.ToLower()));
    if (!player.m_characterID.IsNone()) return player;
    player = players.FirstOrDefault(player => player.m_name.ToLower().Contains(name.ToLower()));
    if (!player.m_characterID.IsNone()) return player;
    throw new InvalidOperationException("Unable to find the player.");
  }

  public static float Float(string arg, float defaultValue = 0f)
  {
    if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
      return defaultValue;
    return result;
  }
  public static float Float(string[] args, int index, float defaultValue = 0f)
  {
    if (args.Length <= index) return defaultValue;
    return Float(args[index], defaultValue);
  }
  public static Vector3 VectorXZY(string[] args)
  {
    var vector = Vector3.zero;
    vector.x = Float(args, 0);
    vector.z = Float(args, 1);
    vector.y = Float(args, 2);
    return vector;
  }
  public static Vector3 Scale(string[] args) => SanityCheck(VectorXZY(args));
  private static Vector3 SanityCheck(Vector3 scale)
  {
    // Sanity check and also adds support for setting all values with a single number.
    if (scale.x == 0) scale.x = 1;
    if (scale.y == 0) scale.y = scale.x;
    if (scale.z == 0) scale.z = scale.x;
    return scale;
  }
}