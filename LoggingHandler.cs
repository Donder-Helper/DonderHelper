using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;

public class LoggingHandler
{
    private DiscordSocketClient _client;
    public LoggingHandler(DiscordSocketClient client, CommandService command)
    {
        _client = client;
        _client.Log += LogAsync;
        command.Log += LogAsync;
        //_client.JoinedGuild += JoinedGuild;
        _client.Ready += Ready;
    }

    private Task Ready()
    {
        Console.WriteLine($"Active in {_client.Guilds.Count} guilds.");
        foreach (var guild in _client.Guilds)
        {
            Console.WriteLine($"Guild ID: {guild.Id} / Guild Locale: {guild.PreferredLocale}");
        }
        return Task.CompletedTask;
    }

    //private Task JoinedGuild(SocketGuild guild)
    //{
    //    Console.WriteLine($"New Guild Joined! Guild ID: {guild.Id} / Guild Locale: {guild.PreferredLocale}");
    //    return Task.CompletedTask;
    //}

    private Task LogAsync(LogMessage message)
    {
        if (message.Exception is CommandException cmdException)
        {
            Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                + $" failed to execute in {cmdException.Context.Channel}.");
            Console.WriteLine(cmdException);
        }
        else
            Console.WriteLine($"[General/{message.Severity}] [{message.Source}] {message.Message}\nException: {message.Exception?.ToString() ?? "None"}");

        return Task.CompletedTask;
    }
}
