using Discord;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

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
            [JsonProperty("japan")]
            public Availability Japan;
            [JsonProperty("asia")]
            public Availability Asia;
            [JsonProperty("oceania")]
            public Availability Oceania;
            [JsonProperty("united-states")]
            public Availability UnitedStates;
            [JsonProperty("china")]
            public Availability China;

            private static bool IsAvailable(Availability region) => region.IsAvailable();

            [JsonIgnore]
            public readonly bool IsAvailableEverywhere => IsAvailable(Japan) && IsAvailable(Asia) && IsAvailable(Oceania) && IsAvailable(UnitedStates) && IsAvailable(China);
            [JsonIgnore]
            public readonly bool IsUnavailableEverywhere => !IsAvailable(Japan) && !IsAvailable(Asia) && !IsAvailable(Oceania) && !IsAvailable(UnitedStates) && !IsAvailable(China) && Japan != Availability.Unknown;
            [JsonIgnore]
            public readonly bool ContainsUnknown => Japan == Availability.Unknown || Asia == Availability.Unknown || Oceania == Availability.Unknown || UnitedStates == Availability.Unknown || China == Availability.Unknown;

            [JsonIgnore]
            public readonly bool IsJapanOnly => Japan.IsExclusive(Asia, Oceania, UnitedStates, China);
            [JsonIgnore]
            public readonly bool IsAsiaOnly => Asia.IsExclusive(Japan, Oceania, UnitedStates, China);
            [JsonIgnore]
            public readonly bool IsOceaniaOnly => Oceania.IsExclusive(Japan, Asia, UnitedStates, China);
            [JsonIgnore]
            public readonly bool IsUnitedStatesOnly => UnitedStates.IsExclusive(Japan, Asia, Oceania, China);
            [JsonIgnore]
            public readonly bool IsChinaOnly => China.IsExclusive(Japan, Asia, Oceania, UnitedStates);
        }
        public struct Chart
        {
            public struct Notes
            {
                public struct Branch
                {
                    [JsonIgnore]
                    public readonly bool IsValid => Normal > 0 || Expert > 0 || Tatsujin > 0;
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
                    public readonly int[] Get() => [Normal, Expert, Tatsujin];
                    public readonly bool ContainsNotes() => Normal > 0 || Expert > 0 || Tatsujin > 0;

                    [JsonProperty("normal")]
                    public int Normal;
                    [JsonProperty("expert")]
                    public int Expert;
                    [JsonProperty("master")]
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
                public readonly bool ContainsNotes() => Single.ContainsNotes() || Double1P.ContainsNotes() || Double2P.ContainsNotes();

                [JsonProperty("0")]
                public Branch Single;
                [JsonProperty("1")]
                public Branch Double1P;
                [JsonProperty("2")]
                public Branch Double2P;
            }
            [JsonProperty("level")]
            public int Level;
            [JsonProperty("style_list")]
            public Notes NoteCount;
            [JsonProperty("source_list")]
            public Dictionary<string, string> SourceUrls;
            public string ImageUrl;
        }
        public struct Difficulty
        {
            [JsonProperty("1")]
            public Chart Easy;
            [JsonProperty("2")]
            public Chart Normal;
            [JsonProperty("3")]
            public Chart Hard;
            [JsonProperty("4")]
            public Chart Extreme;
            [JsonProperty("5")]
            public Chart Hidden;
            public Chart this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return Easy;
                        case 1: return Normal;
                        case 2: return Hard;
                        case 3: return Extreme;
                        case 4: return Hidden;
                        default: throw new IndexOutOfRangeException();
                    }
                }
                set
                {
                    switch (index)
                    {
                        case 0: Easy = value; break;
                        case 1: Normal = value; break;
                        case 2: Hard = value; break;
                        case 3: Extreme = value; break;
                        case 4: Hidden = value; break;
                        default: throw new IndexOutOfRangeException();
                    }
                }
            }

            public Chart this[string name]
            {
                get
                {
                    switch (name.ToLower())
                    {
                        case "easy": return Easy;
                        case "normal": return Normal;
                        case "hard": return Hard;
                        case "ex": return Extreme;
                        case "hidden": return Hidden;
                        default: throw new IndexOutOfRangeException();
                    }
                }
                set
                {
                    switch (name.ToLower())
                    {
                        case "easy": Easy = value; break;
                        case "normal": Normal = value; break;
                        case "hard": Hard = value; break;
                        case "ex": Extreme = value; break;
                        case "hidden": Hidden = value; break;
                        default: throw new IndexOutOfRangeException();
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
            Unknown = 0,
            Pop,
            Anime,
            Kids,
            Vocaloid,
            Game,
            Namco,
            Variety,
            Classical
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
        public string Title { get { return TitleList.ContainsKey("ja") ? TitleList["ja"] : TitleList.Values.FirstOrDefault() ?? "???"; } }
        /// <summary>
        /// Default subtitle
        /// </summary>
        [JsonIgnore]
        public string Subtitle { get { return SubtitleList.ContainsKey("ja") ? SubtitleList["ja"] : SubtitleList.Values.FirstOrDefault() ?? ""; } }
        /// <summary>
        /// Default genre
        /// </summary>
        [JsonIgnore]
        public SongGenre Genre { get { return GenreList.Count > 0 ? GenreList[0] : SongGenre.Unknown; } }

        [JsonProperty("title_list")]
        public Dictionary<string, string> TitleList { get; private set; } = [];
        [JsonProperty("subtitle_list")]
        public Dictionary<string, string> SubtitleList { get; private set; } = [];
        [JsonProperty("genre_list")]
        public List<SongGenre> GenreList { get; private set; } = [];

        [JsonProperty("region_list")]
        public RegionAvailability Region = new() { 
            Japan = Availability.Unknown, 
            Asia = Availability.Unknown, 
            Oceania = Availability.Unknown, 
            UnitedStates = Availability.Unknown, 
            China = Availability.Unknown
        };

        [JsonProperty("chart_list")]
        public Difficulty Difficulties = new()
        {
            Easy = new() { Level = -1, SourceUrls = [], ImageUrl = "" },
            Normal = new() { Level = -1, SourceUrls = [], ImageUrl = "" },
            Hard = new() { Level = -1, SourceUrls = [], ImageUrl = "" },
            Extreme = new() { Level = -1, SourceUrls = [], ImageUrl = "" },
            Hidden = new() { Level = -1, SourceUrls = [], ImageUrl = "" }
        };

        #region Title
        public void SetTitle(string title, string lang = "ja") { 
            if (TitleList.ContainsKey(lang))
                TitleList.Remove(lang);
            TitleList.Add(lang, title);
        }
        public string GetTitle(string locale = "ja") => TitleList.TryGetValue(LocaleData.GetPreferredLocale(locale), out string? output) ? output : Title;
        public bool TryGetTitle(string locale, [MaybeNullWhen(false)] out string title) => TitleList.TryGetValue(LocaleData.GetPreferredLocale(locale), out title);
        public string GetTitleList(bool include_emoji, string priority_locale = "")
        {
            List<string> titles = [];
            List<string> locales = [ "ja", "en-US", "ko", "zh-TW", "zh-CN" ];
            if (!string.IsNullOrWhiteSpace(priority_locale)) priority_locale = LocaleData.GetPreferredLocale(priority_locale);

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
        public string GetSubtitle(string lang = "ja") => SubtitleList.TryGetValue(LocaleData.GetPreferredLocale(lang), out string? output) ? output : Subtitle;
        public bool TryGetSubtitle(string lang, [MaybeNullWhen(false)] out string subtitle) => SubtitleList.TryGetValue(LocaleData.GetPreferredLocale(lang), out subtitle);
        public string GetSubtitleList(bool include_emoji, string priority_locale = "")
        {
            List<string> subtitles = [];
            List<string> locales = ["ja", "en-US", "ko", "zh-TW", "zh-CN"];
            if (!string.IsNullOrWhiteSpace(priority_locale)) priority_locale = LocaleData.GetPreferredLocale(priority_locale);

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

        public Song() { }
    }
}
