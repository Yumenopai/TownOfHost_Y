using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Channels;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Crewmate;
public sealed class LoyalDoggy : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(LoyalDoggy),
            player => new LoyalDoggy(player),
            CustomRoles.LoyalDoggy,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1500,
            SetupOptionItem,
            "ロイヤルドギー",
            "#ffcc29"
        );
    public LoyalDoggy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MasterSeeMasterMark = OptionMasterSeeMasterMark.GetBool();
        IgnoreTaskAfterDeadMaster = OptionIgnoreTaskAfterDeadMaster.GetBool();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        LoyalDoggies.Add(this);
        Master = null;
        masterDecision = false;
        masterIsDead = false;
    }
    public override void OnDestroy()
    {
        LoyalDoggies.Remove(this);
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    private static HashSet<LoyalDoggy> LoyalDoggies = new(15);
    PlayerControl Master;
    bool masterDecision;
    bool masterIsDead;

    private static OptionItem OptionMasterSeeMasterMark;
    private static OptionItem OptionIgnoreTaskAfterDeadMaster;
    private static OptionItem OptionIsInfoPoor;
    private static OptionItem OptionIsClumsy;

    enum OptionName
    {
        LoyalDoggyMasterSeeMasterMark,
        LoyalDoggyIgnoreTaskAfterDeadMaster,
        LoyalDoggyIsInfoPoor,
        LoyalDoggyIsClumsy,
    }
    private static bool MasterSeeMasterMark;
    private static bool IgnoreTaskAfterDeadMaster;

    public static void SetupOptionItem()
    {
        OptionMasterSeeMasterMark = BooleanOptionItem.Create(RoleInfo, 10, OptionName.LoyalDoggyMasterSeeMasterMark, false, false);
        OptionIgnoreTaskAfterDeadMaster = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LoyalDoggyIgnoreTaskAfterDeadMaster, true, false);
        OptionIsInfoPoor = BooleanOptionItem.Create(RoleInfo, 12, OptionName.LoyalDoggyIsInfoPoor, false, false).SetParent(OptionIgnoreTaskAfterDeadMaster);
        OptionIsClumsy = BooleanOptionItem.Create(RoleInfo, 13, OptionName.LoyalDoggyIsClumsy, false, false).SetParent(OptionIgnoreTaskAfterDeadMaster);
    }

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (Player == null || voterId != Player.PlayerId || !Player.IsAlive()) return (votedForId, numVotes, doVote);

        if (MeetingStates.FirstMeeting)
        {
            if (sourceVotedForId != Player.PlayerId && sourceVotedForId < 253)
            {
                Master = Utils.GetPlayerById(sourceVotedForId);
                Logger.Info($"マスターの選択({Player.GetNameWithRole()}) master:{Master.GetNameWithRole()}", "LoyalDoggy");
            }
            else
            {
                Master = Main.AllAlivePlayerControls.ElementAtOrDefault(IRandom.Instance.Next(0, Main.AllAlivePlayerControls.Count()));
                Logger.Info($"マスターはランダムで選択({Player.GetNameWithRole()}) master:{Master.GetNameWithRole()}", "LoyalDoggy");
            }
            numVotes = 0;//投票を見えなくする
            masterDecision = true;
            TargetArrow.Add(Player.PlayerId, Master.PlayerId);
        }
        return (votedForId, numVotes, doVote);
    }

    public override void AfterMeetingTasks()
    {
        if (masterDecision && !Master.IsAlive() && !masterIsDead)
        {
            masterIsDead = true;
            if (OptionIsInfoPoor.GetBool()) Player.RpcSetCustomRole(CustomRoles.InfoPoor);
            if (OptionIsClumsy.GetBool()) Player.RpcSetCustomRole(CustomRoles.Clumsy);
        }
    }
    public static bool IgnoreTask(NetworkedPlayerInfo p)
    {
        if (!IgnoreTaskAfterDeadMaster) return false;

        var pc = p.Object;
        foreach (var dog in LoyalDoggies)
        {
            if (!dog.masterDecision || dog.Player != pc) continue;

            return !dog.Master.IsAlive();
        }
        return false;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        if (!masterDecision) return string.Empty;
        //seenが省略の場合seer
        seen ??= seer;

        if (seer == Player && seen == Master)
            return Utils.ColorString(RoleInfo.RoleColor, "ω");
        return string.Empty;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";
        if (isForMeeting && MeetingStates.FirstMeeting)
        {
            // FirstMeeting Only
            return Translator.GetString("SelectMaster").Color(RoleInfo.RoleColor);
        }

        //矢印表示する必要がなければ無し
        if (!masterDecision || isForMeeting || masterIsDead) return string.Empty;
        if (Master == null || Master.Data.Disconnected) return string.Empty;

        var arrow = TargetArrow.GetArrows(seer, Master.PlayerId);
        var color = Master.IsAlive() ? RoleInfo.RoleColor : Palette.ImpostorRed;

        return arrow == "" ? string.Empty : arrow.Color(color);
    }
    public string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!masterDecision || !MasterSeeMasterMark) return string.Empty;
        seen ??= seer;

        if (seer == Master && seen == Master)
            return Utils.ColorString(RoleInfo.RoleColor, "ω");
        return string.Empty;
    }
}