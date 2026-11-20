using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Translator;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Bakery : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Bakery),
            player => new Bakery(player),
            CustomRoles.Bakery,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 0,
            SetupOptionItem,
            "パン屋",
            "#b58428"
        );
    public Bakery(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => IsNeutral(player) ? HasTask.False : HasTask.True
    )
    {
        changeChances = OptionChangeChances.GetFloat();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public override void OnDestroy()
    {
        nBakeries.Remove(this);
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    public static OptionItem OptionChangeChances;
    enum OptionName
    {
        BakeryChangeChances,
    }
    private static float changeChances;

    public PlayerControl PoisonPlayer;
    public static List<Bakery> nBakeries = new();

    private static void SetupOptionItem()
    {
        OptionChangeChances = FloatOptionItem.Create(RoleInfo, 10, OptionName.BakeryChangeChances, new(0f, 20f, 0.5f), 5f, false)
            .SetValueFormat(OptionFormat.Percent);
    }
    public override void Add()
    {
        PoisonPlayer = null;
    }

    public static bool IsPoisoned(PlayerControl target = null)
    {
        foreach (var nBakery in nBakeries)
        {
            if (target == null && nBakery.PoisonPlayer != null)
            {
                return true;
            }
            if (target != null && nBakery.PoisonPlayer == target)
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsNeutral(PlayerControl bakery)
    {
        foreach (var nBakery in nBakeries)
        {
            if (nBakery.Player == bakery)
            {
                return true;
            }
        }
        return false;
    }

    public override void OverrideShowMainRoleText(ref Color roleColor, ref string roleText)
    {
        if (!IsNeutral(Player)) return;

        if (roleText == null)
        {
            roleText = GetString("NBakery");
        }
        else
        {
            roleText = roleText.Replace(GetRoleName(CustomRoles.Bakery), GetString("NBakery"));
        }
    }

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!nBakeries.Contains(this) || info.IsSuicide) return;

        PoisonPlayer = null;
        Logger.Info($"{target.GetNameWithRole()}の配布毒パン回収", "NBakery");
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting && IsPoisoned(seen))
        {
            return ColorString(RoleInfo.RoleColor, "θ");
        }
        return string.Empty;
    }

    public static (string, int) AddMeetingDisplay()
    {
        int aliveBakeryCount = Main.AllAlivePlayerControls.Count(p => p.Is(CustomRoles.Bakery));
        if (aliveBakeryCount == 0)
        {
            return ("", 0);
        }

        StringBuilder addText = new();

        if (aliveBakeryCount >= 2)
        {
            addText.Append("<size=90%>");
        }
        addText.Append($"<color={GetRoleColorCode(CustomRoles.Bakery)}><u>{GetString("BakeryAliveMessageTitle")}</color>");
        if (aliveBakeryCount >= 2)
        {
            addText.Append($"<color=#ffffff>x{aliveBakeryCount}</color></size>");
        }
        addText.Append("</u>\n");

        return (addText.ToString(), 1);
    }

    public override void OnStartMeeting()
    {
        var BakeryTitle = $"<color={RoleInfo.RoleColorCode}>{GetString("BakeryAliveMessageTitle")}</color>";
        string formatTag = $"<size=70%><line-height=1.6pic>";

        string message = "";

        if (nBakeries.Contains(this) && Player.IsAlive())
        {
            if (!PoisonPlayer.IsAlive())
            {
                PoisonPlayer = null;
            }

            message = GetNeutralBakeryMessage();
        }
        else if (Player.Is(CustomRoles.Bakery) && Player.IsAlive())
        {
            float chance = IRandom.Instance.Next(1, 10001) / 100f;
            if (chance <= changeChances)
            {
                nBakeries.Add(this);
                message = GetString("BakeryChange");
            }
            else
            {
                message = GetBakeryMessage();
            }
        }

        if (!message.IsNullOrWhiteSpace())
        {
            SendMessageAutoSplit($"{formatTag}\n{message}\nㅤ", true, BakeryTitle, formatTag: formatTag);
        }
    }

    private string GetNeutralBakeryMessage()
    {
        return PoisonPlayer.IsAlive() ? GetString("BakeryChangeNow") : GetString("BakeryChangeNone");
    }

    private string GetBakeryMessage()
    {
        string message = "";

        int chance = IRandom.Instance.Next(1, 101);
        if (chance <= 98)
        {
            string messageKey = chance <= 75 ? "BakeryAlliveMessage" : $"BakeryAlliveMessageRare{chance - 75}";
            message = GetString(messageKey);
        }
        else
        {
            List<PlayerControl> targetList = [.. Main.AllAlivePlayerControls.Where(p => !p.Is(CustomRoles.Bakery))];

            if (targetList.Count > 0)
            {
                PlayerControl targetPlayer = targetList[IRandom.Instance.Next(targetList.Count)];
                string messageKey = chance == 99 ? "BakeryAlliveMessageRare24" : "BakeryAlliveMessageRare25";
                string targetName = targetPlayer.GetRealName(Options.GetNameChangeModes() == NameChange.Crew);
                message = string.Format(GetString(messageKey), targetName);
            }
            else
            {
                message = GetString("BakeryAlliveMessage");
            }
        }

        return message;
    }

    public override void AfterMeetingTasks()
    {
        if (!nBakeries.Contains(this)) return;

        if (!Player.IsAlive() || MyState.DeathReason == CustomDeathReason.Vote)
        {
            PoisonPlayer = null;
            return;
        }

        if (PoisonPlayer != null && !Main.AfterMeetingDeathPlayers.ContainsKey(PoisonPlayer.PlayerId))
        {
            PoisonPlayer.SetRealKiller(Player);
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Poisoning, PoisonPlayer.PlayerId);
        }

        //次のターゲットを決めておく
        List<PlayerControl> targetList = new();
        var rand = IRandom.Instance;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (Player == pc || Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId)) continue;
            targetList.Add(pc);
        }
        if (targetList.Count == 0)
        {
            PoisonPlayer = null;
            return;
        }

        var PoisonedSelectPlayer = targetList[rand.Next(targetList.Count)];
        PoisonPlayer = PoisonedSelectPlayer;
        Logger.Info($"{Player.GetNameWithRole()}の次ターン配布先：{PoisonedSelectPlayer.GetNameWithRole()}", "NBakery");
    }
}