using Newtonsoft.Json;

namespace DonderHelper
{
    public class SongDatabase
    {
        public static string SERVER_URL { get; } = Environment.GetEnvironmentVariable(nameof(SERVER_URL)) ?? "http://localhost:8181/";
        public static int TIMEOUT_MS { get; } = int.TryParse(Environment.GetEnvironmentVariable(nameof(TIMEOUT_MS)), out int result) ? result : 5000;
        public static int CACHE_DURATION_SEC { get; } = int.TryParse(Environment.GetEnvironmentVariable(nameof(CACHE_DURATION_SEC)), out int result) ? result : 3600 * 4;
        public static int STATS_REFRESH_HR { get; } = int.TryParse(Environment.GetEnvironmentVariable(nameof(STATS_REFRESH_HR)), out int result) ? result : 8;

        public static Stats Stats => _songStats;
        protected static Dictionary<string, CachedSearch> SearchCache => _searchCache;
        protected static Dictionary<int, CachedSong> SongCache => _songCache;

        private static Stats _songStats = new();
        private static Dictionary<int, CachedSong> _songCache = [];
        private static Dictionary<string, CachedSearch> _searchCache = [];

        private static HttpClient _httpClient = new();

        protected struct CachedSearch
        {
            public DateTime Expires;
            public List<int> IDs;
            public readonly bool Expired => DateTime.UtcNow >= Expires;
        }
        protected struct CachedSong
        {
            public DateTime Expires;
            public Song Song;
            public readonly bool Expired => DateTime.UtcNow >= Expires;
        }

        public static void Initialize()
        {
            _httpClient = new() {
                BaseAddress = new Uri(SERVER_URL),
                Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS)
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public static async Task<Song?> GetSong(int id)
        {
            var response = await GetSongs(id);
            Song? song = response.Values.FirstOrDefault();
            return song;
        }
        public static async Task<Dictionary<int, Song>> GetSongs(string search)
        {
            var ids = await SearchIDs(search);
            return await GetSongs([..ids]);
        }

        public static async Task<Dictionary<int, Song>> GetSongs(params int[] ids)
        {
            Dictionary<int, Song> songs = [];
            if (TryGetAllStoredSongCache(ids.ToList(), out songs, out List<int> stale_ids))
            {
                return songs;
            }
            string url = $"/song?id={string.Join(',', stale_ids)}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                LogResult(url, response);
                return [];
            }
            var stale_songs = JsonConvert.DeserializeObject<Dictionary<int, Song>>(await response.Content.ReadAsStringAsync()) ?? [];
            
            foreach (var song in stale_songs)
            {
                songs[song.Key] = song.Value;
                AddToSongCache(song.Key, song.Value);
            }

            LogResult(url, response);
            return songs;
        }

        public static async Task<Dictionary<int, Song>> GetRandomSongs(int count, Song.SongGenre? genre = null, Song.SongDifficulty? difficulty = null, int? level = null)
        {
            string url = $"/random?limit={count}&genre={(int?)genre}&diff={(int?)difficulty}&level={level}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                LogResult(url, response);
                return [];
            }
            Dictionary<int, Song> songs = JsonConvert.DeserializeObject<Dictionary<int, Song>>(await response.Content.ReadAsStringAsync()) ?? [];

            LogResult(url, response);
            return songs;
        }

        public static async Task<List<int>> SearchIDs(string search)
        {
            search = Uri.EscapeDataString(search);
            string url = $"/search?title={search}&subtitle={search}&title_comparison=or";

            if (TryGetStoredSearchCache(url, out List<int> cache))
            {
                return cache;
            }

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                LogResult(url, response);
                return [];
            }
            List<int> ids = JsonConvert.DeserializeObject<List<int>>(await response.Content.ReadAsStringAsync()) ?? [];

            AddToSearchCache(url, ids);
            LogResult(url, response);
            return ids;
        }

        public static async Task<int> UpdateStats()
        {
            var result = await GetStats();
            if (result.Item1.IsSuccessStatusCode())
            {
                lock (Stats)
                {
                    _songStats = result.Item2;
                }
            }
            return result.Item1;
        }

        private static async Task<(int, Stats)> GetStats()
        {
            string url = "/stats";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                LogResult(url, response);
                return new();
            }

            Stats stats = JsonConvert.DeserializeObject<Stats>(await response.Content.ReadAsStringAsync()) ?? new();
            LogResult(url, response);
            return ((int)response.StatusCode, stats);
        }

        #region Cache
        private static void AddToSongCache(int id, Song value)
        {
            lock (SongCache)
            {
                _songCache[id] = new() { Expires = DateTime.UtcNow + TimeSpan.FromSeconds(CACHE_DURATION_SEC), Song = value };
            }
        }
        private static void AddToSearchCache(string url, List<int> value)
        {
            lock (SearchCache)
            {
                _searchCache[url] = new() { Expires = DateTime.UtcNow + TimeSpan.FromSeconds(CACHE_DURATION_SEC), IDs = value };
            }
        }

        private static void RemoveFromSongCache(params int[] ids)
        {
            lock (SongCache)
            {
                foreach (int id in ids)
                _songCache.Remove(id);
            }
        }
        private static void RemoveFromSearchCache(params string[] urls)
        {
            lock (SearchCache)
            {
                foreach (string url in urls)
                _searchCache.Remove(url);
            }
        }

        private static bool TryGetAllStoredSongCache(List<int> ids, out Dictionary<int, Song> results, out List<int> stale_ids)
        {
            results = [];
            stale_ids = [];
            bool all_unstale = true;

            foreach (int id in ids)
            {
                if (TryGetStoredSongCache(id, out Song song))
                {
                    results[id] = song;
                }
                else
                {
                    stale_ids.Add(id);
                    all_unstale = false;
                }
            }
            return all_unstale;
        }
        private static bool TryGetStoredSongCache(int id, out Song value)
        {
            if (SongCache.TryGetValue(id, out CachedSong cached))
            {
                value = cached.Song;

                if (cached.Expired)
                {
                    RemoveFromSongCache(id);
                    return false;
                }

                return true;
            }
            value = new();
            return false;
        }
        private static bool TryGetStoredSearchCache(string url, out List<int> value)
        {
            if (SearchCache.TryGetValue(url, out CachedSearch cached))
            {
                value = cached.IDs;

                if (cached.Expired)
                {
                    RemoveFromSearchCache(url);
                    return false;
                }

                return true;
            }
            value = new();
            return false;
        }

        public static void CleanStaleCache()
        {
            RemoveFromSongCache(SongCache.Where(item => item.Value.Expired).Select(item => item.Key).ToArray());
            RemoveFromSearchCache(SearchCache.Where(item => item.Value.Expired).Select(item => item.Key).ToArray());
        }
        #endregion

        private static void LogResult(string url, HttpResponseMessage response)
        {
            Console.WriteLine($"URL '{url}' finished with return code {(int)response.StatusCode} {response.ReasonPhrase}");
        }
    }
}
