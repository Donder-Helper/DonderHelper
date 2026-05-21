using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DonderHelper
{
    public class CommandsHandler
    {
        private readonly string donShop_Spring_img = "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2018/05/8251a977b1344fff217f31f37cd1e8fe.png";
        private readonly string donShop_Summer_img = "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2025/05/5dd72fe33b6af14311cb975f2a70a065.png";
        private readonly string donShop_Autumn_img = "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2025/08/4f6df8253e818bc82ad0df7c19e5467d.png";
        private readonly string donShop_Winter_img = "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2025/11/5b376df35c337270e961d9a61c2b9e67.png";

        private readonly Color donShop_Spring_color = new(254, 137, 187);
        private readonly Color donShop_Summer_color = new(117, 237, 254);
        private readonly Color donShop_Autumn_color = new(248, 72, 40);
        private readonly Color donShop_Winter_color = new(186, 202, 255);

        #region Statistics
        public static string last_Update => $"Last update: {Process.GetCurrentProcess().StartTime:yyyy/MM/dd}";
        public static DateTimeOffset readyTime = new();
        #endregion

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly Discord.Interactions.InteractionService _interaction;
        private IReadOnlyCollection<SocketApplicationCommand> command_list = [];

        public CommandsHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
            _interaction = new(_client);

            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandExecuted;
            _client.AutocompleteExecuted += AutocompleteExecuted;
            _client.ButtonExecuted += ButtonExecuted;
        }

        private async Task ButtonExecuted(SocketMessageComponent component)
        {
            Console.WriteLine($"Executing button with CustomId '{component.Data.CustomId}' requested by user {component.User.Id}.");
            try
            {
                string id = component.Data.CustomId;

                if (id.StartsWith("diff"))
                {
                    string[] values = id.Split(',', 3);
                    if (int.TryParse(values[2], out int id_value))
                    {
                        var result = await SongDatabase.GetSong(id_value);
                        if (result != null)
                            await PostDiff(component, id_value, result, Song.GetDifficultyFromString(values[1]));
                        else
                        {
                            Console.WriteLine($"Button interaction failed for 'diff' with id '{id_value}' and diff '{values[1]}' (Song is {(result is null ? "null" : "not null")})");
                            await component.RespondAsync($"This button interaction failed due to an error.", null, false, true);
                        }
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Button interaction failed for 'diff' with value '{values[2]}' and diff '{values[1]}'.");
                        await component.RespondAsync($"This button interaction failed because the bot was recently updated.\n" +
                            $"Please re-run the song command to create a new response with functional buttons.", null, false, true);
                        return;
                    }
                }
                else if (id.StartsWith("song"))
                {
                    string[] values = id.Split(',', 2);
                    if (int.TryParse(values[1], out int id_value))
                    {
                        var result = await SongDatabase.GetSong(id_value);
                        if (result != null)
                            await PostSong(component, id_value, result);
                        else
                        {
                            Console.WriteLine($"Button interaction failed for 'diff' with id '{id_value}' (Song is {(result is null ? "null" : "not null")})");
                            await component.RespondAsync($"This button interaction failed due to an error.", null, false, true);
                        }
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Button interaction failed for 'song' with title '{values[1]}'.");
                        await component.RespondAsync($"This button interaction failed because the bot was recently updated.\n" +
                            $"Please re-run the song command to create a new response with functional buttons.", null, false, true);
                        return;
                    }
                }

                Console.WriteLine($"Button execution with CustomId '{component.Data.CustomId}' failed, or is not yet implemented.");
                await component.RespondAsync("This button interaction failed, or is not implemented.", null, false, true);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General/Error] Something went wrong while interacting with a button. Id: {component.Data.CustomId} / User: {component.User.Id} / Guild: {component.GuildId?.ToString() ?? "(null)"} / Channel: {component.ChannelId?.ToString() ?? "(none)"} / Details:\n{ex}");
                await component.RespondAsync(LocaleData.GetString("DISCLAIMER_ERROR", GetLocale(component)), null, false, true);
                return;
            }
        }

        private async Task AutocompleteExecuted(SocketAutocompleteInteraction interaction)
        {
            DateTimeOffset offset = DateTimeOffset.UtcNow;
            string locale = GetLocale(interaction);
            string name = (string)interaction.Data.Current.Value;
            int? id = int.TryParse(name, out int id_result) ? id_result : null;

            List<AutocompleteResult> result = [];

            #region Song Title
            if (interaction.Data.CommandName == "song" && interaction.Data.Current.Name == "title")
            {
                int Priority(KeyValuePair<int, Song> song)
                {
                    if (id != null && song.Key == id) return -999;
                    if (song.Value.TitleList.Values.Any(title => title.Equals(name, StringComparison.InvariantCultureIgnoreCase))) return -999;
                    if (song.Value.SubtitleList.Values.Any(subtitle => subtitle.Equals(name, StringComparison.InvariantCultureIgnoreCase))) return -999;
                    if (song.Value.TitleList.Values.Any(title => title.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))) return -998;
                    if (song.Value.SubtitleList.Values.Any(subtitle => subtitle.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))) return -998;

                    return song.Value.TitleList.Values.Select(title => string.Compare(title, name, StringComparison.InvariantCultureIgnoreCase)).Sum()
                        + song.Value.SubtitleList.Values.Select(subtitle => string.Compare(subtitle, name, StringComparison.InvariantCultureIgnoreCase)).Sum();
                }
                AutocompleteResult GetResult(KeyValuePair<int, Song> song)
                {
                    if (song.Value.SubtitleList.TryGetValue(locale, out string? subtitle) && !string.IsNullOrWhiteSpace(subtitle))
                    {
                        string full = $"[{song.Key}] {song.Value.GetTitle(locale)} / {subtitle}";
                        if (full.Length >= 100) full = full.Substring(0, 97) + "...";

                        return new(full, song.Key.ToString());
                    }
                    return new($"[{song.Key}] {song.Value.GetTitle(locale)}", song.Key.ToString());
                }

                if (string.IsNullOrEmpty((string)interaction.Data.Current.Value))
                {
                    var random = await SongDatabase.GetRandomSongs(10);
                    random = random.Reverse().ToDictionary();
                    var autocomp = random.Select(GetResult);

                    await interaction.RespondAsync(autocomp, null);
                    goto end;
                }

                var search = await SongDatabase.GetSongs(name);
                search = search.Reverse().ToDictionary();
                if (id != null)
                {
                    Song? song = await SongDatabase.GetSong((int)id);
                    if (song != null) search[(int)id] = song;
                }

                if (search.Count == 0)
                {
                    await interaction.RespondAsync([], null);
                    goto end;
                }

                result = search.
                    OrderBy(Priority).
                    Select(GetResult).
                    Take(25).ToList();

                await interaction.RespondAsync(result, null);
            }
            #endregion
            #region Gaiden Title
            else if (interaction.Data.CommandName == "gaiden" && interaction.Data.Current.Name == "title")
            {
                int Priority(KeyValuePair<string, string> gaiden)
                {
                    if (gaiden.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase)) return -999;
                    if (gaiden.Key.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)) return -998;

                    return string.Compare(gaiden.Key, name, StringComparison.InvariantCultureIgnoreCase);
                }

                if (string.IsNullOrEmpty((string)interaction.Data.Current.Value))
                {
                    Random rand = new();
                    var autocomp = GaidenSonglist.Gaidens.Values.Take(20).
                        Select(gaiden => new AutocompleteResult(gaiden.GetSubtitle(locale), gaiden.Subtitle));

                    await interaction.RespondAsync(autocomp, null);
                    goto end;
                }

                result = GaidenSonglist.GaidenNames.
                    Where(song => song.Key.Contains(name, StringComparison.OrdinalIgnoreCase)).
                    OrderBy(Priority).Take(25).
                    Select(song => new AutocompleteResult(song.Key, song.Value)).ToList();

                await interaction.RespondAsync(result, null);
            }
            #endregion
            end:
            Console.WriteLine($"AutocompleteExecuted (Finished in {(DateTimeOffset.UtcNow - offset).TotalSeconds}s)");
            Console.WriteLine($"Data: {Regex.Replace(name, @"[^\w\.@-]", "")}");
        }

        private async Task Client_Ready()
        {
            readyTime = DateTime.UtcNow;
            try
            {
                InteractionContextType[] context_types = [InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel];
                ApplicationIntegrationType[] integration_types = [ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall];
                Dictionary<string, string> test = new Dictionary<string, string>();

                var command_help = new SlashCommandBuilder();
                command_help.WithName("help");
                command_help.WithDescription("Lists all available commands and their uses.");
                command_help.WithNameLocalizations(LocaleData.GetStrings("COMMAND_HELP_NAME"));
                command_help.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_HELP_DESC"));
                command_help.WithContextTypes(context_types);
                command_help.WithIntegrationTypes(integration_types);

                var command_random = new SlashCommandBuilder();
                command_random.WithName("random");
                command_random.WithDescription("Select a random song.");
                command_random.WithNameLocalizations(LocaleData.GetStrings("COMMAND_RANDOM_NAME"));
                command_random.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_RANDOM_DESC"));
                command_random.AddOption("difficulty", ApplicationCommandOptionType.String, "The specific difficulty of a song.", false, null, false, null, null, null, null, LocaleData.GetStrings("OPTION_DIFFICULTY_NAME"), LocaleData.GetStrings("OPTION_DIFFICULTY_DESC"), null, null,
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Easy",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_EASY"),
                    Value = "easy"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Normal",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_NORMAL"),
                    Value = "normal"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Hard",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_HARD"),
                    Value = "hard"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Extreme",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_EX"),
                    Value = "ex"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Hidden",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_HIDDEN"),
                    Value = "hidden"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Extreme/Hidden",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_BOTH"),
                    Value = "both"
                });
                command_random.AddOption("level", ApplicationCommandOptionType.Number, "The difficulty level.", false, null, false, 1, 10, null, null, LocaleData.GetStrings("OPTION_LEVEL_NAME"), LocaleData.GetStrings("OPTION_LEVEL_DESC"), null, null,
                new ApplicationCommandOptionChoiceProperties() { Name = "1★", Value = 1 },
                new ApplicationCommandOptionChoiceProperties() { Name = "2★", Value = 2 },
                new ApplicationCommandOptionChoiceProperties() { Name = "3★", Value = 3 },
                new ApplicationCommandOptionChoiceProperties() { Name = "4★", Value = 4 },
                new ApplicationCommandOptionChoiceProperties() { Name = "5★", Value = 5 },
                new ApplicationCommandOptionChoiceProperties() { Name = "6★", Value = 6 },
                new ApplicationCommandOptionChoiceProperties() { Name = "7★", Value = 7 },
                new ApplicationCommandOptionChoiceProperties() { Name = "8★", Value = 8 },
                new ApplicationCommandOptionChoiceProperties() { Name = "9★", Value = 9 },
                new ApplicationCommandOptionChoiceProperties() { Name = "10★", Value = 10 }
                );
                command_random.AddOption("genre", ApplicationCommandOptionType.String, "The specific genre that a song belongs in.", false, null, false, null, null, null, null, LocaleData.GetStrings("OPTION_GENRE_NAME"), LocaleData.GetStrings("OPTION_GENRE_DESC"), null, null,
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Pop",
                    NameLocalizations = LocaleData.GetGenreAsStrings(Song.SongGenre.Pop),
                    Value = "pop"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Kids",
                    NameLocalizations = LocaleData.GetGenreAsStrings(Song.SongGenre.Kids),
                    Value = "kids"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Anime",
                    NameLocalizations = LocaleData.GetGenreAsStrings(Song.SongGenre.Anime),
                    Value = "anime"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Vocaloid",
                    NameLocalizations = LocaleData.GetGenreAsStrings(Song.SongGenre.Vocaloid),
                    Value = "vocaloid"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Game Music",
                    NameLocalizations = LocaleData.GetGenreAsStrings(Song.SongGenre.Game),
                    Value = "game"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Variety",
                    NameLocalizations = LocaleData.GetGenreAsStrings(Song.SongGenre.Variety),
                    Value = "variety"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Classical",
                    NameLocalizations = LocaleData.GetGenreAsStrings(Song.SongGenre.Classical),
                    Value = "classical"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Namco Original",
                    NameLocalizations = LocaleData.GetGenreAsStrings(Song.SongGenre.Namco),
                    Value = "namco"
                }
                );
                command_random.WithContextTypes(context_types);
                command_random.WithIntegrationTypes(integration_types);

                var command_song = new SlashCommandBuilder();
                command_song.WithName("song");
                command_song.WithDescription("Get info about a song.");
                command_song.WithNameLocalizations(LocaleData.GetStrings("COMMAND_SONG_NAME"));
                command_song.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_SONG_DESC"));
                command_song.AddOption("title", ApplicationCommandOptionType.String, "The title of the song.", true, null, true, null, null, null, null, LocaleData.GetStrings("OPTION_TITLE_NAME"), LocaleData.GetStrings("OPTION_TITLE_DESC"));
                command_song.AddOption("difficulty", ApplicationCommandOptionType.String, "The specific difficulty of a song.", false, null, false, null, null, null, null, LocaleData.GetStrings("OPTION_DIFFICULTY_NAME"), LocaleData.GetStrings("OPTION_DIFFICULTY_DESC"), null, null,
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Easy",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_EASY"),
                    Value = "easy"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Normal",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_NORMAL"),
                    Value = "normal"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Hard",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_HARD"),
                    Value = "hard"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Extreme",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_EX"),
                    Value = "ex"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Hidden",
                    NameLocalizations = LocaleData.GetStrings("DIFFICULTY_HIDDEN"),
                    Value = "hidden"
                });
                command_song.WithContextTypes(context_types);
                command_song.WithIntegrationTypes(integration_types);

                var command_region = new SlashCommandBuilder();
                command_region.WithName("region");
                command_region.WithDescription("Get the URL for all region locked songs.");
                command_region.WithNameLocalizations(LocaleData.GetStrings("COMMAND_REGION_NAME"));
                command_region.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_REGION_DESC"));
                command_region.WithContextTypes(context_types);
                command_region.WithIntegrationTypes(integration_types);

                var command_campaign = new SlashCommandBuilder();
                command_campaign.WithName("campaign");
                command_campaign.WithDescription("Get the current list of active campaigns.");
                command_campaign.WithNameLocalizations(LocaleData.GetStrings("COMMAND_CAMPAIGN_NAME"));
                command_campaign.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_CAMPAIGN_DESC"));
                command_campaign.AddOption("name", ApplicationCommandOptionType.String, "The name of a currently active campaign.", true, null, false, null, null, null, null, LocaleData.GetStrings("OPTION_CAMPAIGNNAME_NAME"), LocaleData.GetStrings("OPTION_CAMPAIGNNAME_DESC"), null, null,
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "太鼓の達人×転生したらスライムだった件 2026コラボキャンペーン",
                    Value = "tensura2026"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Dark Don Challenge",
                    Value = "darkchallenge_2026",
                    NameLocalizations = new Dictionary<string, string>()
                    {
                        { "ja", "挑戦！闇のドンチャレ" },
                        { "zh-TW", "挑戰！闇黑鼓眾挑戰" },
                        { "ko", "도전! 어둠의 쿵 챌린지" }
                    }
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Don Challenge 2026",
                    Value = "challenge_2026",
                    NameLocalizations = new Dictionary<string, string>()
                    {
                        { "ja", "挑戦！ドンチャレ2026" },
                        { "zh-TW", "挑戰！鼓眾挑戰2026" },
                        { "ko", "도전! 쿵 챌린지 2026" }
                    }
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "『Got Boost?』Campaign",
                    Value = "kamen2025",
                    NameLocalizations = new Dictionary<string, string>()
                    {
                        { "ja", "『Got Boost?』キャンペーン" }
                    }
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "『VISIONS』Campaign",
                    Value = "kamen2026",
                    NameLocalizations = new Dictionary<string, string>()
                    {
                        { "ja", "『VISIONS』キャンペーン" }
                    }
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "彁",
                    Value = "ka"
                }
                );
                command_campaign.WithContextTypes(context_types);
                command_campaign.WithIntegrationTypes(integration_types);

                var command_shop = new SlashCommandBuilder();
                command_shop.WithName("shop");
                command_shop.WithDescription("Get the current active Don Medal shop.");
                command_shop.WithNameLocalizations(LocaleData.GetStrings("COMMAND_SHOP_NAME"));
                command_shop.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_SHOP_DESC"));
                command_shop.WithContextTypes(context_types);
                command_shop.WithIntegrationTypes(integration_types);

                var command_about = new SlashCommandBuilder();
                command_about.WithName("about");
                command_about.WithDescription("Information about the bot and its resources.");
                command_about.WithNameLocalizations(LocaleData.GetStrings("COMMAND_ABOUT_NAME"));
                command_about.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_ABOUT_DESC"));
                command_about.WithContextTypes(context_types);
                command_about.WithIntegrationTypes(integration_types);

                var command_stats = new SlashCommandBuilder();
                command_stats.WithName("stats");
                command_stats.WithDescription("Get statistic about the bot and its song database.");
                command_stats.WithNameLocalizations(LocaleData.GetStrings("COMMAND_STATS_NAME"));
                command_stats.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_STATS_DESC"));
                command_stats.WithContextTypes(context_types);
                command_stats.WithIntegrationTypes(integration_types);

                var command_dan = new SlashCommandBuilder();
                command_dan.WithName("dan");
                command_dan.WithDescription("Get the current Dan Dojo courses.");
                command_dan.WithNameLocalizations(LocaleData.GetStrings("COMMAND_DAN_NAME"));
                command_dan.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_DAN_DESC"));
                command_dan.AddOption("title", ApplicationCommandOptionType.String, "The title of the dan.", true, null, false, null, null, null, null, LocaleData.GetStrings("OPTION_DANTITLE_NAME"), LocaleData.GetStrings("OPTION_DANTITLE_DESC"), null, null,
                DanSonglist.Dans.Values.Select(dan => dan.AsChoice()).ToArray()   
                );
                command_dan.WithContextTypes(context_types);
                command_dan.WithIntegrationTypes(integration_types);

                var command_gaiden = new SlashCommandBuilder();
                command_gaiden.WithName("gaiden");
                command_gaiden.WithDescription("Search for Gaidens and their corresponding QR codes.");
                command_gaiden.WithNameLocalizations(LocaleData.GetStrings("COMMAND_GAIDEN_NAME"));
                command_gaiden.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_GAIDEN_DESC"));
                command_gaiden.AddOption("title", ApplicationCommandOptionType.String, "The title of the gaiden.", true, null, true, null, null, null, null, LocaleData.GetStrings("OPTION_GAIDENTITLE_NAME"), LocaleData.GetStrings("OPTION_GAIDENTITLE_DESC"), null, null);
                command_gaiden.WithContextTypes(context_types);
                command_gaiden.WithIntegrationTypes(integration_types);

                var command_hiroba = new SlashCommandBuilder();
                command_hiroba.WithName("hiroba");
                command_hiroba.WithDescription("Get information about using Donder Hiroba.");
                command_hiroba.WithNameLocalizations(LocaleData.GetStrings("COMMAND_HIROBA_NAME"));
                command_hiroba.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_HIROBA_DESC"));
                command_hiroba.AddOption("guide", ApplicationCommandOptionType.String, "Select a specific area of Donder Hiroba to read about.", false, false, false, null, null, null, null, LocaleData.GetStrings("OPTION_GUIDE_NAME"), LocaleData.GetStrings("OPTION_GUIDE_DESC"), null, null,
                new ApplicationCommandOptionChoiceProperties()
                { 
                    Name = "Change Your Name",
                    Value = "name"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Change Your Title",
                    Value = "title"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Change Your Costume/Mini Character",
                    Value = "costume"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Change Your My-DON's Colors",
                    Value = "color"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Add a Friend",
                    Value = "friend"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Challenge Other Players",
                    Value = "challenge"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Create a Tournament",
                    Value = "tournament_create"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "Join a Tournament",
                    Value = "tournament_join"
                }
                );
                command_hiroba.WithContextTypes(context_types);
                command_hiroba.WithIntegrationTypes(integration_types);

                var command_invite = new SlashCommandBuilder();
                command_invite.WithName("invite");
                command_invite.WithDescription("Add me to your server, or add me as an app!");
                command_invite.WithNameLocalizations(LocaleData.GetStrings("COMMAND_INVITE_NAME"));
                command_invite.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_INVITE_DESC"));
                command_invite.WithContextTypes(context_types);
                command_invite.WithIntegrationTypes(integration_types);

                await _client.BulkOverwriteGlobalApplicationCommandsAsync([command_help.Build(), command_random.Build(), command_song.Build(), command_region.Build(), command_campaign.Build(), command_shop.Build(), command_about.Build(), command_stats.Build(), command_dan.Build(), command_gaiden.Build(), command_hiroba.Build(), command_invite.Build()]);

                command_list = await _client.GetGlobalApplicationCommandsAsync(true) ?? [];

                Console.WriteLine(command_list.Count + " global commands found.");
                if (command_list.Count == 0) Console.WriteLine("Bruh??? Why are there zero commands???");
                foreach (SocketApplicationCommand command in command_list)
                {
                    Console.WriteLine("Command name: " + (command.Name ?? "null") + "\n" +
                        "Integrations: " + (command.IntegrationTypes != null ? string.Join(", ", command.IntegrationTypes) : "null") + "\n" +
                        "Contexts: " + (command.ContextTypes != null ? string.Join(", ", command.ContextTypes) : "null") + "\n");
                }

                Console.WriteLine("Global commands built successfully!");
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                Console.WriteLine("Global commands failed to build.\n" + json);
            }
        }

        private async Task PostSong(SocketInteraction interaction, int id, Song song)
        {
            Console.WriteLine("PostSong interaction created at " + interaction.CreatedAt);
            Console.WriteLine("Current time is " + DateTimeOffset.UtcNow);
            Console.WriteLine("InteractionType is " + interaction.Type);

            try
            {
                string locale = GetLocale(interaction);

                string title = song.TitleList.Values.Distinct().Count() > 1 ? song.GetTitleList(true, locale) : song.GetTitle(locale);
                if (title.Length > 256)
                {
                    title = $"{LocaleData.GetLocaleAsEmoji(locale)} {song.GetTitle(locale)}";
                }

                var builder = new EmbedBuilder()
                {
                    Title = title,
                    Description = song.SubtitleList.Values.Distinct().Count() > 1 ? song.GetSubtitleList(true, locale) : song.GetSubtitle(locale),
                    Color = Song.GetGenreAsColor(song.Genre),
                    Fields = new() {
                        new()
                        {
                            Name = LocaleData.GetString("GENRE_TITLE", locale),
                            Value = song.GenreList.Count > 1
                            ? string.Join("\n", song.GenreList.Select(genre => "- " + LocaleData.GetGenreAsString(genre, locale)))
                            : LocaleData.GetGenreAsString(song.Genre, locale),
                            IsInline = false
                        },
                        new() {
                            Name = LocaleData.GetString("DIFFICULTY_TITLE", locale),
                            Value =
                            $"{(song.Difficulties.Hidden.Level > 0 ? ($"{EmoteData.GetDifficulty(Song.SongDifficulty.Hidden)} " + song.Difficulties.Hidden.Level + "★ " + song.Difficulties.Hidden.NoteCount.ToString() + "\n") : "")}" +
                            $"{EmoteData.GetDifficulty(Song.SongDifficulty.Extreme)} {(song.Difficulties.Extreme.Level > 0 ? song.Difficulties.Extreme.Level + "★ " : "- ") + song.Difficulties.Extreme.NoteCount.ToString()}\n" +
                            $"{EmoteData.GetDifficulty(Song.SongDifficulty.Hard)} {(song.Difficulties.Hard.Level > 0 ? song.Difficulties.Hard.Level + "★ " : "- ") + song.Difficulties.Hard.NoteCount.ToString()}\n" +
                            $"{EmoteData.GetDifficulty(Song.SongDifficulty.Normal)} {(song.Difficulties.Normal.Level > 0 ? song.Difficulties.Normal.Level + "★ " : "- ") + song.Difficulties.Normal.NoteCount.ToString()}\n" +
                            $"{EmoteData.GetDifficulty(Song.SongDifficulty.Easy)} {(song.Difficulties.Easy.Level > 0 ? song.Difficulties.Easy.Level + "★ " : "- ") + song.Difficulties.Easy.NoteCount.ToString()}",
                            IsInline = true
                        },
                        new()
                        {
                            Name = LocaleData.GetString("AVAILABLE_TITLE", locale),
                            Value =
                            LocaleData.GetJapanRegionStatusAsString(song, locale) + "\n" +
                            LocaleData.GetAsiaRegionStatusAsString(song, locale) + "\n" +
                            LocaleData.GetOceaniaRegionStatusAsString(song, locale) + "\n" +
                            LocaleData.GetUSARegionStatusAsString(song, locale) + "\n" +
                            LocaleData.GetChinaRegionStatusAsString(song, locale),
                            IsInline = true
                        }
                    },

                    Timestamp = new(DateTime.UtcNow),
                    Footer = GetFooter(interaction)
                };
                
                var component_builder = new ComponentBuilder();
                if (song.Difficulties.Easy.Level > 0 || song.Difficulties.Easy.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Easy));
                if (song.Difficulties.Normal.Level > 0 || song.Difficulties.Normal.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Normal));
                if (song.Difficulties.Hard.Level > 0 || song.Difficulties.Hard.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Hard));
                if (song.Difficulties.Extreme.Level > 0 || song.Difficulties.Extreme.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Extreme));
                if (song.Difficulties.Hidden.Level > 0 || song.Difficulties.Hidden.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Hidden));

                await interaction.RespondAsync(null, [builder.Build()], false, !CanSendMessage(interaction), null, component_builder.Build());
            }
            catch
            {
                Console.WriteLine($"[General/Error] PostSong failed to respond with song titled \"{song.Title}\".");
                throw;
            }
        }
        private async Task PostDiff(SocketInteraction interaction, int id, Song song, Song.SongDifficulty difficulty)
        {
            Console.WriteLine("PostDiff interaction created at " + interaction.CreatedAt);
            Console.WriteLine("Current time is " + DateTimeOffset.UtcNow);
            Console.WriteLine("InteractionType is " + interaction.Type);

            try
            {
                Song.Chart chart = song.Difficulties[difficulty];

                if (chart.Level < 1 && !chart.NoteCount.ContainsNotes())
                {
                    await interaction.RespondAsync($"The difficulty selected does not exist for this song, or is missing data. {EmoteData.GetEmote("MISS")}", null, false, true);
                    return;
                }

                string locale = GetLocale(interaction);

                var builder = new EmbedBuilder()
                {
                    Title = song.GetTitle(locale) + $" {EmoteData.GetDifficulty(difficulty)} {(chart.Level > 0 ? chart.Level + "★" : "")}",
                    Description = song.GetSubtitle(locale) + $"{(chart.NoteCount.ContainsNotes() ? "\n" : "")}{chart.NoteCount}\n\n" +

                    $"**{LocaleData.GetString("AVAILABLE_TITLE", locale)}**\n" +
                    LocaleData.GetJapanRegionStatusAsString(song, locale) + "\n" +
                    LocaleData.GetAsiaRegionStatusAsString(song, locale) + "\n" +
                    LocaleData.GetOceaniaRegionStatusAsString(song, locale) + "\n" +
                    LocaleData.GetUSARegionStatusAsString(song, locale) + "\n" +
                    LocaleData.GetChinaRegionStatusAsString(song, locale),
                    
                    Color = Song.GetGenreAsColor(song.Genre),
                    Timestamp = DateTimeOffset.UtcNow,
                    Fields = new()
                    {
                        new()
                        {
                            Name = "Details (Taiko Fumen Wiki)",
                            Value = chart.SourceUrls.TryGetValue("taiko-fumen", out string? taiko_fumen) ? taiko_fumen : $"-# {LocaleData.GetString("URL_MISSING", locale, EmoteData.GetEmote("MISS"))}"
                        },
                        new()
                        {
                            Name = "Details (taiko.wiki)",
                            Value = chart.SourceUrls.TryGetValue("taiko-wiki", out string? taiko_wiki) ? taiko_wiki : $"-# {LocaleData.GetString("URL_MISSING", locale, EmoteData.GetEmote("MISS"))}"
                        }
                    },
                    Url = chart.ImageUrl,
                    ImageUrl = chart.ImageUrl,
                    Footer = GetFooter(interaction)
                };

                var component_builder = new ComponentBuilder();
                if (song.Difficulties.Easy.Level > 0 || song.Difficulties.Easy.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Easy));
                if (song.Difficulties.Normal.Level > 0 || song.Difficulties.Normal.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Normal));
                if (song.Difficulties.Hard.Level > 0 || song.Difficulties.Hard.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Hard));
                if (song.Difficulties.Extreme.Level > 0 || song.Difficulties.Extreme.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Extreme));
                if (song.Difficulties.Hidden.Level > 0 || song.Difficulties.Hidden.NoteCount.ContainsNotes()) component_builder.WithButton(CreateSongButton(interaction, id, difficulty: Song.SongDifficulty.Hidden));

                await interaction.RespondAsync(null, [builder.Build()], false, !CanSendMessage(interaction), null, component_builder.Build());
            }
            catch
            {
                Console.WriteLine($"[General/Error] PostDiff failed to respond with song titled \"{song.Title}\".");
                throw;
            }
        }
        private async Task SlashCommandExecuted(SocketSlashCommand command)
        {
            try
            {
                //await command.DeferAsync();
                Console.WriteLine($"User {command.User.Id} executed the '{command.Data.Name}' " +
                    $"command{(command.IsDMInteraction ? " in a DM" : (" in guild " + (command.GuildId?.ToString() ?? "(null)") + " in channel " + (command.ChannelId?.ToString() ?? "(null)") + $" ({command.Channel?.ChannelType.ToString() ?? "null Channel Type"})"))} with the following parameters: {(command.Data.Options.Count > 0 ? string.Join(", ", command.Data.Options.Select(option => $"({option.Name} - {Regex.Replace(option.Value.ToString() ?? "", @"[^\w\.@-]", "")})")) : "(No options)")}");

                string locale = GetLocale(command);
                bool canSendMessage = CanSendMessage(command);

                Console.WriteLine($"Command's locale is {locale}");
                Console.WriteLine($"Message can be sent: {canSendMessage}");

                string command_name = command.Data.Name;
                switch (command_name)
                {
                    case "help":
                    {
                        var help = new EmbedBuilder() {};

                        foreach (SocketApplicationCommand slashcommand in command_list ?? [])
                        {
                            if (string.IsNullOrWhiteSpace(slashcommand.Name)) continue;
                            if (slashcommand.Name == "help") continue;

                            help.AddField(
                                "/" + (slashcommand.NameLocalizations.GetValueOrDefault(locale) ?? slashcommand.Name),
                                slashcommand.DescriptionLocalizations.GetValueOrDefault(locale) ?? slashcommand.Description ?? "",
                                true);
                        };

                        await command.RespondAsync(null, [help.Build()], false, false);
                        break;
                    }
                    case "random":
                    {
                        Random rand = new Random();
                        var level_option = command.Data.Options.Where(item => item.Name == "level");
                        var diff_option = command.Data.Options.Where(item => item.Name == "difficulty");
                        var genre_option = command.Data.Options.Where(item => item.Name == "genre");

                        int? level = level_option.Count() > 0 ? (int)level_option.First().Value : null;
                        string? difficulty = diff_option.Count() > 0 ? (string)diff_option.First().Value : null;
                        string? genre = genre_option.Count() > 0 ? (string)genre_option.First().Value : null;

                        var songlist = await SongDatabase.GetRandomSongs(1,
                            genre != null ? Song.GetGenreFromString(genre) : null,
                            difficulty != null ? Song.GetDifficultyFromString(difficulty) : null,
                            level);

                        if (songlist.Count > 0)
                        {
                            var song = songlist.ElementAt(Random.Shared.Next(songlist.Count));
                            if (difficulty != null)
                                await PostDiff(command, song.Key, song.Value, Song.GetDifficultyFromString(difficulty));
                            else if (level != null)
                                await PostDiff(command, song.Key, song.Value, Song.SongDifficulty.Extreme);
                            else
                                await PostSong(command, song.Key, song.Value);
                        }
                        else
                        {
                            await command.RespondAsync("Could not find any songs with the given parameters.", null, false, true);
                        }
                    
                        break;
                    }
                    case "song":
                    {
                        if (command.Data.Options.Count == 1 || command.Data.Options.Count == 2)
                        {
                            string title = (string)command.Data.Options.First(option => option.Name == "title").Value;
                            int? id = int.TryParse(title, out int result_id) ? result_id : null;
                            Song? song = id != null ? await SongDatabase.GetSong((int)id) : null;
                            if (id != null && song != null)
                            {
                                int song_id = (int)id;
                                if (command.Data.Options.Any(option => option.Name == "difficulty"))
                                {
                                    switch ((string)command.Data.Options.First(option => option.Name == "difficulty").Value)
                                    {
                                        case "easy": await PostDiff(command, song_id, song, Song.SongDifficulty.Easy); break;
                                        case "normal": await PostDiff(command, song_id, song, Song.SongDifficulty.Normal); break;
                                        case "hard": await PostDiff(command, song_id, song, Song.SongDifficulty.Hard); break;
                                        case "ex": await PostDiff(command, song_id, song, Song.SongDifficulty.Extreme); break;
                                        case "hidden": await PostDiff(command, song_id, song, Song.SongDifficulty.Hidden); break;
                                        default: await PostSong(command, song_id, song); break;
                                    }
                                }
                                else
                                    await PostSong(command, song_id, song);
                            }
                            else
                                await command.RespondAsync(LocaleData.GetString("DISCLAIMER_MISSING", command.UserLocale ?? "en-US", title), null, false, true);
                        }
                        else
                        {
                            await command.RespondAsync(
                                "Attempted to run the `/song` command, but 0, or more than 2, options were received. If this error persists, let the bot owner know.", null, false, true);
                        }
                        break;
                    }
                    case "region":
                    {
                        await command.RespondAsync("Information on the region lock status of all songs can be found on this spreadsheet, courtesy of Taiko Time :\n<https://docs.google.com/spreadsheets/d/1Piucd3Wv-QVQJ_yMQjC1xV08Cl2IXGze_8bf8nQZGjs/>\nYou can also help Taiko Time by filling out this form when songs are added/updated :\n<https://forms.gle/49VyswkbbBDp1YB89>", null, false, false);
                        break;
                    }
                    case "campaign":
                    {
                        var campaign_option = command.Data.Options.Where(option => option.Name == "name");
                        string campaign_name = campaign_option.Count() > 0 ? (string)campaign_option.First().Value : "";

                        switch (campaign_name)
                        {
                            case "tensura2026":
                            {
                                string url = "https://taiko.namco-ch.net/taiko/special/ten-sura2026/";
                                var tensura2026 = new EmbedBuilder()
                                {
                                    Title = "太鼓の達人×転生したらスライムだった件 2026コラボキャンペーン",
                                    Url = url,
                                    ImageUrl = "https://media.discordapp.net/attachments/1355004709974446302/1506815369200009376/ten-sura2026.png",
                                    ThumbnailUrl = command.User.Id % 2 == 0 ?
                                    "https://media.discordapp.net/attachments/1355004709974446302/1506817023638896671/tensaru-shion.png" :
                                    "https://media.discordapp.net/attachments/1355004709974446302/1506817023315939368/tensaru-mirimu.png",
                                    Color = new(0x005cb1),

                                    Description = $"{LocaleData.GetString("CAMPAIGN_URL", locale, url)}\n" +
                                    LocaleData.GetString("CAMPAIGN_AVAILABLE", locale, 1784577600) + "\n\n" +
                                    $"-# {LocaleData.GetString("DISCLAIMER_ONLYJAPAN", locale)}",

                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };

                                var songs = await SongDatabase.GetSongs(1254, 1335, 1334, 1505, 1504);
                                var component = new ComponentBuilder();
                                foreach (var song in songs)
                                    component.WithButton(CreateSongButton(command, song));

                                await command.RespondAsync(null, [tensura2026.Build()], false, false, null, component.Build());
                                break;
                            }
                            case "darkchallenge_2026":
                            {
                                string title = locale switch
                                {
                                    "ja" => "挑戦！闇のドンチャレ",
                                    "zh-TW" => "挑戰！闇黑鼓眾挑戰",
                                    "ko" => "도전! 어둠의 쿵 챌린지",
                                    _ => "Dark Don Challenge"
                                };
                                string url = locale switch
                                {
                                    "ja" => "https://taiko-ch.net/blog/?p=16167",
                                    "zh-TW" => "https://taiko-ch.net/blog/?p=16189",
                                    "ko" => "https://taiko-ch.net/blog/?p=16197",
                                    _ => "https://taiko-ch.net/blog/?p=16182"
                                };
                                string story = locale switch
                                {
                                    "ja" => "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/de162d40105e30cd7928ee5b289e6275.png",
                                    "zh-TW" => "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/6faa27a4352704e929a8d65753f59e8c.png",
                                    "ko" => "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/ef9b87a1d80b01cc7a1c74e252ac7f0a.png",
                                    _ => "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/8aecdd0c70a246b6f9af404d1ee4f84f.png"
                                };
                                string rules = locale switch
                                {
                                    "ja" => "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/08d18a914a14b57a099c4d86a0b9f15e.png",
                                    "zh-TW" => "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/08448e779ba5aeb4b6d42840885cda83.png",
                                    "ko" => "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/ebeec18c9e810197dcdd3781292be2b8.png",
                                    _ => "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/b90c5e8c45950aea384e7d8a185293c8.png"
                                };
                                string thumbnail = command.User.Id % 2 == 0 ?
                                    "https://media.discordapp.net/attachments/1355004709974446302/1497868408241393714/image.png" :
                                    "https://media.discordapp.net/attachments/1355004709974446302/1497868407931146392/image.png";

                                var darkchallenge_2026 = new EmbedBuilder()
                                {
                                    Title = title,
                                    Url = url,
                                    ImageUrl = "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2026/04/fc31e8cd7fe013376a923b147552c247.png",
                                    ThumbnailUrl = thumbnail,
                                    Color = new(0xaa33dd),

                                    Description = $"{LocaleData.GetString("CAMPAIGN_URL", locale, url)}\n",

                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };

                                var story_embed = new EmbedBuilder()
                                {
                                    Url = url,
                                    ImageUrl = story
                                };
                                var rules_embed = new EmbedBuilder()
                                {
                                    Url = url,
                                    ImageUrl = rules
                                };

                                await command.RespondAsync(null, [darkchallenge_2026.Build(), story_embed.Build(), rules_embed.Build()], false, false);
                                break;
                            }
                            case "challenge_2026":
                            {
                                string title = locale switch
                                {
                                    "ja" => "挑戦！ドンチャレ2026",
                                    "zh-TW" => "挑戰！鼓眾挑戰2026",
                                    "ko" => "도전! 쿵 챌린지 2026",
                                    _ => "Don Challenge 2026"
                                };
                                string url = locale switch
                                {
                                    "ja" => "https://www.facebook.com/taikoac.asia/posts/pfbid02jmN3qyVf1wB32b87aktVEEq8xwZgpdRcbBun3z8DqHW4HdYokcfgvoKFKgBVmhAzl",
                                    "zh-TW" => "https://www.facebook.com/taikoac.asia/posts/pfbid02zLkz8pu3Xd4kYYKXRZ9CkQHtndcHbHF8twCMjHMjepMn8uG7AH7ty7LYvunsFcCBl",
                                    "ko" => "https://www.facebook.com/taikoac.asia/posts/pfbid0phPKSkQU1dqVZrM2cHr3PVPmJbwFgn6tvrbKV52x3XAe2MKRGojGtFmFJXYbH91ul",
                                    _ => "https://www.facebook.com/taikoac.asia/posts/pfbid02eRyJMpL8wzxaSrPZnoKCLLTqxRj3tZJwJTiVfeFz8sKoEBAqdPpPksqWQd42swnsl"
                                };
                                string image_url = locale switch
                                {
                                    "ja" => "https://cdn.discordapp.com/attachments/1355004709974446302/1493422850105933898/661444476_1465352202272876_1584635110499325430_n.png",
                                    "zh-TW" => "https://media.discordapp.net/attachments/1355004709974446302/1493422851808821308/658139763_1465324715608958_748597247809822865_n.png",
                                    "ko" => "https://media.discordapp.net/attachments/1355004709974446302/1493422851208904805/662013938_1465325938942169_1856873950553760837_n.png",
                                    _ => "https://media.discordapp.net/attachments/1355004709974446302/1493422850646736907/661604847_1465323655609064_5851573694867461963_n.png"
                                };

                                var challenge_2026 = new EmbedBuilder()
                                {
                                    Title = title,
                                    Color = new(0xf48c55),
                                    Url = url,
                                    ImageUrl = image_url,

                                    Description = $"{LocaleData.GetString("CAMPAIGN_URL", locale, url)}\n",

                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };

                                var qr = new EmbedBuilder()
                                {
                                    Url = url,
                                    ImageUrl = "https://media.discordapp.net/attachments/1355004709974446302/1493422890740224120/656754646_1465352258939537_7399666354091764854_n.png"
                                };

                                var component = new ComponentBuilder();
                                var songs = await SongDatabase.GetSongs(1467);
                                component.WithButton(CreateSongButton(command, songs.First()));

                                await command.RespondAsync(null, [challenge_2026.Build(), qr.Build()], false, false, null, component.Build());
                                break;
                            }
                            case "kamen2025":
                            {
                                var kamen2025 = new EmbedBuilder()
                                {
                                    Title = $"『Got Boost?』{(locale == "ja" ? "キャンペーン" : "Campaign")}",
                                    Color = new(0xD9358C),
                                    Url = "https://x.com/taiko_team/status/1904702341053636981",
                                    ImageUrl = "https://pbs.twimg.com/media/GmNSODdaoAAbrzI?format=jpg&name=large",
                                    Description = LocaleData.GetString("CAMPAIGN_AVAILABLE", locale, 1803834000) + "\n\n" +
                                    $"-# {LocaleData.GetString("DISCLAIMER_NOJAPAN", locale)}",
                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };
                                var component = new ComponentBuilder();

                                var song = await SongDatabase.GetSongs(1342);
                                component.WithButton(CreateSongButton(command, song.First()));

                                await command.RespondAsync(null, [kamen2025.Build()], false, false, null, component.Build());
                                break;
                            }
                            case "kamen2026":
                            {
                                var kamen2025 = new EmbedBuilder()
                                {
                                    Title = $"『VISIONS』{(locale == "ja" ? "キャンペーン" : "Campaign")}",
                                    Color = new(0x248f7b),
                                    Url = "https://x.com/taiko_team/status/2036612781504356499",
                                    ImageUrl = "https://pbs.twimg.com/media/HCEKULyaQAA2XQG?format=jpg&name=large",
                                    Description = LocaleData.GetString("CAMPAIGN_AVAILABLE", locale, 1803834000),
                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };
                                var component = new ComponentBuilder();

                                var song = await SongDatabase.GetSongs(1445);
                                component.WithButton(CreateSongButton(command, song.First()));

                                await command.RespondAsync(null, [kamen2025.Build()], false, false, null, component.Build());
                                break;
                            }
                            case "ka":
                            {
                                var qr = EmoteData.GetEmote("QR");
                                var ka = new EmbedBuilder()
                                {
                                    Title = "彁",
                                    Color = new(0x000000),
                                    Url = "https://x.com/taiko_team/status/1509697054313881600",
                                    ImageUrl = "https://pbs.twimg.com/media/FPD5JqzagAQEFl5?format=png&name=medium",
                                    Description = $"{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(LocaleData.GetString("CAMPAIGN_AVAILABLE", locale, 253402268399)))}\n\n" +
                                    $"{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}\n{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}\n{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}\n{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}\n{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}{qr}" +
                                    "\n\n-# " + LocaleData.GetString("DISCLAIMER_NOUSA", locale),
                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };

                                var component = new ComponentBuilder();
                                component.WithButton(CreateSongButton(command, 993, "「こ、これは。。。」").WithEmote(Emote.Parse("<a:questioncorrupt:1429762081711853609>")));

                                await command.RespondAsync(null, [ka.Build()], false, false, null, component.Build());
                                break;
                            }
                            default:
                            {
                                await command.RespondAsync("Campaign could not be found, or was spelled incorrectly.");
                                break;
                            }
                        }

                        break;
                    }
                    case "shop":
                    {
                        var songlist = await SongDatabase.GetSongs(1464, 1465, 1098, 318);
                        var winter2025 = new EmbedBuilder()
                        {
                            Title = LocaleData.GetString("SHOP_MEDAL_NAME", locale, LocaleData.GetString("SEASON_SPRING", locale), 2026),
                            ThumbnailUrl = donShop_Spring_img,
                            Color = donShop_Spring_color,
                            Description =
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, songlist[1464].GetTitle(locale), 60)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, songlist[1465].GetTitle(locale), 60)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, songlist[1098].GetTitle(locale), 50)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDALHIDDEN_DESC", locale, songlist[318].GetTitle(locale), EmoteData.GetDifficulty(Song.SongDifficulty.Hidden), 50)}\n" +
                            $"\n" +
                            $"{LocaleData.GetString("SHOP_MEDAL_URL", locale, "English", "https://docs.google.com/spreadsheets/d/1rVC1x8jPCvgJ1KK6W0XIxdHwyMsZiasqp-pnt7sAOAA/edit?gid=731420565#gid=731420565")}\n" +
                            $"{LocaleData.GetString("SHOP_MEDAL_URL", locale, "日本語", "https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC/%E3%81%A9%E3%82%93%E3%83%A1%E3%83%80%E3%83%AB%E3%82%B7%E3%83%A7%E3%83%83%E3%83%97")}",
                            Timestamp = DateTimeOffset.UtcNow,
                            Footer = GetFooter(command)
                        };

                        var component = new ComponentBuilder();
                        
                        component.WithButton(CreateSongButton(command, 1464, songlist[1464].GetTitle(locale)), 0);
                        component.WithButton(CreateSongButton(command, 1465, songlist[1465].GetTitle(locale)), 0);
                        component.WithButton(CreateSongButton(command, 1098, songlist[1098].GetTitle(locale)), 0);
                        component.WithButton(CreateSongButton(command, 318, songlist[318].GetTitle(locale), Song.SongDifficulty.Hidden), 0);

                        await command.RespondAsync(null, [winter2025.Build()], false, false, null, component.Build());
                        break;
                    }
                    case "about":
                    {
                        var about = new EmbedBuilder()
                        {
                            Title = "Donder Helper",
                            Description = "**Donder Helper** is a Discord bot created to help users easily access information about songs available in Nijiiro ver., " +
                            "as well as information about current events such as campaigns & shops.\n\n" +
                            "The information provided was made possible thanks to the following resources:\n\n" +
                            "- [Taiko no Tatsujin Fumen-toka Wiki](https://wikiwiki.jp/taiko-fumen/)\n" +
                            "- [Fumen Database](https://fumen-database.com/)\n" +
                            "- [Taiko Time's Region Checklist](https://docs.google.com/spreadsheets/d/e/2PACX-1vQYGQxV5Azuid7cnnNAG5EZyRkFI2YAJCARHS1AAgH0uo7OPgbaWODWbAbmk3o4M4h44hENCitbndKP/pubhtml?gid=0&single=true)\n" +
                            "- [Korean Taiko Wiki](https://taiko.wiki/)\n" +
                            "- [Missing Title/Subtitle Data Spreadsheet](https://docs.google.com/spreadsheets/d/1N9OBdkbwj51swS4jqhL6rTKv4looTQqLPWjAvD0lWog/edit?usp=sharing)\n\n" +
                            "Help me translate Donder Helper! https://github.com/Donder-Helper/DonderHelper/tree/main/Lang",
                            ImageUrl = "https://raw.githubusercontent.com/Donder-Helper/.github/refs/heads/main/profile/banner.png"
                        };
                        await command.RespondAsync(null, [about.Build()], false, false);
                        break;
                    }
                    case "stats":
                    {
                        var uptime = DateTime.UtcNow - readyTime;
                        var stats = SongDatabase.Stats;
                        var statistics = new EmbedBuilder()
                        {
                            Title = LocaleData.GetString("STATS_TITLE", locale),
                            Description = $"-# {LocaleData.GetString("DISCLAIMER_STATS", locale)}\n" +
                            $"## {LocaleData.GetString("STATS_TITLE_REGION", locale)}\n" +

                            $"### {LocaleData.GetString("STATS_REGION_COUNT", locale, "REGION_JAPAN", stats.Available.Japan)}\n" +
                            $"{LocaleData.GetString("STATS_REGION_SPECIAL", locale, stats.Exclusive.Japan, stats.Excluded.Japan, stats.Unknown.Japan)}\n" +

                            $"### {LocaleData.GetString("STATS_REGION_COUNT", locale, "REGION_ASIA", stats.Available.Asia)}\n" +
                            $"{LocaleData.GetString("STATS_REGION_SPECIAL", locale, stats.Exclusive.Asia, stats.Excluded.Asia, stats.Unknown.Asia)}\n" +

                            $"### {LocaleData.GetString("STATS_REGION_COUNT", locale, "REGION_OCEANIA", stats.Available.Oceania)}\n" +
                            $"{LocaleData.GetString("STATS_REGION_SPECIAL", locale, stats.Exclusive.Oceania, stats.Excluded.Oceania, stats.Unknown.Oceania)}\n" +

                            $"### {LocaleData.GetString("STATS_REGION_COUNT", locale, "REGION_USA", stats.Available.UnitedStates)}\n" +
                            $"{LocaleData.GetString("STATS_REGION_SPECIAL", locale, stats.Exclusive.UnitedStates, stats.Excluded.UnitedStates, stats.Unknown.UnitedStates)}\n" +

                            $"### {LocaleData.GetString("STATS_REGION_COUNT", locale, "REGION_CHINA", stats.Available.China)}\n" +
                            $"{LocaleData.GetString("STATS_REGION_SPECIAL", locale, stats.Exclusive.China, stats.Excluded.China, stats.Unknown.China)}\n\n" +

                            $"-# {LocaleData.GetString("STATS_REGION_AVAILABLE", locale, stats.AvailableAll)}\n" +
                            $"-# {LocaleData.GetString("STATS_REGION_UNAVAILABLE", locale, stats.UnavailableAll)}\n",
                            Fields =
                            {
                                new()
                                {
                                    Name = LocaleData.GetString("STATS_TITLE_SONG", locale),
                                    IsInline = true,
                                    Value =
                                    $"{LocaleData.GetString("STATS_SONG_COUNT", locale, SongDatabase.Stats.TotalSongs)}\n"
                                },
                                new()
                                {
                                    Name = LocaleData.GetString("STATS_TITLE_TITLE", locale),
                                    IsInline = true,
                                    Value =
                                    $"{LocaleData.GetString("STATS_TITLE_COMPLETE", locale, stats.CompleteTitleCount)}\n" +
                                    $"{LocaleData.GetString("STATS_TITLE_COUNT", locale, "ja", stats.TitleCount.Japanese)}\n" +
                                    $"{LocaleData.GetString("STATS_TITLE_COUNT", locale, "en-US", stats.TitleCount.English)}\n" +
                                    $"{LocaleData.GetString("STATS_TITLE_COUNT", locale, "ko", stats.TitleCount.Korean)}\n" +
                                    $"{LocaleData.GetString("STATS_TITLE_COUNT", locale, "zh-TW", stats.TitleCount.TradChinese)}\n" +
                                    $"{LocaleData.GetString("STATS_TITLE_COUNT", locale, "zh-CN", stats.TitleCount.SimpChinese)}"
                                },
                                new()
                                {
                                    Name = LocaleData.GetString("STATS_TITLE_DISCORD", locale),
                                    IsInline = true,
                                    Value = 
                                    $"{LocaleData.GetString("STATS_DISCORD_UPTIME", locale, string.Format("{0:00}:{1:00}:{2:00}", (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds))}\n" +
                                    $"{LocaleData.GetString("STATS_DISCORD_SERVERCOUNT", locale, _client.Guilds.Count())}"
                                }
                            }
                        };

                        await command.RespondAsync(null, [statistics.Build()], false, false);
                        break;
                    }
                    case "dan":
                    case "gaiden":
                    {
                        var dan_title = command.Data.Options.Where(option => option.Name == "title");
                        string title = dan_title.Count() > 0 ? (string)dan_title.First().Value : "";

                        if (command_name == "dan" && DanSonglist.Dans.TryGetValue(title, out Dan? dan) && dan.DanIsValid())
                        {
                            var dan_embed = new EmbedBuilder()
                            {
                                Title = title + "・" + dan.TitleEN,
                                Color = dan.DiscordColor,
                                Url = dan.Url,

                                Fields = [await dan.SongsToField(locale)],

                                Timestamp = DateTime.UtcNow,
                                Footer = GetFooter(command)
                            };
                            dan_embed.Fields.AddRange(dan.ExamsToFields(locale));

                            var component_builder = new ComponentBuilder();
                            var songlist = await SongDatabase.GetSongs(dan.Song1.Id, dan.Song2.Id, dan.Song3.Id);
                            if (!dan.Song1.Spoiler) component_builder.WithButton(CreateSongButton(command, dan.Song1.Id, songlist[dan.Song1.Id].GetTitle(locale), dan.Song1.Difficulty));
                            if (!dan.Song2.Spoiler) component_builder.WithButton(CreateSongButton(command, dan.Song2.Id, songlist[dan.Song2.Id].GetTitle(locale), dan.Song2.Difficulty));
                            if (!dan.Song3.Spoiler) component_builder.WithButton(CreateSongButton(command, dan.Song3.Id, songlist[dan.Song3.Id].GetTitle(locale), dan.Song3.Difficulty));

                            await command.RespondAsync(null, [dan_embed.Build()], false, false, null, component_builder.Build());
                        }
                        else if (command_name == "gaiden" && GaidenSonglist.Gaidens.TryGetValue(title, out Gaiden? gaiden) && gaiden.DanIsValid())
                        {
                            var dan_embed = new EmbedBuilder()
                            {
                                Title = gaiden.GetSubtitle(locale),
                                Color = gaiden.DiscordColor,
                                Url = gaiden.Url,
                                ImageUrl = gaiden.QRUrl,

                                Description = EmoteData.GetEmote("QR") + " " + gaiden.QRUrl,

                                Fields = [await gaiden.SongsToField(locale)],

                                Timestamp = DateTime.UtcNow,
                                Footer = GetFooter(command)
                            };
                            dan_embed.Fields.AddRange(gaiden.ExamsToFields(locale));

                            var component_builder = new ComponentBuilder();
                            var songlist = await SongDatabase.GetSongs(gaiden.Song1.Id, gaiden.Song2.Id, gaiden.Song3.Id);
                            if (!gaiden.Song1.Spoiler) component_builder.WithButton(CreateSongButton(command, gaiden.Song1.Id, songlist[gaiden.Song1.Id].GetTitle(locale), gaiden.Song1.Difficulty));
                            if (!gaiden.Song2.Spoiler) component_builder.WithButton(CreateSongButton(command, gaiden.Song2.Id, songlist[gaiden.Song2.Id].GetTitle(locale), gaiden.Song2.Difficulty));
                            if (!gaiden.Song3.Spoiler) component_builder.WithButton(CreateSongButton(command, gaiden.Song3.Id, songlist[gaiden.Song3.Id].GetTitle(locale), gaiden.Song3.Difficulty));

                            await command.RespondAsync(null, [dan_embed.Build()], false, false, null, component_builder.Build());
                        }
                        else
                        {
                            var embed = new EmbedBuilder()
                            {
                                Title = "",
                                Description = LocaleData.GetString("DAN_UNAVAILABLE", locale),
                                ImageUrl = "https://raw.githubusercontent.com/Donder-Helper/DonderHelper/refs/heads/main/Images/dan-closed.png"
                            };
                            await command.RespondAsync(null, [embed.Build()], false, false);
                        }

                        //var embed = new EmbedBuilder()
                        //{
                        //    Title = LocaleData.GetString("DAN_CLOSED_TITLE", locale, 2025),
                        //    Description = LocaleData.GetString("DAN_CLOSED", locale, 2025),
                        //    ImageUrl = "https://raw.githubusercontent.com/Donder-Helper/DonderHelper/refs/heads/main/Images/dan-closed.png"
                        //};

                        //await command.RespondAsync(null, [embed.Build()], false, false);
                        break;
                    }
                    case "hiroba":
                    {
                        // Fetch current day + hour offsets instead of hardcoded timestamp, to account for Daylight Savings for Americans
                        TimeSpan time_start = DateTime.UtcNow.Date.AddHours(20) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        TimeSpan time_end = DateTime.UtcNow.Date.AddHours(22) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                        string maintenance = $"-# ＊ Maintenance Hours: <t:{(long)time_start.TotalSeconds}:t> to <t:{(long)time_end.TotalSeconds}:t>\n" +
                            $"-# ＊ Maintenance times may be temporarily extended during software updates.\n" +
                            $"{(20 <= DateTime.UtcNow.Hour && DateTime.UtcNow.Hour < 22 ? "-# :warning: Maintenance is active, you can not edit your profile or use certain features.\n" : "")}\n";

                        if (command.Data.Options.Count == 1)
                        {
                            switch ((string)command.Data.Options.First().Value)
                            {
                                case "tournament_join":
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Join a Tournament",
                                        Url = "https://donderhiroba.jp/compe_list.php",
                                        ImageUrl = "https://taiko.namco-ch.net/taiko/en/images/donhiro/pic_10.png",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance + "https://donderhiroba.jp/\n" +
                                        $"1. Click on「大会挑戦状」(Challenge Room)\n" +
                                        $"2. Click on「大会を検索」(Search for Tournaments)\n" +
                                        $"3. Specify a tournament name in「大会名で検索するドン！（20文字以内）」(Search by name (up to 20 characters))\n" +
                                        $"4. Specify who is hosting in「開催者で検索するドン！」(Search by host name)\n" +
                                        $"5. Specify which song is used in「課題曲で検索するドン！」(Search by song)\n" +
                                        $"6. Specify if its is open to all or friends/followers only in「参加範囲を検索するドン！」(Search by participation)\n" +
                                        $"7. Specify the event period in「開催期間で検索するドン！」(Search by event period)\n" +
                                        $"8. Specify any of the following by checking them:\n" +
                                        $"  - 誰でも歓迎 (Anybody welcome)\n" +
                                        $"  - 初心者歓迎 (Beginners welcome)\n" +
                                        $"  - 上級者歓迎 (Professionals welcome)\n" +
                                        $"9. Click on「検索」(Search) to see a list of tournaments\n" +
                                        $"  - Click on「詳細」(Details) to see more info about the tournament\n" +
                                        $"     - Click on「ランキングを見る」(Rankings) to see the current rankings for this tournament\n" +
                                        $"  - Click on「参加する」(Participate) to join the tournament\n" +
                                        $"  - Click on「もっと見る」(See more) to load more results\n\n" +
                                        $"-# ＊ If a tournament listing contains the text「※ご利用できない曲が設定されている大会です 」, you can not join that tournament due to the selected song(s) not being available in your region."
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                                case "tournament_create":
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Create a Tournament",
                                        Url = "https://donderhiroba.jp/compe_form.php",
                                        ImageUrl = "https://taiko.namco-ch.net/taiko/en/images/donhiro/pic_10.png",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance + "https://donderhiroba.jp/\n" +
                                        $"1. Click on「大会挑戦状」(Challenge Room)\n" +
                                        $"2. Click on「大会を作る」(Create Tournament)\n" +
                                        $"3. Specify your tournament name in「大会名を入力するドン！（20文字以内）」(Tournament Name (up to 20 characters))\n" +
                                        $"4. Choose between 1-3 songs in「課題曲の数を選ぶドン！（最大3曲）」(Select your songs (up to 3 songs))\n" +
                                        $"5. Choose how many people can participate in「参加人数を選ぶドン！」(Choose number of participants)\n" +
                                        $"6. Specify if anyone can participate, or friends only, or friends/followers only in「参加範囲を選ぶドン！」(Choose who can join)\n" +
                                        $"7. Decide if participants should have a specific title obtained before they can join in「参加条件を選ぶドン！」(Choose the participation condition)\n" +
                                        $"  - Select「指定無し」(Not Specified) to not specify any conditions\n" +
                                        $"8. Specify the length of the tournament in「開催期間を選ぶドン！（最長10日間）」(Choose the event period (up to 10 days))\n" +
                                        $"9. Write the tournament description in「大会コメントを書くカッ？」(Want to write a comment?)\n" +
                                        $"10. Check any of the following if they apply:\n" +
                                        $"  - 誰でも歓迎 (Anybody welcome)\n" +
                                        $"  - 初心者歓迎 (Beginners welcome)\n" +
                                        $"  - 上級者歓迎 (Professionals welcome)\n" +
                                        $"11. Click on「大会を作る」(Create Tournament) to publish the tournament\n\n" +
                                        $"-# ＊ Once published, you can not edit or remove this tournament.\n" +
                                        $"-# ＊ Depending on the songs & conditions selected, some players might not be allowed to participate in your tournament."
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                                case "challenge":
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Challenge Other Players",
                                        Url = "https://donderhiroba.jp/challenge_form.php",
                                        ImageUrl = "https://taiko.namco-ch.net/taiko/en/images/donhiro/pic_09.png",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance + "https://donderhiroba.jp/\n" +
                                        $"1. Click on「大会挑戦状」(Challenge Room)\n" +
                                        $"2. Click on「挑戦状を送る」(Send a Challenge)\n" +
                                        $"  - The text 「あと＿件挑戦状を送れるドン！」will indicate how many challenge invitations you are able to send\n" +
                                        $"3. Click on「曲をえらぶ」(Select Song) and pick a song, then select a difficulty; Optionally, you can specify these song modifiers:\n" +
                                        $"  - はやさ (Scroll Speed)\n" +
                                        $"  - ドロン (Hidden Notes)\n" +
                                        $"  - あべこべ (Inverse)\n" +
                                        $"  - ランダム (Random)\n" +
                                        $"  - おまかせ (Doesn't matter) means that any option is fine\n" +
                                        $"4. Click on「検索」(Search) to find an opponent\n" +
                                        $"  - Use「ドンだーネーム・太鼓番検索」(Donder Name・User ID) to search for specific users\n" +
                                        $"  - Use「フレンド」(Friend) to filter by any user/following only/followers only/friends only\n" +
                                        $"5. In the dropdown menu below「開催期間を選ぶドン！（最長10日間）」, specify the challenge deadline (between「日のみ」(Today) and「10日間」(10 Days))\n" +
                                        $"6. Specify your challenge comment in「挑戦コメントを選ぶドン！」(Select a Challenge Comment da-don!):\n" +
                                        $"  - よろしくお願いいたします！ (I look forward to our battle!)\n" +
                                        $"  - 対戦しませんか！？ (Think you can beat me!?)\n" +
                                        $"  - 初心者です、がんばります！ (I'm a beginner, but i'll do my best!)\n" +
                                        $"  - 負けないドン！ (I won't lose, don!)\n" +
                                        $"  - 腕に自信あります！ (I'm confident that I'll win!)\n" +
                                        $"7. Click on「挑戦状を送る」(Send Challenge)"
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                                case "friend":
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Add a Friend",
                                        Url = "https://donderhiroba.jp/user_search.php",
                                        ImageUrl = "https://taiko.namco-ch.net/taiko/en/images/donhiro/pic_08.png",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance + "https://donderhiroba.jp/\n" +
                                        $"1. Click on「ユーザー検索」(User Search)\n" +
                                        $"2. Use「ドンだーネーム・太鼓番検索」(Donder Name・User ID) to search for specific users\n" +
                                        $"  - User IDs are located at the top of a user's profile, starting with「太鼓番：」(Taiko number)\n" +
                                        $"3. Use「都道府県」(Prefecture) to filter by a specific prefecture (Japan only)\n" +
                                        $"4. Use「段位」(Dan) to filter by the user's current Dan Dojo ranking\n" +
                                        $"5. Click on「検索」(Search)\n" +
                                        $"6. Click on the specific user's profile picture, then click「フォローする」to follow them\n" +
                                        $"7. In order to be considered friends, this same user must follow you back\n\n" +
                                        $"-# ＊ Some users may have their profile set to private, preventing you from following them."
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                                case "color":
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Change Your My-DON's Colors",
                                        Url = "https://taiko.namco-ch.net/taiko/en/donhiro/guide/my-don.php",
                                        ImageUrl = "https://taiko.namco-ch.net/taiko/en/images/donhiro/guide/pic_my-don_02.jpg",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance + "https://donderhiroba.jp/\n" +
                                        $"1. Click on「マイページ」(My Page)\n2. Click on「きせかえ」(Costume)\n3. Click on「いろ」(Color) to switch to color mode, and pick from any of the following:\n" +
                                        $"  - かお (Face)\n" +
                                        $"  - どう (Torso)\n" +
                                        $"  - てあし (Limbs)\n" +
                                        $"4. Click on「決定」(Confirm) to save your current outfit\n\n" +
                                        $"You can also use the following:\n" +
                                        $"- 「きせかえタンス」(Save/Load Outfit) to save your current outfit to a slot, or load a saved outfit\n" +
                                        $"  - Select「登録する」(Register) to save your currently worn outfit to this slot\n" +
                                        $"  - Select「きせかえる」(Change Clothes) to wear the clothes shown in the outfit slot\n" +
                                        $"- 「リセット」(Reset) to remove all worn clothes"
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                                case "costume":
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Change Your Costume/Mini Character",
                                        Url = "https://taiko.namco-ch.net/taiko/en/donhiro/guide/change.php",
                                        ImageUrl = "https://taiko.namco-ch.net/taiko/en/images/donhiro/guide/pic_change_02.jpg",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance + "https://donderhiroba.jp/\n" +
                                        $"1. Click on「マイページ」(My Page)\n2. Click on「きせかえ」(Costume)\n3. Click on「きせかえ」(Costume) to switch to costume mode, and pick from any of the following:\n" +
                                        $"  - きぐるみ (Mascot)\n" +
                                        $"     - Selecting any Mascot will remove any outfit pieces + Mini Character currently worn\n" +
                                        $"  - あたま (Head)\n" +
                                        $"  - からだ (Body)\n" +
                                        $"  - メイク (Face)\n" +
                                        $"  - ぷちキャラ (Mini Character)\n" +
                                        $"  - Select「 はずす 」(Remove) to remove your current outfit piece\n" +
                                        $"4. Click on「決定」(Confirm) to save your current outfit\n\n" +
                                        $"You can also use the following:\n" +
                                        $"- 「きせかえタンス」(Save/Load Outfit) to save your current outfit to a slot, or load a saved outfit\n" +
                                        $"  - Select「登録する」(Register) to save your currently worn outfit to this slot\n" +
                                        $"  - Select「きせかえる」(Change Clothes) to wear the clothes shown in the outfit slot\n" +
                                        $"- 「リセット」(Reset) to remove all worn clothes"
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                                case "title":
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Change Your Title",
                                        Url = "https://taiko.namco-ch.net/taiko/en/donhiro/guide/title.php",
                                        ImageUrl = "https://taiko.namco-ch.net/taiko/en/images/donhiro/guide/pic_title_01.jpg",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance + "https://donderhiroba.jp/\n" +
                                        "1. Click on「マイページ」(My Page)\n2. Click on「称号編集」(Edit Title)\n3. Use the dropdown menu to select a title you own\n  - Selecting「称号をはずす」(Remove Title) will remove your title\n4. Click on「称号を設定する」(Set Title)"
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                                case "name":
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Change Your Name",
                                        Url = "https://taiko.namco-ch.net/taiko/en/donhiro/guide/name.php",
                                        ImageUrl = "https://taiko.namco-ch.net/taiko/en/images/donhiro/guide/pic_name_01.jpg",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance + "https://donderhiroba.jp/\n" +
                                        "1. Click on「マイページ」(My Page)\n2. Click on「ドンだーネーム変更」(Change Donder Name)\n3. Enter your username and press「これでOK!」(This is OK!)\n\n" +
                                        "You can use the following in your name (up to 10 chars, **half-width only**):\n- Alpha-numerical characters (A~Z, a~z, 0~9)\n- Any of the following characters:\n  - `-`,`~`,`!`,`?`"
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                                default:
                                {
                                    var hiroba = new EmbedBuilder()
                                    {
                                        Title = "Donder Hiroba (ドンだーひろば)",
                                        Url = "https://donderhiroba.jp/index.php",
                                        ImageUrl = "https://donderhiroba.jp/image/sp/640/top_16_640.png",
                                        Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                        Description = maintenance +
                                        $"Donder Hiroba is a companion website, where you can access your save data and customize your profile.\n\n" +
                                        $"Website: https://donderhiroba.jp/\n" +
                                        $"Details: https://taiko.namco-ch.net/taiko/en/donhiro/"
                                    };
                                    await command.RespondAsync(null, [hiroba.Build()], false, false);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            var hiroba = new EmbedBuilder()
                            {
                                Title = "Donder Hiroba (ドンだーひろば)",
                                Url = "https://donderhiroba.jp/index.php",
                                ImageUrl = "https://donderhiroba.jp/image/sp/640/top_16_640.png",
                                Color = Song.GetGenreAsColor(Song.SongGenre.Namco),

                                Description = maintenance +
                                $"Donder Hiroba is a companion website, where you can access your save data and customize your profile.\n\n" +
                                $"Website: https://donderhiroba.jp/\n" +
                                $"Details: https://taiko.namco-ch.net/taiko/en/donhiro/"
                            };
                            await command.RespondAsync(null, [hiroba.Build()], false, false);
                        }
                        break;
                    }
                    case "invite":
                    {
                        await command.RespondAsync(LocaleData.GetString("INVITE_DESC", locale, $"https://discord.com/oauth2/authorize?client_id={_client.CurrentUser.Id}"));
                        break;
                    }
                    default:
                    {
                        await command.RespondAsync($"Received the \"{command.Data.Name}\" command, which is invalid or not implemented.", null, false, true);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General/Error] Something went wrong while executing a command. Command: {command.CommandName} / User: {command.User.Id} / Guild: {command.GuildId?.ToString() ?? "(null)"} / Channel: {command.ChannelId?.ToString() ?? "(none)"} / Details:\n{ex}");
                await command.RespondAsync(LocaleData.GetString("DISCLAIMER_ERROR", GetLocale(command)), null, false, true);
            }
            
        }
        private string GetLocale(SocketInteraction command)
        {
            string locale = (!command.IsDMInteraction && command.GuildId != null ? command.GuildLocale : command.UserLocale) ?? "en-US";
            if (!CanSendMessage(command)) { locale = command.UserLocale; }
            if (locale == "en-GB") locale = "en-US";
            return locale;
        }
        private bool CanSendMessage(SocketInteraction command)
        {
            if (command.IsDMInteraction) return true;

            bool canSendMessage = command.Permissions.SendMessages;

            if (command.Channel != null)
            {
                ChannelType[] threads = [ChannelType.NewsThread, ChannelType.PublicThread, ChannelType.PrivateThread];
                canSendMessage = threads.Contains(command.Channel.ChannelType) ? command.Permissions.SendMessagesInThreads : canSendMessage;
            }

            return canSendMessage;
        }
        private EmbedFooterBuilder GetFooter(SocketInteraction command)
        {
            string locale = GetLocale(command);

            return new() { 
                Text = LocaleData.GetString("DISCLAIMER_WIP", locale) + "\n" + last_Update,
                IconUrl = command.User?.GetDisplayAvatarUrl() ?? ""
            };
        }

        private ButtonBuilder CreateSongButton(SocketInteraction command, KeyValuePair<int, Song> song)
        {
            return CreateSongButton(command, song.Key, song.Value.GetTitle(GetLocale(command)));
        }

        private ButtonBuilder CreateSongButton(SocketInteraction command, int id, string? title = null, Song.SongDifficulty? difficulty = null)
        {
            string diff = difficulty != null ? difficulty switch
            {
                Song.SongDifficulty.Easy => "easy",
                Song.SongDifficulty.Normal => "normal",
                Song.SongDifficulty.Hard => "hard",
                Song.SongDifficulty.Extreme => "ex",
                Song.SongDifficulty.Hidden => "hidden",
                _ => ""
            } : "";

            return new()
            {
                Label = difficulty != null && title == null ? LocaleData.GetDifficulty(difficulty.Value, GetLocale(command)) : title ?? "???",
                Emote = difficulty != null ? EmoteData.GetDifficulty(difficulty.Value) : EmoteData.GetEmote("SONG"),
                CustomId = difficulty != null ? $"diff,{diff},{id}" : $"song,{id}",
                Style = ButtonStyle.Secondary
            };
        }

        private bool ExecutingUserHasPermission(SocketInteraction command, GuildPermission perm)
        {
            return 
                !command.IsDMInteraction && command.GuildId != null
                ? _client.GetGuild(command.GuildId ?? 0).GetUser(command.User.Id).GuildPermissions.Has(perm)
                : false;
        }
    }
}
