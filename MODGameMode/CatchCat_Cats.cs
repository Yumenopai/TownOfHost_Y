using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class CatNoCat
    {
        public static List<byte> playerIdList = new();

        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }

        public static bool IsEnable() => playerIdList.Count > 0;

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            //シュレディンガーの猫が切られた場合の役職変化スタート
            //直接キル出来る役職チェック
            //killer.SetKillCooldown(Main.AllPlayerKillCooldown[killer.PlayerId]);
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);
            switch (killer.GetCustomRole())
            {
                case CustomRoles.CatRedLeader:
                    target.RpcSetCustomRole(CustomRoles.CatRedCat);
                    break;

                case CustomRoles.CatBlueLeader:
                    target.RpcSetCustomRole(CustomRoles.CatBlueCat);
                    break;

                case CustomRoles.CatYellowLeader:
                    target.RpcSetCustomRole(CustomRoles.CatYellowCat);
                    break;
            }
            NameColorManager.Add(killer.PlayerId, target.PlayerId);

            Utils.NotifyRoles();
            Utils.MarkEveryoneDirtySettings();
            //シュレディンガーの猫の役職変化処理終了
            //第三陣営キル能力持ちが追加されたら、その陣営を味方するシュレディンガーの猫の役職を作って上と同じ書き方で書いてください
        }
    }
}