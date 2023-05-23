using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TownOfHost
{
    public static class ONDeadTargetArrow
    {
        public static Dictionary<byte, bool> IsComplete = new();
        private static HashSet<byte> CompleteSeerList = new();
        private static HashSet<DeadBody> DeadBodyList = new();
        static Dictionary<ArrowInfo, string> TargetArrows = new();

        public static void Init()
        {
            IsComplete.Clear();
            CompleteSeerList.Clear();
            DeadBodyList.Clear();
            TargetArrows.Clear();
        }
        public static void Add(byte playerId)
        {
            IsComplete[playerId] = false;
        }

        //タスクが終わった時、そのプレイヤーを見れるリストに追加
        public static void CheckTask(PlayerControl seer)
        {
            if (!Options.CurrentGameMode.IsOneNightMode()) return;
            if (!seer.IsAlive()) return;

            var seerId = seer.PlayerId;
            var seerTask = seer.GetPlayerTaskState();
            if (IsComplete[seerId] || !seerTask.IsTaskFinished) return;
            
            CompleteSeerList.Add(seerId);

            IsComplete[seerId] = true;

            if (DeadBodyList.Count != 0)
            {
                foreach (var target in DeadBodyList)
                {
                    TargetArrowAdd(seerId, target);
                }
            }
        }

        //死体が生まれた時、発見される側の死体リストに追加
        public static void UpdateDeadBody()
        {
            if (!Options.CurrentGameMode.IsOneNightMode()) return;

            DeadBody[] AllBody = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            DeadBody targetBody = null;

            foreach (var body in AllBody)
            {
                if (!DeadBodyList.Contains(body))
                {
                    DeadBodyList.Add(body);
                    targetBody = body;
                    break;
                }
            }

            if (CompleteSeerList.Count != 0)
            {
                foreach (var seerId in CompleteSeerList)
                {
                    TargetArrowAdd(seerId, targetBody);
                }
            }
        }

        /// <summary>
        /// タスク終了プレイヤーから死体への矢印
        /// </summary>
        public static string GetDeadBodiesArrow(PlayerControl seer,PlayerControl target)
        {
            if (!Options.CurrentGameMode.IsOneNightMode()) return "";

            if (GameStates.IsMeeting) return "";
            if (seer != target) return "";
            var arrows = "";
            foreach (var targetBody in DeadBodyList)
            {
                var arrow = TargetArrowGetArrows(seer, targetBody);
                arrows += Utils.ColorString(Utils.GetRoleColor(CustomRoles.ONWerewolf), arrow);
            }
            return arrows;
        }

        class ArrowInfo
        {
            public byte From;
            public DeadBody To;
            public ArrowInfo(byte from, DeadBody to)
            {
                From = from;
                To = to;
            }
            public bool Equals(ArrowInfo obj)
            {
                return From == obj.From && To == obj.To;
            }
            public override string ToString()
            {
                return $"(From:{From} To:{To})";
            }
        }

        static readonly string[] Arrows = {
            "↑",
            "↗",
            "→",
            "↘",
            "↓",
            "↙",
            "←",
            "↖",
            "・"
        };

        /// <summary>
        /// 新たにターゲット矢印対象を登録
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        /// <param name="coloredArrow"></param>
        public static void TargetArrowAdd(byte seer, DeadBody target)
        {
            var arrowInfo = new ArrowInfo(seer, target);
            if (!TargetArrows.Any(a => a.Key.Equals(arrowInfo)))
                TargetArrows[arrowInfo] = "・";
        }
        /// <summary>
        /// ターゲットの削除
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        public static void TargetArrowRemove(byte seer, DeadBody target)
        {
            var arrowInfo = new ArrowInfo(seer, target);
            var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.Equals(arrowInfo)));
            foreach (var a in removeList)
            {
                TargetArrows.Remove(a);
            }
        }
        /// <summary>
        /// タイプの同じターゲットの全削除
        /// </summary>
        /// <param name="seer"></param>
        public static void TargetArrowRemoveAllTarget(byte seer)
        {
            var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.From == seer));
            foreach (var arrowInfo in removeList)
            {
                TargetArrows.Remove(arrowInfo);
            }
        }
        /// <summary>
        /// 見ることのできるすべてのターゲット矢印を取得
        /// </summary>
        /// <param name="seer"></param>
        /// <returns></returns>
        public static string TargetArrowGetArrows(PlayerControl seer, params DeadBody[] targets)
        {
            var arrows = "";
            foreach (var arrowInfo in TargetArrows.Keys.Where(ai => ai.From == seer.PlayerId && targets.Contains(ai.To)))
            {
                arrows += TargetArrows[arrowInfo];
            }
            return arrows;
        }
        /// <summary>
        /// FixedUpdate毎にターゲット矢印を確認
        /// 更新があったらNotifyRolesを発行
        /// </summary>
        /// <param name="seer"></param>
        public static void OnFixedUpdate(PlayerControl seer)
        {
            if (!GameStates.IsInTask) return;

            var seerId = seer.PlayerId;
            var seerIsDead = !seer.IsAlive();

            var arrowList = new List<ArrowInfo>(TargetArrows.Keys.Where(a => a.From == seer.PlayerId));
            if (arrowList.Count == 0) return;

            var update = false;
            foreach (var arrowInfo in arrowList)
            {
                var target = arrowInfo.To;
                if (seerIsDead || target == null)
                {
                    TargetArrows.Remove(arrowInfo);
                    update = true;
                    continue;
                }
                //対象の方角ベクトルを取る
                var dir = target.transform.position - seer.transform.position;
                int index;
                if (dir.magnitude < 2)
                {
                    //近い時はドット表示
                    index = 8;
                }
                else
                {
                    //-22.5～22.5度を0とするindexに変換
                    // 下が0度、左側が+180まで右側が-180まで
                    // 180度足すことで上が0度の時計回り
                    // 45度単位のindexにするため45/2を加算
                    var angle = Vector3.SignedAngle(Vector3.down, dir, Vector3.back) + 180 + 22.5;
                    index = ((int)(angle / 45)) % 8;
                }
                var arrow = Arrows[index];
                if (TargetArrows[arrowInfo] != arrow)
                {
                    TargetArrows[arrowInfo] = arrow;
                    update = true;
                }
            }
            if (update)
            {
                Utils.NotifyRoles(SpecifySeer: seer);
            }
        }
    }
}
