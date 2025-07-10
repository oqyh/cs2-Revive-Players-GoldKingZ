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
using CounterStrikeSharp.API.Modules.Entities;

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
    

    public static bool IsPlayerInGroupPermission(CCSPlayerController player, string groups)
    {
        if (string.IsNullOrEmpty(groups) || player == null || !player.IsValid)return false;

        return groups.Split('|')
        .Select(segment => segment.Trim())
        .Any(trimmedSegment => Permission_CheckPermissionSegment(player, trimmedSegment));
    }
    private static bool Permission_CheckPermissionSegment(CCSPlayerController player, string segment)
    {
        if (string.IsNullOrEmpty(segment))return false;

        int colonIndex = segment.IndexOf(':');
        if (colonIndex == -1 || colonIndex == 0)return false;

        string prefix = segment.Substring(0, colonIndex).Trim().ToLower();
        string values = segment.Substring(colonIndex + 1).Trim();

        return prefix switch
        {
            "steamid" or "steamids" or "steam" or "steams" => Permission_CheckSteamIds(player, values),
            "flag" or "flags" => Permission_CheckFlags(player, values),
            "group" or "groups" => Permission_CheckGroups(player, values),
            _ => false
        };
    }
    private static bool Permission_CheckSteamIds(CCSPlayerController player, string steamIds)
    {
        steamIds = steamIds.Replace("[", "").Replace("]", "");

        var (steam2, steam3, steam32, steam64) = player.SteamID.GetPlayerSteamID();
        var steam3NoBrackets = steam3.Trim('[', ']');

        return steamIds
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(id => id.Trim())
        .Any(trimmedId =>
            string.Equals(trimmedId, steam2, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedId, steam3NoBrackets, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedId, steam32, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedId, steam64, StringComparison.OrdinalIgnoreCase)
        );
    }
    private static bool Permission_CheckFlags(CCSPlayerController player, string flags)
    {
        return flags.Split(',')
        .Select(flag => flag.Trim())
        .Where(trimmedFlag => trimmedFlag.StartsWith("@"))
        .Any(trimmedFlag => MyPlayerHasPermissions(player, trimmedFlag));
    }
    private static bool Permission_CheckGroups(CCSPlayerController player, string groups)
    {
        return groups.Split(',')
        .Select(group => group.Trim())
        .Where(trimmedGroup => trimmedGroup.StartsWith("#"))
        .Any(trimmedGroup => MyPlayerInGroup(player, trimmedGroup));
    }
    public static bool MyPlayerHasPermissions(CCSPlayerController player, params string[] flags)
    {
        if (player == null) return true;

        if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.IsBot || player.IsHLTV) return false;

        var playerData = AdminManager.GetPlayerAdminData(player);
        if (playerData == null) return false;

        foreach (var domain in playerData.Flags)
        {
            if (string.IsNullOrEmpty(domain.Key)) continue;

            var domainFlags = flags
            .Where(flag => flag.StartsWith($"@{domain.Key}/"))
            .ToArray();

            if (domainFlags.Length == 0) continue;

            if (!playerData.DomainHasFlags(domain.Key, domainFlags, true))
            {
                return false;
            }
        }
        return true;
    }
    public static bool MyPlayerInGroup(CCSPlayerController? player, params string[] groups)
    {
        if (player == null) return true;
        
        if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.IsBot || player.IsHLTV)return false;
        
        return MyPlayerInGroup(player.AuthorizedSteamID, groups);
    }
    public static bool MyPlayerInGroup(SteamID? steamId, params string[] groups)
    {
        if (steamId == null)return false;

        var playerData = AdminManager.GetPlayerAdminData(steamId);
        if (playerData == null)return false;

        var groupsToCheck = groups.ToHashSet();
        foreach (var domain in playerData.Flags)
        {
            if (string.IsNullOrEmpty(domain.Key)) continue;

            if (playerData.Flags[domain.Key].Contains("@" + domain.Key + "/*"))
            {
                groupsToCheck.ExceptWith(groups.Where(group => group.Contains(domain.Key + '/')));
            }
        }
        return playerData.Groups.IsSupersetOf(groupsToCheck);
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

    
    public static void ClearVariables()
    {
        var g_Main = MainPlugin.Instance.g_Main;

        g_Main.Disable_Revive = false;
        g_Main.Counter = 0;
        g_Main.Revive_Limit_CT = 0;
        g_Main.Revive_Limit_T = 0;

        g_Main.Player_Data?.Clear();
        g_Main.DeadPlayer_Data?.Clear();
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

    public static float DistanceTo(My_Vector a, My_Vector b)
    {
        return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }

    public static void CheckPlayerInGlobals(CCSPlayerController player)
    {
        if (!player.IsValid(true)) return;

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
        if (Configs.GetConfigData().DeadBody_Arrow != 2
        || data == null
        || !data.Dead_Player.IsValid(true)
        || data.Dead_Player.IsAlive()
        || data.ArrowBeam.Count == 0
        || data.LineBeam == null
        || !data.LineBeam.IsValid)
        {
            return;
        }

        float t = (float)(DateTime.Now - data.LineBeam_Time).TotalSeconds;
        float offset = MathF.Sin(t * 4f) * 70f;
        
        My_Vector basePos = data.Dead_Player.PlayerPawn.Value!.AbsOrigin!.ToMy_Vector();
        basePos.Z += Configs.GetConfigData().DeadBody_MoveArrow_From_Ground_By + offset;
        
        My_Vector tip = basePos;
        tip.Z += 50;

        data.LineBeam.My_Teleport(position: basePos);
        data.LineBeam.EndPos.X = tip.X;
        data.LineBeam.EndPos.Y = tip.Y;
        data.LineBeam.EndPos.Z = tip.Z;
        Utilities.SetStateChanged(data.LineBeam, "CBeam", "m_vecEndPos");
        
        for (int i = 0; i < data.ArrowBeam.Count; i++)
        {
            var wing = data.ArrowBeam[i];
            if (wing == null || !wing.IsValid) continue;

            float xOffset = (i == 0) ? cons_wing : -cons_wing;
            
            wing.My_Teleport(position: basePos);
            wing.EndPos.X = basePos.X + xOffset;
            wing.EndPos.Y = basePos.Y;
            wing.EndPos.Z = basePos.Z + cons_wing;
            Utilities.SetStateChanged(wing, "CBeam", "m_vecEndPos");
        }
        
    }

    public static (CCSPlayerController? Target, float Distance) FindReviveTarget(CCSPlayerController player)
    {
        var g_Main = MainPlugin.Instance.g_Main;
        float maxDist = Configs.GetConfigData().Revive_Distance;
        float MaxRevive_Distance = maxDist * 1.5f; 

        if (!player.IsValid(true) || !player.IsAlive())return (null, float.MaxValue);

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || playerPawn.AbsOrigin == null)return (null, float.MaxValue);
        
        My_Vector playerPos = playerPawn.AbsOrigin.ToMy_Vector();

        CCSPlayerController? validTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var deadEntry in g_Main.DeadPlayer_Data.Values)
        {
            if (deadEntry == null)continue;

            var deadplayer = deadEntry.Dead_Player;

            if (!deadplayer.IsValid(true) || deadplayer.IsAlive() || deadplayer == player || deadplayer.TeamNum != player.TeamNum)continue;

            var deadPawn = deadplayer.PlayerPawn.Value;
            if (deadPawn == null || deadPawn.AbsOrigin == null)continue;
            My_Vector deadPos = deadPawn.AbsOrigin.ToMy_Vector();

            float distance = DistanceTo(playerPos, deadPos);


            if (distance <= MaxRevive_Distance && distance < closestDistance)
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
        || !g_Main.Player_Data.TryGetValue(playerreviver, out var dataplayer_reviver) 
        || !g_Main.DeadPlayer_Data.TryGetValue(playerdead, out var dataplayer_dead)
        || dataplayer_reviver == null || dataplayer_dead == null) return;

        My_Vector teleportPosition = playerdead.PlayerPawn.Value!.AbsOrigin!.ToMy_Vector();
        My_QAngle teleportRotation = playerdead.PlayerPawn.Value!.AbsRotation!.ToMy_QAngle();
        
        playerdead.Respawn();
        playerdead.PlayerPawn?.Value.My_Teleport(position: teleportPosition, angles: teleportRotation);
        dataplayer_reviver.TargetPlayer = null!;
        
        if (playerdead.Pawn.Value != null && playerdead.Pawn.Value.IsValid && playerdead.PlayerPawn?.Value != null)
        {
            playerdead.PlayerPawn.Value.Health = Configs.GetConfigData().Revive_Health;
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

        My_Vector teleportPosition = player.PlayerPawn!.Value!.AbsOrigin!.ToMy_Vector();
        My_QAngle teleportRotation = player.PlayerPawn!.Value!.AbsRotation!.ToMy_QAngle();

        AnimationEnt.My_Teleport(teleportPosition, teleportRotation);

        var AnimationCloneEnt = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (AnimationCloneEnt == null)return;
        AnimationCloneEnt.DispatchSpawn();
        AnimationCloneEnt.SetModel(orginalmodel);
        AnimationCloneEnt.My_Teleport(teleportPosition, teleportRotation);

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
        if (!Configs.GetConfigData().Revive_FreezeOnReviving ||
        player == null || !player.IsValid ||
        player.Pawn == null || !player.Pawn.IsValid ||
        player.Pawn.Value == null || !player.Pawn.Value.IsValid ||
        player.PlayerPawn == null || !player.PlayerPawn.IsValid ||
        player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;

        var g_Main = MainPlugin.Instance.g_Main;
        if (!g_Main.Player_Data.TryGetValue(player, out var data)) return;

        if (Configs.GetConfigData().Revive_DontUnFreezeIfPlayerWasFreezed)
        {
            CheckPlayerMovment(player);
            if (data.Revive_Freeze != 2) return;
        }

        if (player.PlayerPawn.Value.Flags == 65664 || player.PlayerPawn.Value.Flags == 65672
        || player.Pawn.Value.Flags == 65664 || player.Pawn.Value.Flags == 65672) return;

        player.Freeze();
        
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
        if (!Configs.GetConfigData().Revive_FreezeOnReviving ||
        player == null || !player.IsValid ||
        player.Pawn == null || !player.Pawn.IsValid ||
        player.Pawn.Value == null || !player.Pawn.Value.IsValid ||
        player.PlayerPawn == null || !player.PlayerPawn.IsValid ||
        player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;

        var g_Main = MainPlugin.Instance.g_Main;
        if (!g_Main.Player_Data.TryGetValue(player, out var data)) return;

        if(Configs.GetConfigData().Revive_DontUnFreezeIfPlayerWasFreezed)
        {
            CheckPlayerMovment(player);
            if(data.Revive_Freeze == 1)return;
        }

        if (player.PlayerPawn.Value.Flags == 65664 || player.PlayerPawn.Value.Flags == 65672
        || player.Pawn.Value.Flags == 65664 || player.Pawn.Value.Flags == 65672) return;

        player.Unfreeze();
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

        My_Vector Teleport = player.PlayerPawn.Value!.AbsOrigin!.ToMy_Vector();
        Teleport.Z += Configs.GetConfigData().DeadBody_MovePlayerNameText_From_Ground_By;

        My_QAngle Rotation = player.PlayerPawn.Value!.AbsRotation!.ToMy_QAngle();
        Rotation.Y += 90f;
        Rotation.Z += 90f;
        
        entity.My_Teleport(Teleport, Rotation);
        entity.AcceptInput("FollowEntity", entity , null! , "");
        

        data.PointWorldTextPoint = entity;
    }

    public static void CreateReviveCircle(CCSPlayerController player)
    {
        if (!Configs.GetConfigData().DeadBody_CircleRadius || !player.IsValid(true)) return;

        float radius = Configs.GetConfigData().Revive_Distance;
        int pointCount = 100;

        
        My_Vector point = player.PlayerPawn.Value!.AbsOrigin!.ToMy_Vector();
        point.Z += Configs.GetConfigData().DeadBody_MoveCircleRadius_From_Ground_By;

        My_Vector start = default;
        My_Vector end = default;

        for (int i = 0; i < pointCount; i++)
        {
            if (!player.IsValid(true)) return;

            double angle1 = i * (2 * Math.PI / pointCount);
            double angle2 = (i + 1) * (2 * Math.PI / pointCount);

            start.X = point.X + (float)(radius * Math.Cos(angle1));
            start.Y = point.Y + (float)(radius * Math.Sin(angle1));
            start.Z = point.Z;

            end.X = point.X + (float)(radius * Math.Cos(angle2));
            end.Y = point.Y + (float)(radius * Math.Sin(angle2));
            end.Z = point.Z;

            CreateBeamBetweenPoints(player, start, end);
        }
        
    }

    public static CBeam CreateBeamBetweenPoints(CCSPlayerController? player, My_Vector start, My_Vector end)
    {
        var g_Main = MainPlugin.Instance.g_Main;
        if(!player.IsValid(true) || !g_Main.DeadPlayer_Data.TryGetValue(player, out var data)) return null!;

        CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
        if (beam == null) return null!;

        beam.Render = Configs.GetConfigData().DeadBody_CircleRadius_Color.ToColor();
        beam.Width = 3.0f;
        beam.EndWidth = 3.0f;

        beam.My_Teleport(position: start);
        
        beam.EndPos.X = end.X;
        beam.EndPos.Y = end.Y;
        beam.EndPos.Z = end.Z;
        
        beam.DispatchSpawn();
        data.CircleBeam.Add(beam);
        beam.AcceptInput("FollowEntity", beam, null!, "");
        return beam;
    }

    public static CBeam CreateVerticalLineBeam(CCSPlayerController? player)
    {
        var g_Main = MainPlugin.Instance.g_Main;
        if (Configs.GetConfigData().DeadBody_Arrow == 0 || !player.IsValid(true) || !g_Main.DeadPlayer_Data.TryGetValue(player, out var data)) return null!;

        
        My_Vector basePos = player.PlayerPawn.Value!.AbsOrigin!.ToMy_Vector();
        basePos.Z += Configs.GetConfigData().DeadBody_MoveArrow_From_Ground_By;
        
        My_Vector tip0 = basePos;
        tip0.Z += 50;

        var line = Utilities.CreateEntityByName<CBeam>("beam")!;
        line.BeamType = BeamType_t.BEAM_POINTS;
        line.Width = 2f;
        line.EndWidth = 2f;
        line.Render = Configs.GetConfigData().DeadBody_Arrow_Color.ToColor();
        line.RenderMode = RenderMode_t.kRenderTransAdd;

        line.My_Teleport(position: basePos);
        
        line.EndPos.X = tip0.X;
        line.EndPos.Y = tip0.Y;
        line.EndPos.Z = tip0.Z;
        
        line.DispatchSpawn();
        line.AcceptInput("FollowEntity", line, null!, "");

        data.LineBeam = line;
        data.LineBeam_Time = DateTime.Now;

        CreateArrowHead(basePos, data);

        return line;
    }

    private const float cons_wing = 20f; 
    private static void CreateArrowHead(My_Vector origin, Globals.DeadPlayerDataClass data)
    {

        float wing1X = origin.X + cons_wing;
        float wing2X = origin.X - cons_wing;
        float wingY = origin.Y;
        float wingZ = origin.Z + cons_wing;

        var arrows = new List<CBeam>(2);

        var wing1 = Utilities.CreateEntityByName<CBeam>("beam")!;
        wing1.BeamType = BeamType_t.BEAM_POINTS;
        wing1.Width = 2f;
        wing1.EndWidth = 2f;
        wing1.Render = Configs.GetConfigData().DeadBody_Arrow_Color.ToColor();
        wing1.RenderMode = RenderMode_t.kRenderTransAdd;

        wing1.My_Teleport(position: origin);
        wing1.EndPos.X = wing1X;
        wing1.EndPos.Y = wingY;
        wing1.EndPos.Z = wingZ;

        wing1.DispatchSpawn();
        wing1.AcceptInput("FollowEntity", wing1, null!, "");
        arrows.Add(wing1);

        var wing2 = Utilities.CreateEntityByName<CBeam>("beam")!;
        wing2.BeamType = BeamType_t.BEAM_POINTS;
        wing2.Width = 2f;
        wing2.EndWidth = 2f;
        wing2.Render = Configs.GetConfigData().DeadBody_Arrow_Color.ToColor();
        wing2.RenderMode = RenderMode_t.kRenderTransAdd;

        wing2.My_Teleport(position: origin);
        wing2.EndPos.X = wing2X;
        wing2.EndPos.Y = wingY;
        wing2.EndPos.Z = wingZ;

        wing2.DispatchSpawn();
        wing2.AcceptInput("FollowEntity", wing2, null!, "");
        arrows.Add(wing2);

        Utilities.SetStateChanged(wing1, "CBeam", "m_vecEndPos");
        Utilities.SetStateChanged(wing2, "CBeam", "m_vecEndPos");
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