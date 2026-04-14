using Discord;
using Newtonsoft.Json;

namespace DonderHelper
{
    public class Dan
    {
        public struct DanSong
        {
            [JsonProperty("id")]
            public int Id;
            [JsonProperty("diff")]
            public Song.SongDifficulty Difficulty;
            [JsonProperty("spoiler")]
            public bool Spoiler;

            public DanSong() { Id = -1; Difficulty = Song.SongDifficulty.Extreme; Spoiler = false; }
        }

        public struct Exam
        {
            [JsonProperty("condition")]
            public Condition Condition;
            [JsonProperty("is_less")]
            public bool IsLess;

            [JsonProperty("clear")]
            public int[] Clear;
            [JsonProperty("gold")]
            public int[] Gold;

            [JsonIgnore]
            public bool IsGlobal { get { return Clear.Length == 1; } }

            public Exam() { Condition = Condition.Gauge; IsLess = false; Clear = [-1]; Gold = [-1]; }
        }

        public enum Condition
        {
            Gauge,
            TotalHits,
            Bad,
            Ok,
            Good,
            Drumroll,
            Score,
            Combo
        }

        private struct DanColor
        {
            [JsonProperty("R")]
            byte R;
            [JsonProperty("G")]
            byte G;
            [JsonProperty("B")]
            byte B;
            DanColor(byte r, byte g, byte b) { R = r; G = g; B = b; }
        }

        public static string GetConditionAsString(Condition condition, string locale)
        {
            return condition switch
            {
                Condition.Gauge => LocaleData.GetString("DAN_CONDITION_GAUGE", locale),
                Condition.TotalHits => LocaleData.GetString("DAN_CONDITION_TOTALHIT", locale),
                Condition.Bad => LocaleData.GetString("DAN_CONDITION_BAD", locale),
                Condition.Ok => LocaleData.GetString("DAN_CONDITION_OK", locale),
                Condition.Good => LocaleData.GetString("DAN_CONDITION_GOOD", locale),
                Condition.Drumroll => LocaleData.GetString("DAN_CONDITION_DRUMROLL", locale),
                Condition.Score => LocaleData.GetString("DAN_CONDITION_SCORE", locale),
                Condition.Combo => LocaleData.GetString("DAN_CONDITION_COMBO", locale),
                _ => "???"
            };
        }

        // ---

        [JsonProperty("title")]
        public string Title = "??";
        [JsonProperty("title_en")]
        public string TitleEN = "Unknown Dan";

        [JsonProperty("color")]
        public string Color = "#FFFFFF";
        [JsonIgnore]
        public Discord.Color DiscordColor
        {
            get {
                switch (Color.ToLower())
                {
                    case "kyu": return new(0xffcf75);
                    case "blue": return new(0x4aaaba);
                    case "red": return new(0xf55336);
                    case "jin": return new(0xced6de);
                    case "gold": return new(0xffd700);
                    case "gaiden": return new(0x107b5c);
                    default: if (!Color.StartsWith('#') || Color.Length != 7) return new(0xFFFFFF); break;
                }

                int fromHex(string input) => int.Parse(input, System.Globalization.NumberStyles.HexNumber);
                string color = Color.Remove(0, 1);
                return new(fromHex(color.Substring(0, 2)), fromHex(color.Substring(2, 2)), fromHex(color.Substring(4, 2)));
            }
        }

        [JsonProperty("url")]
        public string Url = "";

        [JsonProperty("song1")]
        public DanSong Song1 { get; set; } = new();
        [JsonProperty("song2")]
        public DanSong Song2 { get; set; } = new();
        [JsonProperty("song3")]
        public DanSong Song3 { get; set; } = new();
        [JsonIgnore]
        public bool AnySpoiler => Song1.Spoiler || Song2.Spoiler || Song3.Spoiler;
        [JsonIgnore]
        public bool AllSpoiler => Song1.Spoiler && Song2.Spoiler && Song3.Spoiler;

        [JsonProperty("exams")]
        public List<Exam> Exams { get; set; } = [];

        public async Task<EmbedFieldBuilder> SongsToField(string locale)
        {
            string spoilerany = AnySpoiler ? "||" : "";
            string spoiler1 = Song1.Spoiler ? "||" : "";
            string spoiler2 = Song2.Spoiler ? "||" : "";
            string spoiler3 = Song3.Spoiler ? "||" : "";

            var songlist = await SongDatabase.GetSongs(Song1.Id, Song2.Id, Song3.Id);
            Song song1 = songlist[Song1.Id];
            Song song2 = songlist[Song2.Id];
            Song song3 = songlist[Song3.Id];
            Song.Chart chart1 = song1.Difficulties[Song1.Difficulty];
            Song.Chart chart2 = song2.Difficulties[Song2.Difficulty];
            Song.Chart chart3 = song3.Difficulties[Song3.Difficulty];
            int notecount =
                Math.Max(chart1.NoteCount.Single.Normal, chart1.NoteCount.Single.Tatsujin) +
                Math.Max(chart2.NoteCount.Single.Normal, chart2.NoteCount.Single.Tatsujin) +
                Math.Max(chart3.NoteCount.Single.Normal, chart3.NoteCount.Single.Tatsujin);

            return new()
            {
                Name = LocaleData.GetString("DAN_SONGS", locale),
                Value = $"{EmoteData.GetEmote("DAN_FIRST")} {spoiler1}{song1.GetTitle(locale)} {EmoteData.GetDifficulty(Song1.Difficulty)} {chart1.Level}★ {chart1.NoteCount}{spoiler1}\n" +
                $"{EmoteData.GetEmote("DAN_SECOND")} {spoiler2}{song2.GetTitle(locale)} {EmoteData.GetDifficulty(Song2.Difficulty)} {chart2.Level}★ {chart2.NoteCount}{spoiler2}\n" +
                $"{EmoteData.GetEmote("DAN_THIRD")} {spoiler3}{song3.GetTitle(locale)} {EmoteData.GetDifficulty(Song3.Difficulty)} {chart3.Level}★ {chart3.NoteCount}{spoiler3}\n" +
                $"-# {spoilerany}**{LocaleData.GetString("DAN_NOTECOUNT", locale, notecount > -1 ? notecount : "???")}**{spoilerany}",
                IsInline = false
            };
        }
        public List<EmbedFieldBuilder> ExamsToFields(string locale)
        {
            List<EmbedFieldBuilder> fields = new();
            for (int i = 0; i < Exams.Count; i++)
            {
                var exam = Exams[i];
                if (exam.IsGlobal)
                {
                    fields.Add(
                        new() { 
                            Name = GetConditionAsString(exam.Condition, locale),
                            IsInline = false,
                            Value = LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE", locale,
                            (exam.Clear[0] > -1 ? exam.Clear[0] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""),
                            (exam.Gold[0] > -1 ? exam.Gold[0] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""))
                        });
                }
                else
                {
                    fields.Add(
                        new()
                        {
                            Name = GetConditionAsString(exam.Condition, locale),
                            IsInline = false,
                            Value =
                            $"{EmoteData.GetEmote("DAN_FIRST")} {LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE", locale,
                            (exam.Clear[0] > -1 ? exam.Clear[0] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""),
                            (exam.Gold[0] > -1 ? exam.Gold[0] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""))}\n" +

                            $"{EmoteData.GetEmote("DAN_SECOND")} {LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE", locale,
                            (exam.Clear[1] > -1 ? exam.Clear[1] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""),
                            (exam.Gold[1] > -1 ? exam.Gold[1] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""))}\n" +

                            $"{EmoteData.GetEmote("DAN_THIRD")} {LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE", locale,
                            (exam.Clear[2] > -1 ? exam.Clear[2] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""),
                            (exam.Gold[2] > -1 ? exam.Gold[2] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""))}"
                        });
                }
            }
            return fields;
        }

        public ApplicationCommandOptionChoiceProperties AsChoice()
        {
            return new ApplicationCommandOptionChoiceProperties
            {
                Name = $"{Title} ({TitleEN})", Value = Title
            };
        }

        public bool DanIsValid()
        {
            return Song1.Id > -1;
        }

        public Dan() { }
    }

    public static class DanSonglist
    {
        private static Dan FifthKyu = new()
        {
            Title = "五級",
            TitleEN = "Fifth Kyu",
            Color = "kyu",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%BA%94%E7%B4%9A",

            Song1 = new() { Id = 1327, Difficulty = Song.SongDifficulty.Normal },
            Song2 = new() { Id = 1336, Difficulty = Song.SongDifficulty.Normal },
            Song3 = new() { Id = 1339, Difficulty = Song.SongDifficulty.Normal },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [92], Gold = [95] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [642], Gold = [664] }
            }
        };

        private static Dan FourthKyu = new()
        {
            Title = "四級",
            TitleEN = "Fourth Kyu",
            Color = "kyu",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%9B%9B%E7%B4%9A",

            Song1 = new() { Id = 416, Difficulty = Song.SongDifficulty.Normal },
            Song2 = new() { Id = 1309, Difficulty = Song.SongDifficulty.Normal },
            Song3 = new() { Id = 1313, Difficulty = Song.SongDifficulty.Normal },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [94], Gold = [97] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [827], Gold = [854] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [82], Gold = [41] }
            }
        };

        private static Dan ThirdKyu = new()
        {
            Title = "三級",
            TitleEN = "Third Kyu",
            Color = "kyu",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B8%89%E7%B4%9A",

            Song1 = new() { Id = 1293, Difficulty = Song.SongDifficulty.Hard },
            Song2 = new() { Id = 1277, Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Id = 1353, Difficulty = Song.SongDifficulty.Hard },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [96], Gold = [99] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [909], Gold = [938] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [75], Gold = [37] }
            }
        };

        private static Dan SecondKyu = new()
        {
            Title = "二級",
            TitleEN = "Second Kyu",
            Color = "kyu",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%BA%8C%E7%B4%9A",

            Song1 = new() { Id = 1228, Difficulty = Song.SongDifficulty.Hard },
            Song2 = new() { Id = 1001, Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Id = 1304, Difficulty = Song.SongDifficulty.Hard },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [97], Gold = [100] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [1409], Gold = [1454] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [82], Gold = [41] }
            }
        };

        private static Dan FirstKyu = new()
        {
            Title = "一級",
            TitleEN = "First Kyu",
            Color = "kyu",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B8%80%E7%B4%9A",

            Song1 = new() { Id = 1267, Difficulty = Song.SongDifficulty.Hard },
            Song2 = new() { Id = 561, Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Id = 979, Difficulty = Song.SongDifficulty.Hard },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [98], Gold = [100] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [1257], Gold = [1296] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [48], Gold = [24] }
            }
        };

        private static Dan FirstDan = new()
        {
            Title = "初段",
            TitleEN = "Shodan / First Dan",
            Color = "blue",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%88%9D%E6%AE%B5",

            Song1 = new() { Id = 886, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 43, Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Id = 3, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [98], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [825], Gold = [874] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [37], Gold = [18] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [169], Gold = [188] }
            }
        };

        private static Dan SecondDan = new()
        {
            Title = "二段",
            TitleEN = "Second Dan",
            Color = "blue",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%BA%8C%E6%AE%B5",

            Song1 = new() { Id = 551, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 867, Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Id = 951, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [98], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [1132], Gold = [1192] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [39], Gold = [19] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [155], Gold = [174] }
            }
        };

        private static Dan ThirdDan = new()
        {
            Title = "三段",
            TitleEN = "Third Dan",
            Color = "blue",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B8%89%E6%AE%B5",

            Song1 = new() { Id = 377, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 61, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 998, Difficulty = Song.SongDifficulty.Extreme },
            
            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [99], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [1252], Gold = [1312] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [33], Gold = [16] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [155], Gold = [174] }
            }
        };

        private static Dan FourthDan = new()
        {
            Title = "四段",
            TitleEN = "Fourth Dan",
            Color = "blue",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%9B%9B%E6%AE%B5",

            Song1 = new() { Id = 488, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 219, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 1256, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [99], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [1487], Gold = [1550] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [30], Gold = [15] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [376], Gold = [419] }
            }
        };

        private static Dan FifthDan = new()
        {
            Title = "五段",
            TitleEN = "Fifth Dan",
            Color = "blue",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%BA%94%E6%AE%B5",

            Song1 = new() { Id = 1307, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 591, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 959, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [99], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [1482], Gold = [1537] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [23], Gold = [11] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [369], Gold = [413] }
            }
        };

        private static Dan SixthDan = new()
        {
            Title = "六段",
            TitleEN = "Sixth Dan",
            Color = "red",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%85%AD%E6%AE%B5",

            Song1 = new() { Id = 278, Difficulty = Song.SongDifficulty.Hidden },
            Song2 = new() { Id = 1171, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 391, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [295], Gold = [246] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [17], Gold = [8] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [58, 105, 122], Gold = [68, 125, 147] }
            }
        };

        private static Dan SeventhDan = new()
        {
            Title = "七段",
            TitleEN = "Seventh Dan",
            Color = "red",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B8%83%E6%AE%B5",

            Song1 = new() { Id = 546, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 85, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 72, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [283], Gold = [199] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [16], Gold = [8] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [124, 153, 90], Gold = [147, 173, 108] }
            }
        };

        private static Dan EigthDan = new()
        {
            Title = "八段",
            TitleEN = "Eigth Dan",
            Color = "red",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%85%AB%E6%AE%B5",

            Song1 = new() { Id = 569, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 1206, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 1052, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [210], Gold = [164] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [12], Gold = [6] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [119, 134, 257], Gold = [138, 146, 272] }
            }
        };

        private static Dan NinthDan = new()
        {
            Title = "九段",
            TitleEN = "Ninth Dan",
            Color = "red",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B9%9D%E6%AE%B5",

            Song1 = new() { Id = 776, Difficulty = Song.SongDifficulty.Hidden },
            Song2 = new() { Id = 359, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 724, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [107], Gold = [68] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [7], Gold = [4] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [0, 77, 24], Gold = [0, 89, 28] }
            }
        };

        private static Dan TenthDan = new()
        {
            Title = "十段",
            TitleEN = "Tenth Dan",
            Color = "red",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%8D%81%E6%AE%B5",

            Song1 = new() { Id = 615, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 1068, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 1383, Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [20, 25, 30], Gold = [15, 19, 23] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [7], Gold = [4] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [26, 213, 23], Gold = [31, 247, 27] }
            }
        };

        private static Dan Kuroto = new()
        {
            Title = "玄人",
            TitleEN = "Kuroto",
            Color = "jin",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E7%8E%84%E4%BA%BA",

            Song1 = new() { Id = 1133, Difficulty = Song.SongDifficulty.Hidden },
            Song2 = new() { Id = 1071, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 1412, Difficulty = Song.SongDifficulty.Hidden },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [50], Gold = [35] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [6], Gold = [3] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [44, 29, 35], Gold = [48, 38, 43] }
            }
        };

        private static Dan Meijin = new()
        {
            Title = "名人",
            TitleEN = "Meijin",
            Color = "jin",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%90%8D%E4%BA%BA",

            Song1 = new() { Id = 1255, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 1085, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 1413, Difficulty = Song.SongDifficulty.Hidden },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [30], Gold = [19] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [5], Gold = [3] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [21, 10, 107], Gold = [25, 11, 113] }
            }
        };

        private static Dan Chojin = new()
        {
            Title = "超人",
            TitleEN = "Chojin",
            Color = "jin",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E8%B6%85%E4%BA%BA",

            Song1 = new() { Id = 992, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 745, Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Id = 1411, Difficulty = Song.SongDifficulty.Hidden },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [15], Gold = [6] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [4], Gold = [2] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [86, 41, 134], Gold = [106, 49, 146] }
            }
        };

        private static Dan Tatsujin = new()
        {
            Title = "達人",
            TitleEN = "Tatsujin",
            Color = "gold",
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E9%81%94%E4%BA%BA",

            Song1 = new() { Id = 1317, Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Id = 1032, Difficulty = Song.SongDifficulty.Hidden },
            Song3 = new() { Id = 1419, Difficulty = Song.SongDifficulty.Hidden },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [8], Gold = [1] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [3], Gold = [1] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [79, 161, 82], Gold = [99, 187, 99] }
            }
        };

        public static readonly Dictionary<string, Dan> Dans = new() {
            {FifthKyu.Title, FifthKyu},
            {FourthKyu.Title, FourthKyu},
            {ThirdKyu.Title, ThirdKyu},
            {SecondKyu.Title, SecondKyu},
            {FirstKyu.Title, FirstKyu},
            {FirstDan.Title, FirstDan},
            {SecondDan.Title, SecondDan},
            {ThirdDan.Title, ThirdDan},
            {FourthDan.Title, FourthDan},
            {FifthDan.Title, FifthDan},
            {SixthDan.Title, SixthDan},
            {SeventhDan.Title, SeventhDan},
            {EigthDan.Title, EigthDan},
            {NinthDan.Title, NinthDan},
            {TenthDan.Title, TenthDan},
            {Kuroto.Title, Kuroto},
            {Meijin.Title, Meijin},
            {Chojin.Title, Chojin},
            {Tatsujin.Title, Tatsujin}
        };
    }
}
