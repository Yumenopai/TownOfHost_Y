using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Csv;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TownOfHostY.Attributes;

namespace TownOfHostY
{
    public static class Translator
    {
        public static Dictionary<string, Dictionary<int, string>> translateMaps;
        public const string LANGUAGE_FOLDER_NAME = "Language";

        [PluginModuleInitializer]
        public static void Init()
        {
            Logger.Info("Language Dictionary Initialize...", "Translator");
            LoadLangs();
            Logger.Info("Language Dictionary Initialize Finished", "Translator");
        }
        public static void LoadLangs()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("TownOfHost_Y.Resources.string.csv");
            translateMaps = new Dictionary<string, Dictionary<int, string>>();

            var options = new CsvOptions()
            {
                HeaderMode = HeaderMode.HeaderPresent,
                AllowNewLineInEnclosedFieldValues = false,
            };
            foreach (var line in CsvReader.ReadFromStream(stream, options))
            {
                if (line.Values[0][0] == '#') continue;
                try
                {
                    Dictionary<int, string> dic = new();
                    for (int i = 1; i < line.ColumnCount; i++)
                    {
                        int id = int.Parse(line.Headers[i]);
                        dic[id] = line.Values[i].Replace("\\n", "\n").Replace("\\r", "\r");
                    }
                    if (!translateMaps.TryAdd(line.Values[0], dic))
                        Logger.Warn($"翻訳用CSVに重複があります。{line.Index}行目: \"{line.Values[0]}\"", "Translator");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.ToString(), "Translator");
                }
            }

            // カスタム翻訳ファイルの読み込み
            if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);

            // 翻訳テンプレートの作成
            CreateTemplateFile();
            foreach (var lang in EnumHelper.GetAllValues<SupportedLangs>())
            {
                if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/{lang}.dat"))
                    LoadCustomTranslation($"{lang}.dat", lang);
            }
        }

        public static string GetString(string s, Dictionary<string, string> replacementDic = null)
        {
            var langId = TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.English;
            if (Main.ForceJapanese.Value) langId = SupportedLangs.Japanese;
            string str = GetString(s, langId);
            if (replacementDic != null)
                foreach (var rd in replacementDic)
                {
                    str = str.Replace(rd.Key, rd.Value);
                }
            return str;
        }

        public static string GetString(string str, SupportedLangs langId)
        {
            var res = $"<INVALID:{str}>";
            if (translateMaps.TryGetValue(str, out var dic) && (!dic.TryGetValue((int)langId, out res) || res == "")) //strに該当する&無効なlangIdかresが空
            {
                res = $"*{dic[0]}";
            }
            if (langId == SupportedLangs.Japanese)
            {
                if (Main.IsValentine || Main.IsWhiteDay)
                {
                    res = str switch
                    {
                        "Bakery" => "チョコレート屋",
                        "NBakery" => "(第三)覚醒チョコ屋",
                        "BakeryInfo" => "みんなにチョコレートを配ろう",
                        "BakeryInfoLong" => "[クルー陣営]\n生存中、会議はじめにチョコレート屋についてのコメントが流れる。レアコメントあり。\n低確率(設定)で試合途中、第三陣営に変化する。",

                        "PanAliveMessageTitle" => "【チョコ屋生存中】",
                        "PanAlive" =>  "\nチョコレート屋が誰かにチョコを渡しました。\nㅤ",
                        "PanAlive1" => "\nチョコレート屋はチョコを溶かすのに夢中に。\nㅤ",
                        "PanAlive2" => Main.IsValentine ? "\nチョコレート屋はバレンタインで大繁盛。\nㅤ" : "\nチョコレート屋はホワイトデーで大繁盛。\nㅤ",
                        "PanAlive3" => "\nチョコレート屋が板チョコ落として割った。\nㅤ",
                        "PanAlive4" => "\nチョコレート屋はちょこっとおっちょこちょい\nㅤ",
                        "PanAlive5" => Main.IsValentine ? "\nチョコレート屋はバレンタインで大繁盛。\nㅤ" : "\nチョコレート屋はホワイトデーで大繁盛。\nㅤ",
                        "PanAlive6" => Main.IsValentine ? "\nチョコレート屋はバレンタインで大繁盛。\nㅤ" : "\nチョコレート屋はホワイトデーで大繁盛。\nㅤ",
                        "PanAlive7" => "\nﾊｲﾊﾟｰﾁｮｺﾚｰﾄﾄﾘﾌﾟﾙﾝﾙﾝﾗｯｷｰﾊﾋﾟﾈｽ!!!!\nㅤ",
                        "PanAlive8" => "\nチョコレート屋が板チョコ落として割った。\nㅤ",
                        "PanAlive9" => "\nチョコレート屋はチョコを溶かすのに夢中に。\nㅤ",
                        "PanAlive10" => "\nチョコレート屋はちょこっとおっちょこちょい\nㅤ",
                        "PanAlive11" => "\nチョコレート屋は新商品の開発に熱が入る。\nㅤ",
                        "PanAlive12" => "\nチョコレート屋は新商品の開発に熱が入る。\nㅤ",
                        "PanAlive13" => "\nチョコレート屋はチョコを溶かすのに夢中に。\nㅤ",
                        "PanAlive14" => Main.IsValentine ? "\nチョコレート屋はバレンタインで大繁盛。\nㅤ" : "\nチョコレート屋はホワイトデーで大繁盛。\nㅤ",
                        "PanAlive15" => "\nチョコレート屋が板チョコ落として割った。\nㅤ",
                        "PanAlive16" => "\nチョコレート屋はちょこっとおっちょこちょい\nㅤ",
                        "PanAlive17" => Main.IsValentine ? "\nチョコレート屋はバレンタインで大繁盛。\nㅤ" : "\nチョコレート屋はホワイトデーで大繁盛。\nㅤ",
                        "PanAlive18" => Main.IsValentine ? "\nチョコレート屋はバレンタインで大繁盛。\nㅤ" : "\nチョコレート屋はホワイトデーで大繁盛。\nㅤ",
                        "PanAlive19" => "\nﾊｲﾊﾟｰﾁｮｺﾚｰﾄﾄﾘﾌﾟﾙﾝﾙﾝﾗｯｷｰﾊﾋﾟﾈｽ!!!!\nㅤ",
                        "PanAlive20" => "\nチョコレート屋が板チョコ落として割った。\nㅤ",
                        "PanAlive21" => "\nチョコレート屋はチョコを溶かすのに夢中に。\nㅤ",
                        "PanAlive22" => "\nチョコレート屋はちょこっとおっちょこちょい\nㅤ",
                        "PanAlive23" => "\nチョコレート屋は新商品の開発に熱が入る。\nㅤ",
                        "PanAlive24" => "\nチョコレート屋も恋するんだよ、、？\nㅤ大好きな{0}に、本命チョコを。\nㅤ",
                        "PanAlive25" => "\nチョコレート屋も恋するんだよ、、？\nㅤ大好きな{0}に、本命チョコを。\nㅤ",

                        "BakeryChange" => "\nチョコレート屋が覚醒し、ㅤㅤㅤㅤㅤㅤㅤ\nㅤ毒入りチョコレートを開発した。\n次ターン以降、覚醒チョコ屋を追放しないと\nㅤ貰った人は毒が回って死亡してしまう。\nㅤ",
                        "BakeryChangeNow" => "\nチョコレート屋が覚醒し、ㅤㅤㅤㅤㅤㅤㅤ\nㅤ毒入りチョコレートを渡した。\n覚醒チョコ屋を追放しないと\nㅤ貰った人は毒が回って死亡してしまう。\nㅤ",
                        "BakeryChangeNONE" => "\nチョコレート屋は覚醒したにも関わらず\nこのターンの毒入りチョコ作りに失敗した。\nㅤ",

                        _ => res
                    };
                }
                else if (Main.IsInitialRelease)
                {
                    res = str switch
                    {
                        "Bakery" => "おにぎり屋",
                        "NBakery" => "(第三)覚醒おにぎり屋",
                        "BakeryInfo" => "みんなにおにぎりを配ろう",
                        "BakeryInfoLong" => "[クルー陣営]\n生存中、会議はじめにおにぎり屋についてのコメントが流れる。レアコメントあり。\n低確率(設定)で試合途中、第三陣営に変化する。",
                        "DeathReason.Poisoning" => "激辛死",

                        "PanAliveMessageTitle" => "【おにぎり屋生存中】",
                        "PanAlive"  => "\nおにぎり屋が誰かにおにぎりを渡しました。\nㅤ",
                        "PanAlive1" => "\nおにぎり屋はおにぎりを握るのに夢中に。\nㅤ",
                        "PanAlive2" => "\nおにぎり屋は梅入りおにぎりを渡しました。\nㅤ",
                        "PanAlive3" => "\nおにぎり屋はツナマヨにぎりを渡しました。\nㅤ",
                        "PanAlive4" => "\nおにぎり屋は鮭入りおにぎりを渡しました。\nㅤ",
                        "PanAlive5" => "\nおにぎり屋は昆布おにぎりを渡しました。\nㅤ",
                        "PanAlive6" => "\nおにぎり屋はエビマヨにぎりを渡しました。\nㅤ",
                        "PanAlive7" => "\nﾊｲﾊﾟｰｵﾆｷﾞﾘﾔｻﾝﾄﾘﾌﾟﾙﾝﾙﾝﾗｯｷｰﾊﾋﾟﾈｽ!!!!\nㅤ",
                        "PanAlive8" => "\nおにぎり屋はたらこおにぎりを渡しました。\nㅤ",
                        "PanAlive9" => "\nおにぎり屋は誰かに塩にぎりを渡しました。\nㅤ",
                        "PanAlive10" => "\nおにぎり屋はいくらおにぎりを渡しました。\nㅤ",
                        "PanAlive11" => "\nおにぎり屋は明太子おにぎりを渡しました。\nㅤ",
                        "PanAlive12" => "\nおにぎり屋はおかかおにぎりを渡しました。\nㅤ",
                        "PanAlive13" => "\nおにぎり屋は鶏唐おにぎりを渡しました。\nㅤ",
                        "PanAlive14" => "\nおにぎり屋は焼肉おにぎりを渡しました。\nㅤ",
                        "PanAlive15" => "\nおにぎり屋はねぎとろにぎりを渡しました。\nㅤ",
                        "PanAlive16" => "\nおにぎり屋はしらすおにぎりを渡しました。\nㅤ",
                        "PanAlive17" => "\nﾊｲﾊﾟｰｵﾆｷﾞﾘﾔｻﾝﾄﾘﾌﾟﾙﾝﾙﾝﾗｯｷｰﾊﾋﾟﾈｽ!!!!\nㅤ",
                        "PanAlive18" => "\nおにぎり屋はのり佃煮にぎりを渡しました。\nㅤ",
                        "PanAlive19" => "\nおにぎり屋はエビ天にぎりを渡しました。\nㅤ",
                        "PanAlive20" => "\nおにぎり屋は高菜おにぎりを渡しました。\nㅤ",
                        "PanAlive21" => "\nおにぎり屋は角煮おにぎりを渡しました。\nㅤ",
                        "PanAlive22" => "\nおにぎり屋はおにぎりよりおむすび派。\nㅤ",
                        "PanAlive23" => "\nおにぎり屋はおにぎりよりお寿司派。\nㅤ",
                        "PanAlive24" => "\nおにぎり屋は{0}におにぎり投げた。\nㅤ",
                        "PanAlive25" => "\nおにぎり屋は{0}におにぎり投げた。\nㅤ",

                        "BakeryChange" => "\nおにぎり屋が覚醒し、ㅤㅤㅤㅤㅤㅤㅤ\nㅤ激辛おにぎりを開発した。\n次ターン以降、覚醒おにぎり屋を追放しないと\nㅤ貰った人はあまりの辛さに死亡してしまう。\nㅤ",
                        "BakeryChangeNow" => "\nおにぎり屋が覚醒し、ㅤㅤㅤㅤㅤㅤㅤ\nㅤ激辛おにぎりを渡した。\n覚醒おにぎり屋を追放しないと\nㅤ貰った人はあまりの辛さに死亡してしまう。\nㅤ",
                        "BakeryChangeNONE" => "\nおにぎり屋は覚醒したにも関わらず\nこのターンの激辛おにぎり作りに失敗した。\nㅤ",

                        _ => res
                    };
                }
            }
            if (!translateMaps.ContainsKey(str)) //translateMapsにない場合、StringNamesにあれば取得する
            {
                var stringNames = EnumHelper.GetAllValues<StringNames>().Where(x => x.ToString() == str);
                if (stringNames != null && stringNames.Any())
                    res = GetString(stringNames.FirstOrDefault());
            }
            return res;
        }
        public static string GetString(StringNames stringName)
            => DestroyableSingleton<TranslationController>.Instance.GetString(stringName, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        public static string GetRoleString(string str)
        {
            var CurrentLanguage = TranslationController.Instance.currentLanguage.languageID;
            var lang = CurrentLanguage;
            if (Main.ForceJapanese.Value && Main.JapaneseRoleName.Value)
                lang = SupportedLangs.Japanese;
            else if (CurrentLanguage == SupportedLangs.Japanese && !Main.JapaneseRoleName.Value)
                lang = SupportedLangs.English;

            return GetString(str, lang);
        }
        public static void LoadCustomTranslation(string filename, SupportedLangs lang)
        {
            string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
            if (File.Exists(path))
            {
                Logger.Info($"カスタム翻訳ファイル「{filename}」を読み込み", "LoadCustomTranslation");
                using StreamReader sr = new(path, Encoding.GetEncoding("UTF-8"));
                string text;
                string[] tmp = Array.Empty<string>();
                while ((text = sr.ReadLine()) != null)
                {
                    tmp = text.Split(":");
                    if (tmp.Length > 1 && tmp[1] != "")
                    {
                        try
                        {
                            translateMaps[tmp[0]][(int)lang] = tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n").Replace("\\r", "\r");
                        }
                        catch (KeyNotFoundException)
                        {
                            Logger.Warn($"「{tmp[0]}」は有効なキーではありません。", "LoadCustomTranslation");
                        }
                    }
                }
            }
            else
            {
                Logger.Error($"カスタム翻訳ファイル「{filename}」が見つかりませんでした", "LoadCustomTranslation");
            }
        }

        private static void CreateTemplateFile()
        {
            var sb = new StringBuilder();
            foreach (var title in translateMaps) sb.Append($"{title.Key}:\n");
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template.dat", sb.ToString());
            sb.Clear();
            foreach (var title in translateMaps) sb.Append($"{title.Key}:{title.Value[0].Replace("\n", "\\n").Replace("\r", "\\r")}\n");
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template_English.dat", sb.ToString());
        }
        public static void ExportCustomTranslation()
        {
            LoadLangs();
            var sb = new StringBuilder();
            var lang = TranslationController.Instance.currentLanguage.languageID;
            foreach (var title in translateMaps)
            {
                if (!title.Value.TryGetValue((int)lang, out var text)) text = "";
                sb.Append($"{title.Key}:{text.Replace("\n", "\\n").Replace("\r", "\\r")}\n");
            }
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/export_{lang}.dat", sb.ToString());
        }
    }
}