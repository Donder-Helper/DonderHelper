using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text;
using System.Diagnostics;

namespace DonderHelper
{
    public class Program()
    {
        // Discord bot private key
        private static readonly string DONDERHELPER_SECRET_KEY = "DONDERHELPER_SECRET_KEY";
        private static readonly string __keypath = $"key.txt";
        private static string __key = "";

#pragma warning disable CS8618
        private static DiscordSocketClient _client;
        private static CommandService _commandService;

        private static LoggingHandler _logginghandler;
        private static CommandsHandler _commandhandler;

        private static Stats _stats;
#pragma warning restore CS8618

        public static async Task Main(params string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (args.Length > 0)
                Console.WriteLine($"Running with args: {string.Join(' ', args)}");

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

            Console.WriteLine("Communicating with Discord...");

            DiscordSocketConfig _config = new()
            {
                GatewayIntents = GatewayIntents.Guilds,
                HandlerTimeout = SongDatabase.TIMEOUT_MS
            };

            _client = new DiscordSocketClient(_config);
            _commandService = new CommandService();

            _logginghandler = new(_client, _commandService);
            _commandhandler = new(_client, _commandService);

            if (File.Exists(__keypath))
                __key = File.ReadAllText(__keypath);

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DONDERHELPER_SECRET_KEY)))
                await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable(DONDERHELPER_SECRET_KEY));
            else if (!string.IsNullOrWhiteSpace(__key))
                await _client.LoginAsync(TokenType.Bot, __key);
            else
                throw new Exception($"Discord bot key could not be found in environment or text file. Please add your Discord bot's secret key to '{__keypath}' or the environment variable '{DONDERHELPER_SECRET_KEY}'.");

            __key = "";
            await _client.StartAsync();
            _ = UpdateSonglist(Math.Max(1, SongDatabase.STATS_REFRESH_HR));
            
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
                GaidenSonglist.Initialize();
                SongDatabase.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Boot Failed.\n" + ex.ToString());
                return false;
            }
            return true;
        }
        private static async Task UpdateSonglist(int hour_rate)
        {
            try
            {
                int code = await SongDatabase.UpdateStats();
                var stats = SongDatabase.Stats;
                if (code.IsSuccessStatusCode())
                {
                    await _client.SetCustomStatusAsync($"Drumming along to {stats.TotalSongs} songs!");
                }
                else
                {
                    await _client.SetCustomStatusAsync($"Drumming along to... how many songs was it again? [E:{code}]");
                }
                Console.WriteLine($"GetStats() returned code {code}");

                SongDatabase.CleanStaleCache();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Something went wrong while getting song stats. Details: {e}");
                await _client.SetCustomStatusAsync($"Drumming along to... how many songs was it again? [EX]");
            }
            finally
            {
                Thread.Sleep(new TimeSpan(hour_rate, 0, 0));
                _ = UpdateSonglist(hour_rate);
            }
        }
    }
}
