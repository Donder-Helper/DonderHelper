using Discord;
using Discord.WebSocket;
using Discord.Commands;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using Hnx8.ReadJEnc;

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
    public class Program()
    {
        // Discord bot private key
        private static string __keypath = $"key.txt";
        // TJA path(s)
        private static string __tjapaths = $"Resources{Path.DirectorySeparatorChar}paths.txt";

        /*
         * All links below contain data to be compiled and used by Donder Helper.
         * You are responsible for manually grabbing this data yourself.
         * Please avoid automating this process through network requests, as this may put stress on some servers.
         */

        // https://docs.google.com/spreadsheets/d/1Piucd3Wv-QVQJ_yMQjC1xV08Cl2IXGze_8bf8nQZGjs/edit?gid=0#gid=0 - TSV
        private static string __regionpath = $"Resources{Path.DirectorySeparatorChar}region.tsv";

        // https://fumen-database.com/difficulty - Full page as HTML
        private static string __fumenonipath = $"Resources{Path.DirectorySeparatorChar}fumendatabase-oni.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC - Full page as HTML
        private static string __taikofumenpath = $"Resources{Path.DirectorySeparatorChar}taiko-fumen.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC/%E3%83%8A%E3%83%A0%E3%82%B3%E3%82%AA%E3%83%AA%E3%82%B8%E3%83%8A%E3%83%AB Full page as HTML
        private static string __taikofumennamcopath = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-namco.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC%E3%82%A2%E3%82%B8%E3%82%A2%E7%89%88%28%E8%8B%B1%E8%AA%9E%29 - Full page as HTML
        private static string __taikofumenenpath = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-en.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC%E3%82%A2%E3%82%B8%E3%82%A2%E7%89%88%28%E8%8B%B1%E8%AA%9E%29/NAMCO%20Original - Full page as HTML
        private static string __taikofumenennamcopath = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-en-namco.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC%E3%82%A2%E3%82%B8%E3%82%A2%E7%89%88%28%E4%B8%AD%E5%9B%BD%E8%AA%9E%29 - Full page as HTML
        private static string __taikofumenzhpath = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-zh.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC%E3%82%A2%E3%82%B8%E3%82%A2%E7%89%88%28%E4%B8%AD%E5%9B%BD%E8%AA%9E%29/NAMCO%20Original - Full page as HTML
        private static string __taikofumenzhnamcopath = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-zh-namco.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC%E4%B8%AD%E5%9B%BD%E7%89%88 - Full page as HTML 
        private static string __taikofumenchpath = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-ch.txt";

        // https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC%E4%B8%AD%E5%9B%BD%E7%89%88/%E5%8D%97%E5%A4%A2%E5%AE%AE%E5%8E%9F%E5%89%B5%E9%9F%B3%E6%A8%82 - Full page as HTML 
        private static string __taikofumenchnamcopath = $"Resources{Path.DirectorySeparatorChar}taiko-fumen-ch-namco.txt";

        // https://github.com/taikowiki/taiko-song-database/blob/main/database.json - JSON
        private static string __taikokopath = $"Resources{Path.DirectorySeparatorChar}ko.json";

        // https://docs.google.com/spreadsheets/d/1N9OBdkbwj51swS4jqhL6rTKv4looTQqLPWjAvD0lWog/edit?gid=1162123291#gid=1162123291 - TSV
        private static string __taikoenpath = $"Resources{Path.DirectorySeparatorChar}english.tsv";


        private static string __key = "";
        public static Dictionary<string, Song> __songs { get; private set; } = [];
        public static Dictionary<string, string> __songNames { get; private set; } = [];

#pragma warning disable CS8618
        private static DiscordSocketClient _client;
        private static CommandService _commandService;

        private static LoggingHandler _logginghandler;
        private static CommandsHandler _commandhandler;
#pragma warning restore CS8618

        public static string GetLocalizedSongTitle(string orig_title, string locale)
        {
            return __songs.TryGetValue(orig_title, out Song? song) ? song.GetTitle(locale) : orig_title;
        }

        public static async Task Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if !DEBUG
            Console.WriteLine("Starting in 20 seconds...");
            Thread.Sleep(20000);
#endif

            Console.WriteLine("Donhirobotスタート！ Let's starting!");
            LocaleData.Initialize();
            EmoteData.Initialize();

            if (!Boot()) {
                Console.WriteLine("Aborting launch & shutting down.");
                return;
            }
#if DEBUG
            SongDatabase.Update(__songs.Values.ToList());

            string tsv = "";
            string title_prepend(string title)
            {
                return title.StartsWith('"') ? ("\"\"\"" + title) : title;
            }
            foreach (var song in __songs.Values)
            {
                tsv += song.Genre + "\t" + song.Title + "\t" + song.Subtitle;
                foreach (string lang in new string[] {"en-US", "ko", "zh-TW", "zh-CN" })
                {
                    tsv += "\t";
                    tsv += (song.TryGetTitle(lang, out string? title) ? title_prepend(title ?? "") : "") + "\t" + (song.TryGetSubtitle(lang, out string? subtitle) ? title_prepend(subtitle ?? "") : "");
                }
                tsv += "\n";
            }
            File.WriteAllText($"Resources{Path.DirectorySeparatorChar}result.tsv", tsv);
#endif
            Console.WriteLine($"Finished! Loaded {__songs.Count} songs.");
            Console.WriteLine($"{__songs.Where(song => !song.Value.Difficulties.ContainsNotes()).ToDictionary().Count} songs do not contain or is missing note counts.");
            Console.WriteLine($"{__songNames.Count} entries for song names are available.");

            Console.WriteLine("Communicating with Discord...");

            DiscordSocketConfig _config = new() {
                GatewayIntents = GatewayIntents.Guilds
            };

            _client = new DiscordSocketClient(_config);
            _commandService = new CommandService();

            _logginghandler = new(_client, _commandService);
            _commandhandler = new(_client, _commandService);

#if DEBUG
            await _client.LoginAsync(TokenType.Bot, __key);
#else
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DONDERHELPER_SECRET_KEY")))
            {
                await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DONDERHELPER_SECRET_KEY"));
            }
            else if (!string.IsNullOrWhiteSpace(__key))
            {
                await _client.LoginAsync(TokenType.Bot, __key);
            }
            else
            {
                throw new Exception("Discord bot key could not be found in environment or text file.");
            }
#endif
            __key = "";
            await _client.StartAsync();
            await _client.SetCustomStatusAsync($"Drumming along to {__songs.Count} songs! ({CommandsHandler.last_Update})");

            Console.WriteLine("Logged in successfully!");

            // Block this task until the program is closed.
            await Process.GetCurrentProcess().WaitForExitAsync();

            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        /*
        * This god awful method, when in Debug mode, will parse all the information it can find in the Resources and compile it into Data/songs.json.
        * Not all websites/spreadsheets/jsons are built equal, so a huge amount of code must be dedicated to carefully breaking down each file.
        * 
        * On Release builds, this method will instead convert Data/songs.json into a Dictionary and be sent to __songs,
        * with keys being the song's original title in string format, and their values being the actual Song classes.
        * Localized titles are added to __songNames, so that they can be used for searching with the /song command.
        * 
        * That being said, it would be much better to put all this code into its own executable,
        * so things can be quickly tested and updated without the need to rebuild the songlist everytime.
        */
        private static bool Boot()
        {

            try
            {
                if (File.Exists(__keypath))
                    __key = File.ReadAllText(__keypath);
#if !DEBUG
            __songs = JsonSerializer.Create(new() { Formatting = Formatting.Indented }).Deserialize<Dictionary<string, Song>>(new JsonTextReader( new StringReader(File.ReadAllText(SongDatabase.jsonpath))));
            foreach (var song in __songs)
            {
                //__songs.TryAdd(song.Title, song);
                foreach (var title in song.Value.TitleList)
                {
                    __songNames.TryAdd(title.Value, song.Key);
                }
            }
            // ^ testing something
#else
                string FixReplace(string value, bool normalize = true)
                {
                    if (value.Contains('⑨')) return value.Replace('‐', '-').Replace('／', '/');
                    if (normalize) return value.Replace('‐', '-').Replace('／', '/').Normalize(NormalizationForm.FormKC);
                    return value.Replace('‐', '-').Replace('／', '/');
                }

                Console.WriteLine("Reading fumen data...");
                // Beginning finding all available songs
                if (File.Exists(__taikofumenpath))
                {
                    HtmlDocument taiko_fumen = new HtmlDocument();
                    taiko_fumen.Load(__taikofumenpath);
                    HtmlDocument taiko_fumen_namco = new HtmlDocument();
                    taiko_fumen_namco.Load(__taikofumennamcopath);

                    HtmlDocument taiko_fumen_en = new HtmlDocument();
                    taiko_fumen_en.Load(__taikofumenenpath);
                    HtmlDocument taiko_fumen_en_namco = new HtmlDocument();
                    taiko_fumen_en_namco.Load(__taikofumenennamcopath);

                    HtmlDocument taiko_fumen_zh = new HtmlDocument();
                    taiko_fumen_zh.Load(__taikofumenzhpath);
                    HtmlDocument taiko_fumen_zh_namco = new HtmlDocument();
                    taiko_fumen_zh_namco.Load(__taikofumenzhnamcopath);

                    HtmlDocument taiko_fumen_ch = new HtmlDocument();
                    taiko_fumen_ch.Load(__taikofumenchpath);
                    HtmlDocument taiko_fumen_ch_namco = new HtmlDocument();
                    taiko_fumen_ch_namco.Load(__taikofumenchnamcopath);

                    TaikoKoTitle[] taiko_ko = JsonConvert.DeserializeObject<TaikoKoTitle[]>(File.ReadAllText(__taikokopath)) ?? [];
                    //taiko_ko = taiko_ko.Where(item => item.ko_title != null).ToArray();

                    var tables = taiko_fumen.DocumentNode.Descendants("table");
                    tables = tables.Concat(taiko_fumen_namco.DocumentNode.Descendants("table"));
                    var tables_en = taiko_fumen_en.DocumentNode.Descendants("table");
                    tables_en = tables_en.Concat(taiko_fumen_en_namco.DocumentNode.Descendants("table"));
                    var tables_zh = taiko_fumen_zh.DocumentNode.Descendants("table");
                    tables_zh = tables_zh.Concat(taiko_fumen_zh_namco.DocumentNode.Descendants("table"));
                    var tables_ch = taiko_fumen_ch.DocumentNode.Descendants("table");
                    tables_ch = tables_ch.Concat(taiko_fumen_ch_namco.DocumentNode.Descendants("table"));

                    Song.SongGenre GetGenre(string info)
                    {
                        switch (info)
                        {
                            case "POP":
                            case "流行音樂":
                            case "流行音乐":
                            case "ポップス": return Song.SongGenre.Pop;

                            case "卡通動晝音樂":
                            case "Anime":
                            case "动漫音乐":
                            case "アニメ": return Song.SongGenre.Anime;

                            case "Kids'":
                            case "兒童音樂":
                            case "儿童音乐":
                            case "キッズ": return Song.SongGenre.Kids;

                            case "VOCALOID™ Music":
                            case "博歌乐™音乐":
                            case "ボーカロイド™曲": return Song.SongGenre.Vocaloid;

                            case "Game Music":
                            case "遊戯音樂":
                            case "游戏音乐":
                            case "ゲームミュージック": return Song.SongGenre.Game;

                            case "Variety":
                            case "综合音樂":
                            case "综合音乐":
                            case "バラエティ": return Song.SongGenre.Variety;

                            case "Classical":
                            case "古典音樂":
                            case "古典音乐":
                            case "クラシック": return Song.SongGenre.Classical;

                            case "NAMCO Original":
                            case "NAMCO 原創音樂":
                            case "南梦宫原创音乐":
                            case "ナムコオリジナル": return Song.SongGenre.Namco;

                            default: return Song.SongGenre.Unknown;
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

                    foreach (var table in tables)
                    {
                        if (table.Descendants("thead").Count() == 0) continue;
                        if (table.Descendants("th").Count() == 0) continue;
                        var genre = GetGenre(System.Net.WebUtility.HtmlDecode(table.Descendants("th").First().InnerText));
                        if (genre == Song.SongGenre.Unknown) continue;

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
                                if (__songs.ContainsKey(titlekey))
                                {
                                    __songs[titlekey].AddGenre(genre);
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
                                    song.Region.Japan = Song.Availability.No;
                                    song.Region.Asia = Song.Availability.No;
                                    song.Region.Oceania = Song.Availability.No;
                                    song.Region.UnitedStates = Song.Availability.No;
                                    song.Region.China = Song.Availability.No;
                                }
                                
                                song.AddGenre(genre);

                                __songs.TryAdd(FixReplace(title), song);
                                __songNames.TryAdd(title, FixReplace(title));
                            }
                        }
                    }
                    Console.WriteLine($"Loaded {__songs.Count} songs.");

                    Console.WriteLine("Loading English data...");
                    int en_count = 0;
                    int en_sub_count = 0;
                    foreach (var table in tables_en)
                    {
                        if (table.Descendants("thead").Count() == 0) continue;
                        if (table.Descendants("tr").Count() == 0) continue;
                        var genre = GetGenre(System.Net.WebUtility.HtmlDecode(table.Descendants("tr").First().InnerText));
                        if (genre == Song.SongGenre.Unknown) continue;

                        foreach (var list in table.Descendants("tbody"))
                        {
                            foreach (var item in list.Descendants("tr"))
                            {
                                var descendants = item.Descendants("td").ToList();
                                if (descendants.Count != 9) continue;
                                var item_title = descendants[2];
                                if (item_title.Descendants("strong").Count() == 0) continue;

                                string title = System.Net.WebUtility.HtmlDecode(item_title.Descendants("strong").First().InnerText);

                                string original = FixReplace(title);
                                #region Title
                                if (title.IndexOf("/") > -1)
                                {
                                    int[] indexes = AllIndexOf(title, "/");
                                    int index = indexes[(indexes.Length / 2) + (indexes.Length > 1 ? (indexes.Length % 2) - 1 : 0)];
                                    if (__songs.ContainsKey(FixReplace(title.Substring(0, index).Trim())))
                                    {
                                        original = FixReplace(title.Substring(0, index).Trim());
                                        string localized = title.Substring(index + 1).Trim();
                                        __songs[original].SetTitle(localized, "en-US");
                                        if (__songNames.TryAdd(localized, original)) en_count++;
                                    }
                                    else
                                        continue;
                                }
                                else if (!__songs.ContainsKey(original))
                                    continue;
                                #endregion

                                var songtest = __songs[original];
                                if (item_title.Descendants("span").Count() == 0) continue;

                                string subtitle = System.Net.WebUtility.HtmlDecode(item_title.Descendants("span").First().InnerText);

                                if (subtitle.IndexOf("/") > -1)
                                {
                                    int[] indexes = AllIndexOf(subtitle, "/");
                                    int index = indexes[(indexes.Length / 2) + (indexes.Length > 1 ? (indexes.Length % 2) - 1 : 0)];
                                    if (__songs[original].Subtitle.IndexOf("/") != index)
                                    {
                                        string localized_sub = subtitle.Substring(index + 1).Trim();

                                        __songs[original].SetSubtitle(localized_sub, "en-US");
                                        en_sub_count++;
                                    }
                                }
                            }
                        }
                    }
                    Console.WriteLine($"Loaded {en_count} English titles.");
                    
                    Console.WriteLine("Loading trad-chinese data...");
                    int zh_count = 0;
                    int zh_sub_count = 0;
                    foreach (var table in tables_zh)
                    {
                        if (table.Descendants("thead").Count() == 0) continue;
                        if (table.Descendants("tr").Count() == 0) continue;
                        var genre = GetGenre(System.Net.WebUtility.HtmlDecode(table.Descendants("tr").First().InnerText));
                        if (genre == Song.SongGenre.Unknown) continue;

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
                                    if (__songs.ContainsKey(FixReplace(title.Substring(0, index).Trim())))
                                    {
                                        original = FixReplace(title.Substring(0, index).Trim());
                                        string localized = title.Substring(index + 1).Trim();
                                        //__songs[original].SetTitle(localized, "zh-CN");
                                        __songs[original].SetTitle(localized, "zh-TW");
                                        if (__songNames.TryAdd(localized, original)) zh_count++;
                                    }
                                    else
                                        continue;
                                }
                                else if (!__songs.ContainsKey(original))
                                    continue;
                                #endregion

                                if (item_title.Descendants("span").Count() == 0) continue;

                                string subtitle = System.Net.WebUtility.HtmlDecode(item_title.Descendants("span").First().InnerText).Trim();

                                if (subtitle.IndexOf("/") > -1)
                                {
                                    int[] indexes = AllIndexOf(subtitle, "/");
                                    int index = indexes[(indexes.Length / 2) + (indexes.Length > 1 ? (indexes.Length % 2) - 1 : 0)];
                                    if (__songs[original].Subtitle.IndexOf("/") != index)
                                    {
                                        string localized_sub = FixReplace(subtitle.Substring(index + 1).Trim());

                                        //__songs[original].SetSubtitle(localized_sub, "zh-CN");
                                        __songs[original].SetSubtitle(localized_sub, "zh-TW");
                                        zh_sub_count++;
                                    }
                                }
                            }
                        }
                    }
                    Console.WriteLine($"Loaded {zh_count} Trad-Chinese titles.");

                    Console.WriteLine("Loading region lock data + adding Chinese-exclusive songs...");
                    if (File.Exists(__regionpath))
                    {
                        string[] songs = File.ReadAllLines(__regionpath);
                        foreach (string song in songs)
                        {
                            Song _song = SongBuilder.CreateSongFromCSVString(song);
                            _song.SetTitle(FixReplace(_song.Title).Trim());

                            if (__songs.ContainsKey(_song.Title))
                            {
                                string title = _song.Title;
                                __songs[title].Region.Japan = _song.Region.Japan;
                                __songs[title].Region.Asia = _song.Region.Asia;
                                __songs[title].Region.Oceania = _song.Region.Oceania;
                                __songs[title].Region.UnitedStates = _song.Region.UnitedStates;
                                __songs[title].Region.China = _song.Region.China;
                            }
                            // Chinese-exclusive songs are not listed on fumen-toka's main page, so let's add them here
                            else if (_song.Region.IsChinaOnly)
                            {
                                __songs.TryAdd(_song.Title, _song);
                                __songNames.TryAdd(_song.Title, _song.Title);
                            }

                            // Sou-uchi check
                            string souuchi = "【双打】 " + _song.Title;
                            if (__songs.ContainsKey(souuchi))
                            {
                                string title = _song.Title;
                                __songs[souuchi].Region.Japan = _song.Region.Japan;
                                __songs[souuchi].Region.Asia = _song.Region.Asia;
                                __songs[souuchi].Region.Oceania = _song.Region.Oceania;
                                __songs[souuchi].Region.UnitedStates = _song.Region.UnitedStates;
                                __songs[souuchi].Region.China = _song.Region.China;
                            }
                        }
                    }
                    else
                        throw new FileNotFoundException("File containing region locks could not be found.");

                    Console.WriteLine("Loading Sim-Chinese data...");
                    int cn_count = 0;
                    int cn_sub_count = 0;
                    foreach (var table in tables_ch)
                    {
                        if (table.Descendants("thead").Count() == 0) continue;
                        if (table.Descendants("tr").Count() == 0) continue;
                        var genre = GetGenre(System.Net.WebUtility.HtmlDecode(table.Descendants("tr").First().InnerText));
                        if (genre == Song.SongGenre.Unknown) continue;

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
                                    if (__songs.ContainsKey(FixReplace(title.Substring(0, index).Trim())))
                                    {
                                        original = FixReplace(title.Substring(0, index).Trim());
                                        string localized = title.Substring(index + 1).Trim();
                                        __songs[original].SetTitle(localized, "zh-CN");

                                        if (__songs[original].Difficulties.Easy.Level < 0) __songs[original].Difficulties.Easy.Level = GetLevel(descendants[4].InnerText);
                                        if (__songs[original].Difficulties.Normal.Level < 0) __songs[original].Difficulties.Normal.Level = GetLevel(descendants[5].InnerText);
                                        if (__songs[original].Difficulties.Hard.Level < 0) __songs[original].Difficulties.Hard.Level = GetLevel(descendants[6].InnerText);
                                        if (__songs[original].Difficulties.Extreme.Level < 0) __songs[original].Difficulties.Extreme.Level = GetLevel(descendants[7].InnerText);
                                        if (__songs[original].Difficulties.Hidden.Level < 0) __songs[original].Difficulties.Hidden.Level = GetLevel(descendants[8].InnerText);

                                        if (string.IsNullOrEmpty(__songs[original].Difficulties.Easy.Url)) __songs[original].Difficulties.Easy.Url = descendants[4].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[4].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                                        if (string.IsNullOrEmpty(__songs[original].Difficulties.Normal.Url)) __songs[original].Difficulties.Normal.Url = descendants[5].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[5].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                                        if (string.IsNullOrEmpty(__songs[original].Difficulties.Hard.Url)) __songs[original].Difficulties.Hard.Url = descendants[6].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[6].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                                        if (string.IsNullOrEmpty(__songs[original].Difficulties.Extreme.Url)) __songs[original].Difficulties.Extreme.Url = descendants[7].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[7].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";
                                        if (string.IsNullOrEmpty(__songs[original].Difficulties.Hidden.Url)) __songs[original].Difficulties.Hidden.Url = descendants[8].Descendants("a").Count() > 0 ? "https://wikiwiki.jp" + descendants[8].Descendants("a").First().Attributes["href"].Value.Split(" ")[0] : "";

                                        if (__songNames.TryAdd(localized, original)) cn_count++;
                                    }
                                    else
                                        continue;
                                }
                                else if (!__songs.ContainsKey(original))
                                    continue;
                                #endregion

                                if (item_title.Descendants("span").Count() == 0) continue;

                                string subtitle = FixReplace(System.Net.WebUtility.HtmlDecode(item_title.Descendants("span").First().InnerText), false).Trim();

                                if (subtitle.IndexOf("/") > -1)
                                {
                                    int[] indexes = AllIndexOf(subtitle, "/");
                                    int index = indexes[(indexes.Length / 2) + (indexes.Length > 1 ? (indexes.Length % 2) - 1 : 0)];
                                    if (__songs[original].Subtitle.IndexOf("/") != index)
                                    {
                                        string localized_sub = subtitle.Substring(index + 1).Trim();

                                        __songs[original].SetSubtitle(localized_sub, "zh-CN");
                                        cn_sub_count++;
                                    }
                                }
                            }
                        }
                    }
                    Console.WriteLine($"Loaded {cn_count} Sim-Chinese titles.");

                    Console.WriteLine("Loading Korean data...");
                    int ko_count = 0;
                    foreach (var song in taiko_ko)
                    {
                        song.ko_title = song.ko_title != null ? song.ko_title.Trim() : song.ko_title;
                        song.en_title = song.en_title != null ? song.en_title.Trim() : song.en_title;
                        song.title = FixReplace(song.title).Trim();

                        if (__songs.ContainsKey(song.title)) {
                            if (!string.IsNullOrWhiteSpace(song.ko_title))
                            {
                                __songs[song.title].SetTitle(song.ko_title, "ko");
                                __songNames.TryAdd(song.ko_title, song.title);

                                ko_count++;
                            }

                            if (!string.IsNullOrWhiteSpace(song.en_title)) {
                                __songs[song.title].SetTitle(song.en_title, "en-US");
                                __songNames.TryAdd(song.en_title, song.title);
                            }

                            __songs[song.title].Difficulties.Easy.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=easy";
                            __songs[song.title].Difficulties.Normal.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=normal";
                            __songs[song.title].Difficulties.Hard.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=hard";
                            __songs[song.title].Difficulties.Extreme.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=oni";
                            __songs[song.title].Difficulties.Hidden.UrlKo = "https://taiko.wiki/song/" + song.song_no + "?diff=ura";

                            __songs[song.title].Difficulties.Easy.ImageUrl = song.courses.easy?.images?.FirstOrDefault("") ?? "";
                            __songs[song.title].Difficulties.Normal.ImageUrl = song.courses.normal?.images?.FirstOrDefault("") ?? "";
                            __songs[song.title].Difficulties.Hard.ImageUrl = song.courses.hard?.images?.FirstOrDefault("") ?? "";
                            __songs[song.title].Difficulties.Extreme.ImageUrl = song.courses.oni?.images?.FirstOrDefault("") ?? "";
                            __songs[song.title].Difficulties.Hidden.ImageUrl = song.courses.ura?.images?.FirstOrDefault("") ?? "";
                        }
                    }
                    Console.WriteLine($"Loaded {ko_count} Korean titles.");

                    Console.WriteLine($"Loading English titles from 'english.tsv'.");
                    int en_tsv_count = 0;
                    if (File.Exists(__taikoenpath))
                    {
                        foreach (string line in File.ReadAllLines(__taikoenpath))
                        {
                            string[] info = line.Split('\t');
                            if (info.Length <= 0) continue;
                            if (string.IsNullOrWhiteSpace(info[2])) continue;
                            info[0] = FixReplace(info[0]);

                            if (__songs.TryGetValue(info[0], out Song? song))
                            {
                                __songs[info[0]].SetTitle(info[2], "en-US");
                                __songs[info[0]].SetSubtitle(info[3], "en-US");
                                __songNames.TryAdd(info[2], info[0]);
                                en_tsv_count++;
                            }
                        }
                    }
                    Console.WriteLine($"Loaded {en_tsv_count} English titles from 'english.tsv'.");
                }
                else
                    throw new FileNotFoundException("File(s) containing taiko-fumen data could not be found.");

                // Use fumen-database oni spreadsheet to assign correct (main) genre
                if (File.Exists(__fumenonipath))
                {
                    HtmlDocument fumen_oni = new HtmlDocument();
                    fumen_oni.Load(__fumenonipath);

                    var entries_test = fumen_oni.DocumentNode.Descendants("div").First(item => item.HasClass("table_song_data"));
                    var entries = entries_test.Descendants("div").Where(item => item.HasClass("table_grid_difficulty")).ToList();

                    Song.SongGenre genre(string info)
                    {
                        if (info.Contains("genre_pops")) return Song.SongGenre.Pop;
                        if (info.Contains("genre_namco")) return Song.SongGenre.Namco;
                        if (info.Contains("genre_game")) return Song.SongGenre.Game;
                        if (info.Contains("genre_variety")) return Song.SongGenre.Variety;
                        if (info.Contains("genre_kids")) return Song.SongGenre.Kids;
                        if (info.Contains("genre_vocaloid")) return Song.SongGenre.Vocaloid;
                        if (info.Contains("genre_classic")) return Song.SongGenre.Classical;
                        if (info.Contains("genre_anime")) return Song.SongGenre.Anime;
                        return Song.SongGenre.Unknown;
                    }

                    foreach ( HtmlNode entry in entries )
                    {
                        var info_finder = entry.Descendants("div").ToList();
                        if (info_finder.Count() >= 5)
                        {
                            var name = info_finder[3].InnerText.Trim();
                            if (__songNames.TryGetValue(name, out var song_title)) {
                                string song_info = entry.Descendants("div").First().Attributes[1].Value;
                                __songs[song_title].SetPriorityGenre(genre(song_info));
                            }
                        }
                    }
                }
                else
                    throw new FileNotFoundException("File containing fumen-database oni data could not be found.");

                Console.WriteLine("Loading chart data...");
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
                        foreach (string tit in lines.Where(line => line.StartsWith("TITLE:") || line.StartsWith("TITLEJA:")).ToArray())
                        {
                            if (tit.StartsWith("TITLEJA:"))
                            {
                                //8 for TITLEJA:, 6 for TITLE:
                                if (__songs.TryGetValue(FixReplace(tit.Substring(8)), out Song? song))
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
                                if (__songs.TryGetValue(FixReplace(tit.Substring(6)), out Song? song))
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
                            if (!is_already_processed) { 
                                switch (diff)
                                {
                                    case 4:
                                        __songs[title].Difficulties.Hidden.NoteCount.Single.Set(notes_n, branched ? notes_e : 0, branched ? notes_m : 0);
                                        __songs[title].Difficulties.Hidden.NoteCount.Double1P.Set(notes_n_1, branched_1 ? notes_e_1 : 0, branched_1 ? notes_m_1 : 0);
                                        __songs[title].Difficulties.Hidden.NoteCount.Double2P.Set(notes_n_2, branched_2 ? notes_e_2 : 0, branched_2 ? notes_m_2 : 0);
                                        break;
                                    case 3:
                                        __songs[title].Difficulties.Extreme.NoteCount.Single.Set(notes_n, branched ? notes_e : 0, branched ? notes_m : 0);
                                        __songs[title].Difficulties.Extreme.NoteCount.Double1P.Set(notes_n_1, branched_1 ? notes_e_1 : 0, branched_1 ? notes_m_1 : 0);
                                        __songs[title].Difficulties.Extreme.NoteCount.Double2P.Set(notes_n_2, branched_2 ? notes_e_2 : 0, branched_2 ? notes_m_2 : 0);
                                        break;
                                    case 2:
                                        __songs[title].Difficulties.Hard.NoteCount.Single.Set(notes_n, branched ? notes_e : 0, branched ? notes_m : 0);
                                        __songs[title].Difficulties.Hard.NoteCount.Double1P.Set(notes_n_1, branched_1 ? notes_e_1 : 0, branched_1 ? notes_m_1 : 0);
                                        __songs[title].Difficulties.Hard.NoteCount.Double2P.Set(notes_n_2, branched_2 ? notes_e_2 : 0, branched_2 ? notes_m_2 : 0);
                                        break;
                                    case 1:
                                        __songs[title].Difficulties.Normal.NoteCount.Single.Set(notes_n, branched ? notes_e : 0, branched ? notes_m : 0);
                                        __songs[title].Difficulties.Normal.NoteCount.Double1P.Set(notes_n_1, branched_1 ? notes_e_1 : 0, branched_1 ? notes_m_1 : 0);
                                        __songs[title].Difficulties.Normal.NoteCount.Double2P.Set(notes_n_2, branched_2 ? notes_e_2 : 0, branched_2 ? notes_m_2 : 0);
                                        break;
                                    case 0:
                                        __songs[title].Difficulties.Easy.NoteCount.Single.Set(notes_n, branched ? notes_e : 0, branched ? notes_m : 0);
                                        __songs[title].Difficulties.Easy.NoteCount.Double1P.Set(notes_n_1, branched_1 ? notes_e_1 : 0, branched_1 ? notes_m_1 : 0);
                                        __songs[title].Difficulties.Easy.NoteCount.Double2P.Set(notes_n_2, branched_2 ? notes_e_2 : 0, branched_2 ? notes_m_2 : 0);
                                        break;
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
                                case "#M": branch = 2;
                                    switch (couple)
                                    {
                                        case 0: branched = true; continue;
                                        case 1: branched_1 = true; continue;
                                        case 2: branched_2 = true; continue;
                                    }
                                    continue;
                                case "#E": branch = 1;
                                    switch (couple)
                                    {
                                        case 0: branched = true; continue;
                                        case 1: branched_1 = true; continue;
                                        case 2: branched_2 = true; continue;
                                    }
                                    continue;
                                case "#N": branch = 0;
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
                                    is_already_processed = get_diff > -1 && __songs[title].Difficulties[get_diff].NoteCount.ContainsNotes();
                                    diff = get_diff;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(title)) reset();
                    }
                }
                Console.WriteLine($"Loaded data from {chartcount} charts.");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("Boot Failed.\n" + ex.ToString());
                return false;
            }
            return true;
        }
    }
}
