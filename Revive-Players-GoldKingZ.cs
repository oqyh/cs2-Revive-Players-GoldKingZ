using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using Revive_Players.Config;
using System.Drawing;
using System.Text;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace Revive_Players;

public class MainPlugin : BasePlugin
{
    public override string ModuleName => "Allow To Revive Players With Flags";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Gold KingZ";
    public override string ModuleDescription => "https://github.com/oqyh";
    public static MainPlugin Instance { get; set; } = new();
    public Globals g_Main = new();
    
    public override void Load(bool hotReload)
    {
        Instance = this;
        Configs.Load(ModuleDirectory);

        RegisterEventHandler<EventPlayerSpawn>(OnEventPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnEventPlayerDeath);
        RegisterEventHandler<EventBotTakeover>(OnEventBotTakeover);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnEventRoundEnd);

        RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.OnTick>(OnTick);

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

        if(hotReload)
        {
            foreach(var players in Helper.GetPlayersController(true,false,false,true,true,true))
            {
                if(players.IsValid(true))
                {
                    Helper.CheckPlayerInGlobals(players);
                }
            }
        }

    }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        try
        {
            string filePath = Path.Combine(ModuleDirectory, "config/ServerPrecacheResources.txt");
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (line.TrimStart().StartsWith("//"))continue;
                manifest.AddResource(line);
                Helper.DebugMessage("ResourceManifest : " + line);
            }
        }
        catch (Exception ex)
        {
            Helper.DebugMessage(ex.Message);
        }
    }

    private HookResult OnTakeDamage(DynamicHook hook)
    {
        if(!Configs.GetConfigData().Revive_BlockDamageOnReviving) return HookResult.Continue;

        var ent = hook.GetParam<CEntityInstance>(0);
        if (ent == null || !ent.IsValid || ent.DesignerName != "player") return HookResult.Continue;

        var damageinfo = hook.GetParam<CTakeDamageInfo>(1);
        if(damageinfo == null) return HookResult.Continue;

        var pawn = ent.As<CCSPlayerPawn>();
        if (pawn == null || !pawn.IsValid) return HookResult.Continue;

        var GetAttacker = damageinfo.Attacker.Value?.As<CBasePlayerPawn>().Controller.Value;
        if (GetAttacker == null || !GetAttacker.IsValid) return HookResult.Continue;

        var Victim = pawn.OriginalController.Get();
        var attacker = Utilities.GetPlayerFromIndex((int)GetAttacker.Index);
        if (Victim == null || !Victim.IsValid || attacker == null || !attacker.IsValid) return HookResult.Continue;
        Helper.CheckPlayerInGlobals(Victim);
        Helper.CheckPlayerInGlobals(attacker);

        bool Check_teammates_are_enemies = ConVar.Find("mp_teammates_are_enemies")!.GetPrimitiveValue<bool>() == false && attacker.TeamNum != Victim.TeamNum;

        if (Check_teammates_are_enemies && g_Main.Player_Data.TryGetValue(attacker, out var dataplayer_reviver) && dataplayer_reviver.Reviving)
        {
            damageinfo.Damage = 0;
            return HookResult.Changed;
        }

        return HookResult.Continue;
    }

    public HookResult OnEventBotTakeover(EventBotTakeover @event, GameEventInfo info)
    {
        if (@event == null)return HookResult.Continue;

        var player = @event.Userid;
        if (!player.IsValid(true)) return HookResult.Continue;

        Helper.RemoveBeams(player);
        
        return HookResult.Continue;
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (!player.IsValid(true)) continue;

            if(!string.IsNullOrEmpty(Configs.GetConfigData().Revive_ModelAnimation) && !string.IsNullOrEmpty(Configs.GetConfigData().Revive_NameAnimation))
            {
                foreach (var data in g_Main.Player_Data.Values)
                {
                    if (data == null || !player.IsValid(true) || !data.Player.IsValid(true) || !data.Reviving) continue;

                    data.Player.PlayerPawn?.Value?
                    .WeaponServices?.MyWeapons?
                    .Where(kv => 
                        kv.Value != null 
                        && kv.Value.IsValid 
                        && kv.Value.DesignerName == "weapon_c4")
                    .Select(kv => kv.Value)
                    .ToList()
                    .ForEach(w => info.TransmitEntities.Remove(w!));
                }
            }

            foreach (var data in g_Main.DeadPlayer_Data.Values)
            {
                if (data == null || !player.IsValid(true) || !data.Dead_Player.IsValid(true)) continue;

                bool shouldRemoveEnt = false;

                if (player.TeamNum != data.Dead_Player_Team || player.TeamNum != data.Dead_Player.TeamNum)
                {
                    shouldRemoveEnt = true;
                }
                

                if(shouldRemoveEnt)
                {
                    if (data.LineBeam != null && data.LineBeam.IsValid)
                    {
                        info.TransmitEntities.Remove(data.LineBeam);
                    }

                    if (data.PointWorldTextPoint != null && data.PointWorldTextPoint.IsValid)
                    {
                        info.TransmitEntities.Remove(data.PointWorldTextPoint);
                    }

                    foreach (var listCircleBeam in data.CircleBeam)
                    {
                        if (listCircleBeam != null && listCircleBeam.IsValid)
                        {
                            info.TransmitEntities.Remove(listCircleBeam);
                        }
                    }

                    foreach (var listLineArrowBeam in data.ArrowBeam)
                    {
                        if (listLineArrowBeam != null && listLineArrowBeam.IsValid)
                        {
                            info.TransmitEntities.Remove(listLineArrowBeam);
                        }
                    }
                }
            }
        }
    }

    private HookResult OnEventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event == null || Helper.IsWarmup())return HookResult.Continue;

        var victim = @event.Userid;
        if (!victim.IsValid(true)) return HookResult.Continue;

        Helper.CheckPlayerInGlobals(victim);
        Helper.SpawnBeams(victim);
        Helper.ResetPlayer(victim);
        
        return HookResult.Continue;
    }
    

    private HookResult OnEventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event == null)return HookResult.Continue;

        var player = @event.Userid;
        if (!player.IsValid(true))return HookResult.Continue;

        Helper.CheckPlayerInGlobals(player);
        Helper.RemoveBeams(player);

        return HookResult.Continue;
    }


    public void OnTick()
    {
        if(g_Main.Disable_Revive)return;

        foreach (var entry in g_Main.DeadPlayer_Data)
        {
            var data = entry.Value;
            if(!data.Dead_Player.IsValid(true))continue;
            data.Dead_Player_Team = data.Dead_Player.TeamNum;

            if (data.Dead_Player.TeamNum != data.Dead_Player_Team)
            {
                Helper.RemoveBeams(data.Dead_Player);
                continue;
            }

            Helper.UpdateVerticalLineBeam(data);
            Helper.UpdateReviveCircle(data);
            Helper.UpdateWorldText(data);
        }
        
        
        foreach (var entry in g_Main.Player_Data)
        {
            var player = entry.Key;
            var playerData = entry.Value;
            
            if (!player.IsValid(true) || !player.PawnIsAlive) continue;

            if (player.Buttons == 0)
            {
                playerData.ButtonReady = true;
                playerData.HoldStartTime = DateTime.MinValue;
                playerData.Reviving = false;
                playerData.TargetPlayer = null!;
                Helper.ResetPlayer(player);

                var (target, distance) = Helper.FindReviveTarget(player);
                if (target != null && target.TeamNum == player.TeamNum && distance <= Configs.GetConfigData().Revive_Distance)
                {
                    StringBuilder builder = new StringBuilder();
                    string centermessage = Localizer[$"PrintCenterToPlayer.Revive.Player", target.PlayerName];
                    builder.AppendFormat(centermessage);
                    var centerhtml = builder.ToString();
                    player.PrintToCenterHtml(centerhtml);
                }
                continue;
            }else if (!Configs.GetConfigData().Revive_CancelRevivingOnAdditionalInput && player.Buttons.HasFlag(PlayerButtons.Use) 
            || Configs.GetConfigData().Revive_CancelRevivingOnAdditionalInput && player.Buttons == PlayerButtons.Use )
            {
                if (playerData.ButtonReady)
                {
                    var (target, distance) = Helper.FindReviveTarget(player);
                    if (target != null && target.TeamNum == player.TeamNum)
                    {
                        float maxDist = Configs.GetConfigData().Revive_Distance;
                        float warnOutside = maxDist * maxDist;
                        
                        if (distance > maxDist && distance <= warnOutside)
                        {
                            if (!Helper.CanPlayerRevive(player, playerData, true))
                            {
                                playerData.ButtonReady = false;
                                playerData.Reviving = false;
                                playerData.TargetPlayer = null!;
                                Helper.ResetPlayer(player);
                                continue;
                            }

                            Helper.AdvancedPlayerPrintToChat(player, null!, Localizer["PrintChatToPlayer.Revive.Too.Far"], maxDist);

                            playerData.ButtonReady = false;
                            playerData.Reviving = false;
                            playerData.TargetPlayer = null!;
                            Helper.ResetPlayer(player);
                            continue;
                        }
                        else if (distance <= maxDist)
                        {
                            if (!Helper.CanPlayerRevive(player, playerData))
                            {
                                playerData.ButtonReady = false;
                                playerData.Reviving = false;
                                playerData.TargetPlayer = null!;
                                Helper.ResetPlayer(player);
                                continue;
                            }

                            playerData.HoldStartTime = DateTime.Now;
                            playerData.ButtonReady = false;
                            playerData.Reviving = false;
                            playerData.TargetPlayer = null!;
                            Helper.ResetPlayer(player);
                            continue;
                        }
                    }
                }
                else if (playerData.HoldStartTime != DateTime.MinValue)
                {
                    var (target, distance) = Helper.FindReviveTarget(player);
                    if (target == null)
                    {
                        playerData.HoldStartTime = DateTime.MinValue;
                        playerData.Reviving = false;
                        playerData.TargetPlayer = null!;
                        Helper.ResetPlayer(player);
                        continue;
                    }

                    if (Helper.CheckHoldDuration(playerData))
                    {
                        if(playerData.TargetPlayer != null && target != playerData.TargetPlayer)continue;
                        
                        playerData.HoldStartTime = DateTime.MinValue;
                        playerData.Reviving = false;
                        playerData.TargetPlayer = null!;
                        Helper.Revive_Complete(player, target);
                        Helper.ResetPlayer(player);
                    }
                    else
                    {
                        if(playerData.TargetPlayer != null && target != playerData.TargetPlayer)continue;
                        
                        StringBuilder builder = new StringBuilder();
                        double elapsed = (DateTime.Now - playerData.HoldStartTime).TotalSeconds;
                        string centermessage = Localizer[$"PrintCenterToPlayer.Reviving", $"{Configs.GetConfigData().Revive_Duration - elapsed:F1}"];
                        builder.AppendFormat(centermessage);
                        var centerhtml = builder.ToString();
                        player.PrintToCenterHtml(centerhtml);
                        Helper.CreateAnimtion(player);
                        Helper.PlayerFreeze(player);
                        playerData.Reviving = true;
                        playerData.TargetPlayer = target;
                    }
                }
            }else
            {
                playerData.ButtonReady = true;
                playerData.HoldStartTime = DateTime.MinValue;
                playerData.Reviving = false;
                playerData.TargetPlayer = null!;
                Helper.ResetPlayer(player);

                var (target, distance) = Helper.FindReviveTarget(player);
                if (target != null && target.TeamNum == player.TeamNum && distance <= Configs.GetConfigData().Revive_Distance)
                {
                    StringBuilder builder = new StringBuilder();
                    string centermessage = Localizer[$"PrintCenterToPlayer.Revive.Player", target.PlayerName];
                    builder.AppendFormat(centermessage);
                    var centerhtml = builder.ToString();
                    player.PrintToCenterHtml(centerhtml);
                }
            }
        }
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (@event == null || Helper.IsWarmup())return HookResult.Continue;

        g_Main.Disable_Revive = false;

        Helper.ResetAllLimit();
        
        return HookResult.Continue;
    }
    public HookResult OnEventRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (@event == null)return HookResult.Continue;

        g_Main.Disable_Revive = true;
        
        return HookResult.Continue;
    }

    public void OnMapEnd()
    {
        Helper.ClearVariables();
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        Helper.ClearVariables();
    }

    /* [ConsoleCommand("css_test", "test")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Test(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!player.IsValid()) return;

    } */
    
}