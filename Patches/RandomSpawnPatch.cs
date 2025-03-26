using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHostY.Roles.Core;

namespace TownOfHostY
{
    class RandomSpawn
    {
        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo), typeof(Vector2), typeof(ushort))]
        public class CustomNetworkTransformPatch
        {
            public static Dictionary<byte, bool> FirstTP = new();
            public static void Postfix(CustomNetworkTransform __instance, [HarmonyArgument(0)] Vector2 position)
            {
                if (!AmongUsClient.Instance.AmHost) return;
                if (position == new Vector2(-25f, 40f)) return; //最初の湧き地点ならreturn
                if (GameStates.IsInTask)
                {
                    var player = Main.AllPlayerControls.Where(p => p.NetTransform == __instance).FirstOrDefault();
                    if (player == null)
                    {
                        Logger.Warn("プレイヤーがnullです", "RandomSpawn");
                        return;
                    }
                    if (player.Is(CustomRoles.GM)) return; //GMは対象外に
                    if (FirstTP[player.PlayerId])
                    {
                        FirstTP[player.PlayerId] = false;
                        if (Main.NormalOptions.MapId != 4) return; //マップがエアシップじゃなかったらreturn
                        player.RpcResetAbilityCooldown();
                        if (Options.FixFirstKillCooldown.GetBool() && MeetingStates.FirstMeeting)
                            player.SetKillCooldown(Main.AllPlayerKillCooldown[player.PlayerId]);
                        if (Options.RandomSpawn_Airship.GetBool()) //ランダムスポーン
                            new AirshipSpawnMap().RandomTeleport(player);
                    }
                }
            }
        }
        public static void TP(CustomNetworkTransform nt, Vector2 location)
        {
            //nt.SnapTo(location);

            //Modded
            var playerLastSequenceId = nt.lastSequenceId + 8;
            nt.SnapTo(location, (ushort)playerLastSequenceId);

            //Vanilla
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable);
            NetHelpers.WriteVector2(location, writer);
            writer.Write(nt.lastSequenceId + 10U);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public abstract class SpawnMap
        {
            public virtual void RandomTeleport(PlayerControl player)
            {
                var location = GetLocation();
                Logger.Info($"{player.Data.PlayerName}:{location}", "RandomSpawn");
                TP(player.NetTransform, location);
            }
            public abstract Vector2 GetLocation();
        }

        public class SkeldSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["Weapons"] = new(9.3f, 1.0f),
                ["Admin"] = new(4.5f, -7.9f),
                ["MedBay"] = new(-9.0f, -4.0f),
                ["Cafeteria"] = new(-1.0f, 3.0f),
                ["Navigation"] = new(16.5f, -4.8f),
                ["Storage"] = new(-1.5f, -15.5f),
                ["UpperEngine"] = new(-17.0f, -1.3f),
                ["LowerEngine"] = new(-17.0f, -13.5f),
                ["O2"] = new(6.5f, -3.8f),
                ["Shields"] = new(9.3f, -12.3f),
                ["Communications"] = new(4.0f, -15.5f),
                ["Electrical"] = new(-7.5f, -8.8f),
                ["Security"] = new(-13.5f, -5.5f),
                ["Reactor"] = new(-20.5f, -5.5f)
            };
            public override Vector2 GetLocation()
            {
                if (Options.DisableNearButton_Skeld.GetBool())
                {
                    return Options.AdditionalSpawn_Skeld.GetBool()
                        ? positions.ToArray()[4..].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                        : positions.ToArray()[4..8].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
                else
                {
                    return Options.AdditionalSpawn_Skeld.GetBool()
                        ? positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                        : positions.ToArray()[3..8].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
            }
        }
        public class MiraHQSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["Balcony"] = new(24.0f, -2.0f),
                ["Storage"] = new(19.5f, 4.0f),
                ["Cafeteria"] = new(25.5f, 2.0f),
                ["MedBay"] = new(15.5f, -0.5f),
                ["Reactor"] = new(2.5f, 10.5f),
                ["Launchpad"] = new(-4.5f, 2.0f),
                ["Admin"] = new(21.0f, 17.5f),
                ["ThreeWay"] = new(17.8f, 11.5f),
                ["Communications"] = new(15.3f, 3.8f),
                ["LockerRoom"] = new(9.0f, 1.0f),
                ["Decontamination"] = new(6.1f, 6.0f),
                ["Laboratory"] = new(9.5f, 12.0f),
                ["Office"] = new(15.0f, 19.0f),
                ["Greenhouse"] = new(17.8f, 23.0f)
            };
            public override Vector2 GetLocation()
            {
                if (Options.DisableNearButton_MiraHQ.GetBool())
                {
                    return Options.AdditionalSpawn_MiraHQ.GetBool()
                    ? positions.ToArray()[3..].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : positions.ToArray()[3..7].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
                else
                {
                    return Options.AdditionalSpawn_MiraHQ.GetBool()
                    ? positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : positions.ToArray()[2..7].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
            }
        }
        public class PolusSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["Admin"] = new(24.0f, -22.5f),
                ["Office2"] = new(26.0f, -17.0f),
                ["Office1"] = new(19.5f, -18.0f),
                ["Dropship"] = new(16.7f, -3.0f),
                ["Security"] = new(3.0f, -12.0f),
                ["O2"] = new(2.0f, -17.5f),
                ["Weapons"] = new(12.0f, -23.5f),
                ["Laboratory"] = new(36.5f, -7.5f),
                ["Communications"] = new(12.5f, -16.0f),
                ["BoilerRoom"] = new(2.3f, -24.0f),
                ["Electrical"] = new(9.5f, -12.5f),
                ["Storage"] = new(20.5f, -12.0f),
                ["Rocket"] = new(26.7f, -8.5f),
                ["Toilet"] = new(34.0f, -10.0f),
                ["SpecimenRoom"] = new(36.5f, -22.0f)
            };
            public override Vector2 GetLocation()
            {
                if (Options.DisableNearButton_Polus.GetBool())
                {
                    return Options.AdditionalSpawn_Polus.GetBool()
                    ? positions.ToArray()[3..].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : positions.ToArray()[3..8].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
                else
                {
                    return Options.AdditionalSpawn_Polus.GetBool()
                    ? positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : positions.ToArray()[2..8].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
            }
        }
        public class AirshipSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["MeetingRoom"] = new(17.1f, 14.9f),
                ["GapRoom"] = new(12.0f, 8.5f),
                ["Brig"] = new(-0.7f, 8.5f),
                ["Engine"] = new(-0.7f, -1.0f),
                ["Kitchen"] = new(-7.0f, -11.5f),
                ["CargoBay"] = new(33.5f, -1.5f),
                ["Records"] = new(20.0f, 10.5f),
                ["MainHall"] = new(15.5f, 0.0f),
                ["Cockpit"] = new(-23.5f, -1.6f),
                ["Security"] = new(5.8f, -10.8f),
                ["Medical"] = new(29.0f, -6.2f),
                ["NapRoom"] = new(6.3f, 2.5f),
                ["Vault"] = new(-8.9f, 12.2f),
                ["Communications"] = new(-13.3f, 1.3f),
                ["Armory"] = new(-10.3f, -5.9f),
                ["ViewingDeck"] = new(-13.7f, -12.6f),
                ["Electrical"] = new(16.3f, -8.8f),
                ["Toilet"] = new(30.9f, 6.8f),
                ["Showers"] = new(21.2f, -0.8f)
            };
            public override Vector2 GetLocation()
            {
                if (Options.DisableNearButton_Airship.GetBool())
                {
                    return Options.AdditionalSpawn_Airship.GetBool()
                    ? positions.ToArray()[3..].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : Options.AdditionalSpawn_AirshipTAKADA.GetBool()
                    ? positions.ToArray()[3..11].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : positions.ToArray()[3..8].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
                else
                {
                    return Options.AdditionalSpawn_Airship.GetBool()
                    ? positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : Options.AdditionalSpawn_AirshipTAKADA.GetBool()
                    ? positions.ToArray()[2..11].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : positions.ToArray()[2..8].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
            }
        }
        public class FungleSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["Bonfire"] = new(-7.4f, 1.4f),
                ["MeetingRoom"] = new(-4.0f, -0.9f),
                ["TheDorm"] = new(2.6f, -1.7f),
                ["SplashZone"] = new(-14.7f, -0.9f),
                ["Dropship"] = new(-8.0f, 9.8f),
                ["Greenhouse"] = new(9.4f, -12.2f),
                ["Communications"] = new(21.5f, 13.4f),
                ["Lookout"] = new(9.1f, 3.6f),
                ["Cafeteria"] = new(-16.4f, 4.6f),
                ["Kitchen"] = new(-15.4f, -7.8f),
                ["Strage"] = new(1.1f, 3.9f),
                ["Laboratory"] = new(-4.2f, -8.8f),
                ["Reactor"] = new(22.0f, -7.7f),
                ["MiningPit"] = new(12.5f, 9.5f),
                ["UpperEngine"] = new(21.7f, 2.6f)
            };
            public override Vector2 GetLocation()
            {
                if (Options.DisableNearButton_Fungle.GetBool())
                {
                    return Options.AdditionalSpawn_Fungle.GetBool()
                    ? positions.ToArray()[3..].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : positions.ToArray()[3..8].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
                else
                {
                    return Options.AdditionalSpawn_Fungle.GetBool()
                    ? positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
                    : positions.ToArray()[2..8].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                }
            }
        }
    }
}