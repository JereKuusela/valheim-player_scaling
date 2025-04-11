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
    new Terminal.ConsoleCommand(name, description, Catch(action), optionsFetcher: fetcher);
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
        action(args);
      }
      catch (InvalidOperationException e)
      {
        AddError(args.Context, e.Message);
      }
    };
  public static string PrintVectorXZY(Vector3 vector) => vector.x.ToString(CultureInfo.InvariantCulture) + "," + vector.z.ToString(CultureInfo.InvariantCulture) + "," + vector.y.ToString(CultureInfo.InvariantCulture);

  public static ZNet.PlayerInfo FindPlayer(string name)
  {
    // Some servers could have hundred players so shouldn't make the code too slow.
    var lower = name.ToLower().Replace(" ", "_");
    var matches = ZNet.instance.m_players.OrderBy(player =>
    {
      var pName = player.m_name.Replace(" ", "_");
      if (pName == name || player.m_userInfo.m_id.m_userID.ToLower() == lower || player.m_characterID.UserID.ToString() == name) return 0;
      var pLower = pName.ToLower();
      if (pLower == lower) return 0;
      if (pLower.StartsWith(lower)) return 1;
      if (pLower.Contains(lower)) return 2;
      return 3;
    });
    // Order by won't filter out the results, so the match must be verified.
    var match = matches.FirstOrDefault();
    if (match.m_name.ToLower().Contains(name.ToLower()))
      return match;
    throw new InvalidOperationException($"Unable to find the player {name}.");
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