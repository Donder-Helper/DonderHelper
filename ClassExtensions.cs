using static DonderHelper.Song;

namespace DonderHelper
{
    public static class SongExtensions
    {
        public static bool IsAvailable(this Availability region)
        {
            return region != Availability.No && region != Availability.CampaignNo && region != Availability.Unknown && region != Availability.Transfer;
        }
        public static bool IsExclusive(this Availability region, params Availability[] regions_to_compare)
        {
            return region.IsAvailable() && regions_to_compare.All(check => !check.IsAvailable());
        }
        public static bool IsWithinRange(this int value, int min, int max) => value >= min && value <= max;
        public static bool IsSuccessStatusCode(this int value) => value.IsWithinRange(200, 299);
    }
}
