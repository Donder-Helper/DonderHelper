using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DonderHelper
{
    public static class SongDatabase
    {
        // Updates songlist during Debug mode
#if DEBUG
        public static string jsonpath = $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}songs.json";
#else
        public static string jsonpath = $"Data{Path.DirectorySeparatorChar}songs.json";
#endif

        public static bool Update(List<Song> songs)
        {
            JsonSerializer serializer = new JsonSerializer() { Formatting = Formatting.Indented };

            using (var file_stream = File.CreateText(jsonpath))
            {
                serializer.Serialize(file_stream, Program.__songs);
            }

            return true;
        }
    }
}
