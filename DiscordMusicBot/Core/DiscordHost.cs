using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Core.DiscordHost
{
  //Hosted service for a Discord Bot
  public sealed class DiscordHost : IHostedService
  {
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordHost> _logger;
    private readonly IConfiguration _configuration;
    private readonly TaskCompletionSource<bool> _clientReadyCompletionSource = new();

    //Constructor initializes all dependencies required by Discord Bot
    public DiscordHost(
        DiscordSocketClient discordSocketClient,
        InteractionService interactionService,
        IServiceProvider serviceProvider,
        ILogger<DiscordHost> logger,
        IConfiguration configuration)
    {
      _discordSocketClient = discordSocketClient ?? throw new ArgumentNullException(nameof(discordSocketClient));
      _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
      _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    //Start bot
    public async Task StartAsync(CancellationToken cancellationToken)
    {
      //Event handlers
      _discordSocketClient.Log += LogAsync;
      _discordSocketClient.InteractionCreated += InteractionCreated;
      _discordSocketClient.Ready += ClientReady;
      
      _logger.LogInformation("Logging in the bot.");
      try
      {
        //Retrieve a token from appsettings
        var token = _configuration["Discord:Token"];
        if (string.IsNullOrEmpty(token))
          throw new InvalidOperationException("Bot token is not configured.");
        
        //Log in to Discord using token
        await _discordSocketClient
            .LoginAsync(TokenType.Bot, token)
            .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error logging in the bot.");
        throw;
      }

      _logger.LogInformation("Starting the bot.");
      try
      {
        //Start the Discord client 
        await _discordSocketClient
            .StartAsync()
            .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error starting the bot.");
        throw;
      }

      _logger.LogInformation("Waiting for the Discord client to be ready.");
      var clientReadyTask = _clientReadyCompletionSource.Task;
      //Wait for the client to be ready for 30 seconds
      if (await Task.WhenAny(clientReadyTask, Task.Delay(TimeSpan.FromSeconds(30))) != clientReadyTask)
      {
        _logger.LogError("Timed out while waiting for Discord client to be ready.");
        throw new TimeoutException("Timed out while waiting for Discord client to be ready.");
      }

      _logger.LogInformation("Discord client is ready.");
    }

    //Stop Discord Bot
    public async Task StopAsync(CancellationToken cancellationToken)
    {
      //Unregister event handlers
      _discordSocketClient.Log -= LogAsync;
      _discordSocketClient.InteractionCreated -= InteractionCreated;
      _discordSocketClient.Ready -= ClientReady;

      _logger.LogInformation("Stopping the bot.");
      //Stop the bot
      await _discordSocketClient
          .StopAsync()
          .ConfigureAwait(false);
    }

    //Event handler for interaction created events
    private Task InteractionCreated(SocketInteraction interaction)
    {
      var interactionContext = new SocketInteractionContext(_discordSocketClient, interaction);
      return _interactionService.ExecuteCommandAsync(interactionContext, _serviceProvider);
    }

    //Event handler on the Discord Client ready
    private async Task ClientReady()
    {
      _logger.LogInformation("Client is ready, adding modules.");
      ulong guildID = ulong.Parse(_configuration["Discord:GuildID"]!);
      try
      {
        //Register all the commands and add all the modules to the interaction service
        await _interactionService
            .AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider)
            .ConfigureAwait(false);

        await _interactionService
            .RegisterCommandsToGuildAsync(guildID) 
            .ConfigureAwait(false);

        _logger.LogInformation("Modules added and commands registered.");
        _clientReadyCompletionSource.SetResult(true);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during ClientReady.");
        throw;
      }
    }

    //Event handler for logging
    private Task LogAsync(LogMessage logMessage)
    {
      _logger.Log(logMessage.Severity.ToLogLevel(), logMessage.Exception, logMessage.Message);
      return Task.CompletedTask;
    }
  }

  // Convert Discord log severity to C# logging
  public static class LogSeverityExtensions
  {
    public static LogLevel ToLogLevel(this LogSeverity severity)
    {
      return severity switch
      {
        LogSeverity.Critical => LogLevel.Critical,
        LogSeverity.Error => LogLevel.Error,
        LogSeverity.Warning => LogLevel.Warning,
        LogSeverity.Info => LogLevel.Information,
        LogSeverity.Verbose => LogLevel.Debug,
        LogSeverity.Debug => LogLevel.Trace,
        _ => LogLevel.None,
      };
    }
  }
}