using Discord;
using Discord.WebSocket;
using Discord.Commands;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using Hnx8.ReadJEnc;

namespace DonderHelper
{
    internal class TaikoKoTitle()
    {
        [JsonProperty("title")]
        public string title = "";
        [JsonProperty("titleKo")]
        public string? ko_title = null;
        [JsonProperty("titleEn")]
        public string? en_title = null;
        [JsonProperty("songNo")]
        public string song_no = "";

        public struct Course
        {
            public string[]? images;
            public Course() { images = null; }
        }
        public struct Courses
        {
            [JsonProperty("easy")]
            public Course? easy;
            [JsonProperty("normal")]
            public Course? normal;
            [JsonProperty("hard")]
            public Course? hard;
            [JsonProperty("oni")]
            public Course? oni;
            [JsonProperty("ura")]
            public Course? ura;
            public Courses() { easy = null; normal = null; hard = null; oni = null; ura = null; }
        }

        [JsonProperty("courses")]
        public Courses courses;
    }
    public class Program()
    {
        // Discord bot private key
        private static string __keypath = $"key.txt";

        private static string __key = "";
        public static Dictionary<string, Song> __songs { get; private set; } = [];
        public static Dictionary<string, string> __songNames { get; private set; } = [];

#pragma warning disable CS8618
        private static DiscordSocketClient _client;
        private static CommandService _commandService;

        private static LoggingHandler _logginghandler;
        private static CommandsHandler _commandhandler;
#pragma warning restore CS8618

        public static string GetLocalizedSongTitle(string orig_title, string locale)
        {
            return __songs.TryGetValue(orig_title, out Song? song) ? song.GetTitle(locale) : orig_title;
        }

        public static async Task Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

#if !DEBUG
            Console.WriteLine("Starting in 20 seconds...");
            Thread.Sleep(20000);
#endif

            Console.WriteLine("Donhirobotスタート！ Let's starting!");
            LocaleData.Initialize();
            EmoteData.Initialize();

            if (!Boot()) {
                Console.WriteLine("Aborting launch & shutting down.");
                return;
            }
#if DEBUG
            SongDatabase.Update(__songs.Values.ToList());

            string tsv = "";
            string title_prepend(string title)
            {
                return title.StartsWith('"') ? ("\"\"\"" + title) : title;
            }
            foreach (var song in __songs.Values)
            {
                tsv += song.Genre + "\t" + song.Title + "\t" + song.Subtitle;
                foreach (string lang in new string[] {"en-US", "ko", "zh-TW", "zh-CN" })
                {
                    tsv += "\t";
                    tsv += (song.TryGetTitle(lang, out string? title) ? title_prepend(title ?? "") : "") + "\t" + (song.TryGetSubtitle(lang, out string? subtitle) ? title_prepend(subtitle ?? "") : "");
                }
                tsv += "\n";
            }
            File.WriteAllText($"Resources{Path.DirectorySeparatorChar}result.tsv", tsv);
#endif
            Console.WriteLine($"Finished! Loaded {__songs.Count} songs.");
            Console.WriteLine($"{__songs.Where(song => !song.Value.Difficulties.ContainsNotes()).ToDictionary().Count} songs do not contain or is missing note counts.");
            Console.WriteLine($"{__songNames.Count} entries for song names are available.");

            Console.WriteLine("Communicating with Discord...");

            DiscordSocketConfig _config = new() {
                GatewayIntents = GatewayIntents.Guilds
            };

            _client = new DiscordSocketClient(_config);
            _commandService = new CommandService();

            _logginghandler = new(_client, _commandService);
            _commandhandler = new(_client, _commandService);

#if DEBUG
            await _client.LoginAsync(TokenType.Bot, __key);
#else
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DONDERHELPER_SECRET_KEY")))
            {
                await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DONDERHELPER_SECRET_KEY"));
            }
            else if (!string.IsNullOrWhiteSpace(__key))
            {
                await _client.LoginAsync(TokenType.Bot, __key);
            }
            else
            {
                throw new Exception("Discord bot key could not be found in environment or text file.");
            }
#endif
            __key = "";
            await _client.StartAsync();
            await _client.SetCustomStatusAsync($"Drumming along to {__songs.Count} songs! ({CommandsHandler.last_Update})");

            Console.WriteLine("Logged in successfully!");

            // Block this task until the program is closed.
            await Process.GetCurrentProcess().WaitForExitAsync();

            await _client.LogoutAsync();
            await _client.StopAsync();
        }
        private static bool Boot()
        {

            try
            {
                if (File.Exists(__keypath))
                    __key = File.ReadAllText(__keypath);
#if !DEBUG
            __songs = JsonSerializer.Create(new() { Formatting = Formatting.Indented }).Deserialize<Dictionary<string, Song>>(new JsonTextReader( new StringReader(File.ReadAllText(SongDatabase.jsonpath))));
            foreach (var song in __songs)
            {
                //__songs.TryAdd(song.Title, song);
                foreach (var title in song.Value.TitleList)
                {
                    __songNames.TryAdd(title.Value, song.Key);
                }
            }
            // ^ testing something
#else
                SongBuilder.BuildSonglist();
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("Boot Failed.\n" + ex.ToString());
                return false;
            }
            return true;
        }
    }
}
