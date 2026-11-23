using System.Collections.Generic;
using TownOfHostY.Attributes;
using TownOfHostY.Roles.Core;

namespace TownOfHostY
{
    public static class CustomWinnerHolder
    {
        // 勝者のチームが格納されます。
        // リザルトの背景色の決定などに使用されます。
        // 注: この変数を変更する時、WinnerRoles・WinnerIdsを同時に変更しないと予期せぬ勝者が現れる可能性があります。
        public static CustomWinner WinnerTeam;
        // 追加勝利するプレイヤーの役職が格納されます。
        // リザルトの表示に使用されます。
        public static HashSet<CustomRoles> AdditionalWinnerRoles;
        // 勝者の役職が格納され、この変数に格納されている役職のプレイヤーは全員勝利となります。
        // チームとなるニュートラルの処理に最適です。
        public static HashSet<CustomRoles> WinnerRoles;
        // 勝者のPlayerIDが格納され、このIDを持つプレイヤーは全員勝利します。
        // 単独勝利するニュートラルの処理に最適です。
        public static HashSet<byte> WinnerIds;
        // 役職での単独勝利者PlayerIdが格納されます。
        // ここに登録されてもWinnerIdsに登録されないと勝利しません。
        public static HashSet<byte> NeutralWinnerIds;
        // 問答無用で敗北するPlayerIDが格納されます。
        // 敗者リストは勝者リスト・勝者チームのリストより優先されます。
        public static HashSet<byte> CantWinPlayerIds;
        // 勝利優先順位の影響で勝利した陣営を含む、全勝利陣営が格納されます（ログ用）。
        public static HashSet<CustomWinner> winners;

        [GameModuleInitializer, PluginModuleInitializer]
        public static void Reset()
        {
            WinnerTeam = CustomWinner.Default;
            AdditionalWinnerRoles = new();
            WinnerRoles = new();
            WinnerIds = new();
            NeutralWinnerIds = new();
            CantWinPlayerIds = new();
            winners = new();
        }
        public static void ClearWinners()
        {
            WinnerRoles.Clear();
            WinnerIds.Clear();
            NeutralWinnerIds.Clear();
            CantWinPlayerIds.Clear();
            winners.Clear();
        }
        /// <summary><para>WinnerTeamに値を代入します。</para><para>すでに代入されている場合、AdditionalWinnerRolesに追加します。</para></summary>
        public static void SetWinnerOrAdditonalWinner(CustomWinner winner)
        {
            if (WinnerTeam == CustomWinner.Default)
            {
                WinnerTeam = winner;
                winners.Add(winner);
            }
            else AdditionalWinnerRoles.Add((CustomRoles)winner);
        }
        /// <summary><para>WinnerTeamに値を代入します。</para><para>すでに代入されている場合、既存の値をAdditionalWinnerRolesに追加してから代入します。</para></summary>
        public static void ShiftWinnerAndSetWinner(CustomWinner winner)
        {
            if (WinnerTeam != CustomWinner.Default)
                AdditionalWinnerRoles.Add((CustomRoles)WinnerTeam);
            WinnerTeam = winner;
            winners.Add(winner);
        }
        /// <summary><para>既存の値をすべて削除してから、WinnerTeamに値を代入します。</para></summary>
        public static void ResetAndSetWinner(CustomWinner winner)
        {
            Reset();
            WinnerTeam = winner;
            winners.Add(winner);
        }

    }
}
