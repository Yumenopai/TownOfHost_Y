using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class BestieWolf : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(BestieWolf),
            player => new BestieWolf(player),
            CustomRoles.BestieWolf,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1300,
            SetupOptionItem,
            "ベスティーウルフ"
        );
    public BestieWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldownSeveral = OptionKillCooldownSeveral.GetFloat();
        KillCooldownSingle = OptionKillCooldownSingle.GetFloat();
        grantMethod = (grantMethodOption)OptionGrantMethod.GetValue();

        for (int i = 0; i < 5; i++)
        {
            if (BuffAddonAssignTarget[i].GetValue() == 0)//Random
            {
                int chance = IRandom.Instance.Next(0, BuffAddonRoles.Length);
                grantAddonRole[i] = BuffAddonRoles[chance];
                Logger.Info($"ランダム付与属性決定：{grantAddonRole[i]}", "BestieWolf");
            }
            else
            {
                grantAddonRole[i] = BuffAddonRoles[BuffAddonAssignTarget[i].GetValue() - 1];
                Logger.Info($"付与属性：{grantAddonRole[i]}", "BestieWolf");
            }
        }

        if (grantMethod == grantMethodOption.GrantRandom)
        {
            grantAddonRole = grantAddonRole.OrderBy(x => IRandom.Instance.Next(5)).ToArray();
        }
    }
    private static OptionItem OptionKillCooldownSeveral;
    private static OptionItem OptionKillCooldownSingle;
    private static OptionItem OptionGrantMethod;
    private static OptionItem[] BuffAddonAssignTarget = new OptionItem[5];

    enum OptionName
    {
        BestieWolfKillCooldownSeveral,
        BestieWolfKillCooldownSingle,
        BestieWolfGrantMethod,
        BestieWolfBuffAddonAssignTarget1,
        BestieWolfBuffAddonAssignTarget2,
        BestieWolfBuffAddonAssignTarget3,
        BestieWolfBuffAddonAssignTarget4,
        BestieWolfBuffAddonAssignTarget5,
    }
    enum grantMethodOption
    {
        GrantOrder,
        GrantRandom,
    };
    grantMethodOption grantMethod;

    private static float KillCooldownSeveral;
    private static float KillCooldownSingle;
    private static CustomRoles[] grantAddonRole = { CustomRoles.NotAssigned, CustomRoles.NotAssigned, CustomRoles.NotAssigned, CustomRoles.NotAssigned, CustomRoles.NotAssigned };

    public static PlayerControl EnableKillFlash = null;
    int killCount = 0;

    static CustomRoles[] BuffAddonRoles = CustomRolesHelper.AllAddOnRoles.Where(role => role.IsBuffAddOn() && role != CustomRoles.Loyalty).ToArray();
    static string[] buffRoleArrays = BuffAddonRoles.Select(role => role.ToString()).ToArray();
    static string[] randArrays = { "Random" };
    static string[] selectStringArray = randArrays.Concat(buffRoleArrays).ToArray();
    public static void SetupOptionItem()
    {
        OptionKillCooldownSeveral = FloatOptionItem.Create(RoleInfo, 10, OptionName.BestieWolfKillCooldownSeveral, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillCooldownSingle = FloatOptionItem.Create(RoleInfo, 11, OptionName.BestieWolfKillCooldownSingle, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionGrantMethod = StringOptionItem.Create(RoleInfo, 12, OptionName.BestieWolfGrantMethod, EnumHelper.GetAllNames<grantMethodOption>(), 0, false);

        for (int i = 0; i < 5; i++)
        {
            Enum name = null;
            switch(i)
            {
                case 0: name = OptionName.BestieWolfBuffAddonAssignTarget1; break;
                case 1: name = OptionName.BestieWolfBuffAddonAssignTarget2; break;
                case 2: name = OptionName.BestieWolfBuffAddonAssignTarget3; break;
                case 3: name = OptionName.BestieWolfBuffAddonAssignTarget4; break;
                case 4: name = OptionName.BestieWolfBuffAddonAssignTarget5; break;
            }
            BuffAddonAssignTarget[i] = StringOptionItem.Create(RoleInfo, 13 + i, name, selectStringArray, 0, false);
        }
    }
    public override void Add()
    {
        killCount = 0;
    }

    public float CalculateKillCooldown() => Main.AliveImpostorCount >= 2 ? KillCooldownSeveral : KillCooldownSingle;
    public override string GetProgressText(bool comms = false)
    {
        if (!Player.IsAlive() || Main.AliveImpostorCount <= 1 || killCount > 5) return string.Empty;

        return Utils.ColorString(Palette.ImpostorRed, $"[{killCount}]");
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (Main.AliveImpostorCount >= 2)
        {
            foreach(var imp in Main.AllAlivePlayerControls.Where(pc=>pc.Is(CustomRoleTypes.Impostor)))
            {
                if (imp == Player) continue;//自身ではない

                imp.RpcSetCustomRole(grantAddonRole[killCount]);
            }
            killCount++;
        }
        else //単独インポスター
        {
            if (!AmongUsClient.Instance.AmHost) return; //爆破処理はホストのみ

            (float d, PlayerControl pc) nearTarget = (2.3f, null);
            foreach (var fire in Main.AllAlivePlayerControls)
            {
                if (fire == killer || fire == target) continue;
                var pos = target.transform.position;
                var dis = Vector2.Distance(pos, fire.transform.position);

                if (dis < nearTarget.d)
                {
                    nearTarget = (dis, fire);
                }
            }
            if (nearTarget.pc != null)
            {
                EnableKillFlash = nearTarget.pc;
                if (CustomRoleManager.OnCheckMurder(
                        killer, nearTarget.pc,
                        nearTarget.pc, nearTarget.pc,
                        true))
                {
                    nearTarget.pc.SetRealKiller(killer);
                }
            }
        }
    }

}