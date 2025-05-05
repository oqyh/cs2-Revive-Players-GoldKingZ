using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Revive_Players;
public class Globals
{
    public bool Disable_Revive = false;
    public int Counter = 0;
    public int Revive_Limit_CT = 0;
    public int Revive_Limit_T = 0;

    public class PlayerDataClass
    {
        public CCSPlayerController Player { get; set; }
        public CCSPlayerController TargetPlayer { get; set; }
        public CDynamicProp Animation { get; set; }
        public CDynamicProp AnimationClone { get; set; }
        public string Animation_Name { get; set; }
        public bool ButtonReady { get; set; }
        public bool Reviving { get; set; }
        public bool AnimationBool { get; set; }
        public int Revive_Limit { get; set; }
        public int Revive_Freeze { get; set; }
        public DateTime HoldStartTime { get; set; }
        public DateTime CoolDown { get; set; }

        public PlayerDataClass(CCSPlayerController Playerr, CCSPlayerController TargetPlayerr, CDynamicProp Animationn, CDynamicProp AnimationClonee, string Animation_Namee, bool ButtonReadyy, bool Revivingg, bool AnimationBooll, int Revive_Limitt, int Revive_Freezee, DateTime HoldStartTimee, DateTime CoolDownn)
        {
            Player = Playerr;
            TargetPlayer = TargetPlayerr;
            Animation = Animationn;
            AnimationClone = AnimationClonee;
            Animation_Name = Animation_Namee;
            ButtonReady = ButtonReadyy;
            Reviving = Revivingg;
            AnimationBool = AnimationBooll;
            Revive_Limit = Revive_Limitt;
            Revive_Freeze = Revive_Freezee;
            HoldStartTime = HoldStartTimee;
            CoolDown = CoolDownn;
        }
    }
    public Dictionary<CCSPlayerController, PlayerDataClass> Player_Data = new Dictionary<CCSPlayerController, PlayerDataClass>();


    public class DeadPlayerDataClass
    {
        public CCSPlayerController Dead_Player { get; set; }
        public byte Dead_Player_Team { get; set; }
        public List<CBeam> CircleBeam  { get; set; }
        public CBeam LineBeam { get; set; }
        public List<CBeam> ArrowBeam { get; set; }
        public CPointWorldText PointWorldTextPoint { get; set; }
        public DateTime LineBeam_Time { get; set; }

        public DeadPlayerDataClass(CCSPlayerController Dead_Playerr, byte Dead_Player_Teamm, List<CBeam> CircleBeamm, CBeam LineBeamm, List<CBeam> ArrowBeamm, CPointWorldText PointWorldTextPointt, DateTime LineBeam_Timee)
        {
            Dead_Player = Dead_Playerr;
            Dead_Player_Team = Dead_Player_Teamm;
            CircleBeam = CircleBeamm;
            LineBeam = LineBeamm;
            ArrowBeam = ArrowBeamm;
            PointWorldTextPoint = PointWorldTextPointt;
            LineBeam_Time = LineBeam_Timee;
        }
    }

    public Dictionary<CCSPlayerController, DeadPlayerDataClass> DeadPlayer_Data = new Dictionary<CCSPlayerController, DeadPlayerDataClass>();

}