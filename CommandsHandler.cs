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
        private readonly string donShop_Winter_img = "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2018/09/04f8358d681100f118e39d88f60bd980.png";

        private readonly Color donShop_Spring_color = new(254, 137, 187);
        private readonly Color donShop_Summer_color = new(117, 237, 254);
        private readonly Color donShop_Autumn_color = new(248, 72, 40);
        private readonly Color donShop_Winter_color = new(186, 202, 255);

        public static string last_Update => $"Last update: {Process.GetCurrentProcess().StartTime.ToLocalTime().ToShortDateString()}";
        public static DateTimeOffset readyTime = new();

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly Discord.Interactions.InteractionService _interaction;

        private Dictionary<string, Embed> Embeds = [];

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
                    if (Program.__songs.TryGetValue(values[2], out Song? song)) {
                        await PostDiff(component, song, Song.GetDifficultyFromString(values[1]));
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Button interaction failed for 'diff' with title '{values[2]}' and diff '{values[1]}'.");
                        await component.RespondAsync("This button interaction failed, as the song title was not recognized.", null, false, true);
                        return;
                    }
                }
                else if (id.StartsWith("song"))
                {
                    string[] values = id.Split(',', 2);
                    if (Program.__songs.TryGetValue(values[1], out Song? song))
                    {
                        await PostSong(component, song);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Button interaction failed for 'song' with title '{values[1]}'.");
                        await component.RespondAsync("This button interaction failed, as the song title was not recognized.", null, false, true);
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

            if (interaction.Data.CommandName == "song" && interaction.Data.Current.Name == "title")
            {
                if (string.IsNullOrEmpty((string)interaction.Data.Current.Value))
                {
                    await interaction.RespondAsync();
                    return;
                }

                string name = (string)interaction.Data.Current.Value;
                List<AutocompleteResult> result = [];

                result.AddRange(
                    Program.__songNames
                    .Where(song => song.Key.Contains(name, StringComparison.InvariantCultureIgnoreCase))
                    .Select(song => new AutocompleteResult(song.Key, song.Value))
                    );

                int Priority(AutocompleteResult auto)
                {
                    if (auto.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) return -2;
                    if (auto.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)) return -1;
                    return 0;
                }

                result = result.OrderBy(song => Priority(song)).ToList();

                await interaction.RespondAsync(result.Take(25), null);
            }
            Console.WriteLine($"AutocompleteExecuted (Finished in {(DateTimeOffset.UtcNow - offset).TotalSeconds}s)");
            Console.WriteLine($"Data: {Regex.Replace((string)interaction.Data.Current.Value, @"[^\w\.@-]", "")}");
        }

        private async Task Client_Ready()
        {
            readyTime = DateTime.UtcNow;
            try
            {
                InteractionContextType[] context_types = [InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel];
                ApplicationIntegrationType[] integration_types = [ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall];
                Dictionary<string, string> test = new Dictionary<string, string>();

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
                command_random.AddOption("level", ApplicationCommandOptionType.Integer, "The difficulty level.", false, null, false, 1, 10, null, null, LocaleData.GetStrings("OPTION_LEVEL_NAME"), LocaleData.GetStrings("OPTION_LEVEL_DESC"), null, null,
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
                    Name = "「カラフルピーチ × 太鼓の達人」コラボキャンペーン",
                    Value = "colorfulpeach"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "『ガンダム45周年×初音ミク』× 太鼓の達人コラボ",
                    Value = "gundammiku"
                },
                new ApplicationCommandOptionChoiceProperties()
                {
                    Name = "『Got Boost?』キャンペーン",
                    Value = "kamen2025"
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

                var command_missing = new SlashCommandBuilder();
                command_missing.WithName("missing");
                command_missing.WithDescription("Lists all songs with missing note counts and region status.");
                command_missing.WithNameLocalizations(LocaleData.GetStrings("COMMAND_MISSING_NAME"));
                command_missing.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_MISSING_DESC"));
                command_missing.WithContextTypes(context_types);
                command_missing.WithIntegrationTypes(integration_types);

                var command_invite = new SlashCommandBuilder();
                command_invite.WithName("invite");
                command_invite.WithDescription("Add me to your server, or add me as an app!");
                command_invite.WithNameLocalizations(LocaleData.GetStrings("COMMAND_INVITE_NAME"));
                command_invite.WithDescriptionLocalizations(LocaleData.GetStrings("COMMAND_INVITE_DESC"));
                command_invite.WithContextTypes(context_types);
                command_invite.WithIntegrationTypes(integration_types);

                await _client.BulkOverwriteGlobalApplicationCommandsAsync([command_random.Build(), command_song.Build(), command_region.Build(), command_campaign.Build(), command_shop.Build(), command_about.Build(), command_stats.Build(), command_dan.Build(), command_hiroba.Build(), command_missing.Build(), command_invite.Build()]);

                var command_list = await _client.GetGlobalApplicationCommandsAsync();

                Console.WriteLine(command_list.Count + " global commands found.");
                foreach (var command in command_list)
                {
                    Console.WriteLine("Command name: " + command.Name + "\n" +
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

        private async Task PostSong(SocketInteraction interaction, Song song)
        {
            Console.WriteLine("PostSong interaction created at " + interaction.CreatedAt);
            Console.WriteLine("Current time is " + DateTimeOffset.UtcNow);
            Console.WriteLine("InteractionType is " + interaction.Type);

            try
            {
                string locale = GetLocale(interaction);

                var builder = new EmbedBuilder()
                {
                    Title = song.TitleList.Values.Distinct().Count() > 1 ? song.GetTitleList(true, locale) : song.GetTitle(locale),
                    Description = song.SubtitleList.Values.Distinct().Count() > 1 ? song.GetSubtitleList(true, locale) : song.GetSubtitle(locale),
                    Color = Song.GetGenreAsColor(song.Genre),
                    Fields = new() {
                        new() {
                            Name = LocaleData.GetString("DIFFICULTY_TITLE", locale),
                            Value =
                            $"{(song.Difficulties.Hidden.Level > -1 ? ($"{EmoteData.GetDifficulty(Song.SongDifficulty.Hidden)} " + song.Difficulties.Hidden.Level + "★ " + song.Difficulties.Hidden.NoteCount.ToString() + "\n") : "")}" +
                            $"{EmoteData.GetDifficulty(Song.SongDifficulty.Extreme)} {(song.Difficulties.Extreme.Level > -1 ? song.Difficulties.Extreme.Level + "★ " + song.Difficulties.Extreme.NoteCount.ToString() : "-")}\n" +
                            $"{EmoteData.GetDifficulty(Song.SongDifficulty.Hard)} {(song.Difficulties.Hard.Level > -1 ? song.Difficulties.Hard.Level + "★ " + song.Difficulties.Hard.NoteCount.ToString() : "-")}\n" +
                            $"{EmoteData.GetDifficulty(Song.SongDifficulty.Normal)} {(song.Difficulties.Normal.Level > -1 ? song.Difficulties.Normal.Level + "★ " + song.Difficulties.Normal.NoteCount.ToString() : "-")}\n" +
                            $"{EmoteData.GetDifficulty(Song.SongDifficulty.Easy)} {(song.Difficulties.Easy.Level > -1 ? song.Difficulties.Easy.Level + "★ " + song.Difficulties.Easy.NoteCount.ToString() : "-")}",
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
                if (song.Difficulties.Easy.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Easy));
                if (song.Difficulties.Normal.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Normal));
                if (song.Difficulties.Hard.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Hard));
                if (song.Difficulties.Extreme.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Extreme));
                if (song.Difficulties.Hidden.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Hidden));

                await interaction.RespondAsync(null, [builder.Build()], false, !CanSendMessage(interaction), null, component_builder.Build());
            }
            catch
            {
                Console.WriteLine($"[General/Error] PostSong failed to respond with song titled \"{song.Title}\".");
                throw;
            }
        }
        private async Task PostDiff(SocketInteraction interaction, Song song, Song.SongDifficulty difficulty)
        {
            Console.WriteLine("PostDiff interaction created at " + interaction.CreatedAt);
            Console.WriteLine("Current time is " + DateTimeOffset.UtcNow);
            Console.WriteLine("InteractionType is " + interaction.Type);

            try
            {
                Song.Chart chart = song.Difficulties[difficulty];

                if (chart.Level < 1)
                {
                    await interaction.RespondAsync($"The difficulty selected does not exist for this song, or is missing data. {EmoteData.GetEmote("MISS")}", null, false, true);
                    return;
                }

                string locale = GetLocale(interaction);

                var builder = new EmbedBuilder()
                {
                    Title = song.GetTitle(locale) + $" {EmoteData.GetDifficulty(difficulty)} {chart.Level}★",
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
                            Value = !string.IsNullOrWhiteSpace(chart.Url) ? chart.Url : $"-# {LocaleData.GetString("URL_MISSING", locale, EmoteData.GetEmote("MISS"))}"
                        },
                        new()
                        {
                            Name = "Details (taiko.wiki)",
                            Value = !string.IsNullOrWhiteSpace(chart.UrlKo) ? chart.UrlKo : $"-# {LocaleData.GetString("URL_MISSING", locale, EmoteData.GetEmote("MISS"))}"
                        }
                    },
                    Url = chart.ImageUrl,
                    ImageUrl = chart.ImageUrl,
                    Footer = GetFooter(interaction)
                };

                var component_builder = new ComponentBuilder();
                if (song.Difficulties.Easy.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Easy));
                if (song.Difficulties.Normal.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Normal));
                if (song.Difficulties.Hard.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Hard));
                if (song.Difficulties.Extreme.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Extreme));
                if (song.Difficulties.Hidden.Level > 0) component_builder.WithButton(CreateSongButton(interaction, Program.__songNames[song.Title], Song.SongDifficulty.Hidden));

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
                Console.WriteLine($"User {command.User.Id} executed the '{command.Data.Name}' " +
                    $"command{(command.IsDMInteraction ? " in a DM" : (" in guild " + (command.GuildId?.ToString() ?? "(null)") + " in channel " + (command.ChannelId?.ToString() ?? "(null)") + $" ({command.Channel?.ChannelType.ToString() ?? "null Channel Type"})"))} with the following parameters: {(command.Data.Options.Count > 0 ? string.Join(", ", command.Data.Options.Select(option => $"({option.Name} - {Regex.Replace(option.Value.ToString() ?? "", @"[^\w\.@-]", "")})")) : "(No options)")}");

                string locale = GetLocale(command);
                bool canSendMessage = CanSendMessage(command);

                Console.WriteLine($"Command's locale is {locale}");
                Console.WriteLine($"Message can be sent: {canSendMessage}");

                switch (command.Data.Name)
                {
                    case "random":
                    {
                        Random rand = new Random();
                        var level_option = command.Data.Options.Where(item => item.Name == "level");
                        var diff_option = command.Data.Options.Where(item => item.Name == "difficulty");
                        var genre_option = command.Data.Options.Where(item => item.Name == "genre");

                        int level = (int)(level_option.Count() > 0 ? (long)level_option.First().Value : 0);
                        string difficulty = diff_option.Count() > 0 ? (string)diff_option.First().Value : "";
                        string genre = genre_option.Count() > 0 ? (string)genre_option.First().Value : "";
                        bool uses_diff_or_level = level > 0 || difficulty != "";

                        Dictionary<string, Song> songlist = Program.__songs;

                        if (genre != "") songlist = songlist.Where(song => song.Value.GetAllGenres().Contains(Song.GetGenreFromString(genre))).ToDictionary();

                        if (uses_diff_or_level)
                        {

                            if (level > 0 && difficulty != "") {
                                if (difficulty == "both")
                                    songlist = songlist.Where(song => song.Value.Difficulties["ex"].Level == level || song.Value.Difficulties["hidden"].Level == level).ToDictionary();
                                else
                                    songlist = songlist.Where(song => song.Value.Difficulties[difficulty].Level == level).ToDictionary();
                            }
                            else if (level > 0) {
                                songlist = songlist.Where(song => song.Value.Difficulties["ex"].Level == level || song.Value.Difficulties["hidden"].Level == level).ToDictionary();
                            }
                            else if (difficulty != "")
                            {
                                if (difficulty == "both")
                                    songlist = songlist.Where(song => song.Value.Difficulties["ex"].Level > 0).ToDictionary();
                                else
                                    songlist = songlist.Where(song => song.Value.Difficulties[difficulty].Level > 0).ToDictionary();
                            }

                            if (songlist.Count > 0)
                            {
                                Song.SongDifficulty randomEx(Song song)
                                {
                                    if (song.Difficulties["hidden"].Level <= 0) return Song.SongDifficulty.Extreme;
                                    if ((level <= 0) || (song.Difficulties["ex"].Level == level && song.Difficulties["hidden"].Level == level)) {
                                        return rand.Next(2) == 1 ? Song.SongDifficulty.Hidden : Song.SongDifficulty.Extreme;
                                    }
                                    return song.Difficulties["hidden"].Level == level ? Song.SongDifficulty.Hidden : Song.SongDifficulty.Extreme;
                                }

                                Song song = songlist.ElementAt(rand.Next(songlist.Count)).Value;
                                await PostDiff(command, song, difficulty switch
                                {
                                    "easy" => Song.SongDifficulty.Easy,
                                    "normal" => Song.SongDifficulty.Normal,
                                    "hard" => Song.SongDifficulty.Hard,
                                    "ex" => Song.SongDifficulty.Extreme,
                                    "hidden" => Song.SongDifficulty.Hidden,
                                    "both" => randomEx(song),
                                    _ => Song.SongDifficulty.Extreme
                                });
                            }
                            else
                            {
                                await command.RespondAsync("Could not find any songs with the given parameters.", null, false, true);
                            }
                        }
                        else
                        {
                            Song song = songlist.ElementAt(rand.Next(songlist.Count)).Value;
                            await PostSong(command, song);
                        }
                    
                        break;
                    }
                    case "song":
                    {
                        if (command.Data.Options.Count == 1 || command.Data.Options.Count == 2)
                        {
                            string title = (string)command.Data.Options.First(option => option.Name == "title").Value;
                            string found_title = Program.__songNames.TryGetValue(title, out string? result) ? result : title;
                            if (Program.__songs.TryGetValue(found_title, out Song? song))
                            {
                                if (command.Data.Options.Any(option => option.Name == "difficulty"))
                                {
                                    switch ((string)command.Data.Options.First(option => option.Name == "difficulty").Value)
                                    {
                                        case "easy": await PostDiff(command, song, Song.SongDifficulty.Easy); break;
                                        case "normal": await PostDiff(command, song, Song.SongDifficulty.Normal); break;
                                        case "hard": await PostDiff(command, song, Song.SongDifficulty.Hard); break;
                                        case "ex": await PostDiff(command, song, Song.SongDifficulty.Extreme); break;
                                        case "hidden": await PostDiff(command, song, Song.SongDifficulty.Hidden); break;
                                        default: await PostSong(command, song); break;
                                    }
                                }
                                else
                                    await PostSong(command, song);
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
                        await command.RespondAsync("Information on the region lock status of all songs can be found on this spreadsheet, courtesy of Taiko Time :\n<https://docs.google.com/spreadsheets/d/e/2PACX-1vQYGQxV5Azuid7cnnNAG5EZyRkFI2YAJCARHS1AAgH0uo7OPgbaWODWbAbmk3o4M4h44hENCitbndKP/pubhtml?gid=0&single=true>", null, false, false);
                        break;
                    }
                    case "campaign":
                    {
                        var campaign_option = command.Data.Options.Where(option => option.Name == "name");
                        string campaign_name = campaign_option.Count() > 0 ? (string)campaign_option.First().Value : "";

                        switch (campaign_name)
                        {
                            case "gundammiku":
                            {
                                var peach = new EmbedBuilder()
                                {
                                    Title = "『ガンダム45周年×初音ミク』× 太鼓の達人コラボ",
                                    Color = new(0x6e67ab),
                                    Url = "https://www.gundam.info/feature/g45th-hatsunemiku-collab/#game",
                                    ImageUrl = "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2025/06/543be319af3b4f56d81163330bc6f2ff.png",
                                    Description = $"-# {LocaleData.GetString("DISCLAIMER_ONLYJAPAN", locale)}",
                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };
                                var component = new ComponentBuilder();
                                component.WithButton(CreateSongButton(command, "アイドル戦士(feat. 初音ミク)"));

                                await command.RespondAsync(null, [peach.Build()], false, false, null, component.Build());
                                break;
                            }
                            case "colorfulpeach":
                            {
                                var peach = new EmbedBuilder()
                                {
                                    Title = "「カラフルピーチ × 太鼓の達人」コラボキャンペーン",
                                    Color = new(0xe01488),
                                    Url = "https://taiko.namco-ch.net/taiko/special/colorful-peach/",
                                    ImageUrl = "https://taiko-ch.net/urgybrhm3ukw/blog/wp-content/uploads/2025/06/eed508a207c1e1a03c37d7dfa423ba5b.png",
                                    Description = LocaleData.GetString("CAMPAIGN_AVAILABLE", locale, 1759078799) + "\n\n" +
                                    $"-# {LocaleData.GetString("DISCLAIMER_ONLYJAPAN", locale)}",
                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };
                                var component = new ComponentBuilder();
                                component.WithButton(CreateSongButton(command, "On-Party!"));

                                await command.RespondAsync(null, [peach.Build()], false, false, null, component.Build());
                                break;
                            }
                            case "kamen2025":
                            {
                                var kamen2025 = new EmbedBuilder()
                                {
                                    Title = "『Got Boost?』キャンペーン",
                                    Color = new(0xD9358C),
                                    Url = "https://x.com/taiko_team/status/1904702341053636981",
                                    ImageUrl = "https://pbs.twimg.com/media/GmNSODdaoAAbrzI?format=jpg&name=large",
                                    Description = LocaleData.GetString("CAMPAIGN_AVAILABLE", locale, 1772204400) + "\n\n" +
                                    $"-# {LocaleData.GetString("DISCLAIMER_ONLYJAPAN", locale)}",
                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };
                                var component = new ComponentBuilder();
                                component.WithButton(CreateSongButton(command, "Got Boost?"));

                                await command.RespondAsync(null, [kamen2025.Build()], false, false, null, component.Build());
                                break;
                            }
                            case "ka":
                            {
                                var ka = new EmbedBuilder()
                                {
                                    Title = "彁",
                                    Color = new(0x000000),
                                    Url = "https://x.com/taiko_team/status/1509697054313881600",
                                    ImageUrl = "https://pbs.twimg.com/media/FPD5JqzagAQEFl5?format=png&name=medium",
                                    Description = $"{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(LocaleData.GetString("CAMPAIGN_AVAILABLE", locale, 253402268399)))}\n\n" +
                                    $"-# {LocaleData.GetString("DISCLAIMER_NOUSA", locale)}",
                                    Timestamp = DateTimeOffset.UtcNow,
                                    Footer = GetFooter(command)
                                };

                                var component = new ComponentBuilder();
                                component.WithButton(CreateSongButton(command, "彁").WithLabel("「こ、これは。。。」").WithEmote(Emoji.Parse(":question:")));

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
                        var autumn2025 = new EmbedBuilder()
                        {
                            Title = ":globe_with_meridians: " + LocaleData.GetString("SHOP_MEDAL_NAME", locale, LocaleData.GetString("SEASON_AUTUMN", locale), 2025),
                            ThumbnailUrl = donShop_Autumn_img,
                            Color = donShop_Autumn_color,
                            Description =
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, Program.GetLocalizedSongTitle("vs.VIGVANGS", locale), 60)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, Program.GetLocalizedSongTitle("今夜はホーミー", locale), 60)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, Program.GetLocalizedSongTitle("SUDDEN GUST OC", locale), 50)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, Program.GetLocalizedSongTitle("SORA-III ヘリオポーズ", locale), 50)}\n" +
                            $"\n" +
                            //$"{LocaleData.GetString("SHOP_MEDAL_URL", locale, "English", "https://docs.google.com/spreadsheets/d/1rVC1x8jPCvgJ1KK6W0XIxdHwyMsZiasqp-pnt7sAOAA/edit?gid=731420565#gid=731420565")}\n" +
                            $"{LocaleData.GetString("SHOP_MEDAL_URL", locale, "日本語", "https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC/%E3%81%A9%E3%82%93%E3%83%A1%E3%83%80%E3%83%AB%E3%82%B7%E3%83%A7%E3%83%83%E3%83%97")}\n\n" +
                            $"-# {LocaleData.GetString("DISCLAIMER_NOUSA", locale)}",
                            Timestamp = DateTimeOffset.UtcNow,
                            Footer = GetFooter(command)
                        };

                        var autumn2024 = new EmbedBuilder()
                        {
                            Title = ":flag_us: " + LocaleData.GetString("SHOP_MEDAL_NAME", locale, LocaleData.GetString("SEASON_AUTUMN", locale), 2024),
                            ThumbnailUrl = donShop_Autumn_img,
                            Color = donShop_Autumn_color,
                            Description =
                            $"- {LocaleData.GetString("SHOP_MEDALHIDDEN_DESC", locale, Program.GetLocalizedSongTitle("第六天魔王", locale), EmoteData.GetDifficulty(Song.SongDifficulty.Hidden), 60)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, Program.GetLocalizedSongTitle("魔導幻想曲", locale), 60)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, Program.GetLocalizedSongTitle("女神な世界 III", locale), 50)}\n" +
                            $"- {LocaleData.GetString("SHOP_MEDAL_DESC", locale, Program.GetLocalizedSongTitle("響け!太鼓の達人", locale), 50)}\n" +
                            $"\n" +
                            $"{LocaleData.GetString("SHOP_MEDAL_URL", locale, "日本語", "https://web.archive.org/web/20241009075818/https://wikiwiki.jp/taiko-fumen/%E4%BD%9C%E5%93%81/%E6%96%B0AC/%E3%81%94%E3%81%BB%E3%81%86%E3%81%B3%E3%82%B7%E3%83%A7%E3%83%83%E3%83%97")}\n\n" +
                            $"-# {LocaleData.GetString("DISCLAIMER_ONLYUSA", locale)}",
                            Timestamp = DateTimeOffset.UtcNow,
                            Footer = GetFooter(command)
                        };

                        var component = new ComponentBuilder();
                        
                        component.WithButton(CreateSongButton(command, "vs.VIGVANGS"), 0);
                        component.WithButton(CreateSongButton(command, "今夜はホーミー"), 0);
                        component.WithButton(CreateSongButton(command, "SUDDEN GUST OC"), 0);
                        component.WithButton(CreateSongButton(command, "SORA-III ヘリオポーズ"), 0);

                        component.WithButton(CreateSongButton(command, "第六天魔王", Song.SongDifficulty.Hidden, true), 1);
                        component.WithButton(CreateSongButton(command, "魔導幻想曲"), 1);
                        component.WithButton(CreateSongButton(command, "女神な世界 III"), 1);
                        component.WithButton(CreateSongButton(command, "響け!太鼓の達人"), 1);

                        await command.RespondAsync(null, [autumn2025.Build(), autumn2024.Build()], false, false, null, component.Build());
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
                            "- [Missing English Data Spreadsheet](https://docs.google.com/spreadsheets/d/1N9OBdkbwj51swS4jqhL6rTKv4looTQqLPWjAvD0lWog/edit?usp=sharing)\n\n" +
                            "Help me translate Donder Helper! https://github.com/Donder-Helper/DonderHelper/tree/main/Lang",
                            ImageUrl = "https://raw.githubusercontent.com/Donder-Helper/.github/refs/heads/main/profile/banner.png"
                        };
                        await command.RespondAsync(null, [about.Build()], false, false);
                        break;
                    }
                    case "stats":
                    {
                        var uptime = DateTime.UtcNow - readyTime;
                        var japan_stats = Program.__songs.Where(song => song.Value.Region.Japan != Song.Availability.No && song.Value.Region.Japan != Song.Availability.CampaignNo && song.Value.Region.Japan != Song.Availability.Unknown);
                        var asia_stats = Program.__songs.Where(song => song.Value.Region.Asia != Song.Availability.No && song.Value.Region.Asia != Song.Availability.CampaignNo && song.Value.Region.Asia != Song.Availability.Unknown);
                        var oceania_stats = Program.__songs.Where(song => song.Value.Region.Oceania != Song.Availability.No && song.Value.Region.Oceania != Song.Availability.CampaignNo && song.Value.Region.Oceania != Song.Availability.Unknown);
                        var usa_stats = Program.__songs.Where(song => song.Value.Region.UnitedStates != Song.Availability.No && song.Value.Region.UnitedStates != Song.Availability.CampaignNo && song.Value.Region.UnitedStates != Song.Availability.Unknown);
                        var china_stats = Program.__songs.Where(song => song.Value.Region.China != Song.Availability.No && song.Value.Region.China != Song.Availability.CampaignNo && song.Value.Region.China != Song.Availability.Unknown);
                        var statistics = new EmbedBuilder()
                        {
                            Title = "Donder Helper's Statistics",
                            Description = $"-# {LocaleData.GetString("DISCLAIMER_STATS", locale)}",
                            Fields =
                            {
                                new()
                                {
                                    Name = "Songlist",
                                    IsInline = true,
                                    Value =
                                    $"Total Songs: {Program.__songs.Count}\n" +
                                    $"Total Unique Titles: {Program.__songNames.Count}\n" +
                                    $"Total with Missing Notes: {Program.__songs.Where(song => !song.Value.Difficulties.ContainsNotes()).Count()}"
                                },
                                new()
                                {
                                    Name = "Region Status",
                                    IsInline = true,
                                    Value =
                                    $"Total ({LocaleData.GetString("REGION_JAPAN", locale)}): {japan_stats.Count()}" +
                                    $"\n-# ({japan_stats.Where(song => song.Value.Title.Contains("【双打】 ")).Count()} Sou-Uchi)" + 
                                    $"\n-# ({japan_stats.Where(song => song.Value.Region.IsJapanOnly).Count()} Exclusive)\n" +
                                    $"Total ({LocaleData.GetString("REGION_ASIA", locale)}): {asia_stats.Count()}" +
                                    $"\n-# ({asia_stats.Where(song => song.Value.Title.Contains("【双打】 ")).Count()} Sou-Uchi)" +
                                    $"\n-# ({asia_stats.Where(song => song.Value.Region.IsAsiaOnly).Count()} Exclusive)\n" +
                                    $"Total ({LocaleData.GetString("REGION_OCEANIA", locale)}): {oceania_stats.Count()}" +
                                    $"\n-# ({oceania_stats.Where(song => song.Value.Title.Contains("【双打】 ")).Count()} Sou-Uchi)" +
                                    $"\n-# ({oceania_stats.Where(song => song.Value.Region.IsOceaniaOnly).Count()} Exclusive)\n" +
                                    $"Total ({LocaleData.GetString("REGION_USA", locale)}): {usa_stats.Count()}" +
                                    $"\n-# ({usa_stats.Where(song => song.Value.Title.Contains("【双打】 ")).Count()} Sou-Uchi)" +
                                    $"\n-# ({usa_stats.Where(song => song.Value.Region.IsUSAOnly).Count()} Exclusive)\n" +
                                    $"Total ({LocaleData.GetString("REGION_CHINA", locale)}): {china_stats.Count()}" +
                                    $"\n-# ({china_stats.Where(song => song.Value.Title.Contains("【双打】 ")).Count()} Sou-Uchi)" +
                                    $"\n-# ({china_stats.Where(song => song.Value.Region.IsChinaOnly).Count()} Exclusive)\n" +
                                    $"Total Available Everywhere: {Program.__songs.Values.Where(song => song.Region.IsAvailable).Count()}\n" +
                                    $"Total Unavailable Everywhere: {Program.__songs.Values.Where(song => song.Region.IsUnavailable).Count()}\n" +
                                    $"Total w/ Unknown Status: {Program.__songs.Values.Where(song => song.Region.ContainsUnknown).Count()}"
                                },
                                new()
                                {
                                    Name = "Title List",
                                    IsInline = true,
                                    Value =
                                    $"Total (ja): {Program.__songs.Values.Where(song => song.TryGetTitle("ja", out string? title)).Count()}\n" +
                                    $"Total (en-US): {Program.__songs.Values.Where(song => song.TryGetTitle("en-US", out string? title)).Count()}\n" +
                                    $"Total (zh-TW): {Program.__songs.Values.Where(song => song.TryGetTitle("zh-TW", out string? title)).Count()}\n" +
                                    $"Total (ko): {Program.__songs.Values.Where(song => song.TryGetTitle("ko", out string? title)).Count()}\n" +
                                    $"Total (zh-CN): {Program.__songs.Values.Where(song => song.TryGetTitle("zh-CN", out string? title)).Count()}\n"
                                },
                                new()
                                {
                                    Name = "Discord Stats",
                                    IsInline = true,
                                    Value = 
                                    $"Uptime: {string.Format("{0:00}:{1:00}:{2:00}", (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds)}\n" +
                                    $"Server Count: {_client.Guilds.Count()}"
                                }
                            }
                        };

                        await command.RespondAsync(null, [statistics.Build()], false, false);
                        break;
                    }
                    case "dan":
                    {
                        var dan_title = command.Data.Options.Where(option => option.Name == "title");
                        string title = dan_title.Count() > 0 ? (string)dan_title.First().Value : "";

                        if (DanSonglist.Dans.TryGetValue(title, out Dan? dan) && dan.DanIsValid())
                        {
                            var dan_embed = new EmbedBuilder()
                            {
                                Title = title + "・" + dan.TitleEN,
                                Description = "-# " + LocaleData.GetString("DISCLAIMER_NOUSA", locale),
                                Color = dan.Color,
                                Url = dan.Url,

                                Fields =
                                {
                                    new()
                                    {
                                        Name = LocaleData.GetString("DAN_SONGS", locale),
                                        Value = $"{EmoteData.GetEmote("DAN_FIRST")} {Program.GetLocalizedSongTitle(dan.Song1.Title, locale)} {EmoteData.GetDifficulty(dan.Song1.Difficulty)} {dan.Song1.Chart.Level}★ {dan.Song1.Chart.NoteCount}\n" +
                                        $"{EmoteData.GetEmote("DAN_SECOND")} {Program.GetLocalizedSongTitle(dan.Song2.Title, locale)} {EmoteData.GetDifficulty(dan.Song2.Difficulty)} {dan.Song2.Chart.Level}★ {dan.Song2.Chart.NoteCount}\n" +
                                        $"{EmoteData.GetEmote("DAN_THIRD")} {Program.GetLocalizedSongTitle(dan.Song3.Title, locale)} {EmoteData.GetDifficulty(dan.Song3.Difficulty)} {dan.Song3.Chart.Level}★ {dan.Song3.Chart.NoteCount}\n" +
                                        $"-# **{LocaleData.GetString("DAN_NOTECOUNT", locale, dan.GetNoteCount() > -1 ? dan.GetNoteCount() : "???")}**",
                                        IsInline = false
                                    }
                                },

                                Timestamp = DateTime.UtcNow,
                                Footer = GetFooter(command)
                            };
                            dan_embed.Fields.AddRange(dan.ExamsToFields(locale));

                            var component_builder = new ComponentBuilder();
                            component_builder.WithButton(CreateSongButton(command, dan.Song1.Title, dan.Song1.Difficulty, true));
                            component_builder.WithButton(CreateSongButton(command, dan.Song2.Title, dan.Song2.Difficulty, true));
                            component_builder.WithButton(CreateSongButton(command, dan.Song3.Title, dan.Song3.Difficulty, true));

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
                        string maintenance = "-# ＊ Maintenance Hours: <t:1746464400:t> to <t:1746482400:t>\n" +
                                $"{(17 <= DateTimeOffset.UtcNow.Hour && DateTimeOffset.UtcNow.Hour < 22 ? "-# :warning: Maintenance is active, you can not edit your profile or use certain features.\n" : "")}\n";

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
                                        $"  - 初心者です、がんばります！ (I may be a noob, but i'll do my best!)\n" +
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
                    case "missing":
                    {
                        var noteslist = Program.__songs.Where(song => !song.Value.Difficulties.ContainsNotes()).ToDictionary();
                        var regionlist = Program.__songs.Where(song => song.Value.Region.ContainsUnknown).ToDictionary();
                        var unavailist = Program.__songs.Where(song => song.Value.Region.IsUnavailable).ToDictionary();
                        string missing_songs = "";
                        string missing_regions = "";
                        string missing_available = "";
                        foreach (string songitem in noteslist.Select(song => song.Value.GetTitle(locale)))
                        {
                            missing_songs += "- " + songitem + "\n";
                        }
                        foreach (string regionitem in regionlist.Select(song => song.Value.GetTitle(locale)))
                        {
                            missing_regions += "- " + regionitem + "\n";
                        }
                        foreach (string availitem in unavailist.Select(song => song.Value.GetTitle(locale)))
                        {
                            missing_available += "- " + availitem + "\n";
                        }

                        var songs_embed = new EmbedBuilder()
                        {
                            Title = LocaleData.GetString("MISSING_NOTES", locale),
                            Description = missing_songs
                        };
                        var region_embed = new EmbedBuilder()
                        {
                            Title = LocaleData.GetString("MISSING_REGION", locale),
                            Description = missing_regions
                        };
                        var avail_embed = new EmbedBuilder()
                        {
                            Title = LocaleData.GetString("MISSING_EVERYWHERE", locale),
                            Description = missing_available
                        };

                        await command.RespondAsync(null, [songs_embed.Build(), region_embed.Build(), avail_embed.Build()], false, false);
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
                await command.RespondAsync(LocaleData.GetString("DISCLAIMER_ERROR", GetLocale(command), EmoteData.GetEmote("MISS")), null, false, true);
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
                IconUrl = command.User.GetAvatarUrl()
            };
        }

        private ButtonBuilder CreateSongButton(SocketInteraction command, string title, Song.SongDifficulty? difficulty = null, bool use_title = false)
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
                Label = difficulty != null && !use_title ? LocaleData.GetDifficulty(difficulty.Value, GetLocale(command)) : Program.GetLocalizedSongTitle(title, GetLocale(command)),
                Emote = difficulty != null ? EmoteData.GetDifficulty(difficulty.Value) : EmoteData.GetEmote("SONG"),
                CustomId = difficulty != null ? $"diff,{diff},{title}" : $"song,{title}",
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
