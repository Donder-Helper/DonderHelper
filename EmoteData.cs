using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DonderHelper
{
    public static class EmoteData
    {
        private static string path = $"Config{Path.DirectorySeparatorChar}emoji.txt";
        private static string _fallback = "<:blank:1416698560371752980>";

        private static Dictionary<string, string> emojis = [];

        public static void Initialize()
        {
            var lines = File.ReadAllLines(path).Where(item => !string.IsNullOrWhiteSpace(item));

            foreach (var line in lines) {
                if (!line.Contains("=")) continue;
                string[] entry = line.Split('=', 2);
                emojis.TryAdd(entry[0], entry[1]);
            }
        }

        public static IEmote GetEmote(string key) {
            return stringToIEmote(
                emojis.TryGetValue(key, out string? value)
                ? value ?? _fallback
                : _fallback);
        }

        public static IEmote GetDifficulty(Song.SongDifficulty difficulty)
        {
            return difficulty switch
            {
                Song.SongDifficulty.Easy => GetEmote("DIFFICULTY_EASY"),
                Song.SongDifficulty.Normal => GetEmote("DIFFICULTY_NORMAL"),
                Song.SongDifficulty.Hard => GetEmote("DIFFICULTY_HARD"),
                Song.SongDifficulty.Extreme => GetEmote("DIFFICULTY_EX"),
                Song.SongDifficulty.Hidden => GetEmote("DIFFICULTY_HIDDEN"),
                _ => GetEmote("DIFFICULTY_UNKNOWN")
            };
        }

        public static IEmote GetAvailability(Song.Availability availability)
        {
            return availability switch
            {
                Song.Availability.Yes => GetEmote("AVAILABLE_YES"),
                Song.Availability.No => GetEmote("AVAILABLE_NO"),
                Song.Availability.Campaign => GetEmote("AVAILABLE_CAMPAIGN"),
                Song.Availability.CampaignNo => GetEmote("AVAILABLE_CAMPAIGNNO"),
                Song.Availability.Shop => GetEmote("AVAILABLE_SHOP"),
                Song.Availability.AIBattle => GetEmote("AVAILABLE_AIBATTLE"),
                Song.Availability.QRCode => GetEmote("AVAILABLE_QRCODE"),
                Song.Availability.Transfer => GetEmote("AVAILABLE_TRANSFER"),
                _ => GetEmote("AVAILABLE_UNKNOWN")
            };
        }

        #region Private
        private static IEmote stringToIEmote(string key)
        {
            if (Emoji.TryParse(key, out var emoji)) { return emoji; }
            if (Emote.TryParse(key, out var emote)) { return emote; }
            return Emote.Parse(_fallback);
        }
        #endregion
    }
}
