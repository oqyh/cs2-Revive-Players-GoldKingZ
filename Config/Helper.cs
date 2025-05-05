using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.RegularExpressions;
using Revive_Players.Config;
using System.Drawing;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using System.Security.Cryptography;
using CounterStrikeSharp.API.Modules.Commands;
using System.Net.Http.Headers;
using CounterStrikeSharp.API.Modules.Memory;

namespace Revive_Players;

public class Helper
{
    public static void AdvancedPlayerPrintToChat(CCSPlayerController player, CommandInfo commandInfo, string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message)) return;

        for (int i = 0; i < args.Length; i++)
        {
            message = message.Replace($"{{{i}}}", args[i]?.ToString() ?? "");
        }

        if (Regex.IsMatch(message, "{nextline}", RegexOptions.IgnoreCase))
        {
            string[] parts = Regex.Split(message, "{nextline}", RegexOptions.IgnoreCase);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                trimmedPart = trimmedPart.ReplaceColorTags();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    if (commandInfo != null && commandInfo.CallingContext == CommandCallingContext.Console)
                    {
                        player.PrintToConsole(" " + trimmedPart);
                    }
                    else
                    {
                        player.PrintToChat(" " + trimmedPart);
                    }
                }
            }
        }
        else
        {
            message = message.ReplaceColorTags();
            if (commandInfo != null && commandInfo.CallingContext == CommandCallingContext.Console)
            {
                player.PrintToConsole(message);
            }
            else
            {
                player.PrintToChat(message);
            }
        }
    }

    public static void AdvancedPlayerPrintToConsole(CCSPlayerController player, string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message)) return;

        for (int i = 0; i < args.Length; i++)
        {
            message = message.Replace($"{{{i}}}", args[i].ToString());
        }
        if (Regex.IsMatch(message, "{nextline}", RegexOptions.IgnoreCase))
        {
            string[] parts = Regex.Split(message, "{nextline}", RegexOptions.IgnoreCase);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                trimmedPart = trimmedPart.ReplaceColorTags();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    player.PrintToConsole(" " + trimmedPart);
                }
            }
        }
        else
        {
            message = message.ReplaceColorTags();
            player.PrintToConsole(message);
        }
    }
    public static void AdvancedServerPrintToChatAll(string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message)) return;

        for (int i = 0; i < args.Length; i++)
        {
            message = message.Replace($"{{{i}}}", args[i].ToString());
        }
        if (Regex.IsMatch(message, "{nextline}", RegexOptions.IgnoreCase))
        {
            string[] parts = Regex.Split(message, "{nextline}", RegexOptions.IgnoreCase);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                trimmedPart = trimmedPart.ReplaceColorTags();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    Server.PrintToChatAll(" " + trimmedPart);
                }
            }
        }
        else
        {
            message = message.ReplaceColorTags();
            Server.PrintToChatAll(message);
        }
    }
    public static List<CCSPlayerController> GetPlayersController(bool IncludeBots = false, bool IncludeHLTV = false, bool IncludeNone = true, bool IncludeSPEC = true, bool IncludeCT = true, bool IncludeT = true) 
    {
        return Utilities
            .FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller")
            .Where(p => 
                p != null && 
                p.IsValid &&
                p.Connected == PlayerConnectedState.PlayerConnected &&
                (IncludeBots || !p.IsBot) &&
                (IncludeHLTV || !p.IsHLTV) &&
                ((IncludeCT && p.TeamNum == (byte)CsTeam.CounterTerrorist) || 
                (IncludeT && p.TeamNum == (byte)CsTeam.Terrorist) || 
                (IncludeNone && p.TeamNum == (byte)CsTeam.None) || 
                (IncludeSPEC && p.TeamNum == (byte)CsTeam.Spectator)))
            .ToList();
    }
    public static int GetPlayersCount(bool IncludeBots = false, bool IncludeHLTV = false, bool IncludeSPEC = true, bool IncludeCT = true, bool IncludeT = true)
    {
        return Utilities.GetPlayers().Count(p => 
            p != null && 
            p.IsValid && 
            p.Connected == PlayerConnectedState.PlayerConnected && 
            (IncludeBots || !p.IsBot) &&
            (IncludeHLTV || !p.IsHLTV) &&
            ((IncludeCT && p.TeamNum == (byte)CsTeam.CounterTerrorist) || 
            (IncludeT && p.TeamNum == (byte)CsTeam.Terrorist) || 
            (IncludeSPEC && p.TeamNum == (byte)CsTeam.Spectator))
        );
    }

    public static bool IsPlayerInGroupPermission(CCSPlayerController player, string groups)
    {
        if (string.IsNullOrEmpty(groups) || player == null || !player.IsValid)
            return false;

        return groups.Split('|')
            .Select(segment => segment.Trim())
            .Any(trimmedSegment => Permission_CheckPermissionSegment(player, trimmedSegment));
    }

    private static bool Permission_CheckPermissionSegment(CCSPlayerController player, string segment)
    {
        if (string.IsNullOrEmpty(segment))return false;

        int colonIndex = segment.IndexOf(':');
        if (colonIndex <= 0)return AdminManager.PlayerInGroup(player, segment);

        string prefix = segment.Substring(0, colonIndex).Trim().ToLower();
        string values = segment.Substring(colonIndex + 1).Trim();

        return prefix switch
        {
            "steamid" or "steamids" or "steam" or "steams" => Permission_CheckSteamIds(player, values),
            "flag" or "flags" => Permission_CheckFlags(player, values),
            "group" or "groups" => Permission_CheckGroups(player, values),
            _ => AdminManager.PlayerInGroup(player, segment)
        };
    }

    private static bool Permission_CheckSteamIds(CCSPlayerController player, string steamIds)
    {
        if (player.AuthorizedSteamID == null)return false;

        return steamIds.Split(',')
            .Select(id => id.Trim())
            .Any(trimmedId => Permission_IsMatchingSteamId(player.AuthorizedSteamID, trimmedId));
    }

    private static bool Permission_IsMatchingSteamId(CounterStrikeSharp.API.Modules.Entities.SteamID authId, string inputId)
    {
        return inputId.Equals(authId.SteamId2.ToString(), StringComparison.OrdinalIgnoreCase) ||
               inputId.Equals(authId.SteamId3.ToString().Trim('[', ']'), StringComparison.OrdinalIgnoreCase) ||
               inputId.Equals(authId.SteamId3.ToString(), StringComparison.OrdinalIgnoreCase) ||
               inputId.Equals(authId.SteamId32.ToString(), StringComparison.OrdinalIgnoreCase) ||
               inputId.Equals(authId.SteamId64.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Permission_CheckFlags(CCSPlayerController player, string flags)
    {
        return flags.Split(',')
            .Select(flag => flag.Trim())
            .Any(trimmedFlag => AdminManager.PlayerHasPermissions(player, trimmedFlag));
    }

    private static bool Permission_CheckGroups(CCSPlayerController player, string groups)
    {
        return groups.Split(',')
            .Select(group => group.Trim())
            .Any(trimmedGroup => AdminManager.PlayerInGroup(player, trimmedGroup));
    }
    public static void ClearVariables()
    {
        var g_Main = MainPlugin.Instance.g_Main;

        g_Main.Disable_Revive = false;
        g_Main.Counter = 0;
        g_Main.Revive_Limit_CT = 0;
        g_Main.Revive_Limit_T = 0;

        g_Main.Player_Data.Clear();
        g_Main.DeadPlayer_Data.Clear();
    }

    public static void DebugMessage(string message, bool prefix = true)
    {
        if (!Configs.GetConfigData().EnableDebug) return;

        Console.ForegroundColor = ConsoleColor.Magenta;
        string output = prefix ? $"[Revive Players]: {message}" : message;
        Console.WriteLine(output);
        
        Console.ResetColor();
    }

    public static CCSGameRules? GetGameRules()
    {
        try
        {
            var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            return gameRulesEntities.First().GameRules;
        }
        catch
        {
            return null;
        }
    }
    public static bool IsWarmup()
    {
        return GetGameRules()?.WarmupPeriod ?? false;
    }

    public static int GetCurrentRound()
    {
        var gameRules = GetGameRules();
        if (gameRules == null)
        {
            return -1;
        }

        int rounds = IsWarmup() ? gameRules.TotalRoundsPlayed : gameRules.TotalRoundsPlayed + 1;

        return rounds;
    }

    public static void CreateResource(string jsonFilePath)
    {
        string headerLine = "////// vvvvvv Add Paths For Precache Resources Down vvvvvvvvvv //////";
        string headerLine2 = "// models/goldkingz/animation/goldkingz_animation.vmdl";
        string headerLine3 = "// models/goldkingz/animation2/animation2.vmdl";
        if (!File.Exists(jsonFilePath))
        {
            using (StreamWriter sw = File.CreateText(jsonFilePath))
            {
                sw.WriteLine(headerLine);
                sw.WriteLine(headerLine2);
                sw.WriteLine(headerLine3);
            }
        }
        else
        {
            string[] lines = File.ReadAllLines(jsonFilePath);
            if (lines.Length == 0 || lines[0] != headerLine)
            {
                using (StreamWriter sw = new StreamWriter(jsonFilePath))
                {
                    sw.WriteLine(headerLine);
                    foreach (string line in lines)
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }
    }

    public static void SetPlayerVisible(CCSPlayerController player)
    {
        if(!player.IsValid(true))return;

        var playerPawnValue = player.PlayerPawn.Value;
        if (playerPawnValue == null || !playerPawnValue.IsValid)return;

        if (playerPawnValue != null && playerPawnValue.IsValid)
        {
            playerPawnValue.Render = Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(playerPawnValue, "CBaseModelEntity", "m_clrRender");
        }

        var activeWeapon = playerPawnValue!.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon != null && activeWeapon.IsValid)
        {
            activeWeapon.Render = Color.FromArgb(255, 255, 255, 255);
            activeWeapon.ShadowStrength = -1.0f;
            Utilities.SetStateChanged(activeWeapon, "CBaseModelEntity", "m_clrRender");
            Utilities.SetStateChanged(activeWeapon, "CBaseModelEntity", "m_flShadowStrength");
        }

        if(playerPawnValue.WeaponServices == null)return;
        
        var myWeapons = playerPawnValue.WeaponServices.MyWeapons;
        if (myWeapons != null)
        {
            foreach (var gun in myWeapons)
            {
                
                var weapon = gun.Value;
                if (weapon != null)
                {
                    weapon.Render = Color.FromArgb(255, 255, 255, 255);
                    weapon.ShadowStrength = -1.0f;
                    Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
                    Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_flShadowStrength");
                }
            }
        }
    }
    public static void SetPlayerInvisible(CCSPlayerController player)
    {
        if(!player.IsValid(true))return;

        var playerPawnValue = player.PlayerPawn.Value;
        if (playerPawnValue == null || !playerPawnValue.IsValid)return;

        if (playerPawnValue != null && playerPawnValue.IsValid)
        {
            playerPawnValue.Render = Color.FromArgb(0, 255, 255, 255);
            Utilities.SetStateChanged(playerPawnValue, "CBaseModelEntity", "m_clrRender");
        }

        var activeWeapon = playerPawnValue!.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon != null && activeWeapon.IsValid)
        {
            activeWeapon.Render = Color.FromArgb(0, 255, 255, 255);
            activeWeapon.ShadowStrength = 0.0f;
            Utilities.SetStateChanged(activeWeapon, "CBaseModelEntity", "m_clrRender");
        }

        if(playerPawnValue.WeaponServices == null)return;

        var myWeapons = playerPawnValue.WeaponServices.MyWeapons;
        if (myWeapons != null)
        {
            foreach (var gun in myWeapons)
            {
                
                var weapon = gun.Value;
                if (weapon != null)
                {
                    weapon.Render = Color.FromArgb(0, 255, 255, 255);
                    weapon.ShadowStrength = 0.0f;
                    Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
                }
            }
        }
    }

    public static float DistanceTo(Vector a, Vector b)
    {
        return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }

    public static void CheckPlayerInGlobals(CCSPlayerController player)
    {
        if(!player.IsValid(true))return;

        if (!MainPlugin.Instance.g_Main.Player_Data.ContainsKey(player))
        {
            MainPlugin.Instance.g_Main.Player_Data.Add(player, new Globals.PlayerDataClass(player, null!, null!, null!, null!, false, false, false, 0, 0, DateTime.Now, DateTime.Now));
        }

        if (!MainPlugin.Instance.g_Main.DeadPlayer_Data.ContainsKey(player))
        {
            MainPlugin.Instance.g_Main.DeadPlayer_Data.Add(player, new Globals.DeadPlayerDataClass(player, player.TeamNum, new List<CBeam>(), null!, new List<CBeam>(), null!, DateTime.Now));
        }
    }

    public static bool CanPlayerRevive(CCSPlayerController player, Globals.PlayerDataClass playerData, bool mute = false)
    {
        var g_Main = MainPlugin.Instance.g_Main;

        if (!string.IsNullOrEmpty(Configs.GetConfigData().Revive_Flags) && !IsPlayerInGroupPermission(player, Configs.GetConfigData().Revive_Flags))
        {
            if(!mute)AdvancedPlayerPrintToChat(player, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revive.Not.Allowed.Flag"]);
            return false;
        }

        if (Configs.GetConfigData().Revive_Limit > 0)
        {
            if (Configs.GetConfigData().Revive_Limit_Mode == 1)
            {
                if (player.TeamNum == (byte)CsTeam.CounterTerrorist && g_Main.Revive_Limit_CT >= Configs.GetConfigData().Revive_Limit)
                {
                    if(!mute)AdvancedPlayerPrintToChat(player, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revive.Not.Allowed.Limit"], Configs.GetConfigData().Revive_Limit - g_Main.Revive_Limit_CT);
                    return false;
                }
                
                if (player.TeamNum == (byte)CsTeam.Terrorist && g_Main.Revive_Limit_T >= Configs.GetConfigData().Revive_Limit)
                {
                    if(!mute)AdvancedPlayerPrintToChat(player, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revive.Not.Allowed.Limit"], Configs.GetConfigData().Revive_Limit - g_Main.Revive_Limit_T);
                    return false;
                }
            }
            else if (Configs.GetConfigData().Revive_Limit_Mode == 2 && playerData.Revive_Limit >= Configs.GetConfigData().Revive_Limit)
            {
                if(!mute)AdvancedPlayerPrintToChat(player, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revive.Not.Allowed.Limit"], Configs.GetConfigData().Revive_Limit - playerData.Revive_Limit);
                return false;
            }
        }

        if (Configs.GetConfigData().Revive_Cost > 0)
        {
            if (Configs.GetConfigData().Revive_Cost_Mode == 1 && player.InGameMoneyServices?.Account < Configs.GetConfigData().Revive_Cost)
            {
                if(!mute)AdvancedPlayerPrintToChat(player, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revive.Not.Allowed.Money"], Configs.GetConfigData().Revive_Cost);
                return false;
            }
            else if (Configs.GetConfigData().Revive_Cost_Mode == 2 && player.Pawn.Value?.Health <= Configs.GetConfigData().Revive_Cost)
            {
                if(!mute)AdvancedPlayerPrintToChat(player, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revive.Not.Allowed.Health"], Configs.GetConfigData().Revive_Cost);
                return false;
            }
        }

        if(Configs.GetConfigData().Revive_CoolDown_After_Revive > 0 && !string.IsNullOrEmpty(Configs.GetConfigData().Revive_Immunity_From_Cooldown_Flags) && !IsPlayerInGroupPermission(player, Configs.GetConfigData().Revive_Immunity_From_Cooldown_Flags))
        {
            if (DateTime.Now < playerData.CoolDown)
            {
                var remainingCooldown = (int)(playerData.CoolDown - DateTime.Now).TotalSeconds;
                if(!mute)AdvancedPlayerPrintToChat(player, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revive.While.OnCooldown"], remainingCooldown);
                return false;
            }
        }
        
        return true;
    }

    public static void UpdateVerticalLineBeam(Globals.DeadPlayerDataClass data)
    {
        if (data == null || !data.Dead_Player.IsValid(true) || data.Dead_Player.IsAlive() || data.ArrowBeam.Count == 0 || data.LineBeam == null || !data.LineBeam.IsValid)return;

        float t      = (float)(DateTime.Now - data.LineBeam_Time).TotalSeconds;
        float offset = MathF.Sin(t * 4f) * 70f;

        Vector basePos = new Vector(
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.X,
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.Y,
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.Z + Configs.GetConfigData().DeadBody_MoveArrow_From_Ground_By + offset
        );
        Vector tip = new Vector(
            basePos.X,
            basePos.Y,
            basePos.Z + 50
        );

        data.LineBeam.Teleport(basePos);
        data.LineBeam.EndPos.X = tip.X;
        data.LineBeam.EndPos.Y = tip.Y;
        data.LineBeam.EndPos.Z = tip.Z;
        Utilities.SetStateChanged(data.LineBeam, "CBeam", "m_vecEndPos");

        const float wingSpread = 20f;
        const float wingDrop   = 20f;
        var dirs = new[] {
            new Vector( wingSpread, 0, +wingDrop),
            new Vector(-wingSpread, 0, +wingDrop)
        };

        for (int i = 0; i < data.ArrowBeam.Count; i++)
        {
            var wing = data.ArrowBeam[i];
            if (wing == null || !wing.IsValid) continue;

            var d = dirs[i];
            wing.Teleport(basePos);
            wing.EndPos.X = basePos.X + d.X;
            wing.EndPos.Y = basePos.Y + d.Y;
            wing.EndPos.Z = basePos.Z + d.Z;
            Utilities.SetStateChanged(wing, "CBeam", "m_vecEndPos");
        }
    }

    public static void UpdateWorldText(Globals.DeadPlayerDataClass data)
    {
        if (data == null || !data.Dead_Player.IsValid(true) || data.Dead_Player.IsAlive() || data.PointWorldTextPoint == null || !data.PointWorldTextPoint.IsValid)return;

        float zOffset = Configs.GetConfigData().DeadBody_MovePlayerNameText_From_Ground_By;
        var newPos = new Vector(
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.X,
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.Y,
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.Z + zOffset
        );
        
        var currentAngles = new QAngle(0, 90, 90);
        data.PointWorldTextPoint.Teleport(newPos, currentAngles);
    }

    public static void UpdateReviveCircle(Globals.DeadPlayerDataClass data)
    {
        if (data == null || !data.Dead_Player.IsValid(true) || data.Dead_Player.IsAlive() || data.CircleBeam.Count == 0)return;


        float radius  = Configs.GetConfigData().Revive_Distance;
        float zOffset = 10.0f;
        Vector center = new Vector(
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.X,
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.Y,
            data.Dead_Player.PlayerPawn?.Value?.AbsOrigin?.Z + zOffset
        );

        int segmentCount = data.CircleBeam.Count;
        double deltaAng  = 2 * Math.PI / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            var beam = data.CircleBeam[i];
            if (beam == null || !beam.IsValid)continue;

            double a1 = i * deltaAng;
            double a2 = (i + 1) * deltaAng;

            Vector start = new Vector(
                center.X + (float)(radius * Math.Cos(a1)),
                center.Y + (float)(radius * Math.Sin(a1)),
                center.Z
            );

            Vector end = new Vector(
                center.X + (float)(radius * Math.Cos(a2)),
                center.Y + (float)(radius * Math.Sin(a2)),
                center.Z
            );

            beam.Teleport(start);
            beam.EndPos.X = end.X;
            beam.EndPos.Y = end.Y;
            beam.EndPos.Z = end.Z;
            Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
        }
    }


    public static (CCSPlayerController? Target, float Distance) FindReviveTarget(CCSPlayerController player)
    {
        var g_Main = MainPlugin.Instance.g_Main;
        int searchRadius = Configs.GetConfigData().Revive_Distance;

        if (!player.IsValid(true) || !player.IsAlive())return (null, float.MaxValue);

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || playerPawn.AbsOrigin == null)return (null, float.MaxValue);
        
        Vector playerPos = playerPawn.AbsOrigin;

        CCSPlayerController? validTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var deadEntry in g_Main.DeadPlayer_Data.Values)
        {
            if (deadEntry == null)continue;

            var deadplayer = deadEntry.Dead_Player;

            if (!deadplayer.IsValid(true) || deadplayer.IsAlive() || deadplayer == player || deadplayer.TeamNum != player.TeamNum)continue;

            var deadPawn = deadplayer.PlayerPawn.Value;
            if (deadPawn == null || deadPawn.AbsOrigin == null)continue;
            Vector deadPos = deadPawn.AbsOrigin;

            float distance = DistanceTo(playerPos, deadPos);

            if (distance <= searchRadius && distance < closestDistance)
            {
                validTarget = deadplayer;
                closestDistance = distance;
            }
        }

        return (validTarget, closestDistance);
    }

    public static void Revive_Complete(CCSPlayerController? playerreviver, CCSPlayerController? playerdead)
    {
        var g_Main = MainPlugin.Instance.g_Main;

        if(!playerreviver.IsValid(true) || !playerdead.IsValid(true) 
        || !g_Main.Player_Data.TryGetValue(playerreviver, out var dataplayer_reviver) || !g_Main.DeadPlayer_Data.TryGetValue(playerdead, out var dataplayer_dead)
        || dataplayer_reviver == null || dataplayer_dead == null)return;

        
        var position = playerdead.PlayerPawn.Value?.AbsOrigin;
        var rotation = playerdead.PlayerPawn.Value?.AbsRotation;
        var teleportpostion = new Vector(position?.X, position?.Y, position?.Z);
        var teleportrotation = new QAngle(rotation?.X, rotation?.Y, rotation?.Z);
        playerdead.Respawn();
        playerdead.PlayerPawn?.Value?.Teleport(teleportpostion, teleportrotation);
        dataplayer_reviver.TargetPlayer = null!;
        
        if (playerdead.Pawn.Value != null && playerdead.Pawn.Value.IsValid && playerdead.PlayerPawn?.Value != null && playerdead.PlayerPawn.Value.IsValid)
        {
            playerdead.Pawn.Value.Health = Configs.GetConfigData().Revive_Health;
            Utilities.SetStateChanged(playerdead.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
        }

        dataplayer_reviver.CoolDown = DateTime.Now.AddSeconds(Configs.GetConfigData().Revive_CoolDown_After_Revive);
        AdvancedPlayerPrintToChat(playerdead, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revived.By"], playerreviver.PlayerName);

        RemoveBeams(playerdead);

        

        if (Configs.GetConfigData().Revive_Limit > 0)
        {
            if (Configs.GetConfigData().Revive_Limit_Mode == 1)
            {
                if (playerreviver.TeamNum == (byte)CsTeam.CounterTerrorist)
                {
                    g_Main.Revive_Limit_CT ++;
                }
                
                if (playerreviver.TeamNum == (byte)CsTeam.Terrorist)
                {
                    g_Main.Revive_Limit_T ++;
                }
            }
            else if (Configs.GetConfigData().Revive_Limit_Mode == 2)
            {
                dataplayer_reviver.Revive_Limit ++;
            }
        }

        if (Configs.GetConfigData().Revive_Cost > 0)
        {
            if (Configs.GetConfigData().Revive_Cost_Mode == 1)
            {
                if (playerreviver.InGameMoneyServices != null)
                {
                    playerreviver.InGameMoneyServices.Account -= Configs.GetConfigData().Revive_Cost;
                    Utilities.SetStateChanged(playerreviver, "CCSPlayerController", "m_pInGameMoneyServices");
                }
            }
            else if (Configs.GetConfigData().Revive_Cost_Mode == 2)
            {
                if (playerreviver.Pawn.Value != null)
                {
                    playerreviver.Pawn.Value.Health -= Configs.GetConfigData().Revive_Cost;
                    Utilities.SetStateChanged(playerreviver.PlayerPawn.Value!, "CBaseEntity", "m_iHealth");
                }
            }
        }

        string limitedrev = Configs.GetConfigData().Revive_Limit > 0 ? MainPlugin.Instance.Localizer["PrintChatToPlayer.Revived.Limit", Configs.GetConfigData().Revive_Limit_Mode == 1 && playerreviver.TeamNum == (byte)CsTeam.CounterTerrorist?Configs.GetConfigData().Revive_Limit - g_Main.Revive_Limit_CT:Configs.GetConfigData().Revive_Limit_Mode == 1 && playerreviver.TeamNum == (byte)CsTeam.Terrorist?Configs.GetConfigData().Revive_Limit - g_Main.Revive_Limit_T:Configs.GetConfigData().Revive_Limit_Mode == 2?Configs.GetConfigData().Revive_Limit - dataplayer_reviver.Revive_Limit:""] : "";
        string showhealthandmoney = Configs.GetConfigData().Revive_Cost > 0 && Configs.GetConfigData().Revive_Cost_Mode == 1 ? MainPlugin.Instance.Localizer["PrintChatToPlayer.Revived.Money", Configs.GetConfigData().Revive_Cost]
        : Configs.GetConfigData().Revive_Cost > 0 && Configs.GetConfigData().Revive_Cost_Mode == 2 ? MainPlugin.Instance.Localizer["PrintChatToPlayer.Revived.Health", Configs.GetConfigData().Revive_Cost]
        : "";
        AdvancedPlayerPrintToChat(playerreviver, null!, MainPlugin.Instance.Localizer["PrintChatToPlayer.Revived.Completed"], playerdead.PlayerName, showhealthandmoney, limitedrev);
        AdvancedServerPrintToChatAll(MainPlugin.Instance.Localizer["PrintChatToAllPlayers.Revived.Completed"], playerreviver.PlayerName, playerdead.PlayerName, showhealthandmoney, limitedrev);
    
    }
    public static bool CheckHoldDuration(Globals.PlayerDataClass playerData)
    {
        return (DateTime.Now - playerData.HoldStartTime).TotalSeconds >= Configs.GetConfigData().Revive_Duration;
    }
    

    public static void CreateAnimtion(CCSPlayerController player)
    {
        if(string.IsNullOrEmpty(Configs.GetConfigData().Revive_ModelAnimation) || string.IsNullOrEmpty(Configs.GetConfigData().Revive_NameAnimation))return;

        var g_Main = MainPlugin.Instance.g_Main;
        if(!player.IsValid(true) || !g_Main.Player_Data.TryGetValue(player, out var playerhandle) || playerhandle.Animation != null && playerhandle.Animation.IsValid)return;
        
        string orginalmodel = player.PlayerPawn?.Value?.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName!;
        if(string.IsNullOrEmpty(orginalmodel))return;

        player.PlayerPawn?.Value?.SetModel("characters/models/ctm_sas/ctm_sas.vmdl");
        SetPlayerInvisible(player);
        player.PlayerPawn?.Value?.SetModel(orginalmodel);
        var AnimationEnt = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (AnimationEnt == null)return;

        AnimationEnt.DispatchSpawn();
        MainPlugin.Instance.g_Main.Counter++;
        string uniqueName = "Animation_" + MainPlugin.Instance.g_Main.Counter;
        if (AnimationEnt.Entity != null)AnimationEnt.Entity.Name = uniqueName;
        AnimationEnt.Spawnflags = 256u;
        AnimationEnt.RenderMode = RenderMode_t.kRenderNone;
        AnimationEnt.NoGhostCollision = true;
        AnimationEnt.Collision.CollisionGroup = 0;
        AnimationEnt.UseAnimGraph = false;
        AnimationEnt.SetModel(Configs.GetConfigData().Revive_ModelAnimation);
        AnimationEnt.Teleport(player.PlayerPawn?.Value?.AbsOrigin, player.PlayerPawn?.Value?.AbsRotation);

        var AnimationCloneEnt = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (AnimationCloneEnt == null)return;
        AnimationCloneEnt.DispatchSpawn();
        AnimationCloneEnt.SetModel(orginalmodel);
        AnimationCloneEnt.Teleport(player.PlayerPawn?.Value?.AbsOrigin, player.PlayerPawn?.Value?.AbsRotation);

        AnimationCloneEnt.AcceptInput("FollowEntity", AnimationCloneEnt , AnimationEnt , uniqueName);
        AnimationEnt.AcceptInput("SetAnimationNotLooping", null, null, Configs.GetConfigData().Revive_NameAnimation);

        playerhandle.Animation = AnimationEnt;
        playerhandle.Animation_Name = uniqueName;
        playerhandle.AnimationClone = AnimationCloneEnt;
        playerhandle.AnimationBool = true;
    }

    public static void RemoveAnimation(CCSPlayerController player)
    {
        if(string.IsNullOrEmpty(Configs.GetConfigData().Revive_ModelAnimation) || string.IsNullOrEmpty(Configs.GetConfigData().Revive_NameAnimation))return;
        var g_Main = MainPlugin.Instance.g_Main;

        if(!player.IsValid(true) || !g_Main.Player_Data.TryGetValue(player, out var playerhandle))return;

        if(playerhandle.Animation != null && playerhandle.Animation.IsValid)
        {
            playerhandle.Animation.Remove();
            playerhandle.AnimationBool = false;
        }

        if(playerhandle.AnimationClone != null && playerhandle.AnimationClone.IsValid)
        {
            playerhandle.AnimationClone.Remove();
            playerhandle.AnimationBool = false;
        }
    }

    public static void ResetPlayer(CCSPlayerController player)
    {
        if (!player.IsValid(true)) return;

        RemoveAnimation(player);
        PlayerUNFreeze(player);
        SetPlayerVisible(player);
    }

    public static void PlayerFreeze(CCSPlayerController player)
    {
        if (!Configs.GetConfigData().Revive_FreezeOnReviving || !player.IsValid(true)) return;

        var g_Main = MainPlugin.Instance.g_Main;
        if (!g_Main.Player_Data.TryGetValue(player, out var data)) return;

        if(Configs.GetConfigData().Revive_DontUnFreezeIfPlayerWasFreezed)
        {
            CheckPlayerMovment(player);
            if(data.Revive_Freeze != 2)return;
        }

        if(player.PlayerPawn.Value!.Flags != 65664 && player.PlayerPawn.Value!.Flags != 65672
        || player.Pawn.Value!.Flags != 65664 && player.Pawn.Value!.Flags != 65672)
        {
            player.Freeze();
        }
        
        
    }
    public static void CheckPlayerMovment(CCSPlayerController player)
    {
        if (!Configs.GetConfigData().Revive_FreezeOnReviving || !player.IsValid(true)) return;

        var g_Main = MainPlugin.Instance.g_Main;
        if (!g_Main.Player_Data.TryGetValue(player, out var data)) return;

        if(player.PlayerPawn?.Value?.MoveType == MoveType_t.MOVETYPE_NONE && player.PlayerPawn?.Value?.ActualMoveType == MoveType_t.MOVETYPE_NONE
        || player.Pawn?.Value?.MoveType == MoveType_t.MOVETYPE_NONE && player.Pawn?.Value?.ActualMoveType == MoveType_t.MOVETYPE_NONE)
        {
            if(data.Revive_Freeze == 2)return;
            
            data.Revive_Freeze = 1;
        }else
        {
            data.Revive_Freeze = 2;
        }
    }
    public static void PlayerUNFreeze(CCSPlayerController? player)
    {
        if (!Configs.GetConfigData().Revive_FreezeOnReviving || !player.IsValid(true)) return;

        var g_Main = MainPlugin.Instance.g_Main;
        if (!g_Main.Player_Data.TryGetValue(player, out var data)) return;

        if(Configs.GetConfigData().Revive_DontUnFreezeIfPlayerWasFreezed)
        {
            CheckPlayerMovment(player);
            if(data.Revive_Freeze == 1)return;
        }

        if(player.PlayerPawn.Value!.Flags != 65664 && player.PlayerPawn.Value!.Flags != 65672
        || player.Pawn.Value!.Flags != 65664 && player.Pawn.Value!.Flags != 65672)
        {
            player.Unfreeze();
        }
    }

    public static void CreatePointWorldTextPoint(CCSPlayerController player)
    {
        var g_Main = MainPlugin.Instance.g_Main;

        if(!Configs.GetConfigData().DeadBody_PlayerNameText || !player.IsValid(true) || !g_Main.DeadPlayer_Data.TryGetValue(player, out var data))return;

        var entity = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        if (entity == null) return;
        float last = Configs.GetConfigData().DeadBody_PlayerNameText_FontSize;
        entity.MessageText = player.PlayerName;
        entity.Enabled = true;
        entity.FontSize = last;
        entity.FontName = Configs.GetConfigData().DeadBody_PlayerNameText_FontName;
        entity.Color = Configs.GetConfigData().DeadBody_PlayerNameText_FontColor.ToColor();
        entity.Fullbright = true;
        if(Configs.GetConfigData().DeadBody_PlayerNameText_WorldUnitsPerPx > 0)
        {
            entity.WorldUnitsPerPx =  Configs.GetConfigData().DeadBody_PlayerNameText_WorldUnitsPerPx;
        }else
        {
            entity.WorldUnitsPerPx =  0.25f / 1050 * last;
        }
        entity.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
        entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
        entity.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_AROUND_UP;
        entity.DispatchSpawn();

        float zOffset = Configs.GetConfigData().DeadBody_MovePlayerNameText_From_Ground_By;
        Vector elevatedCenter = new Vector(player.PlayerPawn?.Value?.AbsOrigin?.X, player.PlayerPawn?.Value?.AbsOrigin?.Y, player.PlayerPawn?.Value?.AbsOrigin?.Z + zOffset);
        entity.Teleport(elevatedCenter, new QAngle(0, 90, 90));
        data.PointWorldTextPoint = entity;
    }

    public static void CreateReviveCircle(CCSPlayerController player)
    {
        if (!Configs.GetConfigData().DeadBody_CircleRadius || !player.IsValid(true)) return;

        float radius = Configs.GetConfigData().Revive_Distance;
        int pointCount = 100;

        float zOffset = Configs.GetConfigData().DeadBody_MoveCircleRadius_From_Ground_By;
        Vector elevatedCenter = new Vector(player.PlayerPawn?.Value?.AbsOrigin?.X, player.PlayerPawn?.Value?.AbsOrigin?.Y, player.PlayerPawn?.Value?.AbsOrigin?.Z + zOffset);

        for (int i = 0; i < pointCount; i++)
        {
            if (!player.IsValid(true)) return;

            double angle1 = i * (2 * Math.PI / pointCount);
            double angle2 = (i + 1) * (2 * Math.PI / pointCount);

            Vector start = new Vector(
                elevatedCenter.X + (float)(radius * Math.Cos(angle1)),
                elevatedCenter.Y + (float)(radius * Math.Sin(angle1)),
                elevatedCenter.Z
            );

            Vector end = new Vector(
                elevatedCenter.X + (float)(radius * Math.Cos(angle2)),
                elevatedCenter.Y + (float)(radius * Math.Sin(angle2)),
                elevatedCenter.Z
            );

            CreateBeamBetweenPoints(player, start, end);
        }
    }

    public static CBeam CreateBeamBetweenPoints(CCSPlayerController? player, Vector start, Vector end)
    {
        var g_Main = MainPlugin.Instance.g_Main;
        if(!player.IsValid(true) || !g_Main.DeadPlayer_Data.TryGetValue(player, out var data))return null!;

        CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
        if (beam == null) return null!;

        beam.Render = Configs.GetConfigData().DeadBody_CircleRadius_Color.ToColor();
        beam.Width = 3.0f;
        beam.EndWidth = 3.0f;

        beam.Teleport(start);
        beam.EndPos.X = end.X;
        beam.EndPos.Y = end.Y;
        beam.EndPos.Z = end.Z;
        beam.DispatchSpawn();
        data.CircleBeam.Add(beam);
        return beam;
    }

    public static CBeam CreateVerticalLineBeam(CCSPlayerController? player)
    {
        var g_Main = MainPlugin.Instance.g_Main;
        if (!Configs.GetConfigData().DeadBody_Arrow || !player.IsValid(true) || !g_Main.DeadPlayer_Data.TryGetValue(player, out var data))return null!;

        Vector basePos = new Vector(player.PlayerPawn?.Value?.AbsOrigin?.X, player.PlayerPawn?.Value?.AbsOrigin?.Y, player.PlayerPawn?.Value?.AbsOrigin?.Z + Configs.GetConfigData().DeadBody_MoveArrow_From_Ground_By);
        Vector tip0    = new Vector(basePos.X,   basePos.Y,   basePos.Z + 50);

        var line = Utilities.CreateEntityByName<CBeam>("beam")!;
        line.BeamType = BeamType_t.BEAM_POINTS;
        line.Width = 2f;
        line.EndWidth = 2f;
        line.Render = Configs.GetConfigData().DeadBody_Arrow_Color.ToColor();
        line.RenderMode = RenderMode_t.kRenderTransAdd;

        line.Teleport(basePos);
        line.EndPos.X = tip0.X;
        line.EndPos.Y = tip0.Y;
        line.EndPos.Z = tip0.Z;
        line.DispatchSpawn();

        data.LineBeam = line;
        data.LineBeam_Time = DateTime.Now;

        CreateArrowHead(basePos, data);

        return line;
    }

    private static void CreateArrowHead(Vector origin, Globals.DeadPlayerDataClass data)
    {
        const float wingSpread = 20f;  
        const float wingDrop   = 20f;  

        var dirs = new[]
        {
            new Vector( wingSpread, 0, +wingDrop),
            new Vector(-wingSpread, 0, +wingDrop)
        };

        var arrows = new List<CBeam>(2);
        foreach (var d in dirs)
        {
            var wing = Utilities.CreateEntityByName<CBeam>("beam")!;
            wing.BeamType = BeamType_t.BEAM_POINTS;
            wing.Width = 2f;
            wing.EndWidth = 2f;
            wing.Render = Configs.GetConfigData().DeadBody_Arrow_Color.ToColor();
            wing.RenderMode = RenderMode_t.kRenderTransAdd;

            wing.Teleport(origin);
            wing.EndPos.X = origin.X + d.X;
            wing.EndPos.Y = origin.Y + d.Y;
            wing.EndPos.Z = origin.Z + d.Z;
            wing.DispatchSpawn();

            Utilities.SetStateChanged(wing, "CBeam", "m_vecEndPos");
            arrows.Add(wing);
        }
        data.ArrowBeam = arrows;
    }

    public static void SpawnBeams(CCSPlayerController player)
    {
        var g_Main = MainPlugin.Instance.g_Main;
        if(!player.IsValid(true) || player.IsAlive())return;

        if(Configs.GetConfigData().Revive_AllowRevivingTeam == 1 && player.TeamNum != (byte)CsTeam.CounterTerrorist
        || Configs.GetConfigData().Revive_AllowRevivingTeam == 2 && player.TeamNum != (byte)CsTeam.Terrorist)return;

        if(!g_Main.DeadPlayer_Data.TryGetValue(player, out var playerhandle))return;
        
        playerhandle.Dead_Player = player;
        playerhandle.Dead_Player_Team = player.TeamNum;

        CreatePointWorldTextPoint(player);
        CreateReviveCircle(player);
        CreateVerticalLineBeam(player);
    }

    public static void RemoveBeams(CCSPlayerController player)
    {
        var g_Main = MainPlugin.Instance.g_Main;

        if(!player.IsValid(true) || !g_Main.DeadPlayer_Data.TryGetValue(player, out var playerhandle))return;
        
        
        foreach (var beam in playerhandle.CircleBeam)
        {
            if (beam != null && beam.IsValid)
            {
                beam.Remove();
            }
        }
        playerhandle.CircleBeam?.Clear();
        

        
        foreach (var ARROWbeam in playerhandle.ArrowBeam)
        {
            if (ARROWbeam != null && ARROWbeam.IsValid)
            {
                ARROWbeam.Remove();
            }
        }
        playerhandle.ArrowBeam?.Clear();
        

        if (playerhandle.LineBeam != null && playerhandle.LineBeam.IsValid)
        {
            playerhandle.LineBeam.Remove();
        }

        if (playerhandle.PointWorldTextPoint != null && playerhandle.PointWorldTextPoint.IsValid)
        {
            playerhandle.PointWorldTextPoint.Remove();
        }

        playerhandle.Dead_Player = null!;
    }

    public static void ResetAllLimit()
    {
        var g_Main = MainPlugin.Instance.g_Main;
        if (Configs.GetConfigData().Revive_Limit <= 0) return;
        bool reset = Configs.GetConfigData().Revive_Limit_Reset || GetCurrentRound() == 1;
        if (!reset) return;
        int mode = Configs.GetConfigData().Revive_Limit_Mode;
        if (mode == 1)
        {
            g_Main.Revive_Limit_CT = 0;
            g_Main.Revive_Limit_T = 0;
        }
        else if (mode == 2)
        {
            foreach(var p in GetPlayersController(true,false,false,true,true,true))
            {
                if(p.IsValid(true) && g_Main.Player_Data.TryGetValue(p, out var ph))
                {
                    ph.Revive_Limit = 0;
                }
            }
        }
    }
}