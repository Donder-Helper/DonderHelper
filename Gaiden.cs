using Discord;
using Newtonsoft.Json;

namespace DonderHelper
{
    public class Gaiden : Dan
    {
        [JsonIgnore]
        public string Subtitle => SubtitleList.TryGetValue("ja", out string? subtitle) ? (subtitle ?? "") : "";
        [JsonProperty("subtitle")]
        public Dictionary<string, string> SubtitleList = new() {
            { "ja", "" }
        };
        public string GetSubtitle(string locale) { return SubtitleList.TryGetValue(locale, out var subtitle) ? subtitle : Subtitle; }

        [JsonProperty("qr_url")]
        public string QRUrl = "";

        public new ApplicationCommandOptionChoiceProperties AsChoice()
        {
            return new ApplicationCommandOptionChoiceProperties
            {
                Name = Subtitle,
                Value = Subtitle,
                NameLocalizations = SubtitleList
            };
        }

        public Gaiden() : base() { Title = "外伝"; TitleEN = "Gaiden"; Color = "gaiden"; }
    }

    public static class GaidenSonglist {
        public static Dictionary<string, Gaiden> Gaidens = [];
        public static Dictionary<string, string> GaidenNames = [];
    }
}
