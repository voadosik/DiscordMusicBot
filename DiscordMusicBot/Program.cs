using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Core.DiscordHost;
using Configuration.Lavalink;

var builder = Host.CreateDefaultBuilder(args); // Creating a host builder

// Configure settings(token and guildID)
builder.ConfigureAppConfiguration(config =>
{
  config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
  config.AddEnvironmentVariables();
});

builder.ConfigureServices((context, services) =>
{
  var lavalinkConfiguration = context.Configuration.GetSection("Lavalink").Get<LavalinkConfiguration>();
  //Discord services
  services.AddSingleton<DiscordSocketClient>();// Adding a singleton instance of DiscordSocketClient
  services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));// Adding a singleton instance of InteractionService
  services.AddHostedService<DiscordHost>(); // Adding a hosted service for DiscordHost
  //Lavalink service
  services.AddLavalink();
  services.ConfigureLavalink(config =>
  {
    config.WebSocketUri = new Uri("ws://localhost:2333/v4/websocket");
  });
  //Logging to console
  services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));
});
var host = builder.Build(); // Building the host
host.Run(); // Running the host