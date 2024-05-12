using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Class;

namespace TownOfHostY.Roles.Crewmate;
public sealed class NiceGuesser : VoteGuesser
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(NiceGuesser),
            player => new NiceGuesser(player),
            CustomRoles.NiceGuesser,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1800,
            SetupOptionItem,
            "ナイスゲッサー",
            "#ffff00"
        );
    public NiceGuesser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NumOfGuess = OptionNumOfGuess.GetInt();
        MultipleInMeeting = OptionMultipleInMeeting.GetBool();
        HideMisfire = OptionHideMisfire.GetBool();
        GuessAfterVote = OptionGuessAfterVote.GetBool();
    }
    private static OptionItem OptionNumOfGuess;
    private static OptionItem OptionMultipleInMeeting;
    private static OptionItem OptionHideMisfire;
    private static OptionItem OptionGuessAfterVote;
    enum OptionName
    {
        GuesserNumOfGuess,
        GuesserMultipleInMeeting,
        GuesserHideMisfire,
        GuesserGuessAfterVote,
    }
    public static void SetupOptionItem()
    {
        OptionNumOfGuess = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuesserNumOfGuess, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionMultipleInMeeting = BooleanOptionItem.Create(RoleInfo, 11, OptionName.GuesserMultipleInMeeting, false, false);
        OptionHideMisfire = BooleanOptionItem.Create(RoleInfo, 12, OptionName.GuesserHideMisfire, false, false);
        OptionGuessAfterVote = BooleanOptionItem.Create(RoleInfo, 13, OptionName.GuesserGuessAfterVote, false, false);
    }
}