using Discord;
using Newtonsoft.Json;

namespace DonderHelper
{
    public class Song
    {
        public static SongGenre GetGenreFromString(string genre)
        {
            switch (genre)
            {
                case "pop": return SongGenre.Pop;
                case "kids": return SongGenre.Kids;
                case "anime": return SongGenre.Anime;
                case "game": return SongGenre.Game;
                case "vocaloid": return SongGenre.Vocaloid;
                case "variety": return SongGenre.Variety;
                case "classical": return SongGenre.Classical;
                case "namco": return SongGenre.Namco;
                default: return SongGenre.Unknown;
            }
        }
        public static Color GetGenreAsColor(SongGenre genre)
        {
            switch (genre)
            {
                case SongGenre.Pop: return new(0x42C0D3);
                case SongGenre.Kids: return new(0xFEC000);
                case SongGenre.Anime: return new(0xFF90D2);
                case SongGenre.Game: return new(0xCC8BEC);
                case SongGenre.Vocaloid: return new(0xCCCFDE);
                case SongGenre.Variety: return new(0x1BC73A);
                case SongGenre.Classical: return new(0xCAC001);
                case SongGenre.Namco: return new(0xFF7028);
                default: return new(0x202020);
            }
        }
        public static SongDifficulty GetDifficultyFromString(string diff)
        {
            switch (diff)
            {
                case "easy": return SongDifficulty.Easy;
                case "normal": return SongDifficulty.Normal;
                case "hard": return SongDifficulty.Hard;
                case "ex": return SongDifficulty.Extreme;
                case "hidden": return SongDifficulty.Hidden;
                default: return SongDifficulty.Extreme;
            }
        }

        public enum Availability
        {
            Unknown = -1,
            No,
            Yes,
            Campaign,
            CampaignNo,
            Shop,
            AIBattle,
            QRCode,
            Transfer
        }
        public struct RegionAvailability
        {
            public Availability Japan;
            public Availability Asia;
            public Availability Oceania;
            public Availability UnitedStates;
            public Availability China;

            [JsonIgnore]
            public bool IsAvailable { get
                {
                    return
                        (Japan == Availability.Yes || Japan == Availability.Campaign || Japan == Availability.Shop || Japan == Availability.AIBattle) &&
                        (Asia == Availability.Yes || Asia == Availability.Campaign || Asia == Availability.Shop || Asia == Availability.AIBattle) &&
                        (Oceania == Availability.Yes || Oceania == Availability.Campaign || Oceania == Availability.Shop || Oceania == Availability.AIBattle) &&
                        (UnitedStates == Availability.Yes || UnitedStates == Availability.Campaign || UnitedStates == Availability.Shop || UnitedStates == Availability.AIBattle) &&
                        (China == Availability.Yes || China == Availability.Campaign || China == Availability.Shop || China == Availability.AIBattle);
                } 
            }
            [JsonIgnore]
            public bool IsUnavailable => Japan == Availability.No && Asia == Availability.No && Oceania == Availability.No && UnitedStates == Availability.No && China == Availability.No;
            [JsonIgnore]
            public bool IsJapanOnly => Japan != Availability.No && Asia == Availability.No && Oceania == Availability.No && UnitedStates == Availability.No && China == Availability.No;
            [JsonIgnore]
            public bool IsAsiaOnly => Japan == Availability.No && Asia != Availability.No && Oceania == Availability.No && UnitedStates == Availability.No && China == Availability.No;
            [JsonIgnore]
            public bool IsOceaniaOnly => Japan == Availability.No && Asia == Availability.No && Oceania != Availability.No && UnitedStates == Availability.No && China == Availability.No;
            [JsonIgnore]
            public bool IsUSAOnly => Japan == Availability.No && Asia == Availability.No && Oceania == Availability.No && UnitedStates != Availability.No && China == Availability.No;
            [JsonIgnore]
            public bool IsChinaOnly => Japan == Availability.No && Asia == Availability.No && Oceania == Availability.No && UnitedStates == Availability.No && China != Availability.No;
            [JsonIgnore]
            public bool ContainsUnknown => Japan == Availability.Unknown || Asia == Availability.Unknown || Oceania == Availability.Unknown || UnitedStates == Availability.Unknown || China == Availability.Unknown;
        }
        public struct Chart
        {
            public struct Notes
            {
                public struct Branch
                {
                    [JsonIgnore]
                    public bool IsValid => Normal > 0 || Expert > 0 || Tatsujin > 0;
                    [JsonIgnore]
                    public readonly bool IsBranching => Expert > 0 || Tatsujin > 0;
                    public override string ToString()
                    {
                        return IsValid ?
                            (IsBranching ? $"(:twisted_rightwards_arrows: {(Normal > 0 ? Normal : "-")}/{(Expert > 0 ? Expert : "-")}/{(Tatsujin > 0 ? Tatsujin : "-")})"
                            : $"({(Normal > 0 ? Normal : "-")})")
                        : "";
                    }
                    public void Set(int normal, int expert, int tatsujin) { Normal = normal; Expert = expert; Tatsujin = tatsujin; }
                    public int[] Get() { return [Normal, Expert, Tatsujin]; }
                    public bool ContainsNotes() { return Normal > 0 || Expert > 0 || Tatsujin > 0; }

                    public int Normal;
                    public int Expert;
                    public int Tatsujin;
                }
                public override string ToString()
                {
                    if (Single.IsValid && Double1P.IsValid && Double2P.IsValid)
                        return $":bust_in_silhouette: {Single} / :one: {Double1P} / :two: {Double2P}";
                    return (Double1P.IsValid || Double2P.IsValid) ? 
                        $":one: {Double1P} / :two: {Double2P}" : 
                        Single.ToString();
                }
                public bool ContainsNotes() { return Single.ContainsNotes() || Double1P.ContainsNotes() || Double2P.ContainsNotes(); }
                public Branch Single;
                public Branch Double1P;
                public Branch Double2P;
            }
            public int Level;
            public Notes NoteCount;
            public string Url;
            public string UrlKo;
            public string ImageUrl;
        }
        public struct Difficulty
        {
            public Chart Easy;
            public Chart Normal;
            public Chart Hard;
            public Chart Extreme;
            public Chart Hidden;
            public Chart this[int value]
            {
                get
                {
                    switch (value)
                    {
                        case 0: return Easy;
                        case 1: return Normal;
                        case 2: return Hard;
                        case 3: return Extreme;
                        case 4: return Hidden;
                        default: throw new IndexOutOfRangeException();
                    }
                }
            }

            public Chart this[string value]
            {
                get
                {
                    switch (value)
                    {
                        case "easy": return Easy;
                        case "normal": return Normal;
                        case "hard": return Hard;
                        case "ex": return Extreme;
                        case "hidden": return Hidden;
                        default: return Extreme;
                    }
                }
            }

            public Chart this[SongDifficulty value]
            {
                get
                {
                    switch (value)
                    {
                        case SongDifficulty.Easy: return Easy;
                        case SongDifficulty.Normal: return Normal;
                        case SongDifficulty.Hard: return Hard;
                        case SongDifficulty.Extreme: return Extreme;
                        case SongDifficulty.Hidden: return Hidden;
                        default: return Extreme;
                    }
                }
            }
            public bool ContainsNotes()
            {
                return Easy.NoteCount.ContainsNotes() || Normal.NoteCount.ContainsNotes() || Hard.NoteCount.ContainsNotes() || Extreme.NoteCount.ContainsNotes() || Hidden.NoteCount.ContainsNotes();
            }
        }
        public enum SongGenre
        {
            Unknown = -1,
            Pop,
            Anime,
            Game,
            Vocaloid,
            Variety,
            Kids,
            Classical,
            Namco
        }
        public enum SongDifficulty
        {
            Easy,
            Normal,
            Hard,
            Extreme,
            Hidden
        }

        /// <summary>
        /// Default title
        /// </summary>
        [JsonIgnore]
        public string Title { get { return TitleList.ContainsKey("ja") ? TitleList["ja"] : "???"; } }
        /// <summary>
        /// Default subtitle
        /// </summary>
        [JsonIgnore]
        public string Subtitle { get { return SubtitleList.ContainsKey("ja") ? SubtitleList["ja"] : ""; } }
        /// <summary>
        /// Default genre
        /// </summary>
        [JsonIgnore]
        public SongGenre Genre { get { return GenreList.Count > 0 ? GenreList[0] : SongGenre.Unknown; } }

        public Dictionary<string, string> TitleList { get; private set; } = [];
        public Dictionary<string, string> SubtitleList { get; private set; } = [];
        public List<SongGenre> GenreList { get; private set; } = [];

        public RegionAvailability Region = new() { 
            Japan = Availability.Unknown, 
            Asia = Availability.Unknown, 
            Oceania = Availability.Unknown, 
            UnitedStates = Availability.Unknown, 
            China = Availability.Unknown
        };

        public Difficulty Difficulties = new()
        {
            Easy = new() { Level = -1, Url = "", UrlKo = "", ImageUrl = "" },
            Normal = new() { Level = -1, Url = "", UrlKo = "", ImageUrl = "" },
            Hard = new() { Level = -1, Url = "", UrlKo = "", ImageUrl = "" },
            Extreme = new() { Level = -1, Url = "", UrlKo = "", ImageUrl = "" },
            Hidden = new() { Level = -1, Url = "", UrlKo = "", ImageUrl = "" }
        };

        #region Title
        public void SetTitle(string title, string lang = "ja") { 
            if (TitleList.ContainsKey(lang))
                TitleList.Remove(lang);
            TitleList.Add(lang, title);
        }
        public string GetTitle(string lang = "ja") => TitleList.TryGetValue(lang, out string? output) ? output : Title;
        public bool TryGetTitle(string lang, out string? title) => TitleList.TryGetValue(lang, out title);
        public string GetTitleList(bool include_emoji, string priority_locale = "")
        {
            List<string> titles = [];
            List<string> locales = [ "ja", "en-US", "ko", "zh-TW", "zh-CN" ];
            if (TryGetTitle(priority_locale, out string? title))
            {
                titles.Add((include_emoji ? LocaleData.GetLocaleAsEmoji(priority_locale) + " " : "") + (title ?? ""));
                locales.Remove(priority_locale);
            }

            foreach (string locale in locales)
            {
                if (TryGetTitle(locale, out string? output))
                {
                    titles.Add((include_emoji ? LocaleData.GetLocaleAsEmoji(locale) + " " : "") + output);
                }
            }
            return string.Join('\n', titles);
        }
        #endregion

        #region Subtitle
        public void SetSubtitle(string subtitle, string lang = "ja")
        {
            if (SubtitleList.ContainsKey(lang))
                SubtitleList.Remove(lang);
            SubtitleList.Add(lang, subtitle);
        }
        public string GetSubtitle(string lang = "ja") => SubtitleList.TryGetValue(lang, out string? output) ? output : Subtitle;
        public bool TryGetSubtitle(string lang, out string? subtitle) => SubtitleList.TryGetValue(lang, out subtitle);
        public string GetSubtitleList(bool include_emoji, string priority_locale = "")
        {
            List<string> subtitles = [];
            List<string> locales = ["ja", "en-US", "ko", "zh-TW", "zh-CN"];
            if (TryGetSubtitle(priority_locale, out string? title))
            {
                subtitles.Add((include_emoji ? LocaleData.GetLocaleAsEmoji(priority_locale) + " " : "") + (title ?? ""));
                locales.Remove(priority_locale);
            }

            foreach (string locale in locales)
            {
                if (TryGetSubtitle(locale, out string? output))
                {
                    subtitles.Add((include_emoji ? LocaleData.GetLocaleAsEmoji(locale) + " " : "") + output);
                }
            }
            return string.Join('\n', subtitles);
        }
        #endregion

        #region Genre
        public void SetPriorityGenre(SongGenre genre)
        {
            int genresort(SongGenre maingenre, SongGenre item)
            {
                return item == maingenre ? -1 : (int)item;
            }

            if (GenreList.Contains(genre))
                GenreList = GenreList.OrderBy(item => genresort(genre, item)).ToList();
            else
                GenreList = GenreList.Prepend(genre).ToList();
        }
        public void AddGenre(SongGenre genre) { if (genre != SongGenre.Unknown && !GenreList.Contains(genre)) GenreList.Add(genre); }
        public List<SongGenre> GetAllGenres() => GenreList;
        #endregion

        public Song()
        {
            SetTitle("???");
            SetSubtitle("");
        }
    }
}
