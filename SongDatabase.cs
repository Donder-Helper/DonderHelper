using Hnx8.ReadJEnc;
using HtmlAgilityPack;
using System.Text;
using static DonderHelper.Song;
using Newtonsoft.Json;

namespace DonderHelper
{
    internal class TaikoKoTitle()
    {
        [JsonProperty("title")]
        public string title = "";
        [JsonProperty("titleKo")]
        public string? ko_title = null;
        [JsonProperty("titleEn")]
        public string? en_title = null;
        [JsonProperty("aliasEn")]
        public string? en_alias = null;
        [JsonProperty("aliasKo")]
        public string? ko_alias = null;
        [JsonProperty("romaji")]
        public string? romaji = null;
        [JsonProperty("songNo")]
        public string song_no = "";

        public struct Course
        {
            public string[]? images;
            public Course() { images = null; }
        }
        public struct Courses
        {
            [JsonProperty("easy")]
            public Course? easy;
            [JsonProperty("normal")]
            public Course? normal;
            [JsonProperty("hard")]
            public Course? hard;
            [JsonProperty("oni")]
            public Course? oni;
            [JsonProperty("ura")]
            public Course? ura;
            public Courses() { easy = null; normal = null; hard = null; oni = null; ura = null; }
        }

        [JsonProperty("courses")]
        public Courses courses;
    }

    public static class SongDatabase
    {
        public static Dictionary<string, Song> Songs { get; private set; } = [];
        public static Dictionary<string, string> SongNames { get; private set; } = [];

#if DEBUG
        // Updates songlist during Debug mode
        private static string songsfile = $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}songs.json";
#else
        private static string songsfile = $"Data{Path.DirectorySeparatorChar}songs.json";
#endif
        private static string gaidenspath = $"Data{Path.DirectorySeparatorChar}Gaidens";

        public static void FetchSongs()
        {
            Songs = JsonConvert.DeserializeObject<Dictionary<string, Song>>(File.ReadAllText(songsfile)) ?? [];
            foreach (var song in Songs)
            {
                foreach (var title in song.Value.TitleList) {
                    SongNames.TryAdd(title.Value, song.Key);
                }
            }

            foreach (string path in Directory.GetFiles(gaidenspath, "*.json").Order())
            {
                var gaiden = JsonConvert.DeserializeObject<Gaiden>(File.ReadAllText(path));
                if (gaiden != null) { 
                    GaidenSonglist.Gaidens.TryAdd(gaiden.Subtitle, gaiden);
                    foreach (var titles in gaiden.SubtitleList) GaidenSonglist.GaidenNames.TryAdd(titles.Value, gaiden.Subtitle);
                }
            }
            GaidenSonglist.Gaidens = GaidenSonglist.Gaidens.Reverse().ToDictionary();
        }
        public static string GetLocalizedSongTitle(string orig_title, string locale)
        {
            return Songs.TryGetValue(orig_title, out Song? song) ? song.GetTitle(locale) : orig_title;
        }

        // TJA path(s)
        private static string __tjapaths = $"Resources{Path.DirectorySeparatorChar}paths.txt";

        /*
         * All links below contain data to be compiled and used by Donder Helper.
         * You are responsible for manually grabbing this data yourself.
         * Please avoid automating this process through network requests, as this may put stress on some servers.
         */

        // https://docs.google.com/spreadsheets/d/1Piucd3Wv-QVQJ_yMQjC1xV08Cl2IXGze_8bf8nQZGjs/edit?gid=0#gid=0 - TSV
        private static string __regionpath = $"Resources{Path.DirectorySeparatorChar}region.tsv";

        // https://fumen-database.com/difficulty - Full page as HTML (TXT)
        private static string __fumenonipath = $"Resources{Path.DirectorySeparatorChar}fumendatabase-oni.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC - Full pages as HTML (TXT)
        private static string __taikofumenfolder = $"Resources{Path.DirectorySeparatorChar}taiko-fumen";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC%E3%82%A2%E3%82%B8%E3%82%A2%E7%89%88%28%E4%B8%AD%E5%9B%BD%E8%AA%9E%29 - Full pages as HTML (TXT)
        private static string __taikofumentwfolder = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-tw";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC%E4%B8%AD%E5%9B%BD%E7%89%88 - Full pages as HTML (TXT)
        private static string __taikofumencnfolder = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-cn";

        // https://docs.google.com/spreadsheets/d/1N9OBdkbwj51swS4jqhL6rTKv4looTQqLPWjAvD0lWog/edit?gid=1162123291#gid=1162123291 - TSV
        private static string __taikoenpath = $"Resources{Path.DirectorySeparatorChar}english.tsv";

        // https://github.com/taikowiki/taiko-song-database/blob/main/database.json - JSON
        private static string __taikokopath = $"Resources{Path.DirectorySeparatorChar}ko.json";

        /*
        * This god awful method will parse all the information it can find in the Resources folder and compile it into Data/songs.json.
        * Not all websites/spreadsheets/jsons are built equal, so a huge amount of code must be dedicated to carefully breaking down each file.
        */
        public static void BuildSonglist()
        {
            #region Temporary Methods
            Song CreateSongFromCSVString(string csv)
            {
                Song song = new Song();

                Availability GetAvailability(string text)
                {
                    switch (text.ToLower())
                    {
                        case "✔":
                            return Availability.Yes;
                        case "✘":
                            return Availability.No;
                        case "cp":
                            return Availability.Campaign;
                        case "no*":
                            return Availability.CampaignNo;
                        case "shop":
                            return Availability.Shop;
                        case "ai battle":
                            return Availability.AIBattle;
                        case "qr code":
                        case "qr":
                            return Availability.QRCode;
                        case "transfer":
                            return Availability.Transfer;
                        case "?":
                            return Availability.Unknown;
                        default:
                            return Availability.No;
                    }
                }
                SongGenre GetGenre(string text)
                {
                    switch (text)
                    {
                        case "PP": return SongGenre.Pop;
                        case "KD": return SongGenre.Kids;
                        case "AN": return SongGenre.Anime;
                        case "VC": return SongGenre.Vocaloid;
                        case "GM": return SongGenre.Game;
                        case "VA": return SongGenre.Variety;
                        case "CL": return SongGenre.Classical;
                        case "NO": return SongGenre.Namco;
                        default: return SongGenre.Unknown;
                    }
                }

                var split = csv.Split('\t');

                for (int i = 0; i < split.Length; i++)
                {
                    string result = split[i];
                    if (i == 0 && result == "CN")
                    {
                        song = new Song();
                        song.SetTitle(split[1].Substring(5).Trim());
                        song.SetPriorityGenre(GetGenre(split[1].Substring(1, 2)));
                        song.Region = new() { Japan = Availability.No, Asia = Availability.No, Oceania = Availability.No, UnitedStates = Availability.No, China = Availability.Yes };
                        break;
                    }

                    switch (i)
                    {
                        // Genre
                        case 0:
                        {
                            song.AddGenre(GetGenre(result));
                            break;
                        }
                        // Title
                        case 1:
                        {
                            if (result.StartsWith('"') && result.EndsWith('"'))
                                result = result.Trim('"');
                            song.SetTitle(result);
                            break;
                        }
                        // Japan
                        case 2:
                        {
                            song.Region.Japan = GetAvailability(result);
                            break;
                        }
                        // Core Asia
                        case 3:
                        {
                            song.Region.Asia = GetAvailability(result);
                            break;
                        }
                        // Oceania / Other Asia
                        case 4:
                        {
                            song.Region.Oceania = GetAvailability(result);
                            break;
                        }
                        // North America
                        case 5:
                        {
                            song.Region.UnitedStates = GetAvailability(result);
                            break;
                        }
                        // China
                        case 6:
                        {
                            song.Region.China = GetAvailability(result);
                            break;
                        }
                    }
                }
                return song;
            }
            string FixReplace(string value, bool normalize = true)
            {
                if (value.Contains('⑨')) return value.Replace('‐', '-').Replace('／', '/');
                if (normalize) return value.Replace('‐', '-').Replace('／', '/').Normalize(NormalizationForm.FormKC);
                return value.Replace('‐', '-').Replace('／', '/');
            }

            SongGenre GetGenre(string info)
            {
                switch (info)
                {
                    case "POP":
                    case "流行音樂":
                    case "流行音乐":
                    case "ポップス": return SongGenre.Pop;

                    case "卡通動晝音樂":
                    case "Anime":
                    case "动漫音乐":
                    case "アニメ": return SongGenre.Anime;

                    case "Kids'":
                    case "兒童音樂":
                    case "儿童音乐":
                    case "キッズ": return SongGenre.Kids;

                    case "VOCALOID™ Music":
                    case "博歌乐™音乐":
                    case "ボーカロイド™曲": return SongGenre.Vocaloid;

                    case "Game Music":
                    case "遊戯音樂":
                    case "游戏音乐":
                    case "ゲームミュージック": return SongGenre.Game;

                    case "Variety":
                    case "综合音樂":
                    case "综合音乐":
                    case "バラエティ": return SongGenre.Variety;

                    case "Classical":
                    case "古典音樂":
                    case "古典音乐":
                    case "クラシック": return SongGenre.Classical;

                    case "NAMCO Original":
                    case "NAMCO 原創音樂":
                    case "南梦宫原创音乐":
                    case "ナムコオリジナル": return SongGenre.Namco;

                    default: return SongGenre.Unknown;
                }
            }

            int GetLevel(string info)
            {
                if (info.Contains("★×10")) return 10;
                if (info.Contains("★×9")) return 9;
                if (info.Contains("★×8")) return 8;
                if (info.Contains("★×7")) return 7;
                if (info.Contains("★×6")) return 6;
                if (info.Contains("★×5")) return 5;
                if (info.Contains("★×4")) return 4;
                if (info.Contains("★×3")) return 3;
                if (info.Contains("★×2")) return 2;
                if (info.Contains("★×1")) return 1;
                return -1;
            }

            int[] AllIndexOf(string text, string item)
            {
                List<int> indexes = [];
                int offset = 0;
                while (text.IndexOf(item, StringComparison.InvariantCulture) > -1)
                {
                    indexes.Add(offset + text.IndexOf(item));
                    offset += text.IndexOf(item) + item.Length;
                    text = text.Substring(text.IndexOf(item) + item.Length);
                }
                //indexes.Reverse();
                return indexes.Count > 0 ? indexes.ToArray() : [-1];
            }
#endregion

            // Beginning finding all available songs
            List<HtmlNode> taiko_fumen_tables = [];
            foreach (string folder in Directory.GetFiles(__taikofumenfolder))
            {
                HtmlDocument html = new(); html.Load(folder);
                taiko_fumen_tables.AddRange(html.DocumentNode.Descendants("table"));
            }
            List<HtmlNode> taiko_fumen_tw_tables = [];
            foreach (string folder in Directory.GetFiles(__taikofumentwfolder))
            {
                HtmlDocument html = new(); html.Load(folder);
                taiko_fumen_tw_tables.AddRange(html.DocumentNode.Descendants("table"));
            }
            List<HtmlNode> taiko_fumen_cn_tables = [];
            foreach (string folder in Directory.GetFiles(__taikofumencnfolder))
            {
                HtmlDocument html = new(); html.Load(folder);
                taiko_fumen_cn_tables.AddRange(html.DocumentNode.Descendants("table"));
            }

            TaikoKoTitle[] taiko_ko = JsonConvert.DeserializeObject<TaikoKoTitle[]>(File.ReadAllText(__taikokopath)) ?? [];
            //taiko_ko = taiko_ko.Where(item => item.ko_title != null).ToArray();

            Console.WriteLine("Reading fumen data...");
            #region Songlist (Japanese)
            foreach (var table in taiko_fumen_tables)
            {
                if (table.Descendants("thead").Count() == 0) continue;
                if (table.Descendants("th").Count() == 0) continue;
                var genre = GetGenre(System.Net.WebUtility.HtmlDecode(table.Descendants("th").First().InnerText));
                if (genre == SongGenre.Unknown) continue;

                foreach (var list in table.Descendants("tbody"))
                {
                    foreach (var item in list.Descendants("tr"))
                    {
                        var descendants = item.Descendants("td").ToList();
                        if (descendants.Count != 9) continue;
                        var item_title = descendants[2];
                        if (item_title.Descendants("strong").Count() == 0) continue;

                        string title = System.Net.WebUtility.HtmlDecode(item_title.Descendants("strong").First().InnerText).Trim();
                        string titlekey = FixReplace(title);
                        if (Songs.ContainsKey(titlekey))
                        {
                            Songs[titlekey].AddGenre(genre);
                            continue;
                        }

                        Song song = new Song();
                        song.SetTitle(title);

                        song.SetSubtitle((
                            item_title.Descendants("span").Count() != 0 ?
                            System.Net.WebUtility.HtmlDecode(item_title.Descendants("span").First().InnerText) :
                            "").Trim());

                        song.Difficulties.Easy.Level = GetLevel(descendants[4].InnerText);
                        song.Difficulties.Normal.Level = GetLevel(descendants[5].InnerText);
                        song.Difficulties.Hard.Level = GetLevel(descendants[6].InnerText);
                        song.Difficulties.Extreme.Level = GetLevel(descendants[7].InnerText);
                        song.Difficulties.Hidden.Level = GetLevel(descendants[8].InnerText);

                        if (song.Difficulties.Easy.Level < 0 && song.Difficulties.Normal.Level < 0 && song.Difficulties.Hard.Level < 0 && song.Difficulties.Extreme.Level < 0 && song.Difficulties.Hidden.Level < 0) continue;

                        song.Difficulties.Easy.Url = descendants[4].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[4].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                        song.Difficulties.Normal.Url = descendants[5].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[5].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                        song.Difficulties.Hard.Url = descendants[6].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[6].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                        song.Difficulties.Extreme.Url = descendants[7].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[7].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                        song.Difficulties.Hidden.Url = descendants[8].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[8].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";

                        if (descendants[0].InnerText.Contains("サ"))
                        {
                            song.Region.Japan = Availability.No;
                            song.Region.Asia = Availability.No;
                            song.Region.Oceania = Availability.No;
                            song.Region.UnitedStates = Availability.No;
                            song.Region.China = Availability.No;
                        }

                        song.AddGenre(genre);

                        Songs.TryAdd(FixReplace(title), song);
                        SongNames.TryAdd(title, FixReplace(title));
                    }
                }
            }
            #endregion
            Console.WriteLine($"Loaded {Songs.Count} songs.");

            Console.WriteLine($"Loading English titles from 'english.tsv'.");
            #region Titles/Subtitles (English)
            int en_tsv_count = 0;
            if (File.Exists(__taikoenpath))
            {
                foreach (string line in File.ReadAllLines(__taikoenpath))
                {
                    string[] info = line.Split('\t');
                    if (info.Length <= 0) continue;
                    if (string.IsNullOrWhiteSpace(info[2])) continue;
                    info[0] = FixReplace(info[0]);

                    if (Songs.TryGetValue(info[0], out Song? song))
                    {
                        Songs[info[0]].SetTitle(info[2], "en-US");
                        if (!string.IsNullOrEmpty(info[3])) Songs[info[0]].SetSubtitle(info[3].Trim(), "en-US");
                        SongNames.TryAdd(info[2], info[0]);
                        en_tsv_count++;
                    }
                }
            }
            #endregion
            Console.WriteLine($"Loaded {en_tsv_count} English titles from 'english.tsv'.");

            Console.WriteLine("Loading trad-chinese data...");
            #region Titles/Subtitles (Trad. Chinese)
            int zh_count = 0;
            int zh_sub_count = 0;
            foreach (var table in taiko_fumen_tw_tables)
            {
                if (table.Descendants("thead").Count() == 0) continue;
                if (table.Descendants("tr").Count() == 0) continue;
                var genre = GetGenre(System.Net.WebUtility.HtmlDecode(table.Descendants("tr").First().InnerText));
                if (genre == SongGenre.Unknown) continue;

                foreach (var list in table.Descendants("tbody"))
                {
                    foreach (var item in list.Descendants("tr"))
                    {
                        var descendants = item.Descendants("td").ToList();
                        if (descendants.Count != 9) continue;
                        var item_title = descendants[2];
                        if (item_title.Descendants("strong").Count() == 0) continue;

                        string title = System.Net.WebUtility.HtmlDecode(item_title.Descendants("strong").First().InnerText).Trim();

                        string original = FixReplace(title);
                        #region Title
                        if (title.IndexOf("/") > -1)
                        {
                            int[] indexes = AllIndexOf(title, "/");
                            int index = indexes[indexes.Length / 2];
                            if (Songs.ContainsKey(FixReplace(title.Substring(0, index).Trim())))
                            {
                                original = FixReplace(title.Substring(0, index).Trim());
                                string localized = title.Substring(index + 1).Trim();
                                //Songs[original].SetTitle(localized, "zh-CN");
                                Songs[original].SetTitle(localized, "zh-TW");
                                if (SongNames.TryAdd(localized, original)) zh_count++;
                            }
                            else
                                continue;
                        }
                        else if (!Songs.ContainsKey(original))
                            continue;
                        #endregion

                        if (item_title.Descendants("span").Count() == 0) continue;

                        string subtitle = System.Net.WebUtility.HtmlDecode(item_title.Descendants("span").First().InnerText).Trim();

                        if (subtitle.IndexOf("/") > -1)
                        {
                            int[] indexes = AllIndexOf(subtitle, "/");
                            int index = indexes[(indexes.Length / 2) + (indexes.Length > 1 ? (indexes.Length % 2) - 1 : 0)];
                            if (Songs[original].Subtitle.IndexOf("/") != index)
                            {
                                string localized_sub = FixReplace(subtitle.Substring(index + 1).Trim());

                                //Songs[original].SetSubtitle(localized_sub, "zh-CN");
                                Songs[original].SetSubtitle(localized_sub, "zh-TW");
                                zh_sub_count++;
                            }
                        }
                    }
                }
            }
            #endregion
            Console.WriteLine($"Loaded {zh_count} Trad-Chinese titles.");

            Console.WriteLine("Loading Sim-Chinese data...");
            #region Titles/Subtitles (Simp. Chinese)
            int cn_count = 0;
            int cn_sub_count = 0;
            foreach (var table in taiko_fumen_cn_tables)
            {
                if (table.Descendants("thead").Count() == 0) continue;
                if (table.Descendants("tr").Count() == 0) continue;
                var genre = GetGenre(System.Net.WebUtility.HtmlDecode(table.Descendants("tr").First().InnerText));
                if (genre == SongGenre.Unknown) continue;

                foreach (var list in table.Descendants("tbody"))
                {
                    foreach (var item in list.Descendants("tr"))
                    {
                        var descendants = item.Descendants("td").ToList();
                        if (descendants.Count != 9) continue;
                        var item_title = descendants[2];
                        if (item_title.Descendants("strong").Count() == 0) continue;

                        string title = System.Net.WebUtility.HtmlDecode(item_title.Descendants("strong").First().InnerText).Trim();

                        string original = FixReplace(title);
                        #region Title
                        if (title.IndexOf("/") > -1)
                        {
                            int[] indexes = AllIndexOf(title, "/");
                            int index = indexes[indexes.Length / 2];
                            if (Songs.ContainsKey(FixReplace(title.Substring(0, index).Trim())))
                            {
                                original = FixReplace(title.Substring(0, index).Trim());
                                string localized = title.Substring(index + 1).Trim();
                                Songs[original].SetTitle(localized, "zh-CN");

                                if (Songs[original].Difficulties.Easy.Level < 0) Songs[original].Difficulties.Easy.Level = GetLevel(descendants[4].InnerText);
                                if (Songs[original].Difficulties.Normal.Level < 0) Songs[original].Difficulties.Normal.Level = GetLevel(descendants[5].InnerText);
                                if (Songs[original].Difficulties.Hard.Level < 0) Songs[original].Difficulties.Hard.Level = GetLevel(descendants[6].InnerText);
                                if (Songs[original].Difficulties.Extreme.Level < 0) Songs[original].Difficulties.Extreme.Level = GetLevel(descendants[7].InnerText);
                                if (Songs[original].Difficulties.Hidden.Level < 0) Songs[original].Difficulties.Hidden.Level = GetLevel(descendants[8].InnerText);

                                if (string.IsNullOrEmpty(Songs[original].Difficulties.Easy.Url)) Songs[original].Difficulties.Easy.Url = descendants[4].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[4].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                                if (string.IsNullOrEmpty(Songs[original].Difficulties.Normal.Url)) Songs[original].Difficulties.Normal.Url = descendants[5].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[5].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                                if (string.IsNullOrEmpty(Songs[original].Difficulties.Hard.Url)) Songs[original].Difficulties.Hard.Url = descendants[6].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[6].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                                if (string.IsNullOrEmpty(Songs[original].Difficulties.Extreme.Url)) Songs[original].Difficulties.Extreme.Url = descendants[7].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[7].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                                if (string.IsNullOrEmpty(Songs[original].Difficulties.Hidden.Url)) Songs[original].Difficulties.Hidden.Url = descendants[8].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[8].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";

                                if (SongNames.TryAdd(localized, original)) cn_count++;
                            }
                            else
                                continue;
                        }
                        else if (!Songs.ContainsKey(original))
                            continue;
                        #endregion

                        if (item_title.Descendants("span").Count() == 0) continue;

                        string subtitle = FixReplace(System.Net.WebUtility.HtmlDecode(item_title.Descendants("span").First().InnerText), false).Trim();

                        if (subtitle.IndexOf("/") > -1)
                        {
                            int[] indexes = AllIndexOf(subtitle, "/");
                            int index = indexes[(indexes.Length / 2) + (indexes.Length > 1 ? (indexes.Length % 2) - 1 : 0)];
                            if (Songs[original].Subtitle.IndexOf("/") != index)
                            {
                                string localized_sub = subtitle.Substring(index + 1).Trim();

                                Songs[original].SetSubtitle(localized_sub, "zh-CN");
                                cn_sub_count++;
                            }
                        }
                    }
                }
            }
            #endregion
            Console.WriteLine($"Loaded {cn_count} Sim-Chinese titles.");

            Console.WriteLine("Loading Korean data...");
            #region Titles + Links + Images (Korean)
            int ko_count = 0;
            void addLocalizedTitle(string title, string localtitle, string lang)
            {
                Songs[title].SetTitle(localtitle, lang);
                SongNames.TryAdd(localtitle, title);
            }

            foreach (var song in taiko_ko)
            {
                song.ko_title = song.ko_title != null ? song.ko_title.Trim() : song.ko_title;
                song.en_title = song.en_title != null ? song.en_title.Trim() : song.en_title;
                song.title = FixReplace(song.title).Trim();

                if (Songs.ContainsKey(song.title))
                {
                    if (!string.IsNullOrWhiteSpace(song.ko_title)) { addLocalizedTitle(song.title, song.ko_title, "ko"); ko_count++; }

                    if (!string.IsNullOrWhiteSpace(song.en_title)) { addLocalizedTitle(song.title, song.en_title, "en-US"); }

                    if (!string.IsNullOrWhiteSpace(song.ko_alias)) { addLocalizedTitle(song.title, song.ko_alias, "ALIAS-ko"); }

                    if (!string.IsNullOrWhiteSpace(song.en_alias)) { addLocalizedTitle(song.title, song.en_alias, "ALIAS-en"); }

                    if (!string.IsNullOrWhiteSpace(song.romaji)) { addLocalizedTitle(song.title, song.romaji, "ALIAS-romaji"); }

                    Songs[song.title].Difficulties.Easy.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=easy";
                    Songs[song.title].Difficulties.Normal.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=normal";
                    Songs[song.title].Difficulties.Hard.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=hard";
                    Songs[song.title].Difficulties.Extreme.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=oni";
                    Songs[song.title].Difficulties.Hidden.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=ura";

                    Songs[song.title].Difficulties.Easy.ImageUrl = song.courses.easy?.images?.FirstOrDefault("") ?? "";
                    Songs[song.title].Difficulties.Normal.ImageUrl = song.courses.normal?.images?.FirstOrDefault("") ?? "";
                    Songs[song.title].Difficulties.Hard.ImageUrl = song.courses.hard?.images?.FirstOrDefault("") ?? "";
                    Songs[song.title].Difficulties.Extreme.ImageUrl = song.courses.oni?.images?.FirstOrDefault("") ?? "";
                    Songs[song.title].Difficulties.Hidden.ImageUrl = song.courses.ura?.images?.FirstOrDefault("") ?? "";
                }
            }
            #endregion
            Console.WriteLine($"Loaded {ko_count} Korean titles.");

            Console.WriteLine("Loading region lock data + adding Chinese-exclusive songs...");
            #region Region Locks
            if (File.Exists(__regionpath))
            {
                string[] songs = File.ReadAllLines(__regionpath);
                foreach (string song in songs)
                {
                    Song _song = CreateSongFromCSVString(song);
                    _song.SetTitle(FixReplace(_song.Title).Trim());

                    string title = _song.Title;
                    if (SongNames.ContainsKey(title)) title = SongNames[title];
                    if (Songs.ContainsKey(title))
                    {
                        Songs[title].Region.Japan = _song.Region.Japan;
                        Songs[title].Region.Asia = _song.Region.Asia;
                        Songs[title].Region.Oceania = _song.Region.Oceania;
                        Songs[title].Region.UnitedStates = _song.Region.UnitedStates;
                        Songs[title].Region.China = _song.Region.China;
                    }
                    // Chinese-exclusive songs are not listed on fumen-toka's main page, so let's add them here
                    else if (_song.Region.IsChinaOnly)
                    {
                        Songs.TryAdd(_song.Title, _song);
                        SongNames.TryAdd(_song.Title, _song.Title);
                    }

                    // Sou-uchi check
                    string souuchi = "【双打】 " + _song.Title;
                    if (Songs.ContainsKey(souuchi))
                    {
                        Songs[souuchi].Region.Japan = _song.Region.Japan;
                        Songs[souuchi].Region.Asia = _song.Region.Asia;
                        Songs[souuchi].Region.Oceania = _song.Region.Oceania;
                        Songs[souuchi].Region.UnitedStates = _song.Region.UnitedStates;
                        Songs[souuchi].Region.China = _song.Region.China;
                    }
                }
            }
            else
                Console.WriteLine("File containing region locks could not be found.");
            #endregion

            // Use fumen-database oni spreadsheet to assign correct (main) genre
            #region Main genre correction
            if (File.Exists(__fumenonipath))
            {
                HtmlDocument fumen_oni = new HtmlDocument();
                fumen_oni.Load(__fumenonipath);

                var entries_test = fumen_oni.DocumentNode.Descendants("div").First(item => item.HasClass("table_song_data"));
                var entries = entries_test.Descendants("div").Where(item => item.HasClass("table_grid_difficulty")).ToList();

                SongGenre genre(string info)
                {
                    if (info.Contains("genre_pops")) return SongGenre.Pop;
                    if (info.Contains("genre_namco")) return SongGenre.Namco;
                    if (info.Contains("genre_game")) return SongGenre.Game;
                    if (info.Contains("genre_variety")) return SongGenre.Variety;
                    if (info.Contains("genre_kids")) return SongGenre.Kids;
                    if (info.Contains("genre_vocaloid")) return SongGenre.Vocaloid;
                    if (info.Contains("genre_classic")) return SongGenre.Classical;
                    if (info.Contains("genre_anime")) return SongGenre.Anime;
                    return SongGenre.Unknown;
                }

                foreach (HtmlNode entry in entries)
                {
                    var info_finder = entry.Descendants("div").ToList();
                    if (info_finder.Count() >= 5)
                    {
                        var name = info_finder[3].InnerText.Trim();
                        if (SongNames.TryGetValue(name, out var song_title))
                        {
                            string song_info = entry.Descendants("div").First().Attributes[1].Value;
                            Songs[song_title].SetPriorityGenre(genre(song_info));
                        }
                    }
                }
            }
            else
                Console.WriteLine("File containing fumen-database oni data could not be found.");
            #endregion

            Console.WriteLine("Loading chart data...");
            #region Note Counts
            List<string> paths = [];
            foreach (string path in File.Exists(__tjapaths) ? File.ReadAllLines(__tjapaths) : [])
            {
                paths.AddRange(Directory.GetFiles(path, "*.tja", SearchOption.AllDirectories));
            }
            int chartcount = 0;
            foreach (var file in paths)
            {
                string[] lines = [];
                using (var reader = new FileReader(new FileInfo(file)))
                {
                    Encoding encoding = reader.Read(new FileInfo(file)).GetEncoding();
                    lines = File.ReadAllLines(file, encoding).Where(item => !item.StartsWith("//") && !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Select(item => item.Contains("//") ? item.Substring(0, item.IndexOf("//")) : item).ToArray();
                }

                if (lines.Any(item => item.StartsWith("TITLE:")))
                {
                    bool is_already_processed = false;
                    string title = "";
                    bool started = false;
                    bool branched = false;
                    bool branched_1 = false;
                    bool branched_2 = false;

                    int diff = -1;
                    //int level = 0;

                    int couple = 0;
                    int branch = -1;
                    int notes_m = 0;
                    int notes_e = 0;
                    int notes_n = 0;
                    int notes_m_1 = 0;
                    int notes_e_1 = 0;
                    int notes_n_1 = 0;
                    int notes_m_2 = 0;
                    int notes_e_2 = 0;
                    int notes_n_2 = 0;

                    bool titleisvalid = false;
                    foreach (string tit in lines.Where(line => line.StartsWith("TITLE:") || line.StartsWith("TITLEJA:") || line.StartsWith("TITLEZH:")).ToArray())
                    {
                        if (tit.StartsWith("TITLEJA:") || tit.StartsWith("TITLEZH:"))
                        {
                            //8 for TITLEJA:, 6 for TITLE:
                            if (Songs.TryGetValue(FixReplace(tit.Substring(8)), out Song? song))
                            {
                                if (song.Difficulties.ContainsNotes()) break;

                                title = FixReplace(tit.Substring(8));
                                titleisvalid = true;
                                chartcount++;
                                break;
                            }
                        }
                        else if (tit.StartsWith("TITLE:"))
                        {
                            //8 for TITLEJA:, 6 for TITLE:
                            if (Songs.TryGetValue(FixReplace(tit.Substring(6)), out Song? song))
                            {
                                if (song.Difficulties.ContainsNotes()) break;

                                title = FixReplace(tit.Substring(6));
                                titleisvalid = true;
                                chartcount++;
                                break;
                            }
                        }

                    }
                    if (!titleisvalid) { continue; }

                    void reset()
                    {
                        if (!is_already_processed)
                        {
                            if (diff > -1 && diff < 5)
                            {
                                var chart = Songs[title].Difficulties[diff];

                                chart.NoteCount.Single.Set(notes_n, branched ? notes_e : 0, branched ? notes_m : 0);
                                chart.NoteCount.Double1P.Set(notes_n_1, branched_1 ? notes_e_1 : 0, branched_1 ? notes_m_1 : 0);
                                chart.NoteCount.Double2P.Set(notes_n_2, branched_2 ? notes_e_2 : 0, branched_2 ? notes_m_2 : 0);

                                Songs[title].Difficulties[diff] = chart;
                            }
                        }

                        couple = 0;
                        branch = -1;
                        notes_m = 0;
                        notes_e = 0;
                        notes_n = 0;
                        notes_m_1 = 0;
                        notes_e_1 = 0;
                        notes_n_1 = 0;
                        notes_m_2 = 0;
                        notes_e_2 = 0;
                        notes_n_2 = 0;

                        branched = false;
                        branched_1 = false;
                        branched_2 = false;
                    }

                    foreach (string line in lines)
                    {
                        switch (line)
                        {
                            case "#START P1": started = true; couple = 1; branch = -1; continue;
                            case "#START P2": started = true; couple = 2; branch = -1; continue;
                            case "#START": started = true; couple = 0; branch = -1; continue;
                            case "#END": started = false; couple = 0; branch = -1; continue;
                            case "#M":
                                branch = 2;
                                switch (couple)
                                {
                                    case 0: branched = true; continue;
                                    case 1: branched_1 = true; continue;
                                    case 2: branched_2 = true; continue;
                                }
                                continue;
                            case "#E":
                                branch = 1;
                                switch (couple)
                                {
                                    case 0: branched = true; continue;
                                    case 1: branched_1 = true; continue;
                                    case 2: branched_2 = true; continue;
                                }
                                continue;
                            case "#N":
                                branch = 0;
                                switch (couple)
                                {
                                    case 0: branched = true; continue;
                                    case 1: branched_1 = true; continue;
                                    case 2: branched_2 = true; continue;
                                }
                                continue;
                            case "#BRANCHEND": branch = -1; continue;
                        }
                        if (started)
                        {
                            if (line.StartsWith('#')) continue;
                            int amount = line.Where(text => text == '1' || text == '2' || text == '3' || text == '4').Count();
                            switch (couple)
                            {
                                case 0:
                                    switch (branch)
                                    {
                                        case -1:
                                            notes_n += amount;
                                            notes_e += amount;
                                            notes_m += amount;
                                            continue;
                                        case 0: notes_n += amount; continue;
                                        case 1: notes_e += amount; continue;
                                        case 2: notes_m += amount; continue;
                                    }
                                    continue;
                                case 1:
                                    switch (branch)
                                    {
                                        case -1:
                                            notes_n_1 += amount;
                                            notes_e_1 += amount;
                                            notes_m_1 += amount;
                                            continue;
                                        case 0: notes_n_1 += amount; continue;
                                        case 1: notes_e_1 += amount; continue;
                                        case 2: notes_m_1 += amount; continue;
                                    }
                                    continue;
                                case 2:
                                    switch (branch)
                                    {
                                        case -1:
                                            notes_n_2 += amount;
                                            notes_e_2 += amount;
                                            notes_m_2 += amount;
                                            continue;
                                        case 0: notes_n_2 += amount; continue;
                                        case 1: notes_e_2 += amount; continue;
                                        case 2: notes_m_2 += amount; continue;
                                    }
                                    continue;
                            }
                        }
                        else
                        {
                            if (line.StartsWith("COURSE:"))
                            {
                                int get_diff = -1;
                                switch (line.Substring(7).Trim().ToLower())
                                {
                                    case "edit":
                                    case "ura":
                                    case "4":
                                        get_diff = 4; break;
                                    case "oni":
                                    case "3":
                                        get_diff = 3; break;
                                    case "hard":
                                    case "2":
                                        get_diff = 2; break;
                                    case "normal":
                                    case "1":
                                        get_diff = 1; break;
                                    case "easy":
                                    case "0":
                                        get_diff = 0; break;
                                }

                                reset();
                                is_already_processed = get_diff > -1 && Songs[title].Difficulties[get_diff].NoteCount.ContainsNotes();
                                diff = get_diff;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(title)) reset();
                }
            }
            #endregion
            Console.WriteLine($"Loaded data from {chartcount} charts.");
        }

        public static void WriteSonglist(bool write_tsv)
        {
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };

            using (var file_stream = File.CreateText(songsfile))
            {
                serializer.Serialize(file_stream, Songs);
            }

            if (write_tsv)
            {
                Console.WriteLine($"Writing current songlist to 'Resources{Path.DirectorySeparatorChar}result.tsv'...");
                string tsv = "";
                string title_prepend(string title)
                {
                    return title.StartsWith('"') ? ("\"\"\"" + title) : title;
                }
                foreach (var song in Songs.Values)
                {
                    tsv += song.Genre + "\t" + song.Title + "\t" + song.Subtitle;
                    foreach (string lang in new string[] { "en-US", "ko", "zh-TW", "zh-CN" })
                    {
                        tsv += "\t";
                        tsv += (song.TryGetTitle(lang, out string? title) ? title_prepend(title ?? "") : "") + "\t" + (song.TryGetSubtitle(lang, out string? subtitle) ? title_prepend(subtitle ?? "") : "");
                    }
                    tsv += "\n";
                }
                File.WriteAllText($"Resources{Path.DirectorySeparatorChar}result.tsv", tsv);
                Console.WriteLine("Done!");
            }
        }
    }
}
