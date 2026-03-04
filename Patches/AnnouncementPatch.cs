using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data.Player;
using Assets.InnerNet;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace TownOfHostY;

[HarmonyPatch]
public class ModNews
{
    public static List<ModNews> AllModNews = new();
    public static List<ModNews> JsonAndAllModNews = new();

    public int Number;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public ModNews(int number, string title, string subtitle, string shortTitle, string text, string date)
    {
        Number = number;
        Title = title;
        SubTitle = subtitle;
        ShortTitle = shortTitle;
        Text = text;
        Date = date;
    }

    public ModNews() { } // Init() でオブジェクト初期化する用

    public Announcement ToAnnouncement()
    {
        return new Announcement
        {
            Number = Number,
            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Language = (uint)AmongUs.Data.DataManager.Settings.Language.CurrentLanguage,
            Date = Date,
            Id = "ModNews"
        };
    }

   
    public const string ModNewsURL =
        "https://raw.githubusercontent.com/Yumenopai/TownOfHost_Y/main/modNews.json";

    static bool downloaded = false;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    public static void StartPostfix(MainMenuManager __instance)
    {
        static IEnumerator FetchModNews()
        {
            if (downloaded)
                yield break;

            downloaded = true;
            var request = UnityWebRequest.Get(ModNewsURL);
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                downloaded = false;
                Logger.Info("ModNews Error Fetch:" + request.responseCode.ToString(), "ModNews");
                yield break;
            }

            var json = JObject.Parse(request.downloadHandler.text);
            for (var news = json["News"].First; news != null; news = news.Next)
            {
                var n = new ModNews(
                    int.Parse(news["Number"].ToString()),
                    news["Title"]?.ToString(),
                    news["Subtitle"]?.ToString(),
                    news["Short"]?.ToString(),
                    news["Body"]?.ToString(),
                    news["Date"]?.ToString()
                );
                JsonAndAllModNews.Add(n);
            }
        }

        __instance.StartCoroutine(FetchModNews().WrapToIl2Cpp());
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance,
        [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        if (AllModNews.Count < 1)
        {
            
            AllModNews.Do(n => JsonAndAllModNews.Add(n));
            JsonAndAllModNews.Sort((a1, a2) =>
                DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)));
        }

        List<Announcement> FinalAllNews = new();
        JsonAndAllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));

        foreach (var news in aRange)
        {
            if (!JsonAndAllModNews.Any(x => x.Number == news.Number))
                FinalAllNews.Add(news);
        }

        FinalAllNews.Sort((a1, a2) =>
            DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)));

        aRange = new(FinalAllNews.Count);
        for (int i = 0; i < FinalAllNews.Count; i++)
            aRange[i] = FinalAllNews[i];

        return true;
    }
}
