using Discord;
using Newtonsoft.Json;

namespace DonderHelper
{
    public class Dan
    {
        public struct DanSong
        {
            [JsonProperty("title")]
            public string Title;
            [JsonProperty("diff")]
            public Song.SongDifficulty Difficulty;
            [JsonProperty("spoiler")]
            public bool Spoiler;

            [JsonIgnore]
            public Song.Chart Chart { 
                get { 
                    return Song.Difficulties[Difficulty];
                } 
            }
            [JsonIgnore]
            public Song Song
            {
                get
                {
                    return Program.__songs.TryGetValue(Title, out var song) ? song : new();
                }
            }
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
            public bool IsGlobal { get { return Clear.Length <= 1; } }
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
        [JsonIgnore]
        public string Subtitle => d_Subtitle.TryGetValue("ja", out string? subtitle) ? (subtitle ?? "") : "";
        [JsonProperty("subtitle")]
        public Dictionary<string, string> d_Subtitle = new() {
            { "ja", "" }
        };
        public string GetSubtitle(string locale) { return d_Subtitle.TryGetValue(locale, out var subtitle) ? subtitle : Subtitle; }

        [JsonProperty("color")]
        public Discord.Color Color = new();

        [JsonProperty("url")]
        public string Url = "";

        [JsonProperty("song1")]
        public DanSong Song1 { get; set; } = new();
        [JsonProperty("song2")]
        public DanSong Song2 { get; set; } = new();
        [JsonProperty("song3")]
        public DanSong Song3 { get; set; } = new();

        [JsonProperty("exams")]
        public List<Exam> Exams { get; set; } = [];

        public List<EmbedFieldBuilder> ExamsToFields(string locale)
        {
            List<EmbedFieldBuilder> fields = new();
            foreach (var exam in Exams)
            {
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
                            Value = $"{EmoteData.GetEmote("DAN_FIRST")} " + LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE", locale,
                            (exam.Clear[0] > -1 ? exam.Clear[0] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""),
                            (exam.Gold[0] > -1 ? exam.Gold[0] : "???") + (exam.Condition == Condition.Gauge ? "%" : "")) + "\n" +

                            $"{EmoteData.GetEmote("DAN_SECOND")} " + LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE", locale,
                            (exam.Clear[1] > -1 ? exam.Clear[1] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""),
                            (exam.Gold[1] > -1 ? exam.Gold[1] : "???") + (exam.Condition == Condition.Gauge ? "%" : "")) + "\n" +

                            $"{EmoteData.GetEmote("DAN_THIRD")} " + LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE", locale,
                            (exam.Clear[2] > -1 ? exam.Clear[2] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""),
                            (exam.Gold[2] > -1 ? exam.Gold[2] : "???") + (exam.Condition == Condition.Gauge ? "%" : ""))
                        });
                }
            }
            return fields;
        }

        public List<EmbedBuilder> ExamsToEmbedBuilders(string locale)
        {
            List<EmbedBuilder> builder = new();

            foreach (var exam in Exams)
            {
                if (exam.IsGlobal)
                {
                    builder.Add(new()
                    {
                        Color = Color,
                        Fields = new()
                        {
                            new() {
                            Name = GetConditionAsString(exam.Condition, locale),
                            IsInline = false,
                            Value = LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE",
                            locale, exam.Clear[0] + (exam.Condition == Condition.Gauge ? "%" : ""),
                            exam.Gold[0] + (exam.Condition == Condition.Gauge ? "%" : ""))
                            }
                        }
                    });
                }
                else
                {
                    builder.Add(new()
                    {
                        Color = Color,
                        Fields = new()
                        {
                            new() {
                            Name = exam.Condition.ToString(),
                            IsInline = true,
                            Value = LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE",
                            locale, exam.Clear[0] + (exam.Condition == Condition.Gauge ? "%" : ""),
                            exam.Gold[0] + (exam.Condition == Condition.Gauge ? "%" : ""))
                            },
                            new() {
                                Name = exam.Condition.ToString(),
                                IsInline = true,
                                Value = LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE",
                                locale, exam.Clear[1] + (exam.Condition == Condition.Gauge ? "%" : ""),
                                exam.Gold[1] + (exam.Condition == Condition.Gauge ? "%" : ""))
                            },
                            new() {
                                Name = exam.Condition.ToString(),
                                IsInline = true,
                                Value = LocaleData.GetString(exam.IsLess ? "DAN_CONDITION_LESS" : "DAN_CONDITION_MORE",
                                locale, exam.Clear[2] + (exam.Condition == Condition.Gauge ? "%" : ""),
                                exam.Gold[2] + (exam.Condition == Condition.Gauge ? "%" : ""))
                            }
                        }
                    });
                }
            }
            return builder;
        }

        public ApplicationCommandOptionChoiceProperties AsChoice()
        {
            return new ApplicationCommandOptionChoiceProperties
            {
                Name = $"{Title} ({TitleEN})", Value = Title
            };
        }

        public int GetNoteCount()
        {
            return Song1.Chart.NoteCount.ContainsNotes() && Song2.Chart.NoteCount.ContainsNotes() && Song3.Chart.NoteCount.ContainsNotes()
                ?
                Math.Max(Song1.Chart.NoteCount.Single.Normal, Song1.Chart.NoteCount.Single.Tatsujin) +
                Math.Max(Song2.Chart.NoteCount.Single.Normal, Song2.Chart.NoteCount.Single.Tatsujin) +
                Math.Max(Song3.Chart.NoteCount.Single.Normal, Song3.Chart.NoteCount.Single.Tatsujin)
                : -1;
        }

        public bool DanIsValid()
        {
            return !string.IsNullOrWhiteSpace(Song1.Title);
        }

        public Dan() { }
    }

    public static class DanSonglist
    {
        private static readonly Discord.Color Kyu = new(0xffcf75);
        private static readonly Discord.Color Blue = new(0x4aaaba);
        private static readonly Discord.Color Red = new(0xf55336);
        private static readonly Discord.Color Jin = new(0xced6de);
        private static readonly Discord.Color Gold = new(0xffd700);

        public static Dan FifthKyu = new()
        {
            Title = "五級",
            TitleEN = "Fifth Kyu",
            Color = Kyu,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%BA%94%E7%B4%9A",

            Song1 = new() { Title = "はいよろこんで", Difficulty = Song.SongDifficulty.Normal },
            Song2 = new() { Title = "シカ色デイズ", Difficulty = Song.SongDifficulty.Normal },
            Song3 = new() { Title = "ライラック", Difficulty = Song.SongDifficulty.Normal },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [92], Gold = [95] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [642], Gold = [664] }
            }
        };

        public static Dan FourthKyu = new()
        {
            Title = "四級",
            TitleEN = "Fourth Kyu",
            Color = Kyu,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%9B%9B%E7%B4%9A",

            Song1 = new() { Title = "Help me, ERINNNNNN!!", Difficulty = Song.SongDifficulty.Normal },
            Song2 = new() { Title = "ココドコ?多分ドッカ島!", Difficulty = Song.SongDifficulty.Normal },
            Song3 = new() { Title = "Bling-Bang-Bang-Born", Difficulty = Song.SongDifficulty.Normal },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [94], Gold = [97] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [827], Gold = [854] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [82], Gold = [41] }
            }
        };

        public static Dan ThirdKyu = new()
        {
            Title = "三級",
            TitleEN = "Third Kyu",
            Color = Kyu,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B8%89%E7%B4%9A",

            Song1 = new() { Title = "最高到達点", Difficulty = Song.SongDifficulty.Hard },
            Song2 = new() { Title = "空想打破", Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Title = "ファタール", Difficulty = Song.SongDifficulty.Hard },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [96], Gold = [99] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [909], Gold = [938] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [75], Gold = [37] }
            }
        };

        public static Dan SecondKyu = new()
        {
            Title = "二級",
            TitleEN = "Second Kyu",
            Color = Kyu,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%BA%8C%E7%B4%9A",

            Song1 = new() { Title = "強風オールバック(feat.歌愛ユキ)", Difficulty = Song.SongDifficulty.Hard },
            Song2 = new() { Title = "なんどでも笑おう", Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Title = "who are you? who are you?", Difficulty = Song.SongDifficulty.Hard },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [97], Gold = [100] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [1409], Gold = [1454] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [82], Gold = [41] }
            }
        };

        public static Dan FirstKyu = new()
        {
            Title = "一級",
            TitleEN = "First Kyu",
            Color = Kyu,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B8%80%E7%B4%9A",

            Song1 = new() { Title = "唱", Difficulty = Song.SongDifficulty.Hard },
            Song2 = new() { Title = "輝きを求めて", Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Title = "轟け!太鼓の達人", Difficulty = Song.SongDifficulty.Hard },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [98], Gold = [100] },
                new() { Condition = Dan.Condition.TotalHits, IsLess = false, Clear = [1257], Gold = [1296] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [48], Gold = [24] }
            }
        };

        public static Dan FirstDan = new()
        {
            Title = "初段",
            TitleEN = "Shodan / First Dan",
            Color = Blue,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%88%9D%E6%AE%B5",

            Song1 = new() { Title = "ハロー!どんちゃん", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "さよならワーリャ", Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Title = "願いはエスペラント", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [98], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [825], Gold = [874] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [37], Gold = [18] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [169], Gold = [188] }
            }
        };

        public static Dan SecondDan = new()
        {
            Title = "二段",
            TitleEN = "Second Dan",
            Color = Blue,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%BA%8C%E6%AE%B5",

            Song1 = new() { Title = "蝶戀", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "うちゅうひこうし冒険譚", Difficulty = Song.SongDifficulty.Hard },
            Song3 = new() { Title = "アイドル狂戦士(feat.佐藤貴文)", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [98], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [1132], Gold = [1192] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [39], Gold = [19] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [155], Gold = [174] }
            }
        };

        public static Dan ThirdDan = new()
        {
            Title = "三段",
            TitleEN = "Third Dan",
            Color = Blue,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B8%89%E6%AE%B5",

            Song1 = new() { Title = "恋の処方箋", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "オフ♨ロック", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "Connected World", Difficulty = Song.SongDifficulty.Extreme },
            
            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [99], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [1252], Gold = [1312] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [33], Gold = [16] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [155], Gold = [174] }
            }
        };

        public static Dan FourthDan = new()
        {
            Title = "四段",
            TitleEN = "Fourth Dan",
            Color = Blue,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%9B%9B%E6%AE%B5",

            Song1 = new() { Title = "シューガク トラベラーズ", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "トータル・エクリプス 2035", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "転生〈TENSEI〉-喜与志が待つ強者-", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [99], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [1487], Gold = [1550] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [30], Gold = [15] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [376], Gold = [419] }
            }
        };

        public static Dan FifthDan = new()
        {
            Title = "五段",
            TitleEN = "Fifth Dan",
            Color = Blue,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%BA%94%E6%AE%B5",

            Song1 = new() { Title = "大多羅捌伍伍壱", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "ネテモネテモ", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "螺旋周回軌道", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [99], Gold = [100] },
                new() { Condition = Dan.Condition.Good, IsLess = false, Clear = [1482], Gold = [1537] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [23], Gold = [11] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [369], Gold = [413] }
            }
        };

        public static Dan SixthDan = new()
        {
            Title = "六段",
            TitleEN = "Sixth Dan",
            Color = Red,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%85%AD%E6%AE%B5",

            Song1 = new() { Title = "花漾", Difficulty = Song.SongDifficulty.Hidden },
            Song2 = new() { Title = "共奏鼓祭", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "SORA-VI 火ノ鳥", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [295], Gold = [246] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [17], Gold = [8] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [58, 105, 122], Gold = [68, 125, 147] }
            }
        };

        public static Dan SeventhDan = new()
        {
            Title = "七段",
            TitleEN = "Seventh Dan",
            Color = Red,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%85%AB%E6%AE%B5",

            Song1 = new() { Title = "指先からはじまる物語", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "ヘ調の協奏曲 第3楽章", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "The Carnivorous Carnival", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [283], Gold = [199] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [16], Gold = [8] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [124, 153, 90], Gold = [147, 173, 108] }
            }
        };

        public static Dan EigthDan = new()
        {
            Title = "八段",
            TitleEN = "Eigth Dan",
            Color = Red,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%85%AB%E6%AE%B5",

            Song1 = new() { Title = "My Muscle Heart", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "Crystal Hail", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "仮想現実のテレスコープ", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [210], Gold = [164] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [12], Gold = [6] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [119, 134, 257], Gold = [138, 146, 272] }
            }
        };

        public static Dan NinthDan = new()
        {
            Title = "九段",
            TitleEN = "Ninth Dan",
            Color = Red,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E4%B9%9D%E6%AE%B5",

            Song1 = new() { Title = "GO GET'EM!", Difficulty = Song.SongDifficulty.Hidden },
            Song2 = new() { Title = "RIDGE RACER STEPS - GMT remix -", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "よーいドン!", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [107], Gold = [68] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [7], Gold = [4] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [0, 77, 24], Gold = [0, 89, 28] }
            }
        };

        public static Dan TenthDan = new()
        {
            Title = "十段",
            TitleEN = "Tenth Dan",
            Color = Red,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%8D%81%E6%AE%B5",

            Song1 = new() { Title = "天狗囃子", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "Spectral Rider", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "SoulStone -闇喰イサァカス団-", Difficulty = Song.SongDifficulty.Extreme },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [20, 25, 30], Gold = [15, 19, 23] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [7], Gold = [4] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [26, 213, 23], Gold = [31, 247, 27] }
            }
        };

        public static Dan Kuroto = new()
        {
            Title = "玄人",
            TitleEN = "Kuroto",
            Color = Jin,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E7%8E%84%E4%BA%BA",

            Song1 = new() { Title = "Doppelgangers", Difficulty = Song.SongDifficulty.Hidden },
            Song2 = new() { Title = "ex寅 Trap!!", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "案山子姫 -Princess Scarecrow-", Difficulty = Song.SongDifficulty.Hidden },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [50], Gold = [35] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [6], Gold = [3] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [44, 29, 35], Gold = [48, 38, 43] }
            }
        };

        public static Dan Meijin = new()
        {
            Title = "名人",
            TitleEN = "Meijin",
            Color = Jin,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E5%90%8D%E4%BA%BA",

            Song1 = new() { Title = "電脳幻夜の星言詠", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "弧", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "ピッチフェイダ", Difficulty = Song.SongDifficulty.Hidden },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [30], Gold = [19] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [5], Gold = [3] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [21, 10, 107], Gold = [25, 11, 113] }
            }
        };

        public static Dan Chojin = new()
        {
            Title = "超人",
            TitleEN = "Chojin",
            Color = Jin,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E8%B6%85%E4%BA%BA",

            Song1 = new() { Title = "LECIEL GLISSANDO", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "プチポチ", Difficulty = Song.SongDifficulty.Extreme },
            Song3 = new() { Title = "Lightning Boys", Difficulty = Song.SongDifficulty.Hidden },

            Exams = new()
            {
                new() { Condition = Dan.Condition.Gauge, IsLess = false, Clear = [100], Gold = [100] },
                new() { Condition = Dan.Condition.Ok, IsLess = true, Clear = [15], Gold = [6] },
                new() { Condition = Dan.Condition.Bad, IsLess = true, Clear = [4], Gold = [2] },
                new() { Condition = Dan.Condition.Drumroll, IsLess = false, Clear = [86, 41, 134], Gold = [106, 49, 146] }
            }
        };

        public static Dan Tatsujin = new()
        {
            Title = "達人",
            TitleEN = "Tatsujin",
            Color = Gold,
            Url = "https://wikiwiki.jp/taiko-fumen/%E6%AE%B5%E4%BD%8D%E9%81%93%E5%A0%B4/%E3%83%8B%E3%82%B8%E3%82%A4%E3%83%AD2025/%E9%81%94%E4%BA%BA",

            Song1 = new() { Title = "POLARiSNAUT", Difficulty = Song.SongDifficulty.Extreme },
            Song2 = new() { Title = "Emma", Difficulty = Song.SongDifficulty.Hidden },
            Song3 = new() { Title = "vs.VIGVANGS", Difficulty = Song.SongDifficulty.Hidden },

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
