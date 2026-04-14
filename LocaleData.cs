using System.Diagnostics.CodeAnalysis;

namespace DonderHelper
{
    public static class LocaleData
    {
        private static Dictionary<string, Dictionary<string, string>> dicts = [];

        public static void Initialize()
        {
            Dictionary<string, string> AddKeys(string path)
            {
                if (File.Exists(path))
                {
                    Dictionary<string, string> dict = [];
                    string[] strings = File.ReadAllLines(path).Where(item => !string.IsNullOrWhiteSpace(item) && !item.StartsWith("//")).ToArray();
                    foreach (string s in strings)
                    {
                        if (!s.Contains('=')) continue;
                        string[] entry = s.Split('=', 2);
                        dict.TryAdd(entry[0], entry[1].Replace("\\n", "\n"));
                    }
                    return dict;
                }
                return [];
            }

            foreach (string textfile in Directory.GetFiles("Lang", "*.txt", SearchOption.TopDirectoryOnly))
            {
                string locale = Path.GetFileNameWithoutExtension(textfile).Replace(".txt", "");
                dicts.TryAdd(locale, AddKeys(textfile));
            }
        }
        public static bool HasLanguage(string locale) => dicts.ContainsKey(locale);

        public static bool HasKey(string key, string locale)
        {
            return TryGetString(key, locale, out _);
        }

        public static bool TryGetString(string input, string locale, [MaybeNullWhen(false)] out string result)
        {
            result = null;
            return dicts.TryGetValue(locale, out var dict) && dict.TryGetValue(input, out result);
        }

        public static string GetString(string input, string locale, params object?[] items)
        {
            object?[] items_safe = items != null ? items : [""];
            var dict = dicts.TryGetValue(locale, out var dictionary) ? dictionary : dicts["en-US"];

            for (int i = 0; i < items_safe.Length; i++)
            {
                if (dict.TryGetValue(items_safe[i]?.ToString() ?? "", out string? output_key))
                    items_safe[i] = output_key;
            }
            return dict.TryGetValue(input, out string? output) ? String.Format(output, items_safe) : $"INVALID KEY: {input}";
        }
        public static Dictionary<string, string> GetStrings(string input, params object?[] items)
        {
            Dictionary<string, string> strings = [];
            foreach (var dict in dicts) {
                strings.Add(dict.Key, GetString(input, dict.Key, items));
                if (dict.Key == "en-US") strings.Add("en-GB", GetString(input, dict.Key, items));
            }
            return strings;
        }
        public static string GetAIBattleItemInfo(string name, string type, int wins, string locale)
        {
            return GetString("CAMPAIGN_AIBATTLE_DESC", locale, wins, GetItemInfo(name, type, locale));
        }
        public static string GetItemInfo(string name, string type, string locale)
        {
            return GetString("ITEM_INFO", locale, name, GetString(type, locale));
        }
        #region Song
        public static string GetDifficulty(Song.SongDifficulty difficulty, string locale)
        {
            switch (difficulty)
            {
                case Song.SongDifficulty.Easy: return GetString("DIFFICULTY_EASY", locale);
                case Song.SongDifficulty.Normal: return GetString("DIFFICULTY_NORMAL", locale);
                case Song.SongDifficulty.Hard: return GetString("DIFFICULTY_HARD", locale);
                case Song.SongDifficulty.Extreme: return GetString("DIFFICULTY_EX", locale);
                case Song.SongDifficulty.Hidden: return GetString("DIFFICULTY_HIDDEN", locale);
                default: return "???";
            }
        }
        public static string GetAvailability(Song.Availability availability, string locale)
        {
            switch (availability)
            {
                case Song.Availability.No: return GetString("AVAILABLE_NO", locale);
                case Song.Availability.Yes: return GetString("AVAILABLE_YES", locale);
                case Song.Availability.Campaign: return GetString("AVAILABLE_CAMPAIGN", locale);
                case Song.Availability.CampaignNo: return GetString("AVAILABLE_CAMPAIGNNO", locale);
                case Song.Availability.Shop: return GetString("AVAILABLE_SHOP", locale);
                case Song.Availability.AIBattle: return GetString("AVAILABLE_AIBATTLE", locale);
                case Song.Availability.QRCode: return GetString("AVAILABLE_QRCODE", locale);
                case Song.Availability.Transfer: return GetString("AVAILABLE_TRANSFER", locale);
                default: return GetString("AVAILABLE_UNKNOWN", locale);
            }
        }
        public static string GetGenreAsString(Song.SongGenre genre, string locale)
        {
            switch (genre)
            {
                case Song.SongGenre.Pop: return GetString("GENRE_POP", locale);
                case Song.SongGenre.Kids: return GetString("GENRE_KIDS", locale);
                case Song.SongGenre.Anime: return GetString("GENRE_ANIME", locale);
                case Song.SongGenre.Game: return GetString("GENRE_GAME", locale);
                case Song.SongGenre.Vocaloid: return GetString("GENRE_VOCALOID", locale);
                case Song.SongGenre.Variety: return GetString("GENRE_VARIETY", locale);
                case Song.SongGenre.Classical: return GetString("GENRE_CLASSICAL", locale);
                case Song.SongGenre.Namco: return GetString("GENRE_NAMCO", locale);
                default: return GetString("GENRE_UNKNOWN", locale);
            }
        }
        public static Dictionary<string, string> GetGenreAsStrings(Song.SongGenre genre)
        {
            Dictionary<string, string> strings = [];
            foreach (var dict in dicts)
                strings.Add(dict.Key, GetGenreAsString(genre, dict.Key));
            return strings;
        }
        public static string GetJapanRegionStatusAsString(Song song, string locale)
        {
            return GetString("AVAILABLE_REGION", locale,
                GetString("REGION_JAPAN", locale),
                EmoteData.GetAvailability(song.Region.Japan),
                GetAvailability(song.Region.Japan, locale));
        }
        public static string GetAsiaRegionStatusAsString(Song song, string locale)
        {
            return GetString("AVAILABLE_REGION", locale,
                GetString("REGION_ASIA", locale),
                EmoteData.GetAvailability(song.Region.Asia),
                GetAvailability(song.Region.Asia, locale));
        }
        public static string GetOceaniaRegionStatusAsString(Song song, string locale)
        {
            return GetString("AVAILABLE_REGION", locale,
                GetString("REGION_OCEANIA", locale),
                EmoteData.GetAvailability(song.Region.Oceania),
                GetAvailability(song.Region.Oceania, locale));
        }
        public static string GetUSARegionStatusAsString(Song song, string locale)
        {
            return GetString("AVAILABLE_REGION", locale,
                GetString("REGION_USA", locale),
                EmoteData.GetAvailability(song.Region.UnitedStates),
                GetAvailability(song.Region.UnitedStates, locale));
        }
        public static string GetChinaRegionStatusAsString(Song song, string locale)
        {
            return GetString("AVAILABLE_REGION", locale,
                GetString("REGION_CHINA", locale),
                EmoteData.GetAvailability(song.Region.China),
                GetAvailability(song.Region.China, locale));
        }
        #endregion
        public static string GetLocaleAsEmoji(string locale)
        {
            return locale switch
            {
                "ja" => ":flag_jp:",
                "en-US" => ":flag_au:",
                "ko" => ":flag_kr:",
                "zh-TW" => ":flag_tw:",
                "zh-CN" => ":flag_cn:",
                _ => ""
            };
        }
        public static string GetPreferredLocale(string lang)
        {
            return TryGetString("PREFERRED_LOCALE", lang, out string? locale) ? locale : lang;
        }
    }
}
