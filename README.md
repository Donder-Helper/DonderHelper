![](https://github.com/Donder-Helper/.github/blob/main/profile/banner.png)

# Donder Helper

**Donder Helper** is a Nijiiro database Discord bot. Information about songs, dans, campaigns, events, etc. can be accessed through Slash Commands.

For information on how Donder Helper uses certain data, please [read here](https://github.com/Donder-Helper/.github/blob/main/about/Privacy.md).

## Using the Bot

If you want to run the bot on your own system, you can use the Dockerfile provided or build it via. this command (the executable will be found in `/bin/Release/net8.0/win-x64/publish/`) :

```
dotnet publish --configuration Release --self-contained -p:PublishSingleFile=True --runtime win-x64
```

(Note that you must have the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed in order for this command to work.)

In that folder where your executable is located, create a `key.txt` file, with the contents of that file being your Discord bot's token. After that, run `DonderHelper.exe`. ***Never share this token publicly!!!***

## Contributing

For coding, you must install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) in order to build and test the project.

For contributing translations, edit the files found in the [Lang](https://github.com/Donder-Helper/DonderHelper/tree/main/Lang) folder.
