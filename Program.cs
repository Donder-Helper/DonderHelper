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
    public class Program()
    {
        // Discord bot private key
        private static readonly string __environmentname = "DONDERHELPER_SECRET_KEY";
        private static readonly string __keypath = $"key.txt";
        private static string __key = "";

#pragma warning disable CS8618
        private static DiscordSocketClient _client;
        private static CommandService _commandService;

        private static LoggingHandler _logginghandler;
        private static CommandsHandler _commandhandler;
#pragma warning restore CS8618

        public static async Task Main(params string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (args.Length > 0)
                Console.WriteLine($"Running with args: {string.Join(' ', args)}");

            #region Build Songlist
            if (args.Contains("--build-songlist"))
            {
                Console.WriteLine("Building songlist...");
#if DEBUG
                Console.WriteLine("HEADS UP! You are building the songlist in Debug Mode! The finished songlist will be created in '../../../Data/songs.json'. If you want to output the songlist to 'Data/songs.json', please compile a Release build instead.");
#endif
                try
                {
                    LocaleData.Initialize();
                    SongDatabase.BuildSonglist();
                    SongDatabase.WriteSonglist(args.Contains("--write-tsv"));
                }
                catch
                {
                    Console.WriteLine("NOOOOOOOOOOOO SOMETHING BROKE!!!!!!!");
                    throw;
                }
                Console.WriteLine("Songlist successfully built. Please run this executable again, without the '--build-songlist' arg, to run the Donder Helper bot.");
                return;
            }
            #endregion

            #region Skip Timer
            if (!args.Contains("--skip-timer"))
            {
                Console.WriteLine("Starting in 20 seconds...");
                Thread.Sleep(20000);
            }
            #endregion

            Console.WriteLine("Donhirobotスタート！ Let's starting!");

            if (!Boot()) {
                Console.WriteLine("Aborting launch & shutting down.");
                return;
            }

            Console.WriteLine($"Loaded {SongDatabase.Songs.Count} songs.");
            Console.WriteLine($"{SongDatabase.Songs.Where(song => !song.Value.Difficulties.ContainsNotes()).ToDictionary().Count} songs do not contain or is missing note counts.");
            Console.WriteLine($"{SongDatabase.SongNames.Count} entries for song names are available.");

            Console.WriteLine("Communicating with Discord...");

            DiscordSocketConfig _config = new() {
                GatewayIntents = GatewayIntents.Guilds
            };

            _client = new DiscordSocketClient(_config);
            _commandService = new CommandService();

            _logginghandler = new(_client, _commandService);
            _commandhandler = new(_client, _commandService);

            if (File.Exists(__keypath))
                __key = File.ReadAllText(__keypath);

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(__environmentname)))
                await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable(__environmentname));
            else if (!string.IsNullOrWhiteSpace(__key))
                await _client.LoginAsync(TokenType.Bot, __key);
            else
                throw new Exception($"Discord bot key could not be found in environment or text file. Please add your Discord bot's secret key to '{__keypath}' or the environment variable '{__environmentname}'.");

            __key = "";
            await _client.StartAsync();
            await _client.SetCustomStatusAsync($"Drumming along to {SongDatabase.Songs.Count} songs! ({CommandsHandler.last_Update})");

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
                LocaleData.Initialize();
                EmoteData.Initialize();
                SongDatabase.FetchSongs();
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
