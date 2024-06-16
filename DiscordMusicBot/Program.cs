using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Core.DiscordHost;
using Configuration.Lavalink;

var builder = Host.CreateDefaultBuilder(args);

// Configure app settings
builder.ConfigureAppConfiguration(config =>
{
  config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
  config.AddEnvironmentVariables();
});

// Discord
builder.ConfigureServices((context, services) =>
{
  var lavalinkConfiguration = context.Configuration.GetSection("Lavalink").Get<LavalinkConfiguration>();

  services.AddSingleton<DiscordSocketClient>();
  services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
  services.AddHostedService<DiscordHost>();

  // Lavalink
  services.AddLavalink();
  services.ConfigureLavalink(config =>
  {
    config.WebSocketUri = new Uri("ws://localhost:2333/v4/websocket");
  });

  services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));
});

var host = builder.Build();
host.Run();