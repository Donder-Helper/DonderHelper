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
        public string GetSubtitle(string locale) { return SubtitleList.TryGetValue(LocaleData.GetPreferredLocale(locale), out var subtitle) ? subtitle : Subtitle; }

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
        private static string gaidenspath = $"Data{Path.DirectorySeparatorChar}Gaidens";

        public static Dictionary<string, Gaiden> Gaidens = [];
        public static Dictionary<string, string> GaidenNames = [];

        public static void Initialize()
        {
            foreach (string path in Directory.GetFiles(gaidenspath, "*.json").Order())
            {
                var gaiden = JsonConvert.DeserializeObject<Gaiden>(File.ReadAllText(path));
                if (gaiden != null)
                {
                    Gaidens.TryAdd(gaiden.Subtitle, gaiden);
                    foreach (var titles in gaiden.SubtitleList) GaidenNames.TryAdd(titles.Value, gaiden.Subtitle);
                }
            }
            Gaidens = Gaidens.Reverse().ToDictionary();
        }
    }
}
