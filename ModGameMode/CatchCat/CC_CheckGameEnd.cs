using TownOfHostY.Roles.Core;

namespace TownOfHostY.CatchCat;

class GameEndPredicate : TownOfHostY.GameEndPredicate
{
    public override bool CheckForEndGame(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

        if (CheckGameEndByLivingPlayers(out reason)) return true;
        return false;
    }

    public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;

        int[] counts = Common.CountLivingPlayersByPredicates(
            pc => pc.Is(CustomRoles.CCRedLeader),//0
            pc => pc.Is(CustomRoles.CCBlueLeader),//1
            pc => pc.Is(CustomRoles.CCYellowLeader),//2
            pc => pc.Is(CustomRoles.CCNoCat),//3
            pc => pc.Is(CustomRoles.CCRedCat),//4
            pc => pc.Is(CustomRoles.CCBlueCat),//5
            pc => pc.Is(CustomRoles.CCYellowCat)//6
        );
        int Leader = counts[0] + counts[1] + counts[2];
        int NoCat = counts[3];
        int RedTeam = counts[0] + counts[4];
        int BlueTeam = counts[1] + counts[5];
        int YellowTeam = counts[2] + counts[6];

        if (Leader == 0 && NoCat == 0) //全滅
        {
            reason = GameOverReason.ImpostorsByKill;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
        }
        else if (Leader == 1) //リーダーが残り1名になった
        {
            reason = GameOverReason.ImpostorsByKill;
            if (counts[0] == 1)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RedL);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.CCRedCat);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCRedLeader);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCRedCat);
            }
            else if (counts[1] == 1)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BlueL);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.CCBlueCat);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCBlueLeader);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCBlueCat);
            }
            else if (counts[2] == 1)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YellowL);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.CCYellowCat);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCYellowLeader);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCYellowCat);
            }
        }
        else if (NoCat <= 0) //無陣営の猫がいなくなった
        {
            reason = GameOverReason.ImpostorsByKill;

            if (RedTeam > BlueTeam && RedTeam > YellowTeam)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RedL);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.CCRedCat);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCRedLeader);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCRedCat);
            }
            else if (RedTeam < BlueTeam && BlueTeam > YellowTeam)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BlueL);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.CCBlueCat);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCBlueLeader);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCBlueCat);
            }
            else if (RedTeam < YellowTeam && BlueTeam < YellowTeam)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YellowL);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.CCYellowCat);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCYellowLeader);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.CCYellowCat);
            }
        }
        else if (Leader == 0) //クルー勝利(インポスター切断など)
        {
            reason = GameOverReason.ImpostorDisconnect;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
        }
        else return false; //勝利条件未達成

        return true;
    }
}