using Newtonsoft.Json;

namespace DonderHelper
{
    public static class SongDatabase
    {
        // Updates songlist during Debug mode
#if DEBUG
        private static string jsonpath = $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}songs.json";
#else
        private static string jsonpath = $"Data{Path.DirectorySeparatorChar}songs.json";
#endif

        public static bool Update(List<Song> songs)
        {
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };

            using (var file_stream = File.CreateText(jsonpath))
            {
                serializer.Serialize(file_stream, Program.__songs);
            }
            foreach (string path in Directory.GetFiles($"Data{Path.DirectorySeparatorChar}Gaidens", "*.json"))
            {
                var gaiden = JsonConvert.DeserializeObject<Gaiden>(File.ReadAllText(path));
                if (gaiden != null) { 
                    GaidenSonglist.Gaidens.TryAdd(gaiden.Subtitle, gaiden);
                    foreach (var titles in gaiden.SubtitleList) GaidenSonglist.GaidenNames.TryAdd(titles.Value, gaiden.Subtitle);
                }
            }
            GaidenSonglist.Gaidens = GaidenSonglist.Gaidens.Reverse().ToDictionary();

            return true;
        }
    }
}
