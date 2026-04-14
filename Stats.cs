using Newtonsoft.Json;

namespace DonderHelper
{
    public class Stats
    {
        public struct Region
        {
            [JsonProperty("japan")]
            public int Japan;
            [JsonProperty("asia")]
            public int Asia;
            [JsonProperty("oceania")]
            public int Oceania;
            [JsonProperty("united-states")]
            public int UnitedStates;
            [JsonProperty("china")]
            public int China;

            public Region() { Japan = 0; Asia = 0; Oceania = 0; UnitedStates = 0; China = 0; }
        }

        public struct Language
        {
            [JsonProperty("ja")]
            public int Japanese;
            [JsonProperty("en-US")]
            public int English;
            [JsonProperty("ko")]
            public int Korean;
            [JsonProperty("zh-TW")]
            public int TradChinese;
            [JsonProperty("zh-CN")]
            public int SimpChinese;

            public Language() { Japanese = 0; English = 0; Korean = 0; TradChinese = 0; SimpChinese = 0; }
        }

        [JsonProperty("total_count")]
        public int TotalSongs;
        [JsonProperty("available_all_count")]
        public int AvailableAll;
        [JsonProperty("unavailable_all_count")]
        public int UnavailableAll;

        [JsonProperty("available_count")]
        public Region Available;
        [JsonProperty("exclusive_count")]
        public Region Exclusive;
        [JsonProperty("excluded_count")]
        public Region Excluded;
        [JsonProperty("unknown_count")]
        public Region Unknown;

        [JsonProperty("complete_title_count")]
        public long CompleteTitleCount;
        [JsonProperty("title_count")]
        public Language TitleCount;

        public Stats()
        {
            TotalSongs = 0; AvailableAll = 0; UnavailableAll = 0;
            Available = new(); Exclusive = new(); Excluded = new(); Unknown = new();
            TitleCount = new(); CompleteTitleCount = 0;
        }
    }
}
