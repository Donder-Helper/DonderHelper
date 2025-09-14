using System.Text.RegularExpressions;
using static DonderHelper.Song;

namespace DonderHelper
{
    public static class SongBuilder
    {
        public static Song CreateSongFromCSVString(string csv)
        {
            Song song = new Song();

            Availability GetAvailability(string text)
            {
                switch (text.ToLower())
                {
                    case "yes":
                        return Availability.Yes;
                    case "no":
                        return Availability.No;
                    case "campaign":
                        return Availability.Campaign;
                    case "no*":
                        return Availability.CampaignNo;
                    case "shop":
                        return Availability.Shop;
                    case "ai battle":
                        return Availability.AIBattle;
                    case "qr code":
                    case "qr":
                        return Availability.QRCode;
                    case "transfer":
                        return Availability.Transfer;
                    default:
                        return Availability.Unknown;
                }
            }
            SongGenre GetGenre(string text)
            {
                switch (text)
                {
                    case "PP": return SongGenre.Pop;
                    case "KD": return SongGenre.Kids;
                    case "AN": return SongGenre.Anime;
                    case "VC": return SongGenre.Vocaloid;
                    case "GM": return SongGenre.Game;
                    case "VA": return SongGenre.Variety;
                    case "CL": return SongGenre.Classical;
                    case "NO": return SongGenre.Namco;
                    default: return SongGenre.Unknown;
                }
            }

            var split = csv.Split('\t');

            for (int i = 1; i < split.Length; i++)
            {
                string result = split[i];
                if (i == 1 && result == "CN")
                {
                    song = new Song();
                    song.SetTitle(split[2].Substring(5).Trim());
                    song.SetGenre(GetGenre(split[2].Substring(1, 2)));
                    song.Region = new() { Japan = Availability.No, Asia = Availability.No, Oceania = Availability.No, UnitedStates = Availability.No, China = Availability.Yes };
                    break;
                }

                switch (i)
                {
                    // Genre
                    case 1:
                    {
                        song.SetGenre(GetGenre(result));
                        break;
                    }
                    // Title
                    case 2:
                    {
                        if (result.StartsWith('"') && result.EndsWith('"'))
                            result = result.Trim('"');
                        song.SetTitle(result);
                        break;
                    }
                    // Japan
                    case 4:
                    {
                        song.Region.Japan = GetAvailability(result);
                        break;
                    }
                    // Core Asia
                    case 5:
                    {
                        song.Region.Asia = GetAvailability(result);
                        break;
                    }
                    // Oceania / Other Asia
                    case 6:
                    {
                        song.Region.Oceania = GetAvailability(result);
                        break;
                    }
                    // North America
                    case 7:
                    {
                        song.Region.UnitedStates = GetAvailability(result);
                        break;
                    }
                    // China
                    case 8:
                    {
                        song.Region.China = GetAvailability(result);
                        break;
                    }
                }
            }
            return song;
        }
    }
}
